using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Adriva.Web.Core.Templating
{

    public abstract class BaseTemplate<TModel> : BaseTemplate
    {
        // this will map to @Model (property name)
        public new TModel Model
        {
            get { return (TModel)base.Model; }
            set { base.Model = value; }
        }

    }

    public abstract class BaseTemplate
    {
        private struct AttributeInfo
        {
            public AttributeInfo(string name, string prefix, int prefixOffset, string suffix, int suffixOffset, int attributeValuesCount)
            {
                this.Name = name;
                this.Prefix = prefix;
                this.PrefixOffset = prefixOffset;
                this.Suffix = suffix;
                this.SuffixOffset = suffixOffset;
                this.AttributeValuesCount = attributeValuesCount;
            }

            public int AttributeValuesCount { get; }

            public string Name { get; }

            public string Prefix { get; }

            public int PrefixOffset { get; }

            public string Suffix { get; }

            public int SuffixOffset { get; }

        }

        private readonly StringBuilder OutputBuffer = new StringBuilder();
        private readonly Stack<AttributeInfo> AttributeStack = new Stack<AttributeInfo>();

        public object Model { get; set; }

        public void WriteLiteral(object value)
        {
            if (null != value)
            {
                this.WriteLiteral(value.ToString());
            }
        }

        public void WriteLiteral(string literal)
        {
            this.OutputBuffer.Append(literal);
        }

        public void Write(object obj)
        {
            this.OutputBuffer.Append(obj);
        }

        public void BeginWriteAttribute(string name, string prefix, int prefixOffset, string suffix, int suffixOffset, int attributeValuesCount)
        {
            AttributeInfo attributeInfo = new AttributeInfo(name, prefix, prefixOffset, suffix, suffixOffset, attributeValuesCount);
            this.AttributeStack.Push(attributeInfo);
            this.WriteLiteral(prefix);
        }

        public void WriteAttributeValue(string prefix, int prefixOffset, object value, int valueOffset, int valueLength, bool isLiteral)
        {
            if (null != value)
            {
                string stringValue = value as string;

                if (isLiteral && null != stringValue)
                {
                    this.WriteLiteral(stringValue);
                }
                else if (isLiteral)
                {
                    this.WriteLiteral(value);
                }
                else if (null != stringValue)
                {
                    this.Write(stringValue);
                }
                else
                {
                    this.Write(value);
                }
            }
        }

        public void EndWriteAttribute()
        {
            if (0 == this.AttributeStack.Count) return;
            AttributeInfo attributeInfo = this.AttributeStack.Pop();
            this.WriteLiteral(attributeInfo.Suffix);
        }

        public async virtual Task ExecuteAsync()
        {
            await Task.Yield();
        }

        public string GetText()
        {
            return this.OutputBuffer.ToString();
        }
    }
}
