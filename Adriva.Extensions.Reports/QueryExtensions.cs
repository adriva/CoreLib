using System;
using System.Text;
using Adriva.Common.Core;

namespace Adriva.Extensions.Reports
{
    public static class QueryExtensions
    {
        public static string GetUniqueId(this IQuery query)
        {
            if (null == query) throw new ArgumentNullException(nameof(query));

            StringBuilder buffer = new StringBuilder(query.CommandText);
            buffer.AppendLine();
            foreach (var parameter in query.Parameters)
            {
                string parameterValue;

                if (null == parameter.Value) parameterValue = "null";
                else if (parameter.Value is string stringParameter)
                {
                    parameterValue = stringParameter;
                }
                else if (parameter.Value.GetType().IsPrimitive)
                {
                    parameterValue = Convert.ToString(parameter.Value);
                }
                else
                {
                    parameterValue = Utilities.SafeSerialize(parameter.Value);
                }

                buffer.AppendLine($"{parameter.Name}:{parameterValue}");
            }

            return Utilities.CalculateKeySafeHash(buffer.ToString());
        }
    }
}