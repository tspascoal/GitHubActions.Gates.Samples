using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using AZSB = Azure.Messaging.ServiceBus;
using Moq;

using GitHubActions.Gates.Framework.Clients;
using GitHubActions.Gates.Framework.Models;
using GitHubActions.Gates.Framework.Models.WebHooks;
using GitHubActions.Gates.Framework.Tests.Helpers;

namespace GitHubActions.Gates.Framework.Tests
{
    public class ServiceBusClientTests
    {
        public class SendMessageAsync
        {
            [Fact]
            public async Task SendMessageAsync_SendsMessageToQueue()
            {
                var queueName = "test-queue";

                var sbCLientMock = new Mock<AZSB.ServiceBusClient>();
                var sbClient = new ServiceBusClient(Factories.CreateServiceBusConfigMock().Object, sbCLientMock.Object);
                var senderMock = new Mock<AZSB.ServiceBusSender>();

                var message = new EventMessage
                {
                    TryNumber = 2,
                    WebHookPayload = new DeploymentProtectionRuleWebHook { }
                };

                sbCLientMock.Setup(sb => sb.CreateSender(queueName))
                    .Returns(senderMock.Object);
                senderMock.Setup(s => s.SendMessageAsync(
                    It.IsAny<AZSB.ServiceBusMessage>(),
                    It.IsAny<CancellationToken>()));

                await sbClient.SendMessage(queueName, message);

                // Check if message was sent
                senderMock.Verify(c => c.SendMessageAsync(
                        It.Is<AZSB.ServiceBusMessage>(
                            m => m.ScheduledEnqueueTime == new DateTime() &&
                            m.ContentType == "application/json" && ValidateMsg(m.Body, message)),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once);
            }

            [Fact]
            public async Task SendMessageAsync_SendsMessageToQueueEnqueued()
            {
                var queueName = "test-queue";

                var sbCLientMock = new Mock<AZSB.ServiceBusClient>();
                var sbClient = new ServiceBusClient(Factories.CreateServiceBusConfigMock().Object, sbCLientMock.Object);
                var senderMock = new Mock<AZSB.ServiceBusSender>();

                var message = new EventMessage
                {
                    TryNumber = 3,
                    WebHookPayload = new DeploymentProtectionRuleWebHook { }
                };

                sbCLientMock.Setup(sb => sb.CreateSender(queueName))
                    .Returns(senderMock.Object);
                senderMock.Setup(s => s.SendMessageAsync(
                    It.IsAny<AZSB.ServiceBusMessage>(),
                    It.IsAny<CancellationToken>()));

                var expectedQueueTime = new DateTime(2030, 1, 3, 10, 30, 34);

                await sbClient.SendMessage(queueName, message, expectedQueueTime);

                // Check if message was sent
                senderMock.Verify(c => c.SendMessageAsync(
                        It.Is<AZSB.ServiceBusMessage>(
                            m => m.ScheduledEnqueueTime == expectedQueueTime &&
                            m.ContentType == "application/json" && ValidateMsg(m.Body, message)),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Once);
            }

        }

        public class CreateServiceBusClientFactory
        {

            [Fact]
            public void WithConnectionString_CreatesServiceBusClient()
            {
                // This is just an heuristic. Unfortunately there is no way to know if we are using a managed identity
                // from the client (unless we looked at private data)
                var configMock = Factories.CreateServiceBusConfigMock(connectionString: "Endpoint=sb://serviceconnection.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=1234567890");

                var sb = new ServiceBusClient(configMock.Object);

                var client = sb.CreateServiceBusClientFactory();

                Assert.Equal("serviceconnection.servicebus.windows.net", client.FullyQualifiedNamespace);
            }

            [Fact]
            public void WithManagedIdentity_CreatesServiceBusClient()
            {
                // This is just an heuristic. Unfortunately there is no way to know if we are using a managed identity
                // from the client (unless we looked at private data)
                var configMock = Factories.CreateServiceBusConfigMock(fullyQualifiedNamespace: "managed.servicebus.windows.net");


                var sb = new ServiceBusClient(configMock.Object);

                var client = sb.CreateServiceBusClientFactory();

                Assert.Equal("managed.servicebus.windows.net", client.FullyQualifiedNamespace);
            }

        }

        private static bool ValidateMsg<T>(BinaryData body, T message)
        {
            var msg = body.ToObjectFromJson<T>();

            if (msg == null || message == null) return false;

            // Cheap way to compare objects without deep comparing it via reflection

            var msgJson = JsonConvert.SerializeObject(msg);
            var messageJson = JsonConvert.SerializeObject(message);

            return msgJson == messageJson;
        }
    }
}