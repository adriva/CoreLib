using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Adriva.Extensions.Reports
{

    public class ParameterBinder : IParameterBinder
    {
        private const string ParameterMatchExpression = @"(?<parameter>@(?<parameterName>\w+))";
        private const string ParameterGroupName = "parameter";
        private const string ParameterNameGroupName = "parameterName";

        private readonly IMemoryCache Cache;
        private readonly ReportingServiceOptions Options;

        public ParameterBinder(IMemoryCache cache, IOptions<ReportingServiceOptions> optionsAccessor)
        {
            this.Cache = cache;
            this.Options = optionsAccessor.Value;
        }

        private object GetQueryParameterValue(FilterDefinition filterDefinition, object filterValue, object contextProvider)
        {
            object valueCandidate = null;

            if (null == filterDefinition)
            {
                filterDefinition = new FilterDefinition()
                {
                    Type = ParameterType.Filter
                };
            }

            if (ParameterType.Constant == filterDefinition.Type)
            {
                valueCandidate = filterDefinition.DefaultValue;
            }
            else
            {
                if (ParameterType.Filter == filterDefinition.Type || ParameterType.NonUserItem == filterDefinition.Type) //non user items can still be passed from the client
                {
                    valueCandidate = filterValue;
                    if (null == filterValue && null != filterDefinition.DefaultValue)
                    {
                        valueCandidate = filterDefinition.DefaultValue;
                    }
                }

                if (ParameterType.Context == filterDefinition.Type)
                {
                    valueCandidate = ReflectionHelpers.GetPropertyValue(contextProvider, Convert.ToString(filterDefinition.DefaultValue), this.Cache);
                }
            }

            if (valueCandidate is StringValue stringValue)
            {
                if (stringValue.HasValue) return (string)stringValue;
                return null;
            }

            if (null == valueCandidate) return null;

            try
            {
                return Convert.ChangeType(valueCandidate, filterDefinition.DataType, this.Options.Culture ?? CultureInfo.CurrentCulture);
            }
            catch (Exception conversionException) { 
                throw new Exception($"Failed to convert {filterDefinition.Name}, DefaultValue='{filterDefinition.DefaultValue??"NULL"}',ValueCandidate='{valueCandidate??"NULL"}',DataType='{filterDefinition.DataType}',Culture='{this.Options.Culture?.Name??"Default"}'.", conversionException);
            }
        }

        public void Bind(IQuery query, ReportingContext context, Func<object, object> valueFormatter = null)
        {
            IEnumerable<FilterDefinition> filters = context.Report.Filters;
            FilterValues values = context.Values;
            object contextProvider = context.ContextProvider;

            if (null == query) throw new ArgumentNullException(nameof(query));
            filters = filters ?? Enumerable.Empty<FilterDefinition>();

            if (string.IsNullOrWhiteSpace(query.CommandText)) return;

            Dictionary<string, QueryParameter> extractedParameters = new Dictionary<string, QueryParameter>();

            var matches = Regex.Matches(query.CommandText, ParameterBinder.ParameterMatchExpression, RegexOptions.Compiled | RegexOptions.CultureInvariant);

            // fill implicit parameters
            if (null != matches && 0 < matches.Count)
            {
                foreach (Match parameterMatch in matches)
                {
                    if (parameterMatch.Groups[ParameterBinder.ParameterGroupName].Success)
                    {
                        string parameterName = parameterMatch.Groups[ParameterBinder.ParameterGroupName].Value;
                        if (!extractedParameters.ContainsKey(parameterName))
                        {
                            extractedParameters.Add(parameterName, null);
                        }
                    }
                }
            }

            Queue<KeyValuePair<string, QueryParameter>> asssignmentQueue = new Queue<KeyValuePair<string, QueryParameter>>();

            // try to get a value for the extracted parameter 
            foreach (var extractedParameter in extractedParameters)
            {
                string extractedParameterName = extractedParameter.Key;
                if (extractedParameterName.StartsWith("@", StringComparison.Ordinal))
                {
                    extractedParameterName = extractedParameterName.Substring(1);
                }

                FilterDefinition filterDefinition = filters.FirstOrDefault(f => 0 == string.Compare(extractedParameterName, f.Name, StringComparison.OrdinalIgnoreCase));

                string filterValue = null;

                if (null != values)
                {
                    if (!values.TryGetValue(extractedParameter.Key, out filterValue))
                    {
                        if (extractedParameter.Key.StartsWith("@", StringComparison.Ordinal))
                        {
                            if (!values.TryGetValue(extractedParameter.Key.Substring(1), out filterValue))
                            {
                                filterValue = StringValue.Unassigned;
                            }
                        }
                        else
                        {
                            if (!values.TryGetValue("@" + extractedParameter.Key, out filterValue))
                            {
                                filterValue = StringValue.Unassigned;
                            }
                        }
                    }
                }

                object parameterValue = this.GetQueryParameterValue(filterDefinition, filterValue, contextProvider);

                if (null != valueFormatter)
                {
                    parameterValue = valueFormatter.Invoke(parameterValue);
                }

                asssignmentQueue.Enqueue(new KeyValuePair<string, QueryParameter>(extractedParameter.Key, new QueryParameter(extractedParameter.Key, parameterValue)));
            }

            while (0 < asssignmentQueue.Count)
            {
                var pair = asssignmentQueue.Dequeue();
                query.Parameters.Add(new QueryParameter(pair.Key, pair.Value.Value));
            }
        }
    }
}