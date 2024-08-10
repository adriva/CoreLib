using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Adriva.Extensions.Reports.DataSources
{

    public class EnumDataSource : DataSource<object>
    {

        private class QueryBuilder : IQueryBuilder
        {
            public IQuery Build(IParameterBinder parameterBinder, ReportingContext context)
            {
                return new EnumQuery(context.Query.Command);
            }
        }

        public class EnumQuery : IQuery
        {
            public string CommandText { get; private set; }

            public IList<QueryParameter> Parameters => Array.Empty<QueryParameter>();

            public EnumQuery(string typeName)
            {
                this.CommandText = typeName;
            }

        }

        private readonly IMemoryCache Cache;

        public EnumDataSource(IConfiguration parameterConfiguration, IMemoryCache cache, ILogger<EnumDataSource> logger) : base(parameterConfiguration, logger)
        {
            this.Cache = cache;
        }

        public override IQueryBuilder CreateQueryBuilder() => new EnumDataSource.QueryBuilder();

        public override Task OpenAsync()
        {
            return Task.CompletedTask;
        }

        public override Task CloseAsync()
        {
            return Task.CompletedTask;
        }

        public override async Task<IDataSet> GetDataAsync(IQuery query)
        {
            await Task.CompletedTask;

            Type enumType = Type.GetType(query.CommandText, false, true);
            Type enumBaseType = enumType.GetEnumUnderlyingType();

            var names = enumType.GetEnumNames();

            ObjectDataSet<EnumItem> dataset = new ObjectDataSet<EnumItem>(this.Cache);

            foreach (var name in names)
            {
                var value = Convert.ChangeType(Enum.Parse(enumType, name), enumBaseType);

                string description = null;

                var memberInfo = enumType.GetMember(name)?.FirstOrDefault();

                if (null != memberInfo)
                {
                    var descriptionAttribute = memberInfo.GetCustomAttribute<DescriptionAttribute>();
                    description = descriptionAttribute?.Description;

                    if (string.IsNullOrWhiteSpace(description))
                    {
                        var displayAttribute = memberInfo.GetCustomAttribute<DisplayAttribute>();
                        description = displayAttribute?.Name;
                    }
                }

                EnumItem item = new EnumItem(name, value, description);
                dataset.AddItem(item);
            }

            return dataset;
        }

        public override Task<ValueType> ExecuteAsync(IQuery query)
        {
            throw new NotSupportedException("EnumDataSource does not support ExecuteAsync method.");
        }
    }
}