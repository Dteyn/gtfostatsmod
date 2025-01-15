using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTFOStats.Data
{
    public class DanosChallengeData
    {
        public string Title { get; set; }
        public int Progress { get; set; }
        public int Goal { get; set; }
        public int XpReward { get; set; }
        public TimeSpan TimeRemaining { get; set; }
    }
}
