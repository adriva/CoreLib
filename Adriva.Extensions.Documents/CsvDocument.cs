using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adriva.Extensions.Documents
{

    internal class CsvDocument : ICsvDocument
    {

        private StreamWriter Writer;

        public void Create(Stream stream)
        {
            stream.SetLength(0);
            this.Writer = new StreamWriter(stream, Encoding.UTF8, 4096, true);
        }

        public void Open(string path)
        {
            var fileStream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            this.Writer = new StreamWriter(fileStream, Encoding.UTF8, 4096, true);
        }

        public void Open(string path, string workingFilePath)
        {
            throw new NotSupportedException("This method is not supported for CSV files.");
        }

        public void Open(Stream stream)
        {
            this.Writer = new StreamWriter(stream, Encoding.UTF8, 4096, true);
        }

        private string FormatItem(object value)
        {
            if (null == value) return string.Empty;

            TypeCode typeCode = Type.GetTypeCode(value.GetType());

            switch (typeCode)
            {
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
                case TypeCode.DateTime:
                    return "\"" + ((DateTime)value).ToString(CultureInfo.CurrentCulture) + "\"";
                default:
                    return "\"" + Convert.ToString(value, CultureInfo.InvariantCulture).Replace("\"", "\"\"") + "\"";
            }
        }

        public async Task WriteRowAsync(object[] row)
        {
            if (null == row) return;

            string csvRow = string.Join(",", row.Select(x => this.FormatItem(x)));
            await this.Writer.WriteLineAsync(csvRow);
        }

        public void Dispose()
        {
            this.Writer?.Dispose();
        }
    }
}
