using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Adriva.Extensions.Reports
{

    public static class Helpers
    {
        private static readonly Regex FieldMatchRegex = new Regex(@"(?<start>\{)+(?<property>[\w\.\[\]]+)(:(?<format>[^}]+)?)?(?<end>\})+", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string ApplyTemplate(IDataItem dataItem, string template)
        {
            if (null == dataItem) return null;
            if (string.IsNullOrWhiteSpace(template)) return Convert.ToString(dataItem);

            return Helpers.FieldMatchRegex.Replace(template, (match) =>
            {
                string format = null;

                if (match.Groups["format"].Success)
                {
                    format = match.Groups["format"].Value;

                    if (string.IsNullOrWhiteSpace(format))
                    {
                        format = null;
                    }
                }

                if (match.Groups["property"].Success)
                {
                    string propertyName = match.Groups["property"].Value;

                    if (!string.IsNullOrWhiteSpace(propertyName))
                    {
                        if (string.IsNullOrWhiteSpace(format))
                        {
                            return Convert.ToString(dataItem.GetValue(propertyName));
                        }
                        else
                        {
                            if (dataItem.GetValue(propertyName) is IFormattable formattable)
                            {
                                return formattable.ToString(format, CultureInfo.CurrentUICulture);
                            }
                            else
                            {
                                return Convert.ToString(dataItem.GetValue(propertyName));
                            }
                        }
                    }
                }

                return null;
            });
        }

        private static bool TryGetFormatterMethod(IDictionary<string, MethodInfo> methodCache, string cacheKey, string formatter, out MethodInfo formatterMethod)
        {
            formatterMethod = null;

            if (!methodCache.TryGetValue(cacheKey, out formatterMethod))
            {
                var match = Regex.Match(formatter, @"^(?<typeName>(\w|\.)+)\:(?<methodName>\w+)\s*\,\s*(?<assemblyName>(\w|\.)+)$");

                if (
                    match.Groups["typeName"].Success && match.Groups["assemblyName"].Success && match.Groups["methodName"].Success &&
                    !string.IsNullOrWhiteSpace(match.Groups["typeName"].Value) && !string.IsNullOrWhiteSpace(match.Groups["assemblyName"].Value) && !string.IsNullOrWhiteSpace(match.Groups["methodName"].Value)
                )
                {
                    string typeName = $"{match.Groups["typeName"].Value}, {match.Groups["assemblyName"].Value}";
                    string methodName = match.Groups["methodName"].Value;

                    Type targetType = Type.GetType(typeName, false);
                    if (null == targetType) return false;
                    formatterMethod = targetType.GetMethod(methodName, BindingFlags.Static | BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Public);
                    if (null == formatterMethod || 1 != formatterMethod.GetParameters().Length)
                    {
                        formatterMethod = null;
                    }
                    methodCache.Add(cacheKey, formatterMethod);
                }
            }
            return null != formatterMethod;
        }

        public static object ApplyMethodFormatter(IDataItem dataItem, string fieldName, string formatter, IDictionary<string, MethodInfo> methodCache)
        {
            if (null == dataItem) return null;
            if (string.IsNullOrWhiteSpace(fieldName)) return null;
            if (string.IsNullOrWhiteSpace(formatter)) return Convert.ToString(dataItem);
            if (null == methodCache) return Convert.ToString(dataItem);

            string value = Convert.ToString(dataItem.GetValue(fieldName));
            string cacheKey = $"{fieldName}:{formatter}";

            if (!Helpers.TryGetFormatterMethod(methodCache, cacheKey, formatter, out MethodInfo formatterMethod)) return value;
            return formatterMethod.Invoke(null, new object[] { value });
        }

        public static object ApplyMethodFormatter(object value, string formatter, IDictionary<string, MethodInfo> methodCache)
        {
            if (string.IsNullOrWhiteSpace(formatter)) return value;
            if (null == methodCache) return value;

            if (!Helpers.TryGetFormatterMethod(methodCache, $"Formatter:{formatter}", formatter, out MethodInfo formatterMethod)) return value;
            return formatterMethod.Invoke(null, new object[] { value });
        }

        public static object[] GetDataArray(this ReportOutput reportOutput, IDataItem dataItem, Func<object, object> dataValueFormatter = null)
        {
            if (null == dataItem) return Array.Empty<object>();

            object[] rowValues = new object[reportOutput.ColumnDefinitons.Count];

            for (int loop = 0; loop < reportOutput.ColumnDefinitons.Count; loop++)
            {
                var columnDefinition = reportOutput.ColumnDefinitons[loop];

                rowValues[loop] = null;

                if (null == dataValueFormatter)
                {
                    rowValues[loop] = dataItem.GetValue(columnDefinition.Field);
                }
                else
                {
                    rowValues[loop] = dataValueFormatter.Invoke(dataItem.GetValue(columnDefinition.Field));
                }
            }

            return rowValues;
        }
    }
}