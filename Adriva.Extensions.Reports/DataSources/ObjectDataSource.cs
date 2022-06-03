using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Adriva.Common.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Adriva.Extensions.Reports.DataSources
{

    public sealed class ObjectDataSourceParameters
    {
        public string TypeName { get; set; }

    }

    public sealed class ObjectDataSource : DataSource<ObjectDataSourceParameters>
    {
        private class ObjectQuery : IQuery
        {
            public string CommandText { get; private set; }

            public IList<QueryParameter> Parameters { get; private set; } = new List<QueryParameter>();

            public ObjectQuery(string commandText)
            {
                this.CommandText = commandText;
            }
        }

        private class ObjectQueryBuilder : IQueryBuilder
        {

            private static string NormalizeCommandText(string text)
            {
                if (string.IsNullOrWhiteSpace(text)) return string.Empty;

                StringBuilder buffer = new StringBuilder(text.Length);

                foreach (char c in text)
                {
                    if (!char.IsLetterOrDigit(c) && '_' != c && '-' != c && '@' != c)
                    {
                        buffer.Append(' ');
                    }
                    else buffer.Append(c);
                }

                return Utilities.CompressWhitespaces(buffer.ToString());
            }

            public IQuery Build(IParameterBinder parameterBinder, ReportingContext context)
            {
                ObjectQuery tempQuery = new ObjectQuery(context.Query.Command);
                parameterBinder.Bind(tempQuery, context);

                string commandText = ObjectQueryBuilder.NormalizeCommandText(tempQuery.CommandText);

                foreach (var tempParameter in tempQuery.Parameters)
                {
                    commandText = commandText.Replace(tempParameter.Name, null);
                }

                ObjectQuery query = new ObjectQuery(commandText.Trim());
                foreach (var tempParameter in tempQuery.Parameters)
                {
                    query.Parameters.Add(new QueryParameter(tempParameter.Name.Substring(1), tempParameter.Value));
                }
                return query;
            }
        }

        private readonly IServiceProvider ServiceProvider;
        private readonly IMemoryCache Cache;
        private object TargetObject;

        public ObjectDataSource(IServiceProvider serviceProvider, IConfiguration parameterConfiguration, IMemoryCache cache, ILogger<ObjectDataSource> logger) : base(parameterConfiguration, logger)
        {
            this.ServiceProvider = serviceProvider;
            this.Cache = cache;
        }

        public override IQueryBuilder CreateQueryBuilder()
        {
            return new ObjectQueryBuilder();
        }

        public override Task OpenAsync()
        {
            Type targetType = Type.GetType(this.Parameters.TypeName, true, true);
            this.TargetObject = ActivatorUtilities.CreateInstance(this.ServiceProvider, targetType);
            return Task.CompletedTask;
        }

        public override async Task<IDataSet> GetDataAsync(IQuery query)
        {
            var output = await ReflectionHelpers.CallMethodAsync(this.TargetObject, query, this.Cache);
            var dataset = new ObjectDataSet<object>(this.Cache);

            if (output is IEnumerable enumerable)
            {
                foreach (var enumerableItem in enumerable)
                {
                    dataset.AddItem(enumerableItem);
                }
            }
            else
            {
                dataset.AddItem(output);
            }

            return dataset;
        }

        public override async Task<ValueType> ExecuteAsync(IQuery query)
        {
            var output = await ReflectionHelpers.CallMethodAsync(this.TargetObject, query, this.Cache);
            if (output is ValueType valueType) return valueType;
            else throw new NotSupportedException($"Query '{query.CommandText}' in ObjectDataSource.ExecuteAsync method returned a reference type result. ExecuteAsync method must return a value type.");
        }

        public override Task CloseAsync()
        {
            if (this.TargetObject is IDisposable disposableObject)
            {
                disposableObject.Dispose();
            }

            this.TargetObject = null;
            return Task.CompletedTask;
        }
    }
}