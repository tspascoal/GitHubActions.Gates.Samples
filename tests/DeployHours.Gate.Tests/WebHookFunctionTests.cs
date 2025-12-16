using DeployHours.Gate.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace DeployHours.Gate.Tests
{
    public class WebHookFunctionTests
    {
        [Fact]
        public async Task Run_CallsBaseProcessWebHook()
        {
            var loggerMock = new Mock<ILogger<WebHookFunction>>();
            var deployHoursFunctionWebHookFunctionMock = new Mock<WebHookFunction>(loggerMock.Object) { CallBase = false };
            var req = new Mock<HttpRequest>();
            var expectedQueueName = "deployHoursProcessing";

            await deployHoursFunctionWebHookFunctionMock.Object.Run(req.Object);
            deployHoursFunctionWebHookFunctionMock
                .Protected()
                .Verify(
                    "ProcessWebHook", Times.Once(),
                    req.Object,
                    ItExpr.IsAny<ILogger>(),
                    expectedQueueName
                );
        }
    }
}