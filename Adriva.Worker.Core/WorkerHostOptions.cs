namespace Adriva.Worker.Core
{
    public class WorkerHostOptions
    {
        public bool UseBuiltInBindings { get; set; } = true;

        public bool UseAzureStorageCoreServices { get; set; } = true;

        public bool UseAzureStorage { get; set; } = true;

        public bool UseTimers { get; set; } = true;
    }
}