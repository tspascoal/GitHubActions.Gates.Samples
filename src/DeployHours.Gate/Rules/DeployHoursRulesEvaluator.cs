using DeployHours.Gate.Models;
using GitHubActions.Gates.Framework.Exceptions;
using System;
using System.Linq;

namespace DeployHours.Gate.Rules
{
    public class DeployHoursRulesEvaluator
    {
        private readonly DeployHoursConfiguration _deployHoursConfig;
        internal DeployHoursRulesEvaluator() { }

        public DeployHoursRulesEvaluator(DeployHoursConfiguration config)
        {
            _deployHoursConfig = config;
        }

        public virtual bool InLockout()
        {
            return _deployHoursConfig.Lockout;
        }

        /// <summary>
        ///  Given the current time checks if it's within the list of deploy hour ranges
        /// </summary>
        /// <param name="currentTime"></param>
        /// <returns></returns>
        public virtual bool IsDeployHour(DateTime currentTime, string Environment)
        {
            if (!IsDeployDay(currentTime))
            {
                return false;
            }

            var rule = GetRuleOrThrow(Environment);

            // Get ordered list of ranges (in case user has not ordered them
            var sortedRanges = rule.DeploySlots.OrderBy(r => r.Start.Value.ToTimeSpan()).ToList();

            // Check if current time is within any of the ranges
            foreach (var range in sortedRanges)
            {
                if (currentTime.TimeOfDay >= range.Start.Value.ToTimeSpan() && currentTime.TimeOfDay <= range.End.Value.ToTimeSpan())
                {
                    return true;
                }
            }
            return false;
        }

        public virtual DateTime GetNextDeployHour(DateTime currentTime, string Environment)
        {
            var rule = GetRuleOrThrow(Environment);
            var nextTime = currentTime.AddDays(0);

            // Get first range of the day
            var firstHourRange = rule.DeploySlots.OrderBy(r => r.Start.Value.ToTimeSpan()).First();

            //TODO: optimize so we are not constantly getting data for environments
            while (!IsDeployHour(nextTime, Environment))
            {
                // If it's not a working day, move to the next day
                // We could be clever and look at working days, but this is fast enough
                if (!IsDeployDay(nextTime))
                {
                    nextTime = new DateTime(nextTime.Year, nextTime.Month, nextTime.Day, firstHourRange.Start.Value.Hour, firstHourRange.Start.Value.Minute, firstHourRange.Start.Value.Second, DateTimeKind.Utc).AddDays(1);
                    continue;
                }

                // If it's a working day, move to the next deploy hour range
                var nextRange = rule.DeploySlots.FirstOrDefault(r => r.Start.Value.ToTimeSpan() > nextTime.TimeOfDay);
                if (nextRange != null)
                {
                    nextTime = new DateTime(nextTime.Year, nextTime.Month, nextTime.Day, nextRange.Start.Value.Hour, nextRange.Start.Value.Minute, nextRange.Start.Value.Second, DateTimeKind.Utc);
                }
                else
                {
                    // If there is no next range, move to the next day
                    nextTime = new DateTime(nextTime.Year, nextTime.Month, nextTime.Day, firstHourRange.Start.Value.Hour, firstHourRange.Start.Value.Minute, firstHourRange.Start.Value.Second, DateTimeKind.Utc).AddDays(1);
                }
            }

            return nextTime;
        }

        private DeployHoursRule GetRuleOrThrow(string Environment)
        {
            return _deployHoursConfig.GetRule(Environment) ?? throw new RejectException($"No rule found for {Environment ?? "Any Environment"} environment");
        }

        internal bool IsDeployDay(DateTime current)
        {
            return _deployHoursConfig.DeployDays.Contains(current.DayOfWeek);
        }
    }
}
