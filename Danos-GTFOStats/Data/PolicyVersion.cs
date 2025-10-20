using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTFOStats.Data
{


    // Default Policy version in-line with the privacy policy at release of the mod to ensure users are aware of the policy version even if the site is down and the mod can't fetch the latest version.
    public class PolicyVersion
    {
        public string Version { get; set; } = "1.0.1";

        public DateTime LastUpdated { get; set; } = new DateTime(2025, 1, 16);
        public string DescriptionOfChanges { get; set; } = "Newly drafted policy to clear some things up.";

        // Calculate how many days ago the policy was updated
        public string LastUpdatedDaysAgo => $"{(DateTime.UtcNow - LastUpdated).Days} day(s) ago";
    }
}
