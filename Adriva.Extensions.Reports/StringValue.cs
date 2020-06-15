using System;

namespace Adriva.Extensions.Reports
{
    public sealed class StringValue
    {
        private readonly string Value;

        public static StringValue Unassigned => new StringValue(null) { HasValue = false };

        public bool HasValue { get; private set; }

        public StringValue(string text)
        {
            this.Value = text;
            this.HasValue = true;
        }

        public static implicit operator string(StringValue stringValue)
        {
            return stringValue?.Value;
        }

        public static implicit operator StringValue(string value)
        {
            return new StringValue(value);
        }

        public static bool operator ==(StringValue first, StringValue second)
        {
            if (null == (object)first) return null == (object)second;
            if (null == (object)second) return false;
            return 0 == string.Compare(first.Value, second.Value, StringComparison.Ordinal);
        }

        public static bool operator !=(StringValue first, StringValue second)
        {
            if (null == (object)first) return null != (object)second;
            if (null == (object)second) return true;
            return 0 != string.Compare(first.Value, second.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (null == obj) return false;
            if (obj is StringValue stringValue)
            {
                if (null == this.Value) return null == stringValue.Value;
                return this.Value.Equals(stringValue.Value);
            }
            else if (obj is string value)
            {
                if (null == this.Value) return null == value;
                return this.Value.Equals(value);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return null == this.Value ? 0 : this.Value.GetHashCode();
        }
    }
}