using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTFOStats.Data
{
    public class DanosChallengeResponse
    {
        public List<DanosChallengeData> DailyChallenges { get; set; } = new List<DanosChallengeData>();
        public List<DanosChallengeData> WeeklyChallenges { get; set; } = new List<DanosChallengeData>();
        public int CurrentXp { get; set; } // User's current XP
        public DateTime FirstMatchDate { get; set; } // Date of first recorded match
    }
}
