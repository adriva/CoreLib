using System.Collections.Generic;

namespace Adriva.AppInsights.Serialization.Contracts
{
    public partial class AvailabilityData
        : Domain
    {
        public int ver { get; set; }

        public string id { get; set; }

        public string name { get; set; }

        public string duration { get; set; }

        public bool success { get; set; }

        public string runLocation { get; set; }

        public string message { get; set; }

        public Dictionary<string, string> properties { get; set; }

        public Dictionary<string, double> measurements { get; set; }

        public AvailabilityData()
            : this("AI.AvailabilityData", "AvailabilityData")
        { }

        protected AvailabilityData(string fullName, string name)
        {
            ver = 2;
            id = "";
            this.name = "";
            duration = "";
            runLocation = "";
            message = "";
            properties = new Dictionary<string, string>();
            measurements = new Dictionary<string, double>();
        }
    }
}