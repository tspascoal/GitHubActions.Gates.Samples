using DeployHours.Gate.Models;
using DeployHours.Gate.Rules;
using GitHubActions.Gates.Framework.Exceptions;

namespace DeployHours.Gate.Tests
{
    public class DeployHoursRulesEvaluatorTests
    {
        [Fact]
        public void IsDeployHour_SingleRangeGlobalConfigTrue()
        {
            var deployHours = new DeployHoursConfiguration()
            {
                Lockout = false,
                Rules = new List<DeployHoursRule>
                {
                    new DeployHoursRule
                    {
                        Environment = String.Empty,
                        DeploySlots =  new List<DeploySlotRange>
                        {
                            new DeploySlotRange
                            {
                                Start = new TimeOnly(9, 0, 0),
                                End = new TimeOnly(17, 0, 0)
                            }
                        }
                    }
                }
            };

            var evaluator = new DeployHoursRulesEvaluator(deployHours);

            var isDeployHour = evaluator.IsDeployHour(new DateTime(2023, 1, 2, 10, 0, 0).ToUniversalTime(), "dummy");

            Assert.True(isDeployHour);
        }

        [Fact]
        public void IsDeployHour_SingleRangeEnvironmentRuleTrue()
        {
            var deployHours = new DeployHoursConfiguration()
            {
                Lockout = false,
                Rules = new List<DeployHoursRule>
                {
                    new DeployHoursRule
                    {
                        Environment = "production",
                        DeploySlots =  new List<DeploySlotRange>
                        {
                            new DeploySlotRange
                            {
                                Start = new TimeOnly(9, 0, 0),
                                End = new TimeOnly(17, 0, 0)
                            }
                        }
                    }
                }
            };

            var evaluator = new DeployHoursRulesEvaluator(deployHours);

            var isDeployHour = evaluator.IsDeployHour(new DateTime(2023, 1, 2, 10, 0, 0).ToUniversalTime(), "production");

            Assert.True(isDeployHour);
        }

        [Fact]
        public void IsDeployHour_MultipleRangeBothRangesTrue()
        {

            var deployHours = new DeployHoursConfiguration()
            {
                Lockout = false,
                Rules = new List<DeployHoursRule>
                {
                    new DeployHoursRule
                    {
                        Environment = String.Empty,
                        DeploySlots =  new List<DeploySlotRange>
                        {
                          new DeploySlotRange
                            {
                                Start = new TimeOnly(9, 0, 0),
                                End = new TimeOnly(12, 0, 0)
                            },
                            new DeploySlotRange
                            {
                                Start = new TimeOnly(13, 0, 0),
                                End = new TimeOnly(20, 0, 0)
                            }
                        }
                    }
                }
            };

            var evaluator = new DeployHoursRulesEvaluator(deployHours);

            var isDeployHour1 = evaluator.IsDeployHour(new DateTime(2023, 1, 2, 10, 0, 0), "dummy");
            var isDeployHour2 = evaluator.IsDeployHour(new DateTime(2023, 1, 2, 14, 0, 0), "dummy");

            Assert.True(isDeployHour1);
            Assert.True(isDeployHour2);
        }

        [Fact]
        public void IsDeployHour_SingleRangeBeforeFalse()
        {
            var deployHours = new DeployHoursConfiguration()
            {
                Lockout = false,
                Rules = new List<DeployHoursRule>
                {
                    new DeployHoursRule {
                    Environment = String.Empty,
                    DeploySlots =  new List<DeploySlotRange>
                        {
                            new DeploySlotRange
                            {
                                Start = new TimeOnly(9, 0, 0),
                                End = new TimeOnly(17, 0, 0)
                            }
                        }
                    }
                }
            };

            var evaluator = new DeployHoursRulesEvaluator(deployHours);

            var isDeployHour = evaluator.IsDeployHour(new DateTime(2023, 1, 2, 8, 0, 23), "dummy");

            Assert.False(isDeployHour);
        }

        [Fact]
        public void IsDeployHour_SingleRangeAfterFalse()
        {
            var deployHours = new DeployHoursConfiguration()
            {
                Lockout = false,
                Rules = new List<DeployHoursRule>
                {
                    new DeployHoursRule {
                        Environment = String.Empty,
                        DeploySlots =  new List<DeploySlotRange>
                        {
                                new DeploySlotRange
                                {
                                    Start = new TimeOnly(9, 0, 0),
                                    End = new TimeOnly(17, 0, 0)
                                }

                        }
                    }
                }
            };

            var evaluator = new DeployHoursRulesEvaluator(deployHours);

            var isBusinessHour = evaluator.IsDeployHour(new DateTime(2023, 1, 2, 18, 0, 23), "dummy");

            Assert.False(isBusinessHour);
        }

        [Fact]
        public void IsDeployHour_MultipleRangeBothRangesFalse()
        {
            var deployHours = new DeployHoursConfiguration()
            {
                Lockout = false,
                Rules = new List<DeployHoursRule>
                {
                    new DeployHoursRule {
                        Environment = String.Empty,
                        DeploySlots =  new List<DeploySlotRange>
                        {
                            new DeploySlotRange
                            {
                                Start = new TimeOnly(9, 0, 0),
                                End = new TimeOnly(12, 0, 0)
                            },                        new DeploySlotRange
                            {
                                Start = new TimeOnly(13, 0, 0),
                                End = new TimeOnly(20, 0, 0)
                            }
                        }
                    }
                }
            };

            var evaluator = new DeployHoursRulesEvaluator(deployHours);

            var isBusinessHour1 = evaluator.IsDeployHour(new DateTime(2023, 1, 2, 12, 34, 0), "dummy");
            var isBusinessHour2 = evaluator.IsDeployHour(new DateTime(2023, 1, 2, 21, 0, 0), "dummy");

            Assert.False(isBusinessHour1);
            Assert.False(isBusinessHour2);
        }

        [Fact]
        public void IsDeployHour_MultipleRangeUnorderedTrue()
        {
            var deployHours = new DeployHoursConfiguration()
            {
                Lockout = false,
                Rules = new List<DeployHoursRule>
                {
                    new DeployHoursRule {
                        Environment = String.Empty,
                        DeploySlots =  new List<DeploySlotRange>
                        {
                            new DeploySlotRange
                            {
                                Start = new TimeOnly(13, 0, 0),
                                End = new TimeOnly(20, 0, 0)
                            },
                            new DeploySlotRange
                            {
                                Start = new TimeOnly(9, 0, 0),
                                End = new TimeOnly(12, 0, 0)
                            }
                        }
                    }
                }
            };

            var evaluator = new DeployHoursRulesEvaluator(deployHours);

            var isDeployHour1 = evaluator.IsDeployHour(new DateTime(2023, 1, 2, 11, 35, 0), "dummy");
            var isDeployHour2 = evaluator.IsDeployHour(new DateTime(2023, 1, 2, 19, 59, 0), "dummy");

            Assert.True(isDeployHour1);
            Assert.True(isDeployHour2);
        }

        [Fact]
        public void IsDeployHour_WithNullStartSkipsRange()
        {
            var deployHours = new DeployHoursConfiguration()
            {
                Lockout = false,
                Rules = new List<DeployHoursRule>
                {
                    new DeployHoursRule
                    {
                        Environment = String.Empty,
                        DeploySlots = new List<DeploySlotRange>
                        {
                            new DeploySlotRange
                            {
                                Start = null,
                                End = new TimeOnly(12, 0, 0)
                            },
                            new DeploySlotRange
                            {
                                Start = new TimeOnly(13, 0, 0),
                                End = new TimeOnly(17, 0, 0)
                            }
                        }
                    }
                }
            };

            var evaluator = new DeployHoursRulesEvaluator(deployHours);

            var isDeployHour1 = evaluator.IsDeployHour(new DateTime(2023, 1, 2, 10, 0, 0), "dummy");
            var isDeployHour2 = evaluator.IsDeployHour(new DateTime(2023, 1, 2, 14, 0, 0), "dummy");

            Assert.False(isDeployHour1);
            Assert.True(isDeployHour2);
        }

        [Fact]
        public void IsDeployHour_NoEnvironment()
        {
            var deployHours = new DeployHoursConfiguration()
            {
                Lockout = false,
                Rules = new List<DeployHoursRule>
                {
                    new DeployHoursRule
                    {
                        Environment = "production",
                        DeploySlots =  new List<DeploySlotRange>
                        {
                            new DeploySlotRange
                            {
                                Start = new TimeOnly(9, 0, 0),
                                End = new TimeOnly(17, 0, 0)
                            }
                        }
                    }
                }
            };

            var evaluator = new DeployHoursRulesEvaluator(deployHours);

            var rejectException = Assert.Throws<RejectException>(() => evaluator.IsDeployHour(new DateTime(2023, 1, 2, 10, 0, 0).ToUniversalTime(), "dummy"));
            Assert.Equal("No rule found for dummy environment", rejectException.Message);
        }

        public class InLockout
        {
            [Fact]
            public void True()
            {
                var deployHours = new DeployHoursConfiguration()
                {
                    Lockout = true
                };

                var evaluator = new DeployHoursRulesEvaluator(deployHours);

                var lockout = evaluator.InLockout();

                Assert.True(lockout);
            }

            [Fact]
            public void False()
            {
                var deployHours = new DeployHoursConfiguration()
                {
                    Lockout = false
                };

                var evaluator = new DeployHoursRulesEvaluator(deployHours);

                var lockout = evaluator.InLockout();

                Assert.False(lockout);
            }
        }

        public class GetNextDeployHour
        {
            [Fact]
            public void ButAlreadyIsDeployHour()
            {
                var businessHours = new DeployHoursConfiguration()
                {
                    Lockout = false,
                    Rules = new List<DeployHoursRule>
                {
                    new DeployHoursRule {
                        Environment = String.Empty,
                        DeploySlots =  new List<DeploySlotRange>
                        {
                            new DeploySlotRange
                            {
                                Start = new TimeOnly(9, 0, 0),
                                End = new TimeOnly(12, 0, 0)
                            },
                            new DeploySlotRange
                            {
                                Start = new TimeOnly(13, 0, 0),
                                End = new TimeOnly(17, 0, 0)
                            }
                        }
                    }
                }
                };

                var evaluator = new DeployHoursRulesEvaluator(businessHours);

                // Value between business hours in this day
                var nextBusinessHour = evaluator.GetNextDeployHour(new DateTime(2023, 1, 2, 9, 01, 0), "dummy");

                Assert.Equal(new DateTime(2023, 1, 2, 9, 1, 0), nextBusinessHour);
            }

            [Fact]
            public void SameDaySecondRange()
            {
                var businessHours = new DeployHoursConfiguration()
                {
                    Lockout = false,
                    Rules = new List<DeployHoursRule>
                {
                    new DeployHoursRule {
                        Environment = String.Empty,
                        DeploySlots =  new List<DeploySlotRange>
                        {
                            new DeploySlotRange
                            {
                                Start = new TimeOnly(9, 0, 0),
                                End = new TimeOnly(12, 0, 0)
                            },
                            new DeploySlotRange
                            {
                                Start = new TimeOnly(13, 0, 0),
                                End = new TimeOnly(17, 0, 0)
                            }
                        }
                    }
                }
                };

                var evaluator = new DeployHoursRulesEvaluator(businessHours);

                // Value between business hours in this day
                var nextBusinessHour = evaluator.GetNextDeployHour(new DateTime(2023, 1, 2, 12, 37, 0), "dummy");

                Assert.Equal(new DateTime(2023, 1, 2, 13, 0, 0), nextBusinessHour);
            }

            [Fact]
            public void SameDayLastRange()
            {
                var businessHours = new DeployHoursConfiguration()
                {
                    Lockout = false,
                    Rules = new List<DeployHoursRule>
                {
                    new DeployHoursRule {
                        Environment = String.Empty,
                        DeploySlots =  new List<DeploySlotRange>
                        {
                            new DeploySlotRange
                            {
                                Start = new TimeOnly(9, 0, 0),
                                End = new TimeOnly(12, 0, 0)
                            },
                            new DeploySlotRange
                            {
                                Start = new TimeOnly(13, 0, 0),
                                End = new TimeOnly(14, 30, 0)
                            },
                            new DeploySlotRange
                            {
                                Start = new TimeOnly(15, 30, 0),
                                End = new TimeOnly(22, 15, 0)
                            }
                        }
                    }
                }
                };

                var evaluator = new DeployHoursRulesEvaluator(businessHours);

                // Value between business hours in this day
                var nextBusinessHour = evaluator.GetNextDeployHour(new DateTime(2023, 1, 2, 14, 55, 0), "dummy");

                Assert.Equal(new DateTime(2023, 1, 2, 15, 30, 0), nextBusinessHour);
            }

            [Fact]
            public void NextBusinessDay()
            {
                var businessHours = new DeployHoursConfiguration()
                {
                    DeployDays = new DayOfWeek[] { DayOfWeek.Monday },
                    Lockout = false,
                    Rules = new List<DeployHoursRule>
                {
                    new DeployHoursRule {
                        Environment = String.Empty,
                        DeploySlots =  new List<DeploySlotRange>
                        {

                            new DeploySlotRange
                            {
                                Start = new TimeOnly(9, 0, 0),
                                End = new TimeOnly(12, 0, 0)
                            },
                            new DeploySlotRange
                            {
                                Start = new TimeOnly(13, 0, 0),
                                End = new TimeOnly(17, 0, 0)
                            }
                        }
                    }
                }
                };

                var evaluator = new DeployHoursRulesEvaluator(businessHours);

                // Value between business hours in this day
                var nextBusinessHour = evaluator.GetNextDeployHour(new DateTime(2023, 1, 2, 17, 12, 0), "dummy");

                Assert.Equal(new DateTime(2023, 1, 9, 9, 0, 0), nextBusinessHour);
            }

            [Fact]
            public void ThrowsExceptionWhenNoValidStartTime()
            {
                var businessHours = new DeployHoursConfiguration()
                {
                    Lockout = false,
                    Rules = new List<DeployHoursRule>
                    {
                        new DeployHoursRule
                        {
                            Environment = "production",
                            DeploySlots = new List<DeploySlotRange>
                            {
                                new DeploySlotRange
                                {
                                    Start = null,
                                    End = new TimeOnly(12, 0, 0)
                                },
                                new DeploySlotRange
                                {
                                    Start = null,
                                    End = new TimeOnly(17, 0, 0)
                                }
                            }
                        }
                    }
                };

                var evaluator = new DeployHoursRulesEvaluator(businessHours);

                var rejectException = Assert.Throws<RejectException>(() => 
                    evaluator.GetNextDeployHour(new DateTime(2023, 1, 2, 10, 0, 0), "production"));
                
                Assert.Equal("No deploy slot with a valid start time found for production environment", rejectException.Message);
            }

        }
        public class IsDeployDay
        {
            [Fact]
            public void True()
            {
                var businessHours = new DeployHoursConfiguration()
                {
                    DeployDays = new DayOfWeek[]
                    {
                    DayOfWeek.Monday,
                    DayOfWeek.Tuesday,
                    DayOfWeek.Wednesday,
                    DayOfWeek.Thursday
                    }
                };

                var evaluator = new DeployHoursRulesEvaluator(businessHours);

                var isWorkingDay = evaluator.IsDeployDay(new DateTime(2023, 1, 2, 10, 0, 0));

                Assert.True(isWorkingDay);
            }

            [Fact]
            public void WithDefaultsTrue()
            {
                var businessHours = new DeployHoursConfiguration() { };

                var evaluator = new DeployHoursRulesEvaluator(businessHours);

                var isWorkingDay = evaluator.IsDeployDay(new DateTime(2023, 1, 2, 10, 0, 0));

                Assert.True(isWorkingDay);
            }
            [Fact]
            public void False()
            {
                var businessHours = new DeployHoursConfiguration()
                {
                    DeployDays = new DayOfWeek[]
                    {
                    DayOfWeek.Monday,
                    DayOfWeek.Tuesday,
                    DayOfWeek.Wednesday,
                    DayOfWeek.Thursday
                    }
                };

                var evaluator = new DeployHoursRulesEvaluator(businessHours);

                var isWorkingDay = evaluator.IsDeployDay(new DateTime(2023, 1, 1, 10, 0, 0));

                Assert.False(isWorkingDay);
            }
        }
    }
}