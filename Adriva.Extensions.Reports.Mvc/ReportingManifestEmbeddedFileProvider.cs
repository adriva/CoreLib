using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Adriva.Extensions.Reports.Mvc
{
    // Copyright (c) .NET Foundation. All rights reserved. Licensed under the Apache License, Version 2.0.
    //
    // Modifications: Modified ManifestParser to work around substitution of RESX files with empty manifest element stubs:
    //				  https://github.com/aspnet/Extensions/issues/1333
    //				  when <GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest> is used.

    internal class ReportingManifestEmbeddedFileProvider : IFileProvider
    {
        #region ManifestParser (Modified)

        internal static class ManifestParser
        {
            private const string DefaultManifestName = "Microsoft.Extensions.FileProviders.Embedded.Manifest.xml";

            public static EmbeddedFilesManifest Parse(Assembly assembly)
            {
                return Parse(assembly, DefaultManifestName);
            }

            public static EmbeddedFilesManifest Parse(Assembly assembly, string name)
            {
                if (assembly == null)
                    throw new ArgumentNullException(nameof(assembly));
                if (name == null)
                    throw new ArgumentNullException(nameof(name));

                var manifestResourceStream = assembly.GetManifestResourceStream(name);
                if (manifestResourceStream == null)
                    throw new InvalidOperationException($"Could not load the embedded file manifest '{name}' for assembly '{assembly.GetName().Name}'.");

                var manifest = EnsureElement(XDocument.Load(manifestResourceStream), "Manifest");
                var manifestVersion = EnsureElement(manifest, "ManifestVersion");

                var ensureText = EnsureText(manifestVersion);
                if (!string.Equals("1.0", ensureText, StringComparison.Ordinal))
                    throw new InvalidOperationException($"The embedded file manifest '{name}' for assembly '{assembly.GetName().Name}' specifies an unsupported file format version: '{ensureText}'.");

                var elements = EnsureElement(manifest, "FileSystem").Elements();

                // Workaround:
                var containsResources = assembly.GetManifestResourceNames().Any(manifestResourceName => manifestResourceName.EndsWith(".resources"));
                if (containsResources)
                    elements = elements.SkipWhile(x =>
                        string.Equals(x.Name.LocalName, "File", StringComparison.Ordinal) &&
                        EnsureName(x) != null && EnsureElement(x, "ResourcePath").Value == "");

                var entriesList = elements.Select(BuildEntry).ToList();
                ValidateEntries(entriesList);
                return new EmbeddedFilesManifest(ManifestDirectory.CreateRootDirectory(entriesList.ToArray()));
            }

            private static void ValidateEntries(IReadOnlyList<ManifestEntry> entriesList)
            {
                for (var i = 0; i < entriesList.Count - 1; ++i)
                    for (var j = i + 1; j < entriesList.Count; ++j)
                        if (string.Equals(entriesList[i].Name, entriesList[j].Name,
                            StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException($"Found two entries with the same name but different casing '{entriesList[i].Name}' and '{entriesList[j]}'");
            }

            private static ManifestEntry BuildEntry(XElement element)
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
                if (element.NodeType != XmlNodeType.Element)
                    throw new InvalidOperationException($"Invalid manifest format. Expected a 'File' or a 'Directory' node: '{element}'");

                if (string.Equals(element.Name.LocalName, "File", StringComparison.Ordinal))
                    return new ManifestFile(EnsureName(element), EnsureText(EnsureElement(element, "ResourcePath")));
                if (!string.Equals(element.Name.LocalName, "Directory", StringComparison.Ordinal))
                    throw new InvalidOperationException($"Invalid manifest format.Expected a 'File' or a 'Directory' node. Got '{element.Name.LocalName}' instead.");
                var name = EnsureName(element);
                var entriesList = element.Elements().Select(BuildEntry).ToList();
                ValidateEntries(entriesList);
                return ManifestDirectory.CreateDirectory(name, entriesList.ToArray());
            }

            private static XElement EnsureElement(XContainer container, string elementName)
            {
                var element = container.Element(elementName);
                if (element != null)
                    return element;
                throw new InvalidOperationException($"Invalid manifest format. Missing '{elementName}' element name");
            }

            private static string EnsureName(XElement element)
            {
                var name = element.Attribute("Name")?.Value;
                if (name != null)
                    return name;
                throw new InvalidOperationException($"Invalid manifest format. '{element.Name as object}' must contain a 'Name' attribute.");
            }

            private static string EnsureText(XElement element)
            {
                if (!element.Elements().Any() && !element.IsEmpty && element.Nodes().Count() == 1 &&
                    element.FirstNode.NodeType == XmlNodeType.Text)
                    return element.Value;
                throw new InvalidOperationException($"Invalid manifest format. '{element.Name.LocalName}' must contain a text value. '{element.Value}'");
            }
        }

        #endregion

        #region ManifestEmbeddedFileProvider

        private readonly DateTimeOffset _lastModified;

        public ReportingManifestEmbeddedFileProvider(Assembly assembly)
            : this(assembly, ManifestParser.Parse(assembly), ResolveLastModified(assembly))
        {
        }

        public ReportingManifestEmbeddedFileProvider(Assembly assembly, string root)
            : this(assembly, root, ResolveLastModified(assembly))
        {
        }

        public ReportingManifestEmbeddedFileProvider(
            Assembly assembly,
            string root,
            DateTimeOffset lastModified)
            : this(assembly, ManifestParser.Parse(assembly).Scope(root), lastModified)
        {
        }

        public ReportingManifestEmbeddedFileProvider(
            Assembly assembly,
            string root,
            string manifestName,
            DateTimeOffset lastModified)
            : this(assembly, ManifestParser.Parse(assembly, manifestName).Scope(root), lastModified)
        {
        }

        internal ReportingManifestEmbeddedFileProvider(
            Assembly assembly,
            EmbeddedFilesManifest manifest,
            DateTimeOffset lastModified)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            Assembly = assembly;
            Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            _lastModified = lastModified;
        }

        public Assembly Assembly { get; }

        internal EmbeddedFilesManifest Manifest { get; }

        public IDirectoryContents GetDirectoryContents(string subPath)
        {
            var manifestEntry = Manifest.ResolveEntry(subPath);
            if (manifestEntry == null || manifestEntry == ManifestEntry.UnknownPath)
                return NotFoundDirectoryContents.Singleton;
            if (!(manifestEntry is ManifestDirectory directory))
                return NotFoundDirectoryContents.Singleton;
            return new ManifestDirectoryContents(Assembly, directory, _lastModified);
        }

        public IFileInfo GetFileInfo(string subPath)
        {
            switch (Manifest.ResolveEntry(subPath))
            {
                case ManifestFile file:
                    return new ManifestFileInfo(Assembly, file, _lastModified);
                case ManifestDirectory manifestDirectory:
                    if (manifestDirectory != ManifestEntry.UnknownPath)
                        return new NotFoundFileInfo(manifestDirectory.Name);
                    break;
                case null:
                    return new NotFoundFileInfo(subPath);
            }

            return new NotFoundFileInfo(subPath);
        }

        public IChangeToken Watch(string filter)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));
            return NullChangeToken.Singleton;
        }

        private static DateTimeOffset ResolveLastModified(Assembly assembly)
        {
            var dateTimeOffset = DateTimeOffset.UtcNow;
            if (string.IsNullOrEmpty(assembly.Location)) return dateTimeOffset;
            try
            {
                dateTimeOffset = File.GetLastWriteTimeUtc(assembly.Location);
            }
            catch (PathTooLongException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return dateTimeOffset;
        }

        #endregion

        #region ManifestDirectoryInfo

        internal class ManifestDirectoryInfo : IFileInfo
        {
            public ManifestDirectoryInfo(ManifestDirectory directory, DateTimeOffset lastModified)
            {
                Directory = directory ?? throw new ArgumentNullException(nameof(directory));
                LastModified = lastModified;
            }

            public ManifestDirectory Directory { get; }

            public bool Exists => true;

            public long Length => -1;

            public string PhysicalPath => null;

            public string Name => Directory.Name;

            public DateTimeOffset LastModified { get; }

            public bool IsDirectory => true;

            public Stream CreateReadStream()
            {
                throw new InvalidOperationException("Cannot create a stream for a directory.");
            }
        }

        #endregion

        #region ManifestFileInfo

        internal class ManifestFileInfo : IFileInfo
        {
            private long? _length;

            public ManifestFileInfo(Assembly assembly, ManifestFile file, DateTimeOffset lastModified)
            {
                Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
                ManifestFile = file ?? throw new ArgumentNullException(nameof(file));
                LastModified = lastModified;
            }

            public Assembly Assembly { get; }

            public ManifestFile ManifestFile { get; }

            public bool Exists => true;

            public long Length => EnsureLength();

            public string PhysicalPath => null;

            public string Name => ManifestFile.Name;

            public DateTimeOffset LastModified { get; }

            public bool IsDirectory => false;

            public Stream CreateReadStream()
            {
                var manifestResourceStream = Assembly.GetManifestResourceStream(ManifestFile.ResourcePath);
                if (_length.HasValue) return manifestResourceStream;
                if (manifestResourceStream != null)
                    _length = manifestResourceStream.Length;
                return manifestResourceStream;
            }

            private long EnsureLength()
            {
                if (_length != null) return _length.GetValueOrDefault();
                using (var manifestResourceStream = Assembly.GetManifestResourceStream(ManifestFile.ResourcePath))
                    if (manifestResourceStream != null)
                        _length = manifestResourceStream.Length;

                return _length.GetValueOrDefault();
            }
        }

        #endregion

        #region ManifestDirectoryContents

        internal class ManifestDirectoryContents : IDirectoryContents
        {
            private readonly DateTimeOffset _lastModified;
            private IFileInfo[] _entries;

            public ManifestDirectoryContents(
                Assembly assembly,
                ManifestDirectory directory,
                DateTimeOffset lastModified)
            {
                Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
                Directory = directory ?? throw new ArgumentNullException(nameof(directory));
                _lastModified = lastModified;
            }

            public Assembly Assembly { get; }

            public ManifestDirectory Directory { get; }

            public bool Exists => true;

            public IEnumerator<IFileInfo> GetEnumerator()
            {
                return EnsureEntries().GetEnumerator();

                IReadOnlyList<IFileInfo> EnsureEntries()
                {
                    return _entries = _entries ?? ResolveEntries().ToArray();
                }

                IEnumerable<IFileInfo> ResolveEntries()
                {
                    if (Directory != ManifestEntry.UnknownPath)
                    {
                        if (Directory.Children != null)
                        {
                            var enumerator = Directory.Children.GetEnumerator();
                            while (enumerator.MoveNext())
                                switch (enumerator.Current)
                                {
                                    case ManifestFile file:
                                        yield return new ManifestFileInfo(Assembly, file, _lastModified);
                                        continue;
                                    case ManifestDirectory directory:
                                        yield return new ManifestDirectoryInfo(directory, _lastModified);
                                        continue;
                                    default:
                                        throw new InvalidOperationException("Unknown entry type");
                                }

                            enumerator.Dispose();
                        }
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        #endregion

        #region EmbeddedFilesManifest

        internal class EmbeddedFilesManifest
        {
            private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars().Where(c =>
            {
                if (c != Path.DirectorySeparatorChar)
                    return c != Path.AltDirectorySeparatorChar;
                return false;
            }).ToArray();

            private static readonly char[] Separators = new[]
            {
                Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar
            };

            private readonly ManifestDirectory _rootDirectory;

            internal EmbeddedFilesManifest(ManifestDirectory rootDirectory)
            {
                _rootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
            }

            internal ManifestEntry ResolveEntry(string path)
            {
                if (string.IsNullOrEmpty(path) || HasInvalidPathChars(path))
                    return null;
                var stringSegment = RemoveLeadingAndTrailingDirectorySeparators(path);
                if (stringSegment.Length == 0)
                    return _rootDirectory;
                var stringTokenizer = new StringTokenizer(stringSegment, Separators);
                var manifestEntry = (ManifestEntry)_rootDirectory;
                foreach (var segment in stringTokenizer)
                {
                    if (segment.Equals(""))
                        return null;
                    manifestEntry = manifestEntry.Traverse(segment);
                }

                return manifestEntry;
            }

            private static StringSegment RemoveLeadingAndTrailingDirectorySeparators(
                string path)
            {
                var offset = Array.IndexOf(Separators, path[0]) == -1 ? 0 : 1;
                if (offset == path.Length)
                    return StringSegment.Empty;
                var num = Array.IndexOf(Separators, path[path.Length - 1]) == -1 ? path.Length : path.Length - 1;
                return new StringSegment(path, offset, num - offset);
            }

            internal EmbeddedFilesManifest Scope(string path)
            {
                if (ResolveEntry(path) is ManifestDirectory manifestDirectory &&
                    manifestDirectory != ManifestEntry.UnknownPath)
                    return new EmbeddedFilesManifest(manifestDirectory.ToRootDirectory());
                throw new InvalidOperationException("Invalid path: '" + path + "'");
            }

            private static bool HasInvalidPathChars(string path)
            {
                return path.IndexOfAny(InvalidFileNameChars) != -1;
            }
        }

        #endregion

        #region ManifestFile

        internal class ManifestFile : ManifestEntry
        {
            public ManifestFile(string name, string resourcePath)
                : base(name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("'name' must not be null, empty or whitespace.", nameof(name));
                if (string.IsNullOrWhiteSpace(resourcePath))
                    throw new ArgumentException("'resourcePath' must not be null, empty or whitespace.",
                        nameof(resourcePath));
                ResourcePath = resourcePath;
            }

            public string ResourcePath { get; }

            public override ManifestEntry Traverse(StringSegment segment)
            {
                return UnknownPath;
            }
        }

        #endregion

        #region ManifestRootDirectory

        internal sealed class ManifestRootDirectory : ManifestDirectory
        {
            public ManifestRootDirectory(ManifestEntry[] children)
                : base(null, children) =>
                SetParent(ManifestSinkDirectory.Instance);

            public override ManifestDirectory ToRootDirectory()
            {
                return this;
            }
        }

        #endregion

        #region ManifestSinkDirectory

        internal sealed class ManifestSinkDirectory : ManifestDirectory
        {
            private ManifestSinkDirectory()
                : base(null, Array.Empty<ManifestEntry>())
            {
                SetParent(this);
                Children = new[] { this };
            }

            public static ManifestDirectory Instance { get; } = new ManifestSinkDirectory() as ManifestDirectory;

            public override ManifestEntry Traverse(StringSegment segment)
            {
                return this;
            }
        }

        #endregion

        #region ManifestEntry

        internal abstract class ManifestEntry
        {
            protected ManifestEntry(string name) => Name = name;

            public ManifestEntry Parent { get; private set; }

            public string Name { get; }

            public static ManifestEntry UnknownPath { get; } = ManifestSinkDirectory.Instance as ManifestEntry;

            protected internal virtual void SetParent(ManifestDirectory directory)
            {
                if (Parent != null)
                    throw new InvalidOperationException("Directory already has a parent.");
                Parent = directory;
            }

            public abstract ManifestEntry Traverse(StringSegment segment);
        }

        #endregion

        #region ManifestDirectory

        internal class ManifestDirectory : ManifestEntry
        {
            protected ManifestDirectory(string name, ManifestEntry[] children)
                : base(name)
            {
                Children = children ?? throw new ArgumentNullException(nameof(children));
            }

            public IReadOnlyList<ManifestEntry> Children { get; protected set; }

            public override ManifestEntry Traverse(StringSegment segment)
            {
                if (segment.Equals(".", StringComparison.Ordinal))
                    return this;
                if (segment.Equals("..", StringComparison.Ordinal))
                    return Parent;
                foreach (var child in Children)
                    if (segment.Equals(child.Name, StringComparison.OrdinalIgnoreCase))
                        return child;
                return UnknownPath;
            }

            public virtual ManifestDirectory ToRootDirectory()
            {
                return CreateRootDirectory(CopyChildren());
            }

            public static ManifestDirectory CreateDirectory(
                string name,
                ManifestEntry[] children)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("'name' must not be null, empty or whitespace.", nameof(name));
                if (children == null)
                    throw new ArgumentNullException(nameof(children));
                var parent = new ManifestDirectory(name, children);
                ValidateChildrenAndSetParent(children, parent);
                return parent;
            }

            public static ManifestRootDirectory CreateRootDirectory(
                ManifestEntry[] children)
            {
                if (children == null)
                    throw new ArgumentNullException(nameof(children));
                var manifestRootDirectory = new ManifestRootDirectory(children);
                ValidateChildrenAndSetParent(children, manifestRootDirectory);
                return manifestRootDirectory;
            }

            internal static void ValidateChildrenAndSetParent(
                ManifestEntry[] children,
                ManifestDirectory parent)
            {
                foreach (var child in children)
                {
                    if (child == UnknownPath)
                        throw new InvalidOperationException("Invalid entry type 'ManifestSinkDirectory'");
                    if (child is ManifestRootDirectory)
                        throw new InvalidOperationException("Can't add a root folder as a child");
                    child.SetParent(parent);
                }
            }

            private ManifestEntry[] CopyChildren()
            {
                var manifestEntryList = new List<ManifestEntry>();
                foreach (var child in Children)
                    switch (child)
                    {
                        case ManifestSinkDirectory _:
                        case ManifestRootDirectory _:
                            throw new InvalidOperationException("Unexpected manifest node.");
                        case ManifestDirectory manifestDirectory:
                            var directory = CreateDirectory(manifestDirectory.Name, manifestDirectory.CopyChildren());
                            manifestEntryList.Add(directory);
                            break;
                        case ManifestFile manifestFile:
                            var manifestFile1 = new ManifestFile(manifestFile.Name, manifestFile.ResourcePath);
                            manifestEntryList.Add(manifestFile1);
                            break;
                        default:
                            throw new InvalidOperationException("Unexpected manifest node.");
                    }

                return manifestEntryList.ToArray();
            }
        }

        #endregion
    }
}