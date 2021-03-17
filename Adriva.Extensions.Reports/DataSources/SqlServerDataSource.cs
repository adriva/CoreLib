using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adriva.Extensions.Reports.DataSources
{

    public sealed class SqlServerDataSource : DataSource<SqlServerDataSourceParameters>
    {
        private class SqlQuery : IQuery
        {
            public string CommandText { get; private set; }

            public IList<QueryParameter> Parameters { get; private set; } = new List<QueryParameter>();

            public SqlQuery(string commandText)
            {
                this.CommandText = commandText;
            }
        }

        private class SqlQueryBuilder : IQueryBuilder
        {
            private static readonly Func<object, object> ValueFormatter = (value) =>
            {
                if (null == value) return DBNull.Value;
                return value;
            };

            public IQuery Build(IParameterBinder parameterBinder, ReportingContext context)
            {
                SqlQuery query = new SqlQuery(context.Query.Command);
                parameterBinder.Bind(query, context, SqlQueryBuilder.ValueFormatter);
                return query;
            }


        }

        private readonly SqlConnection Connection;
        private readonly ReportingServiceOptions ServiceOptions;

        public SqlServerDataSource(IConfiguration parameterConfiguration,
                                    IOptions<ReportingServiceOptions> optionsAccessor,
                                    ILogger<SqlServerDataSource> logger)
                                        : base(parameterConfiguration, logger)
        {
            this.ServiceOptions = optionsAccessor.Value;
            this.Connection = new SqlConnection();
        }

        public override async Task OpenAsync()
        {
            this.Logger.LogInformation($"Opening connection to SQL Server.");
            this.Connection.ConnectionString = this.Parameters.ConnectionString;
            await this.Connection.OpenAsync();
            this.Logger.LogInformation($"SQL Server connection opened. [{this.Connection.Database}]");
        }

        public override Task CloseAsync()
        {
            this.Logger.LogInformation($"Closing SQL Server connection... [{this.Connection.Database}]");
            this.Connection.Close();
            this.Connection.Dispose();
            this.Logger.LogInformation($"SQL Server connection closed. [{this.Connection.Database}]");
            return Task.CompletedTask;
        }

        public override IQueryBuilder CreateQueryBuilder()
        {
            return new SqlQueryBuilder();
        }

        public override async Task<IDataSet> GetDataAsync(IQuery query)
        {
            using (SqlCommand sqlCommand = this.Connection.CreateCommand())
            {
                sqlCommand.CommandText = query.CommandText;
                foreach (var queryParameter in query.Parameters)
                {
                    var sqlParameter = sqlCommand.CreateParameter();
                    sqlParameter.ParameterName = queryParameter.Name;
                    sqlParameter.Value = queryParameter.Value;
                    sqlCommand.Parameters.Add(sqlParameter);

                }

                if (this.ServiceOptions.IsDebugMode)
                {
                    StringBuilder buffer = new StringBuilder();
                    buffer.Append(null == query.CommandText ? "<NULL COMMAND TEXT>" : query.CommandText);

                    foreach (var commandParameter in query.Parameters)
                    {
                        string groupChar = string.Empty;
                        if (commandParameter.Value is string || commandParameter.Value is DateTime)
                        {
                            groupChar = "\'";
                        }

                        buffer.Replace(commandParameter.Name, null == commandParameter.Value || DBNull.Value == commandParameter.Value ? "null" : $"{groupChar}{commandParameter.Value}{groupChar}");
                    }

                    base.DebugDataDictionary["SqlCommand"] = buffer.ToString();
                    buffer.Clear();
                }
                SqlDataReader dataReader = null;

                if (this.ServiceOptions.IsDebugMode)
                {
                    Stopwatch timer = new Stopwatch();
                    timer.Start();
                    dataReader = await sqlCommand.ExecuteReaderAsync(CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);
                    timer.Stop();
                    base.DebugDataDictionary["Query Execution Time"] = timer.Elapsed;
                }
                else
                {
                    dataReader = await sqlCommand.ExecuteReaderAsync(CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);
                }

                DbReaderDataSet dataSet = new DbReaderDataSet(dataReader);

                while (await dataReader.ReadAsync())
                {
                    object[] row = new object[dataReader.FieldCount];
                    dataReader.GetValues(row);
                    dataSet.AddItem(row);
                }

                return dataSet;
            }
        }

        public override async Task<ValueType> ExecuteAsync(IQuery query)
        {
            using (SqlCommand sqlCommand = this.Connection.CreateCommand())
            {
                sqlCommand.CommandText = query.CommandText;

                foreach (var queryParameter in query.Parameters)
                {
                    var sqlParameter = sqlCommand.CreateParameter();
                    sqlParameter.ParameterName = queryParameter.Name;
                    sqlParameter.Value = queryParameter.Value;
                    sqlCommand.Parameters.Add(sqlParameter);
                }

                return (ValueType)await sqlCommand.ExecuteScalarAsync();
            }
        }
    }
}