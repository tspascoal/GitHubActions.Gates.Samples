using DeployHours.Gate.Models;

namespace DeployHours.Gate.Tests
{
    public class DeployHoursConfigurationTests
    {
        private static DeployHoursConfiguration LoadYAML(string YAML)
        {
            var config = new DeployHoursConfiguration();
            config.Load(YAML);

            return config;
        }

        [Fact]
        public void Load_LockoutTrue()
        {
            var config = LoadYAML(@"Lockout: true");

            Assert.True(config.Lockout);
        }

        [Fact]
        public void Load_LockoutFalse()
        {

            var config = LoadYAML(@"Lockout: false");

            Assert.False(config.Lockout);
        }

        [Fact]
        public void Load_RangeOneElement()
        {


            var config = LoadYAML(@"Rules:
            - Environment:
              DeploySlots:
                - Start: 01:46:40
                  End:  12:43:40
        ");

            Assert.False(config.Lockout);

            Assert.NotNull(config.Rules);
            Assert.Single(config.Rules);
            Assert.Single(config.Rules[0].DeploySlots);
            Assert.Equal(new TimeOnly(1, 46, 40), config.Rules[0].DeploySlots[0].Start);
            Assert.Equal(new TimeOnly(12, 43, 40), config.Rules[0].DeploySlots[0].End);
            Assert.Null(config.Validate());
        }

        [Fact]
        public void Load_RangeTwoElements()
        {


            var config = LoadYAML(@"Rules:
            - Environment:
              DeploySlots:
              - Start: 01:46:40
                End:  12:46:40
              - Start: 13:00:40
                End:  16:21:40");

            Assert.False(config.Lockout);

            Assert.NotNull(config.Rules);
            Assert.Equal(2, config.Rules[0].DeploySlots.Count);
            Assert.Equal(new TimeOnly(1, 46, 40), config.Rules[0].DeploySlots[0].Start);
            Assert.Equal(new TimeOnly(12, 46, 40), config.Rules[0].DeploySlots[0].End);
            Assert.Equal(new TimeOnly(13, 00, 40), config.Rules[0].DeploySlots[1].Start);
            Assert.Equal(new TimeOnly(16, 21, 40), config.Rules[0].DeploySlots[1].End);
        }

        [Fact]
        public void Load_RangeTwoElementsMissingElements()
        {


            var config = LoadYAML(@"Rules:
            - Environment:
              DeploySlots:
              - Start: 01:46:40
              - End:  16:21:40");

            Assert.False(config.Lockout);

            Assert.NotNull(config.Rules);
            Assert.Equal(2, config.Rules[0].DeploySlots.Count);
            Assert.Equal(new TimeOnly(1, 46, 40), config.Rules[0].DeploySlots[0].Start);
            Assert.Equal(new TimeOnly(16, 21, 40), config.Rules[0].DeploySlots[1].End);
        }

        [Fact]
        public void Load_DeployDays()
        {

            var config = LoadYAML("DeployDays: [\"Monday\",\"Tuesday\", \"Wednesday\", \"Thursday\"]");

            Assert.Equal(4, config.DeployDays.Length);

            Assert.Equal(
                new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday },
                config.DeployDays);
        }

        [Fact]
        public void Load_HasDefaultWorkingDays()
        {
            var config = LoadYAML("Version: 1");

            Assert.Equal(5, config.DeployDays.Length);

            Assert.Equal(
                new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
                config.DeployDays);
        }

        [Fact]
        public void Validate_ReturnsError_DeployDaysCannotBeEmpty()
        {
            var config = new DeployHoursConfiguration
            {
                DeployDays = Array.Empty<DayOfWeek>(),
                Rules = new List<DeployHoursRule>
                {
                    new DeployHoursRule
                    {
                        Environment = "production",
                        DeploySlots = new List<DeploySlotRange>
                        {
                            new DeploySlotRange
                            {
                                Start = new TimeOnly(10,0),
                                End = new TimeOnly(10,45)
                            }
                        }
                    }
                }
            };

            var errors = config.Validate();

            Assert.Single(errors);
            Assert.Equal("If DeployDays is defined it cannot be empty", errors[0]);
        }

        [Fact]
        public void Validate_ReturnsError_WhenRuleBusinessHoursIsEmpty()
        {

            var config = new DeployHoursConfiguration
            {
                Rules = new List<DeployHoursRule>
                {
                    new DeployHoursRule
                    {
                        DeploySlots = new List<DeploySlotRange>()
                    }
                }
            };


            var errors = config.Validate();

            Assert.Single(errors);
            Assert.Equal("DeployHours element is mandatory (environment: ANY)", errors[0]);
        }

        [Fact]
        public void Validate_ReturnsError_WhenRulesIsEmpty()
        {

            var config = new DeployHoursConfiguration
            {
                Rules = new List<DeployHoursRule>()
            };


            var errors = config.Validate();

            // Assert
            Assert.Single(errors);
            Assert.Equal("Rules is mandatory", errors[0]);
        }

        [Fact]
        public void Validate_ReturnsError_WhenRulesIsNull()
        {

            var config = new DeployHoursConfiguration
            {
                Rules = null
            };


            var errors = config.Validate();

            // Assert
            Assert.Single(errors);
            Assert.Equal("Rules is mandatory", errors[0]);
        }
        [Fact]
        public void Validate_ReturnsError_WhenRuleBusinessHoursIsNull()
        {

            var config = new DeployHoursConfiguration
            {
                Rules = new List<DeployHoursRule>
                {
                    new DeployHoursRule
                    {
                        Environment = "production",
                        DeploySlots = null
                    }
                }
            };


            var errors = config.Validate();

            // Assert
            Assert.Single(errors);
            Assert.Equal("DeployHours element is mandatory (environment: production)", errors[0]);
        }

        [Fact]
        public void Validate_ReturnsError_WhenRuleBusinessHoursIsMissingElements()
        {

            var config = new DeployHoursConfiguration
            {
                Rules = new List<DeployHoursRule>
                {
                    new DeployHoursRule
                    {
                        Environment = "production",
                        DeploySlots = new List<DeploySlotRange>
                        {
                            new DeploySlotRange { } // Missing Start and End
                        }
                    }
                }
            };


            var errors = config.Validate();

            // Assert
            Assert.Equal(2, errors.Count);
            Assert.Equal(new List<string> { "Start is required", "End is required" }, errors);
        }

        [Fact]
        public void Validate_ReturnsError_WhenRuleBusinessHoursStartIsGreaterEnd()
        {

            var config = new DeployHoursConfiguration
            {
                Rules = new List<DeployHoursRule>
                {
                    new DeployHoursRule
                    {
                        Environment = "production",
                        DeploySlots = new List<DeploySlotRange>
                        {
                            new DeploySlotRange
                            {
                                Start = new TimeOnly(13,0),
                                End = new TimeOnly(10,45)
                            }
                        }
                    }
                }
            };


            var errors = config.Validate();

            Assert.Single(errors);
            Assert.Equal(new List<string> { "End should be greater than Start" }, errors);
        }
    }
}
