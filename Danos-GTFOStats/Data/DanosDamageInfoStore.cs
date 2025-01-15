using Agents;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTFOStats.Data
{
    public class DanosDamageInfoStore
    {
        // Dictionary to store the last damage info for each player
        public static Dictionary<long, DamageRecord> _lastDamageByPlayer = new();

        





        // Record structure to store damage details
        public class DamageRecord
        {
            public float Damage { get; set; }

            //ignore for serialization
            [System.Text.Json.Serialization.JsonIgnore]
            public PlayerAgent DamagedAgent { get; set; }
            [System.Text.Json.Serialization.JsonIgnore]

            public Agent SourceAgent { get; set; }
            public string SourceType { get; set; } // E.g., "Enemy", "Environment", etc.
            public System.DateTime Timestamp { get; set; } // To track when the damage occurred
        }

        // Method to update damage info for a player
        public static void UpdateDamage(PlayerAgent damagedAgent, float damage, Agent sourceAgent, string sourceType)
        {
            if (damagedAgent == null || damagedAgent.m_replicator == null)
                return;

            var steamID = (long)damagedAgent.m_replicator.OwningPlayer.Lookup;
            if (!_lastDamageByPlayer.TryGetValue(steamID, out var record))
            {
                record = new DamageRecord();
                _lastDamageByPlayer[steamID] = record;
            }

            record.Damage = damage;
            record.DamagedAgent = damagedAgent;
            record.SourceAgent = sourceAgent;
            record.SourceType = sourceType;
            record.Timestamp = System.DateTime.Now;
        }

        // Method to retrieve the last damage info for a player
        public static DamageRecord? GetLastDamage(long damagedAgent)
        {




            return _lastDamageByPlayer.TryGetValue(damagedAgent, out var record) ? record : null;
        }

        //reset all damage info
        public static void Reset()
        {
            _lastDamageByPlayer.Clear();
        }
    }
}
