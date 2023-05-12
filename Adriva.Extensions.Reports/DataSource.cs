using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Adriva.Extensions.Reports
{
    public abstract class DataSource<TParameters> : IDebugDataProvider, IDataSource where TParameters : class, new()
    {
        protected Dictionary<string, object> DebugDataDictionary { get; private set; }

        protected TParameters Parameters { get; private set; }

        protected ILogger Logger { get; private set; }

        protected DataSource(IConfiguration parameterConfiguration, ILogger logger)
        {
            if (null == parameterConfiguration)
            {
                this.Parameters = new TParameters();
            }
            else
            {
                this.Parameters = parameterConfiguration.Get<TParameters>();
            }

            this.Logger = logger;
            this.DebugDataDictionary = new Dictionary<string, object>();
        }

        public abstract Task OpenAsync();

        public abstract Task CloseAsync();

        public abstract IQueryBuilder CreateQueryBuilder();

        public abstract Task<IDataSet> GetDataAsync(IQuery query);

        public abstract Task<ValueType> ExecuteAsync(IQuery query);

        public IDictionary<string, object> GetDebugData() => this.DebugDataDictionary;
    }
}