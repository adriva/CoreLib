using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Adriva.Web.Core.Templating
{
    public class TemplateManager
    {
        private const string MainNamespace = "Adriva.Dynamic.Templates";
        private const string DynamicAssemblyName = "Adriva.Dynamic.Templates";

        private static object SingletonLock = new object();
        private static TemplateManager StaticInstance;

        private readonly object PrepareLock = new object();
        private bool IsReady = false;
        private Assembly DynamicAssembly = null;

        public static TemplateManager Current
        {
            get
            {
                if (null == TemplateManager.StaticInstance)
                {
                    lock (TemplateManager.SingletonLock)
                    {
                        if (null == TemplateManager.StaticInstance)
                        {
                            TemplateManager.StaticInstance = new TemplateManager();
                        }
                    }
                }

                return TemplateManager.StaticInstance;
            }
        }

        public void Prepare(string workingPath, IEnumerable<string> templatePaths, params Type[] additionalTypesUsed)
        {
            if (this.IsReady) return;

            lock (this.PrepareLock)
            {
                if (this.IsReady) return;
                this.PrepareSynchronized(workingPath, templatePaths, additionalTypesUsed);
                this.IsReady = true;
            }
        }

        private void PrepareSynchronized(string workingPath, IEnumerable<string> templatePaths, params Type[] additionalTypesUsed)
        {

            // points to the local path
            var projectFileSystem = RazorProjectFileSystem.Create(workingPath);

            var configuration = RazorConfiguration.Create(RazorLanguageVersion.Latest, "dynamic_template", Array.Empty<RazorExtension>());

            var projectEngine = RazorProjectEngine.Create(configuration, projectFileSystem, (builder) =>
            {

                InheritsDirective.Register(builder); // make sure the engine understand the @inherits directive in the input templates
                ModelDirective.Register(builder);

                builder.ConfigureClass((codeDocument, classNode) =>
                {
                    classNode.ClassName = Path.GetFileNameWithoutExtension(codeDocument.Source.FilePath);

                    string modelTypeName = ModelDirective.GetModelType(codeDocument.GetDocumentIntermediateNode());

                    if (string.IsNullOrWhiteSpace(classNode.BaseType)) classNode.BaseType = $"Adriva.Web.Core.Templating.BaseTemplate<{modelTypeName}>";
                });

                builder.SetNamespace(TemplateManager.MainNamespace); // define a namespace for the Template class
                builder.AddDefaultImports(new[] { "@using System", "@using System.Collections.Generic" });
            });

            var templateEngine = new RazorTemplateEngine(projectEngine.Engine, projectEngine.FileSystem);

            List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();

            foreach (var templatePath in templatePaths)
            {
                var item = projectFileSystem.GetItem(templatePath);
                var razorCodeDocument = projectEngine.Process(item);
                razorCodeDocument.SetCodeGenerationOptions(RazorCodeGenerationOptions.Create(b =>
                {
                    b.SetDesignTime(false);
                    b.SuppressChecksum = true;
                    b.SuppressMetadataAttributes = true;
                }));
                var csharpDocument = templateEngine.GenerateCode(razorCodeDocument);

#if DEBUG
                // parse and generate C# code, outputs it on the console
                Console.WriteLine(csharpDocument.GeneratedCode);
#endif

                // now, use roslyn, parse the C# code
                var syntaxTree = CSharpSyntaxTree.ParseText(csharpDocument.GeneratedCode);
                syntaxTrees.Add(syntaxTree);
            }

            // define the dll

            List<MetadataReference> references = new List<MetadataReference>();
            references.AddRange(new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location), // include corlib
                    MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location), // this file (that contains the MyTemplate base class)
                    MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.DynamicAttribute).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).Assembly.Location),
                    MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "System.Runtime.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "netstandard.dll"))
                });

            if (null != additionalTypesUsed && 0 < additionalTypesUsed.Length)
            {
                foreach (var additionalTypeUsed in additionalTypesUsed)
                {
                    string assemblyPath = additionalTypeUsed.Assembly.Location;
                    references.Add(MetadataReference.CreateFromFile(assemblyPath));
                }
            }

            var compilation = CSharpCompilation.Create(TemplateManager.DynamicAssemblyName, syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));


            // compile the dll
            string path = Path.Combine(workingPath, $"{TemplateManager.DynamicAssemblyName}.dll");
            var result = compilation.Emit(path);
            if (!result.Success)
            {
                throw new Exception(string.Join(Environment.NewLine, result.Diagnostics));
            }

            this.DynamicAssembly = Assembly.LoadFile(path);
        }

        public async Task<TemplateResult> ProcessAsync(string templateNameOrPath, dynamic model)
        {

            if (!this.IsReady) return TemplateResult.CreateError("TemplateManager is not ready. (did you miss calling the Prepare method?)");

            string className = Path.GetFileNameWithoutExtension(templateNameOrPath);
            // the generated type is defined in our custom namespace, as we asked. "Template" is the type name that razor uses by default.
            try
            {
                BaseTemplate template = (BaseTemplate)Activator.CreateInstance(
                    this.DynamicAssembly.GetType($"{TemplateManager.MainNamespace}.{className}", true, true)
                );

                template.Model = model;
                // run the code.
                await template.ExecuteAsync();
                return TemplateResult.CreateSuccess(template.GetText());
            }
            catch (Exception exception)
            {
                return TemplateResult.CreateError(exception.Message);
            }
        }
    }
}
