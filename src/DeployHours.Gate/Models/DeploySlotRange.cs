using System;
using System.Collections.Generic;

namespace DeployHours.Gate.Models
{
    public class DeploySlotRange
    {
        public TimeOnly? Start;
        public TimeOnly? End;

        public List<string> Validate()
        {
            var validationErrors = new List<string>();

            if(Start == null)
            {
                validationErrors.Add("Start is required");
            }
            if (End == null)
            {
                validationErrors.Add("End is required");
            }
            if(Start != null && End != null && Start > End) {
                validationErrors.Add("End should be greater than Start");
            }

            return validationErrors;
        }
    }
}