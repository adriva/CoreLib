using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adriva.Extensions.Reports
{

    internal sealed class ReportingService : IReportingService
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IParameterBinder ParameterBinder;
        private readonly ReportingServiceOptions Options;
        private readonly IMemoryCache Cache;
        private ILogger Logger;

        public ReportingService(IServiceProvider serviceProvider,
                                IParameterBinder parameterBinder,
                                IOptions<ReportingServiceOptions> optionsAccessor,
                                ILogger<ReportingService> logger)
        {
            this.ServiceProvider = serviceProvider;
            this.ParameterBinder = parameterBinder;
            this.Options = optionsAccessor.Value;
            this.Cache = this.Options.DisableCache ? new NullMemoryCache() : (serviceProvider.GetService<IMemoryCache>() ?? new NullMemoryCache());
            this.Logger = logger;
        }

        private string ResolveReportDefinitionPath(string name)
        {
            string path = null;
            name = name.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            if (!string.IsNullOrWhiteSpace(this.Options.EnvironmentName))
            {
                path = Path.Combine(this.Options.RootPath, this.Options.EnvironmentName, $"{name}.json");
                if (File.Exists(path)) return path;
            }

            return Path.Combine(this.Options.RootPath, $"{name}.json");
        }

        public async Task<ReportDefinition> LoadReportAsync(string name)
        {
            this.Logger.LogInformation($"Loading report definition for '{name}' from cache.");
            return await this.Cache.GetOrCreateAsync<ReportDefinition>($"Def_{name}", async (entry) =>
            {
                await Task.CompletedTask;

                string path = this.ResolveReportDefinitionPath(name);

                this.Logger.LogInformation($"Couldn't locate report definition '{name}' in cache.");
                this.Logger.LogInformation($"Loading report definition '{name}' from '{path}'.");

                entry.SetSlidingExpiration(TimeSpan.FromMinutes(30));

                Stack<string> inheritanceStack = new Stack<string>();
                bool hasBase = false;

                do
                {
                    ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
                    configurationBuilder.AddJsonFile(path, false);
                    IConfiguration configuration = configurationBuilder.Build();
                    ReportDefinition definition = configuration.Get<ReportDefinition>();

                    inheritanceStack.Push(path);

                    if (!string.IsNullOrEmpty(definition.BaseReport))
                    {
                        hasBase = true;
                        path = this.ResolveReportDefinitionPath(definition.BaseReport);

                        if (inheritanceStack.Any(x => x.Equals(path, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            throw new NotSupportedException($"Circular references in report templates are not supported. '{definition.BaseReport}' in '{name}' has been referenced before.");
                        }

                    }
                    else
                    {
                        hasBase = false;
                    }
                } while (hasBase);

                this.Logger.LogInformation($"Generating definition for report '{name}'.");

                ConfigurationBuilder finalBuilder = new ConfigurationBuilder();
                while (0 < inheritanceStack.Count)
                {
                    string reportPath = inheritanceStack.Pop();
                    finalBuilder.AddJsonFile(reportPath);
                }

                IConfigurationRoot finalConfiguration = finalBuilder.Build();
                ReportDefinition finalDefinition = finalConfiguration.Get<ReportDefinition>();

                finalDefinition.DataSourceConfigurations = finalConfiguration.GetDataSourceParameters();

                for (int loop = 0; loop < finalDefinition.Filters.Count; loop++)
                {
                    finalDefinition.Filters[loop].RendererOptionsConfiguration = finalConfiguration.GetFilterRendererOptions(loop);
                }

                finalDefinition.Output.RendererOptionsConfiguration = finalConfiguration.GetOutputRendererOptions();

                for (int loop = 0; loop < finalDefinition.Output.ColumnDefinitions.Count; loop++)
                {
                    finalDefinition.Output.ColumnDefinitions[loop].RendererOptionsConfiguration = finalConfiguration.GetColumnRendererOptions(loop);
                }

                this.Logger.LogInformation($"Report definition '{name}' loaded.");

                this.FixupReportDefinition(finalDefinition);

                return finalDefinition;
            });
        }

        private void FixupReportDefinition(ReportDefinition reportDefinition)
        {
            int loop = 0;
            foreach (var columnDefinition in reportDefinition.Output.ColumnDefinitions)
            {
                if (string.IsNullOrWhiteSpace(columnDefinition.Field))
                {
                    columnDefinition.Field = $"DynamicField{loop++}";
                }
            }

            if (null != reportDefinition.Output.Paging)
            {
                if (reportDefinition.TryGetFilter(reportDefinition.Output.Paging.PageIndexParameter, out FilterDefinition pageIndexFilter))
                {
                    if (null == pageIndexFilter.DefaultValue) pageIndexFilter.DefaultValue = 1;
                }
                else
                {
                    reportDefinition.Filters.Add(new FilterDefinition()
                    {
                        Name = reportDefinition.Output.Paging.PageIndexParameter,
                        DataType = TypeCode.Int32,
                        DefaultValue = 1,
                        IsRequired = true,
                        Type = ParameterType.NonUserItem
                    });
                }

                if (reportDefinition.TryGetFilter(reportDefinition.Output.Paging.PageSizeParameter, out FilterDefinition pageSizeFilter))
                {
                    if (null == pageSizeFilter.DefaultValue) pageIndexFilter.DefaultValue = 10;
                }
                else
                {
                    reportDefinition.Filters.Add(new FilterDefinition()
                    {
                        Name = reportDefinition.Output.Paging.PageSizeParameter,
                        DataType = TypeCode.Int32,
                        DefaultValue = 20,
                        IsRequired = true,
                        Type = ParameterType.NonUserItem
                    });
                }
            }
        }

        private IDataSource GetDataSource(string name, DynamicTypeDefinition typeDefinition, ReportDefinition reportDefinition)
        {
            reportDefinition.GetDataSource(name, out string typeName, out IConfigurationSection configuration);

            if (string.IsNullOrWhiteSpace(typeName)) throw new ArgumentNullException(nameof(typeName));

            typeName = $"{typeName}DataSource";

            var dataSourceType = this.Options.DataSourceTypes.FirstOrDefault(dt => 0 == string.Compare(dt.Name, typeName, StringComparison.OrdinalIgnoreCase));

            if (null == dataSourceType) throw new ArgumentException($"Data source type '{typeName}' [{name}] not found.");

            return (IDataSource)ActivatorUtilities.CreateInstance(this.ServiceProvider, dataSourceType, configuration);
        }

        private ReadOnlyDictionary<string, IDataSource> PopulateDataSources(ReportDefinition reportDefinition)
        {
            Dictionary<string, IDataSource> dataSources = new Dictionary<string, IDataSource>();

            foreach (var dataSourceEntry in reportDefinition.DataSources)
            {
                if (!dataSources.ContainsKey(dataSourceEntry.Key))
                {
                    IDataSource dataSource = this.GetDataSource(dataSourceEntry.Key, dataSourceEntry.Value, reportDefinition);
                    dataSources.Add(dataSourceEntry.Key, dataSource);
                }
            }

            return new ReadOnlyDictionary<string, IDataSource>(dataSources);
        }

        private object CreateContextProvider(ReportDefinition reportDefinition)
        {
            if (string.IsNullOrWhiteSpace(reportDefinition?.ContextProvider)) return null;

            Type typeOfContextProvider = Type.GetType(reportDefinition.ContextProvider, true, true);
            return ActivatorUtilities.CreateInstance(this.ServiceProvider, typeOfContextProvider);
        }

        public async Task<ReportOutput> GetReportOutputAsync(string name, FilterValues filterValues, Func<ReportDefinition, IQuery, bool> checkSkipPopulateData = null)
        {
            var reportDefinition = await this.LoadReportAsync(name);
            var filters = reportDefinition.GetFilters();
            object contextProvider = this.CreateContextProvider(reportDefinition);
            var dataSources = this.PopulateDataSources(reportDefinition);

            if (!dataSources.TryGetValue(reportDefinition.Output.DataSource, out IDataSource reportDataSource))
            {
                throw new ArgumentException($"Report '{name}' references a data source '{reportDefinition.Output.DataSource}' which could not be found.");
            }


            this.Logger.LogInformation($"Building query '{reportDefinition.Output.Query}'.");
            IQueryBuilder queryBuilder = reportDataSource.CreateQueryBuilder();
            QueryDefinition queryDefinition = reportDefinition.GetQuery(reportDefinition.Output.Query);
            IQuery query = queryBuilder.Build(this.ParameterBinder, new ReportingContext(queryDefinition, reportDefinition, filterValues, contextProvider));

            this.Logger.LogInformation($"Running query '{query.CommandText}'.");

            IDataSet dataSet = DataSet.Empty;

            Func<Task<IDataSet>> getDataFunction = async () =>
            {
                try
                {
                    this.Logger.LogInformation($"Opening data source '{reportDataSource.GetType().FullName}'.");
                    await reportDataSource.OpenAsync();
                    return await reportDataSource.GetDataAsync(query);
                }
                finally
                {
                    this.Logger.LogInformation($"Closing data source '{reportDataSource.GetType().FullName}'.");
                    await reportDataSource.CloseAsync();
                }
            };

            if (null == checkSkipPopulateData || !checkSkipPopulateData.Invoke(reportDefinition, query))
            {
                if (0 == queryDefinition.SlidingExpiration.Value.TotalSeconds)
                {
                    dataSet = await getDataFunction();
                }
                else
                {
                    dataSet = await this.Cache.GetOrCreateAsync($"ReportQuery:{query.GetUniqueId()}", async (entry) =>
                    {
                        entry.AbsoluteExpirationRelativeToNow = queryDefinition.SlidingExpiration.Value;
                        return await getDataFunction();
                    });
                }
            }

            if (this.Options.IsDebugMode && reportDataSource is IDebugDataProvider debugDataProvider)
            {
                var debugData = debugDataProvider.GetDebugData();
                foreach (var debugDataItem in debugData)
                {
                    dataSet.Metadata.Add(debugDataItem.Key, debugDataItem.Value);
                }
            }

            var output = new ReportOutput(name, reportDefinition, dataSet, filterValues);

            if (dataSet is DataSet baseDataSet)
            {
                if (null != reportDefinition.Output?.Paging)
                {
                    if (!string.IsNullOrWhiteSpace(reportDefinition.Output.Paging.PageIndexParameter) && filterValues.TryGetValue(reportDefinition.Output.Paging.PageIndexParameter, out string pageIndexParameter) && int.TryParse(pageIndexParameter, out int pageIndex))
                    {
                        baseDataSet.PageIndex = pageIndex;
                    }

                    if (!string.IsNullOrWhiteSpace(reportDefinition.Output.Paging.PageSizeParameter) && filterValues.TryGetValue(reportDefinition.Output.Paging.PageSizeParameter, out string pageSizeParameter) && int.TryParse(pageSizeParameter, out int pageSize))
                    {
                        baseDataSet.PageCount = (int)Math.Ceiling((double)output.TotalCount / pageSize);
                    }

                    baseDataSet.HasMore = baseDataSet.PageCount > baseDataSet.PageIndex;
                }
            }

            return output;
        }

        public async Task<FilterOutput> GetFilterOutputAsync(string name, FilterValues filterValues)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

            var reportDefinition = await this.LoadReportAsync(name);
            var filterDefinitions = reportDefinition.GetFilters();
            object contextProvider = this.CreateContextProvider(reportDefinition);
            var dataSources = this.PopulateDataSources(reportDefinition);

            List<FilterItem> filterItems = new List<FilterItem>();

            foreach (var filterDefinition in filterDefinitions)
            {
                IDataSet dataSet = DataSet.Empty;
                if (!string.IsNullOrWhiteSpace(filterDefinition.DataSource))
                {
                    //load filter data
                    if (!dataSources.TryGetValue(filterDefinition.DataSource, out IDataSource filterDataSource))
                    {
                        throw new ArgumentException($"Filter '{filterDefinition.Name}' references a data source '{reportDefinition.Output.DataSource}' which could not be found.");
                    }

                    QueryDefinition queryDefinition = reportDefinition.GetQuery(filterDefinition.Query);

                    IMemoryCache cache = (queryDefinition.AbsoluteExpiration.HasValue || queryDefinition.SlidingExpiration.HasValue) ? this.Cache : new NullMemoryCache();

                    dataSet = await cache.GetOrCreateAsync($"filterdata:{reportDefinition.Title}:{filterDefinition.DataSource}:{filterDefinition.Query}", async (entry) =>
                     {
                         await filterDataSource.OpenAsync();

                         try
                         {
                             this.Logger.LogInformation($"Building filter query '{filterDefinition.Query}'.");
                             IQueryBuilder queryBuilder = filterDataSource.CreateQueryBuilder();

                             IQuery query = queryBuilder.Build(this.ParameterBinder, new ReportingContext(queryDefinition, reportDefinition, filterValues, contextProvider));

                             this.Logger.LogInformation($"Running filter query '{query.CommandText}'.");
                             return await filterDataSource.GetDataAsync(query);
                         }
                         finally
                         {
                             this.Logger.LogInformation($"Closing data source '{filterDataSource.GetType().FullName}'.");
                             await filterDataSource.CloseAsync();
                         }
                     });
                }
                FilterItem filterItem = new FilterItem(filterDefinition, dataSet);
                filterItems.Add(filterItem);
            }

            return new FilterOutput(reportDefinition, filterItems);
        }

        public async Task<ValueType> ExecuteQueryAsync(string name, string queryName, FilterValues filterValues, string dataSourceName = null)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Report name cannot be null or empty.", nameof(name));
            if (string.IsNullOrEmpty(queryName)) throw new ArgumentException("Query name cannot be null or empty.", nameof(queryName));

            filterValues = filterValues ?? new FilterValues();

            var reportDefinition = await this.LoadReportAsync(name);
            var dataSources = this.PopulateDataSources(reportDefinition);

            dataSourceName = string.IsNullOrWhiteSpace(dataSourceName) ? reportDefinition.Output.DataSource : dataSourceName;

            if (!dataSources.TryGetValue(dataSourceName, out IDataSource dataSource))
            {
                throw new ArgumentException($"Output data source '{reportDefinition.Output.DataSource}' could not be found in report definition.");
            }

            object contextProvider = this.CreateContextProvider(reportDefinition);
            var queryDefinition = reportDefinition.GetQuery(queryName);
            var queryBuilder = dataSource.CreateQueryBuilder();
            var query = queryBuilder.Build(this.ParameterBinder, new ReportingContext(queryDefinition, reportDefinition, filterValues, contextProvider));
            try
            {
                await dataSource.OpenAsync();
                return await dataSource.ExecuteAsync(query);
            }
            finally
            {
                await dataSource.CloseAsync();
            }
        }
    }
}