using System;
using System.Collections.Generic;

namespace Adriva.Extensions.Reports
{

    public class FilterDefinition : RendererOptionsProvider
    {
        private TypeCode DataTypeField = TypeCode.String;

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

        public object DefaultValue { get; set; }

        public override string ToString()
        {
            return $"[Filter] name = '{this.Name}'";
        }
    }


}