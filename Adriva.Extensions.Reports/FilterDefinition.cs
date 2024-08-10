using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

namespace Adriva.Extensions.Reports
{

    public class FilterDefinition : RendererOptionsProvider
    {
        [JsonIgnore]
        private static readonly Dictionary<string, MethodInfo> FormatterMethodsCache = new Dictionary<string, MethodInfo>();

        private TypeCode DataTypeField = TypeCode.String;

        private object DefaultValueField = null;

        public IList<FilterDefinition> Filters { get; set; } = new List<FilterDefinition>();

        public string Name { get; set; }

        public string Title { get; set; }

        public string DataSource { get; set; }

        public string Query { get; set; }

        public TypeCode DataType
        {
            get => this.DataTypeField;
            set
            {
                this.DataTypeField = TypeCode.Empty == value ? TypeCode.String : value;
            }
        }

        public ParameterType Type { get; set; }

        public bool IsRequired { get; set; }

        public object DefaultValue
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.DefaultValueFormatter)) return this.DefaultValueField;
                return Helpers.ApplyMethodFormatter(this.DefaultValueField, this.DefaultValueFormatter, FilterDefinition.FormatterMethodsCache);
            }
            set
            {
                this.DefaultValueField = value;
            }
        }

        public string DefaultValueFormatter { get; set; }

        public override string ToString()
        {
            return $"[Filter] name = '{this.Name}'";
        }
    }


}