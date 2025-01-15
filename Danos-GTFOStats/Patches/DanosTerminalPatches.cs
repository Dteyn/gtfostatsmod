using GTFOStats.Data;
using HarmonyLib;
using LevelGeneration;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GTFOStats.Patches
{
    [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), "ReceiveCommand")]
    class DanosTerminalCommandPatch
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private static readonly string ApiEndpoint = "https://localhost:7007/api/rundown/challenges"; 

        static void Postfix(LG_ComputerTerminalCommandInterpreter __instance, TERM_Command cmd, string inputLine, string param1, string param2)
        {
            if (inputLine.Equals("challenges", StringComparison.OrdinalIgnoreCase))
            {
                __instance.ClearOutputQueueAndScreenBuffer();
                __instance.AddOutput(TerminalLineType.SpinningWaitNoDone, "<color=yellow>Loading Challenge Data...</color>", 1.5f);

                DisplayBootupSequence(__instance);

                string steamId = GetSteamIdForPlayer();

                // Handle the case where SteamID is null
                if (string.IsNullOrEmpty(steamId))
                {
                    __instance.AddOutput(TerminalLineType.Fail, "<color=red>Unable to retrieve SteamID. Challenge data cannot be loaded.</color>", 0.5f);
                    return;
                }
                __instance.AddOutput(TerminalLineType.Fail, "<color=red>No challenge data available for today.</color>", 0.5f);

                //var challengeData = FetchChallengesAsync(steamId).Result;

                //if (challengeData == null || (challengeData.DailyChallenges.Count == 0 && challengeData.WeeklyChallenges.Count == 0))
                //{
                //    __instance.AddOutput(TerminalLineType.Fail, "<color=red>No challenge data available for today.</color>", 0.5f);
                //    return;
                //}

                //DisplayUserInfo(__instance, challengeData);
                //DisplayChallenges(__instance, challengeData);
            }
        }
        private static void DisplayBootupSequence(LG_ComputerTerminalCommandInterpreter interpreter)
        {
            interpreter.ClearOutputQueueAndScreenBuffer();

            // ASCII Art for GTFOStats
            interpreter.AddOutput(TerminalLineType.Normal, @"   _____ _______ ______ ____   _____ _        _       ", 0.3f);
            interpreter.AddOutput(TerminalLineType.Normal, @"  / ____|__   __|  ____/ __ \ / ____| |      | |      ", 0.3f);
            interpreter.AddOutput(TerminalLineType.Normal, @" | |  __   | |  | |__ | |  | | (___ | |_ __ _| |_ ___ ", 0.3f);
            interpreter.AddOutput(TerminalLineType.Normal, @" | | |_ |  | |  |  __|| |  | |\___ \| __/ _` | __/ __|", 0.3f);
            interpreter.AddOutput(TerminalLineType.Normal, @" | |__| |  | |  | |   | |__| |____) | || (_| | |_\__ \", 0.3f);
            interpreter.AddOutput(TerminalLineType.Normal, @"  \_____|  |_|  |_|    \____/|_____/ \__\__,_|\__|___/", 0.3f);
            interpreter.AddOutput(TerminalLineType.Normal, @"                                                     ", 0.3f);
            interpreter.AddOutput(TerminalLineType.Normal, @"                                                     ", 0.3f);

            // Immersive Boot-Up Messages
            interpreter.AddOutput(TerminalLineType.Normal, "<color=green>Initializing GTFOStats Subsystem...</color>", 0.5f);
            interpreter.AddOutput(TerminalLineType.Normal, "Connecting to SplitStats Database...", 0.5f);
            interpreter.AddOutput(TerminalLineType.Normal, "Authenticating with SplitStats Servers...", 0.5f);
            interpreter.AddOutput(TerminalLineType.Normal, "System Diagnostics: <color=green>OK</color>", 0.3f);
            interpreter.AddOutput(TerminalLineType.Normal, "Telemetry Systems: <color=yellow>ACTIVE</color>", 0.3f);
            interpreter.AddOutput(TerminalLineType.Normal, "Collecting Initial Player Data...", 0.5f);
            interpreter.AddOutput(TerminalLineType.Normal, "<color=green>GTFOStats Subsystem Online</color>", 1.0f);

            // Website Info
            interpreter.AddOutput(TerminalLineType.Normal, "<color=yellow>View your stats online at:</color>", 0.3f);
            interpreter.AddOutput(TerminalLineType.Warning, "https://gtfo.splitstats.io", 0.5f);

            // Separator
            interpreter.AddOutput(TerminalLineType.Normal, "------------------------------------", 0.3f);
        }

        // Fetch challenges from the API with SteamID
        private static async Task<DanosChallengeResponse?> FetchChallengesAsync(string steamId)
        {
            try
            {
                // Append the SteamID as a query parameter
                var requestUrl = $"{ApiEndpoint}?steamId={Uri.EscapeDataString(steamId)}";

                // Send a GET request to the API endpoint
                var request = HttpClient.GetAsync(requestUrl);

                if (await Task.WhenAny(request, Task.Delay(3000)) == request) // Give it 3 seconds to complete or timeout
                {
                    var response = await request;
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(jsonString);
                        return JsonSerializer.Deserialize<DanosChallengeResponse>(jsonString);
                    }
                    else
                    {
                        Console.WriteLine($"Error fetching challenges: {response.StatusCode}");
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching challenges: {ex.Message}");
            }

            return null; // Return null if request fails or times out
        }

        // Display user info (Current XP and Service Length)
        private static void DisplayUserInfo(LG_ComputerTerminalCommandInterpreter terminal, DanosChallengeResponse challenges)
        {
            TimeSpan serviceLength = DateTime.Now - challenges.FirstMatchDate;
            terminal.AddOutput(TerminalLineType.Normal, "<color=green>--- Player Info ---</color>", 0.5f);
            terminal.AddOutput(TerminalLineType.Normal, $"<color=yellow>Current XP: {challenges.CurrentXp}</color>", 0.5f);
            terminal.AddOutput(TerminalLineType.Normal, $"<color=yellow>Service Length: {serviceLength.Days}d {serviceLength.Hours}h</color>", 0.5f);
            terminal.AddOutput(TerminalLineType.Normal, "", 0.5f); // Spacer
        }

        // Display challenges
        private static void DisplayChallenges(LG_ComputerTerminalCommandInterpreter terminal, DanosChallengeResponse challenges)
        {
            // Display Daily Challenges
            if (challenges.DailyChallenges.Count > 0)
            {
                terminal.AddOutput(TerminalLineType.Normal, "<color=green>--- Today's Daily Challenges ---</color>", 0.5f);
                foreach (var challenge in challenges.DailyChallenges)
                {
                    terminal.AddOutput(TerminalLineType.Normal, $"<color=orange>{challenge.Title}</color>", 0.5f);
                    terminal.AddOutput(TerminalLineType.Normal, GetProgressBar(challenge.Progress, challenge.Goal), 0.5f);
                    terminal.AddOutput(TerminalLineType.Normal, $"Progress: {challenge.Progress}/{challenge.Goal}", 0.5f);
                    terminal.AddOutput(TerminalLineType.Normal, $"<color=yellow>XP Reward: {challenge.XpReward} XP</color>", 0.5f);
                    terminal.AddOutput(TerminalLineType.Normal, $"<color=yellow>Time Remaining: {challenge.TimeRemaining.Days}d {challenge.TimeRemaining.Hours}h</color>", 0.5f);
                    terminal.AddOutput(TerminalLineType.Normal, "", 0.5f); // Spacer
                }
            }

            // Display Weekly Challenges
            if (challenges.WeeklyChallenges.Count > 0)
            {
                terminal.AddOutput(TerminalLineType.Normal, "<color=green>--- Weekly Challenges ---</color>", 0.5f);
                foreach (var challenge in challenges.WeeklyChallenges)
                {
                    terminal.AddOutput(TerminalLineType.Normal, $"<color=orange>{challenge.Title}</color>", 0.5f);
                    terminal.AddOutput(TerminalLineType.Normal, GetProgressBar(challenge.Progress, challenge.Goal), 0.5f);
                    terminal.AddOutput(TerminalLineType.Normal, $"Progress: {challenge.Progress}/{challenge.Goal}", 0.5f);
                    terminal.AddOutput(TerminalLineType.Normal, $"<color=yellow>XP Reward: {challenge.XpReward} XP</color>", 0.5f);
                    terminal.AddOutput(TerminalLineType.Normal, $"<color=yellow>Time Remaining: {challenge.TimeRemaining.Days}d {challenge.TimeRemaining.Hours}h</color>", 0.5f);
                    terminal.AddOutput(TerminalLineType.Normal, "", 0.5f); // Spacer
                }
            }
        }

        // Generate text-based progress bar
        private static string GetProgressBar(int current, int total)
        {
            int barLength = 20; // Length of the progress bar
            int filledLength = (int)((double)current / total * barLength);

            string filledBar = new string('=', filledLength); // Filled part
            string emptyBar = new string('-', barLength - filledLength); // Empty part

            return $"[{filledBar}{emptyBar}]";
        }

        // Dummy method to retrieve SteamID (replace with actual implementation)
        private static string GetSteamIdForPlayer()
        {
            var steamid = GameStatePatch.GetLocalPlayerSteamID(); // Call the method to get the SteamID
            if (steamid <= 0)
            {
                return null;
            }


            return steamid.ToString();
        }
    }
}
