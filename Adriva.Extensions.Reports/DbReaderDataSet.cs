using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Adriva.Extensions.Reports
{
    public class DbReaderDataSet : DataSet
    {
        private class DbItemRecord : IDataItem
        {
            private readonly DbReaderDataSet DataSet;
            private readonly object[] Values;

            internal DbItemRecord(DbReaderDataSet dataSet, object[] values)
            {

                for (int loop = 0; loop < values.Length; loop++)
                {
                    if (values[loop] == DBNull.Value)
                    {
                        values[loop] = null;
                    }
                }

                this.Values = values;
                this.DataSet = dataSet;
            }

            public object GetValue(string fieldName)
            {
                int index = this.DataSet.GetFieldIndex(fieldName);
                return -1 < index ? this.Values[index] : null;
            }
        }

        private readonly DbDataReader Reader;
        private readonly Dictionary<string, int> FieldIndices = new Dictionary<string, int>();
        private readonly LinkedList<DbItemRecord> Rows = new LinkedList<DbItemRecord>();

        public override IEnumerable<IDataItem> Items
        {
            get
            {
                return this.Rows;
            }
        }

        public DbReaderDataSet(DbDataReader reader)
        {
            this.Reader = reader;

            this.FieldNames = new string[this.Reader.FieldCount];

            for (int loop = 0; loop < this.Reader.FieldCount; loop++)
            {
                string fieldName = this.Reader.GetName(loop);
                this.FieldIndices[fieldName] = loop;
                this.FieldNames[loop] = fieldName;
            }
        }

        internal int GetFieldIndex(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName)) throw new ArgumentNullException(fieldName);
            return this.FieldIndices.ContainsKey(fieldName) ? this.FieldIndices[fieldName] : -1;
        }

        public void AddItem(object[] values)
        {
            if (null == values) throw new ArgumentNullException(nameof(values));
            DbItemRecord record = new DbItemRecord(this, values);
            this.Rows.AddLast(record);
        }
    }
}