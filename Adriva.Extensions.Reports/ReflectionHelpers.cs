using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Adriva.Extensions.Reports
{
    public static class ReflectionHelpers
    {
        public static string[] GetPropertyNames(Type itemType, IMemoryCache cache)
        {
            if (null == itemType) throw new ArgumentNullException(nameof(itemType));

            return cache.GetOrCreate<string[]>($"{itemType.FullName}:", (entry) =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(60);
                entry.Priority = CacheItemPriority.Normal;

                var properties = TypeDescriptor.GetProperties(itemType);
                var enumerator = properties.GetEnumerator();

                string[] output = new string[properties.Count];
                int loop = 0;

                while (enumerator.MoveNext())
                {
                    output[loop++] = ((PropertyDescriptor)enumerator.Current).Name;
                }

                return output;
            });
        }

        public static object GetPropertyValue(object item, string propertyName, IMemoryCache cache)
        {
            if (null == item) throw new ArgumentNullException(nameof(item));
            if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentNullException(nameof(propertyName));

            Type typeOfItem = item.GetType();

            PropertyDescriptor property = cache.GetOrCreate($"{typeOfItem.FullName}:{propertyName}", (entry) =>
            {
                var properties = TypeDescriptor.GetProperties(typeOfItem);
                return properties.Find(propertyName, true);
            });

            if (null == property) throw new ArgumentException($"Property '{propertyName} doesn't exist on type '{typeOfItem.FullName}'.'");
            return property.GetValue(item);
        }

        public static async Task<object> CallMethodAsync(object item, IQuery query, IMemoryCache cache)
        {
            await Task.CompletedTask;
            if (null == item) throw new ArgumentNullException(nameof(item));

            if (string.IsNullOrWhiteSpace(query.CommandText)) throw new ArgumentNullException(nameof(query.CommandText));

            Type typeOfItem = item.GetType();

            var allMethods = typeOfItem.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod);
            var matchingMethods = allMethods.Where(m =>
            {
                if (m.IsSpecialName) return false;
                if (0 != string.Compare(query.CommandText, m.Name, StringComparison.OrdinalIgnoreCase)) return false;

                var methodParameters = m.GetParameters();
                if (methodParameters.Length != query.Parameters.Count) return false;

                if (!query.Parameters.All(qp =>
                        methodParameters.Any(mp =>
                            0 == string.Compare(mp.Name, qp.Name, StringComparison.OrdinalIgnoreCase)
                            && ((null == qp.Value && !mp.ParameterType.IsValueType) || mp.ParameterType.IsAssignableFrom(qp.Value.GetType())))
                        ))
                    return false;

                return true;
            });

            var matchingMethod = matchingMethods.FirstOrDefault();

            if (null == matchingMethod)
            {
                throw new MethodAccessException($"Couldn't locate object data source method '{query.CommandText}'.");
            }
            else if (matchingMethod.ReturnType.Equals(typeof(void)))
            {
                throw new InvalidOperationException($"Data source method '{query.CommandText}' returns System.Void.");
            }

            object[] arguments = query.Parameters.Select(x => x.Value).ToArray();

            object result = matchingMethod.Invoke(item, arguments);

            if (result is Task taskResult)
            {
                taskResult.GetAwaiter().GetResult();
                return ReflectionHelpers.GetPropertyValue(taskResult, "Result", cache);
            }
            else
                return result;
        }
    }
}