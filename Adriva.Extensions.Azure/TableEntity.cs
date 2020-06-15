using System;
using AzureTableEntity = Microsoft.WindowsAzure.Storage.Table.TableEntity;

namespace Adriva.Extensions.Azure
{

    [Serializable]
    public abstract class TableEntity : AzureTableEntity
    {

        public virtual void Normalize()
        {

        }

    }
}
