using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Adriva.Extensions.Documents
{
    public sealed class DocumentManager : IDocumentManager
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly List<Type> RegisteredDocumentTypes = new List<Type>();

        public DocumentManager(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;

            this.RegisteredDocumentTypes.AddRange(
                new[] {
                    typeof(ExcelDocument),
                    typeof(ExcelWorkbook),
                    typeof(CsvDocument),
                    typeof(ExcelTemplatedDocument),
                }
            );
        }

        public IDocument<ExcelSheetRow> GetExcel()
        {
            return new ExcelDocument();
        }

        public T Get<T>() where T : class, IDocument
        {
            Type typeofT = typeof(T);

            Type matchingType = this.RegisteredDocumentTypes.FirstOrDefault(t => typeofT.IsAssignableFrom(t));

            if (null == matchingType) throw new ArgumentException($"Requested document manager '{typeofT.FullName}' could not be found.");

            return (T)ActivatorUtilities.CreateInstance(this.ServiceProvider, matchingType);
        }
    }
}