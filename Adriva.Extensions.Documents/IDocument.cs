using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;

namespace Adriva.Extensions.Documents
{
    public interface IDocument
    {
        void Create(Stream stream);

        void Open(string path);

        void Open(Stream stream);
    }

    public interface IDocument<TPart> : IDocument
    {
        void AddPart<TData>(Expression<Func<TData, TPart>> partMappingExpression, IEnumerable<TData> data) where TData : class;

        IEnumerator<DocumentData> GetContentEnumerator(object arguments);

        void Close();
    }
}
