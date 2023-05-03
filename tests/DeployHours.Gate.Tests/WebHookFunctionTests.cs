using DeployHours.Gate.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Moq;
using Moq.Protected;

namespace DeployHours.Gate.Tests
{
    public class WebHookFunctionTests
    {
        [Fact]
        public async Task Run_CallsBaseProcessWebHook()
        {
            var deployHoursFunctionWebHookFunctionMock = new Mock<WebHookFunction>() { CallBase = false };
            var log = Factories.CreateLoggerMock();
            var req = new Mock<HttpRequest>();
            var expectedQueueName = "deployHoursProcessing";

            await deployHoursFunctionWebHookFunctionMock.Object.Run(req.Object, log.Object);
            deployHoursFunctionWebHookFunctionMock
                .Protected()
                .Verify(
                    "ProcessWebHook", Times.Once(),
                    req.Object,
                    log.Object,
                    expectedQueueName
                );
        }
    }
}