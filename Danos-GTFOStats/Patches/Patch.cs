using CellMenu;
using GameEvent;
using HarmonyLib;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Diagnostics;
using System.Text.Json;
using System.Collections;
using GTFOStats.Data;
using BepInEx.Unity.IL2CPP.Utils;
using System.Net.Http.Headers;
using System.Net;
using static UnityEngine.Rendering.PostProcessing.BloomRenderer;
using SNetwork;
using GameData;
using Il2CppSystem.Data;
using Dissonance;
using Il2CppInterop.Runtime;
using System.Reflection;
namespace GTFOStats.Patches
{
    [HarmonyPatch]
    public static class GameStatePatch
    {
        private static Coroutine positionCollectorCoroutine;

        [HarmonyPatch(typeof(GameEventManager), nameof(GameEventManager.PostEvent),
        new[] {
            typeof(eGameEvent),
            typeof(PlayerAgent),
            typeof(float),
            typeof(string),
            typeof(Il2CppSystem.Collections.Generic.Dictionary<string, string>)
        })]
        [HarmonyPrefix]
        public static void Patch_PostEvent_Overload2(
            eGameEvent e,
            PlayerAgent player,
            float floatVal,
            string stringVal,
            Il2CppSystem.Collections.Generic.Dictionary<string, string> customAnalyticsPayload)
        {
            string eventName = e.ToString();

            if (eventName == "gs_StopElevatorRide")
            {
                Console.WriteLine("Elevator ride ended");
                ResetRunDownData();
                InitializeRunDownData();
                StartPositionCollector();

            }

            else if (eventName == "gs_ExpeditionFail" || eventName == "gs_ExpeditionSuccess")
            {
                Console.WriteLine("Expedition ended");
                ExportRunDownData(eventName);
                StopPositionCollector();

            }

            else if(eventName == "gs_ExpeditionAbort")
            {
                Console.WriteLine("Expedition Aborted");
                ExportRunDownData(eventName);
                StopPositionCollector();
            }


            if(DanosStaticStore.currentRunDownDataStore == null)
            {
                return;
            }


            else if (eventName == "player_downed")
            {
                DanosStaticStore.currentRunDownDataStore.AddPlayerDownData((long)player.Owner.Lookup, player);
            }
            else if (eventName == "player_reload")
            {
                DanosStaticStore.currentRunDownDataStore.AddPlayerReloadData((long)player.Owner.Lookup);
            }
            else if (eventName == "player_apply_medikit")
            {
                DanosStaticStore.currentRunDownDataStore.AddPlayerHealthPackUsed((long)player.Owner.Lookup);
            }
            else if (eventName == "player_apply_ammokit")
            {
                DanosStaticStore.currentRunDownDataStore.AddPlayerAmmoPackUsed((long)player.Owner.Lookup);
            }
            else if (eventName == "player_apply_disinfection")
            {
                DanosStaticStore.currentRunDownDataStore.AddPlayerDisinfectionUsed((long)player.Owner.Lookup);
            }
            else if (eventName == "player_pickup_artifact")
            {
                DanosStaticStore.currentRunDownDataStore.AddPlayerArtifactPickup((long)player.Owner.Lookup);
            }
            else if (eventName == "player_place_tripmine")
            {
                DanosStaticStore.currentRunDownDataStore.AddPlayerTripMinePlaced((long)player.Owner.Lookup);
            }
            else if (eventName == "player_pickup_keycard")
            {
                DanosStaticStore.currentRunDownDataStore.AddPlayerKeyCardPickup((long)player.Owner.Lookup);
            }
            else if (eventName == "player_hacking_success")
            {
                DanosStaticStore.currentRunDownDataStore.AddPlayerHackingSuccess((long)player.Owner.Lookup);
            }



            //Globals (No player data  so apply them to the whole summary rather than player specific)
            else if (eventName == "bioscan_start")
            {
                DanosStaticStore.currentRunDownDataStore.IncrementBioscanStart();
            }
            else if (eventName == "scout_enemy_dead")
            {
                DanosStaticStore.currentRunDownDataStore.IncrementScoutEnemiesDead();
            }
            else if (eventName == "enemy_dead")
            {
                DanosStaticStore.currentRunDownDataStore.IncrementEnemiesDead();
            }
            else if (eventName == "enemy_wave_spawned")
            {
                DanosStaticStore.currentRunDownDataStore.IncrementEnemyWavesSpawned();
            }
            else if (eventName == "scout_enemy_found_player")
            {
                DanosStaticStore.currentRunDownDataStore.IncrementScoutEnemiesFoundPlayer();
            }
            else if (eventName == "hibernating_enemy_dead")
            {
                DanosStaticStore.currentRunDownDataStore.IncrementHibernatingEnemiesDead();
            }
            else if (eventName == "hibernating_enemy_wakeup")
            {
                DanosStaticStore.currentRunDownDataStore.IncrementHibernatingEnemiesWokeUp();
            }
            else if (eventName == "enemy_dead_from_melee")
            {
                DanosStaticStore.currentRunDownDataStore.IncrementEnemiesDeadFromMelee();
            }
            else if (eventName == "enemy_scout_dead_from_melee")
            {
                DanosStaticStore.currentRunDownDataStore.IncrementScoutEnemiesDeadFromMelee();
            }






        }

        private static void StartPositionCollector()
        {
            if (positionCollectorCoroutine == null)
            {
                positionCollectorCoroutine = PlayerManager.GetLocalPlayerAgent()?.StartCoroutine(CollectPositionsCoroutine());
                if (positionCollectorCoroutine == null)
                {
                    Console.WriteLine("Failed to start position collector coroutine.");
                }
            }
        }

        private static void CollectPlayerPositions()
        {
            try
            {
                var allPlayers = PlayerManager.PlayerAgentsInLevel;
                var roundStartTimestamp = DanosStaticStore.currentRunDownDataStore.st;
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                var timesinceRoundStart = timestamp - roundStartTimestamp;

                foreach (PlayerAgent playerAgent in allPlayers)
                {
                    if (playerAgent == null || playerAgent.m_replicator == null)
                        continue;

                    var position = playerAgent.transform.position;
                    var steamID = playerAgent.m_replicator.OwningPlayer.Lookup;


                    var positionalData = new DanosPositionalDataTransfer
                    {
                        x = position.x,
                        z = position.z,
                        Timestamp = timesinceRoundStart,
                        sid = (long)steamID,
                        Name = playerAgent.PlayerName
                    };

                    DanosStaticStore.currentRunDownDataStore.AddPositionalData(positionalData);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error collecting player positions: {ex.Message}");
            }
        }
        private static IEnumerator CollectPositionsCoroutine()
        {
            while (true)
            {
                CollectPlayerPositions();
                yield return new WaitForSeconds(3f); // Collect positions every 10 seconds
            }
        }
        private static void StopPositionCollector()
        {
            if (positionCollectorCoroutine != null)
            {
                PlayerManager.GetLocalPlayerAgent()?.StopCoroutine(positionCollectorCoroutine);
                positionCollectorCoroutine = null;
            }
        }

        private static void ExportRunDownData(string reason)
        {
            try
            {
                if (DanosStaticStore.currentRunDownDataStore == null)
                {
                    Debug.LogError("No rundown data store to export.");
                    return;
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new OneDecimalJsonConverter() }
                };

                DanosStaticStore.currentRunDownDataStore.et = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                DanosStaticStore.currentRunDownDataStore.rer = reason;
                DanosStaticStore.currentRunDownDataStore.wasHost = SNet.IsMaster;


                //Check that all players are in summary data, if not add them.
                var allPlayers = PlayerManager.PlayerAgentsInLevel;
                foreach (PlayerAgent playerAgent in allPlayers)
                {
                    if (playerAgent == null || playerAgent.m_replicator == null)
                        continue;
                    DanosStaticStore.currentRunDownDataStore.AddPlayerToSummary((long)playerAgent.m_replicator.OwningPlayer.Lookup, playerAgent);
                }



                // Serialize your main JSON
                string json = JsonSerializer.Serialize(DanosStaticStore.currentRunDownDataStore, options);

                // Collect additional JSON contributions
                var additionalData = new List<string>();
                foreach (var contributor in DanosStaticStore.JsonContributors)
                {
                    try
                    {
                        string additionalJson = contributor.Invoke();
                        if (!string.IsNullOrEmpty(additionalJson))
                        {
                            additionalData.Add(additionalJson);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error invoking JSON contributor: {ex.Message}");
                    }
                }

                // Merge additional JSON chunks into the main JSON object
                var mainData = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
                foreach (var additionalJson in additionalData)
                {
                    var additionalDict = JsonSerializer.Deserialize<Dictionary<string, object>>(additionalJson, options);
                    if (additionalDict != null)
                    {
                        foreach (var kvp in additionalDict)
                        {
                            mainData[kvp.Key] = kvp.Value; // Overwrite or add new keys
                        }
                    }
                }

                // Serialize the merged data back to JSON
                string mergedJson = JsonSerializer.Serialize(mainData, options);

                // Write to file (optional)
                string filePath = Path.Combine(Application.persistentDataPath, "RunDownData.json");
                File.WriteAllText(filePath, mergedJson);

                Console.WriteLine($"Rundown data exported to {filePath}");

                // Post the merged JSON
                PostDataToAPI(mergedJson).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting rundown data: {ex.Message}");
            }
        }


        private static async Task PostDataToAPI(string json)
        {
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;

                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://gtfoapi.splitstats.io/api/rundown/upload", content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Data posted to API successfully.");
                }
                else
                {
                    Console.WriteLine($"Error posting data to API: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error posting data to API: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }

        }



        private static void InitializeRunDownData()
        {
            try
            {
                // Initialize default values for the rundown data store
                DanosRunDownDataStore currentRunDownDataStore = new DanosRunDownDataStore
                {
                    st = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), // Current timestamp
                    msid = GetLocalPlayerSteamID(), // Steam ID
                    mv = DanosStaticStore.ModVersion // Mod version
                };

                // Assign simple values first
                DanosStaticStore.currentRunDownDataStore = currentRunDownDataStore;

                string expeditionTier = string.Empty;
                string expeditionIndex = string.Empty;
                string sessionid = string.Empty;

                // Fetch active expedition data
                try
                {
                    pActiveExpedition expdata = RundownManager.GetActiveExpeditionData();
                    if (expdata == null)
                    {
                        throw new Exception("Active expedition data is null");
                    }

                    expeditionTier = expdata.tier.ToString();
                    expeditionIndex = expdata.expeditionIndex.ToString();
                    sessionid = expdata.sessionGUID.m_data.ToString();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error fetching expedition data: {ex.Message}");
                }

                // Initialize extra rundown data
                DanosExtraRundownData extraData = new DanosExtraRundownData();
                string rundownString = string.Empty;

                try
                {
                    string storyTitle = string.Empty;
                    string externalTitle = string.Empty;
                    string surfaceDescription = string.Empty;

                    if (RundownManager.TryGetIdFromLocalRundownKey(RundownManager.ActiveRundownKey, out uint rundownID))
                    {
                        RundownDataBlock dataBlock = RundownDataBlock.GetBlock(rundownID);
                        if (dataBlock?.StorytellingData != null)
                        {
                            storyTitle = dataBlock.StorytellingData.Title;
                            surfaceDescription = dataBlock.StorytellingData.SurfaceDescription;
                            externalTitle = dataBlock.StorytellingData.ExternalExpTitle;
                        }
                    }

                    ExpeditionInTierData expeditionData = RundownManager.ActiveExpedition;
                    DescriptiveData nameData = expeditionData.Descriptive;

                    string prefix = nameData.Prefix + (!nameData.SkipExpNumberInName ? (expeditionIndex + 1).ToString() : "");
                    string expeditionName = nameData.PublicName + (nameData.IsExtraExpedition ? "://EXT" : "");

                    extraData = new DanosExtraRundownData
                    {
                        rundownKey = RundownManager.ActiveRundownKey,
                        rundownName = expeditionName,
                        otherTitle = externalTitle,
                        rundownprefix = prefix,
                        storyDescription = surfaceDescription,
                        storyTitle = storyTitle,
                        devInfo = nameData.DevInfo,
                        matchmakingtier = nameData.CustomMatchmakingTier.ToString(),
                        desc2 = nameData.ExpeditionDescription,
                        depth = nameData.ExpeditionDepth.ToString()
                    };

                    rundownString = prefix;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error initializing extra rundown data: {ex.Message}");
                }

                // Update the rundown data store with additional data
                currentRunDownDataStore.rid = rundownString;
                currentRunDownDataStore.en = expeditionTier;
                currentRunDownDataStore.ei = expeditionIndex;
                currentRunDownDataStore.rsg = sessionid;
                currentRunDownDataStore.extraData = extraData;

                // Store the updated data
                DanosStaticStore.currentRunDownDataStore = currentRunDownDataStore;

            }
            catch (Exception ex)
            {
                Debug.LogError($"Error initializing DanosRunDownDataStore: {ex.Message}");
            }
        }

        

        private static void LogEventData(string eventName, PlayerAgent player, string customString, float floatVal = 0f, Il2CppSystem.Collections.Generic.Dictionary<string, string> customAnalyticsPayload = null)
        {
            try
            {
                //transfer from ill2cpp to .net dictionary
                if (customAnalyticsPayload == null) {
                    customAnalyticsPayload = new Il2CppSystem.Collections.Generic.Dictionary<string, string>();
                }
                var customAnalyticsPayloadNet = new Dictionary<string, string>();

                foreach (var item in customAnalyticsPayload)
                {
                    if (item.Key == null || item.Value == null)
                        continue;

                    if (customAnalyticsPayloadNet.ContainsKey(item.Key))
                        continue;

                    customAnalyticsPayloadNet[item.Key] = item.Value;
                }



                var eventObj = new DanosGameEvent
                {
                    eventName = eventName,
                    playerInfo = player?.PlayerName ?? "No Player",
                    customString = customString ?? "No String",

                };

                var json = JsonSerializer.Serialize(eventObj);
                Console.WriteLine($"Event Data: {json}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error logging event data: {ex.Message}");
            }
        }

        public static long GetLocalPlayerSteamID()
        {
            try
            {
                var localPlayer = PlayerManager.GetLocalPlayerAgent();
                if(localPlayer == null)
                {
                    return -1;
                }

                if(localPlayer.m_replicator == null)
                {
                    return -2;
                }

                if (localPlayer.m_replicator.OwningPlayer == null)
                {
                    return -3;
                }

                


                return (long)(localPlayer?.m_replicator?.OwningPlayer.Lookup ?? 0);
            }
            catch
            {
                return -4;
            }
        }

        public static void ResetRunDownData()
        {
            DanosStaticStore.currentRunDownDataStore = new DanosRunDownDataStore();
        }
    }


    


    //create class to send data to webhook
    public class DanosGameEvent
    {
        public string eventName { get; set; }
        public string playerInfo { get; set; }
        public string customString { get; set; }

    }
}
