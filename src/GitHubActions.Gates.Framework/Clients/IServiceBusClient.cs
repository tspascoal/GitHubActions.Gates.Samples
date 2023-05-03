namespace GitHubActions.Gates.Framework.Clients
{
    internal interface IServiceBusClient
    {
        Task SendMessage(string QueueName, object Message, DateTime? schedule = null);
    }
}