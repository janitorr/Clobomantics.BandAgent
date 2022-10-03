namespace MessageProcessor
{
    internal class EventHubOptions
    {
        public const string EventHub = "EventHub";

        public string HubName { get; set; } = string.Empty;
        public string IotHubConnectionString { get; set; } = string.Empty;
        public string StorageConnectionString { get; set; } = string.Empty;
        public string StorageContainerName { get; set; } = string.Empty;
    }
}
