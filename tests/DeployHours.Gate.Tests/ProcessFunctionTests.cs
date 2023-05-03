using DeployHours.Gate.Rules;
using DeployHours.Gate.Tests.Helpers;
using GitHubActions.Gates.Framework.Models;
using GitHubActions.Gates.Framework.Models.WebHooks;
using Moq;
using Moq.Protected;

namespace DeployHours.Gate.Tests
{
    public class ProcessFunctionTests
    {
        private const string LockoutMessage = "You can't deploy. We are in Lockout mode.";

        public class Process
        {

            [Fact]
            public async Task ShouldReject_WhenInLockout()
            {
                var rulesEvaluatorMock = new Mock<DeployHoursRulesEvaluator>(null); // No need for config. This is a mock
                var deployHoursFunctionProcessMock = new Mock<DeployHoursGateTestableWrapper>(rulesEvaluatorMock.Object) { CallBase = true };

                deployHoursFunctionProcessMock.Setup(s => s.Reject(It.IsAny<string>()))
                    .Returns(Task.CompletedTask).Verifiable();

                rulesEvaluatorMock.Setup(r => r.InLockout()).Returns(true);

                var webhook = Factories.BuildWebHook();                                
                
                await deployHoursFunctionProcessMock.Object.CallProcess(webhook);
                deployHoursFunctionProcessMock.Verify(p => p.Reject(LockoutMessage), Times.Once);
            }

            [Fact]
            public async Task ShouldApprove_WhenInDeployHours()
            {
                var rulesEvaluatorMock = new Mock<DeployHoursRulesEvaluator>(null); // No need for config. This is a mock
                var deployHoursFunctionProcessMock = new Mock<DeployHoursGateTestableWrapper>(rulesEvaluatorMock.Object) { CallBase = true };

                deployHoursFunctionProcessMock
                    .Setup(s => s.Approve(It.IsAny<string?>(), It.IsAny<DateTime?>()))
                    .Returns(Task.CompletedTask).Verifiable();

                rulesEvaluatorMock.Setup(r => r.IsDeployHour(It.IsAny<DateTime>(), It.IsAny<string>()))
                    .Returns(true);

                DeploymentProtectionRuleWebHook webhook = Factories.BuildWebHook();

                await deployHoursFunctionProcessMock.Object.CallProcess(webhook);
                deployHoursFunctionProcessMock.Verify(p => p.Approve(null, null), Times.Once);
            }

            [Fact]
            public async Task Should_AddCommentAndDelayApproval_WhenNotInDeployHours()
            {
                var rulesEvaluatorMock = new Mock<DeployHoursRulesEvaluator>(null); // No need for config. This is a mock
                var deployHoursFunctionProcessMock = new Mock<DeployHoursGateTestableWrapper>(rulesEvaluatorMock.Object) { CallBase = true };

                deployHoursFunctionProcessMock
                    .Setup(s => s.Approve(It.IsAny<string?>(), It.IsAny<DateTime?>()))
                    .Returns(Task.CompletedTask).Verifiable();
                deployHoursFunctionProcessMock
                    .Setup(s => s.AddComment(It.IsAny<string>()))
                    .Returns(Task.CompletedTask).Verifiable();

                rulesEvaluatorMock.Setup(r => r.IsDeployHour(It.IsAny<DateTime>(), It.IsAny<string>()))
                    .Returns(false);

                var expectedQueueTime = new DateTime(2020, 1, 1, 17, 0, 0);
                var expectedComment = "Deploy requested outside deploy hours. Will be automatically approved on next deploy block on **Wednesday, 01 January 2020 17:00 UTC**.";

                rulesEvaluatorMock.Setup(r => r.GetNextDeployHour(It.IsAny<DateTime>(), It.IsAny<string>()))
                    .Returns(expectedQueueTime);

                DeploymentProtectionRuleWebHook webhook = Factories.BuildWebHook();

                await deployHoursFunctionProcessMock.Object.CallProcess(webhook);
                deployHoursFunctionProcessMock.Verify(p => p.AddComment(expectedComment), Times.Once);
                deployHoursFunctionProcessMock.Verify(p => p.Approve(null, expectedQueueTime), Times.Once);
            }
        }

        public class Run
        {
            [Fact]
            public async Task CallsBaseProcess()
            {
                var deployHoursFunctionProcessMock = new Mock<ProcessFunction>() { CallBase = false };

                var expectedMessage = new EventMessage { TryNumber = 1 };
                var log = Factories.CreateLoggerMock();

                await deployHoursFunctionProcessMock.Object.Run(expectedMessage, log.Object);

                deployHoursFunctionProcessMock
                    .Protected()
                    .Verify(
                        "ProcessProcessing",
                        Times.Once(),
                        expectedMessage,
                        log.Object
                    );
            }

        }        
    }
}