using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTFOStats.Data
{
    public static class DanosAnonymization
{
        public static ulong MinSteamId64 = 0x0110000100000000;
        public static ulong MaxSteamId64 = 0x01100001FFFFFFFF;
        public static bool ValidateSteamId64(ulong steamId)
        {
            return steamId >= MinSteamId64 && steamId <= MaxSteamId64 && steamId.ToString().Length == 17;
        }
        public static DanosRunDownDataStore AnonymizeRundownStore(DanosRunDownDataStore store)
        {
            var anonymizedStore = new DanosRunDownDataStore
            {
                wasHost = store.wasHost,
                mv = store.mv,
                rer = store.rer,
                st = store.st,
                et = store.et,
                rid = store.rid,
                en = store.en,
                ei = store.ei,
                rsg = store.rsg,
                msid = store.msid,
                xp = store.xp,
                bioscanStarts = store.bioscanStarts,
                scoutEnemiesDead = store.scoutEnemiesDead,
                enemiesDead = store.enemiesDead,
                enemyWavesSpawned = store.enemyWavesSpawned,
                scoutEnemiesFoundPlayer = store.scoutEnemiesFoundPlayer,
                hibernatingEnemiesDead = store.hibernatingEnemiesDead,
                hibernatingEnemiesWokeUp = store.hibernatingEnemiesWokeUp,
                enemiesDeadFromMelee = store.enemiesDeadFromMelee,
                scoutEnemiesDeadFromMelee = store.scoutEnemiesDeadFromMelee,
                edc = new Dictionary<string, int>(store.edc), // Copy enemy death counts
                extraData = store.extraData // Assume this is shallow-copied safely
            };

            var steamIdMapping = new Dictionary<long, long>();
            long nextSteamIndex = 10; // Start indexing Steam IDs from 10 or higher to avoid clashing with bot IDs (1-9)

            // Helper function to anonymize a Steam ID
            long GetAnonymizedId(long steamId)
            {
                if (steamId == store.msid || !ValidateSteamId64((ulong)steamId))
                {
                    return steamId; // Don't anonymize own Steam ID or invalid IDs (bots)
                }

                if (!steamIdMapping.ContainsKey(steamId))
                {
                    steamIdMapping[steamId] = nextSteamIndex++;
                }

                return steamIdMapping[steamId];
            }

            // Sort IDs into bots and Steam IDs
            var botIds = store.pl.Keys.Where(id => !ValidateSteamId64((ulong)id)).OrderBy(id => id).ToList();
            var steamIds = store.pl.Keys.Where(id => ValidateSteamId64((ulong)id)).OrderBy(id => id).ToList();

            // Add bot IDs first
            foreach (var botId in botIds)
            {
                anonymizedStore.pl[botId] = store.pl[botId];
                if (store.pd.ContainsKey(botId)) anonymizedStore.pd[botId] = store.pd[botId];
                if (store.sd.ContainsKey(botId)) anonymizedStore.sd[botId] = store.sd[botId];
                if (store.ds.ContainsKey(botId)) anonymizedStore.ds[botId] = store.ds[botId];
            }

            // Add anonymized Steam IDs
            foreach (var steamId in steamIds)
            {
                long anonymizedId = GetAnonymizedId(steamId);
                anonymizedStore.pl[anonymizedId] = store.pl[steamId];

                //Change their pl.value to "Anon X"
                anonymizedStore.pl[anonymizedId] = "Anon " + anonymizedId;



                if (store.pd.ContainsKey(steamId)) anonymizedStore.pd[anonymizedId] = store.pd[steamId];
                if (store.sd.ContainsKey(steamId)) anonymizedStore.sd[anonymizedId] = store.sd[steamId];
                if (store.ds.ContainsKey(steamId)) anonymizedStore.ds[anonymizedId] = store.ds[steamId];
            }

            return anonymizedStore;
        }

    }
}
