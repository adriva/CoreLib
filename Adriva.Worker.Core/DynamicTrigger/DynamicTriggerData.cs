
namespace Adriva.Worker.Core.DynamicTrigger
{
    public class DynamicTriggerData
    {

        private readonly object Data;

        public DynamicTriggerData(object data)
        {
            this.Data = data;
        }

        public T GetData<T>()
        {
            return (T)this.Data;
        }
    }
}