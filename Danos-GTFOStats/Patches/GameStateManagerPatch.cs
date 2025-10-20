using Enemies;
using GTFOStats.Data;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GTFOStats.Patches
{
    [HarmonyPatch]
    public class GameStateManagerPatch
    { 

        public static PolicyVersion policy = new PolicyVersion();

        [HarmonyPatch(typeof(GameStateManager), "ChangeState")]
        [HarmonyPrefix]
        public static bool ChangeStatePrefix(eGameStateName nextState)
        {
            try
            {
                policy = GetPolicy().Result;
                if (HasUserAcceptedPolicy(policy.Version))
                {
                    Debug.Log("User has already accepted the current policy version.");
                    DanosPlugin.ApplyAdditionalPatches();

                    return true; // Skip showing the popup
                }
            }
            catch (Exception e)
            {
                Debug.Log("Error getting policy: " + e.Message);
            }



            if (nextState == eGameStateName.NoLobby)
            {
                ShowPopup(
                    () =>
                    {
                        // User accepted
                        Debug.Log("User accepted GTFOStats terms.");
                        SaveUserResponse(true, policy.Version); // Save the response and policy version to a file
                        DanosPlugin.ApplyAdditionalPatches();
                    },
                    () =>
                    {
                        // User declined
                        Debug.Log("User declined GTFOStats terms. Closing the game.");
                        Application.Quit();
                    });

                
                return true;
            }

            return true;
        }

        private static bool HasUserAcceptedPolicy(string policyVersion)
        {
            try
            {
                // Get the directory of the currently executing assembly
                string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string directory = System.IO.Path.GetDirectoryName(assemblyLocation);
                string filePath = System.IO.Path.Combine(directory, "GTFOStatsPrivacyResponse.txt");

                // Check if the file exists
                if (!System.IO.File.Exists(filePath))
                {
                    return false; // User has not accepted any policy
                }

                // Read the file content
                string[] lines = System.IO.File.ReadAllLines(filePath);

                // Check if the file contains the current policy version
                foreach (var line in lines)
                {
                    if (line.Contains($"Policy Version: {policyVersion}"))
                    {
                        return true; // User has accepted the current policy version
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("Error checking user response: " + e.Message);
            }

            return false; // Default to false if any error occurs
        }
        private static void SaveUserResponse(bool accepted, string policyVersion)
        {
            try
            {
                // Get the directory of the currently executing assembly (where the mod is installed)
                string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string directory = System.IO.Path.GetDirectoryName(assemblyLocation);
                string filePath = System.IO.Path.Combine(directory, "GTFOStatsPrivacyResponse.txt");

                // Write the response and policy version to the file
                string response = accepted ? "Accepted" : "Declined";
                string content = $"User Response: {response}\nPolicy Version: {policyVersion}\nDate: {DateTime.Now}";
                System.IO.File.WriteAllText(filePath, content);
                Debug.Log("User response saved to: " + filePath);
            }
            catch (Exception e)
            {
                Debug.Log("Error saving user response: " + e.Message);
            }
        }
        [HarmonyPatch(typeof(GameStateManager), "Update")]
        [HarmonyPrefix]
        public static void UpdatePostfix()
        {
            GlobalInputManager.Update();

        }
        public static void ShowPopup(Action onAccept, Action onDecline)
        {
            // Check if the popup is already shown
            if (GameObject.Find("GTFOStatsPopupCanvas") != null)
            {
                return;
            }


            
            // Create a Canvas
            var popupCanvas = new GameObject("GTFOStatsPopupCanvas");
            var canvas = popupCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var canvasScaler = popupCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;

            popupCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Create a Panel
            var panel = new GameObject("Panel");
            var panelRectTransform = panel.AddComponent<RectTransform>();
            panelRectTransform.sizeDelta = new Vector2(500, 400); 
            panelRectTransform.SetParent(popupCanvas.transform, false);

            var panelImage = panel.AddComponent<UnityEngine.UI.Image>();
            panelImage.color = HexToColor("#181a22"); // Background color

            // Add Header
            CreateText(panel, "GTFOStats", new Vector2(0, 150), 36, TextAnchor.UpperCenter, "#6ac4a7");

            // Add Explanation Text
            string message = "Welcome to GTFOStats! By using this mod, you agree to the privacy policy at https://gtfo.splitstats.io/privacypolicy.\n\n"
                           + "Game stats will be collected and uploaded during gameplay. Your response to the policy will be saved in the mod folder until you uninstall the mod or the policy changes.\n\n"
                           + "Manage or delete your data using tools on our website. Decline to close the game and uninstall the mod.";

            // Display policy details
            string message2 = $"We do not store contact information, so we cannot directly inform you of any changes, however this prompt will popup any time we make any.\n\nVersion: {policy.Version}\nLast Updated: {policy.LastUpdated.ToShortDateString()}\nChanges: {policy.DescriptionOfChanges}\nLast Updated: {policy.LastUpdatedDaysAgo}.";

            CreateText(panel, message, new Vector2(0, 75), 8, TextAnchor.MiddleCenter, "#FFFFFF");
            CreateText(panel, message2, new Vector2(0, -25), 5, TextAnchor.MiddleCenter, "#FFFFFF");

            CreateText(panel, "[F5] Open Privacy Policy", new Vector2(0, -60), 24, TextAnchor.MiddleCenter, "#6ac4a7");
            CreateText(panel, "[F6] Accept", new Vector2(-100, -120), 24, TextAnchor.MiddleCenter, "#6ac4a7");
            CreateText(panel, "[F7] Decline", new Vector2(100, -120), 24, TextAnchor.MiddleCenter, "#FF6A6A");

            //Listen for f5 key
            GlobalInputManager.ListenForKeys(KeyCode.F5, () =>
            {
                Application.OpenURL("https://gtfo.splitstats.io/privacypolicy");

            });

            // Monitor keyboard inputs globally
            GlobalInputManager.ListenForKeys(KeyCode.F6, () =>
            {
                UnityEngine.Object.Destroy(popupCanvas);
                GlobalInputManager.StopListeningForKeys(KeyCode.F5);

                GlobalInputManager.StopListeningForKeys(KeyCode.F6);
                GlobalInputManager.StopListeningForKeys(KeyCode.F7);
                onAccept?.Invoke();
            });

            GlobalInputManager.ListenForKeys(KeyCode.F7, () =>
            {
                UnityEngine.Object.Destroy(popupCanvas);
                GlobalInputManager.StopListeningForKeys(KeyCode.F5);

                GlobalInputManager.StopListeningForKeys(KeyCode.F6);
                GlobalInputManager.StopListeningForKeys(KeyCode.F7);
                onDecline?.Invoke();
            });
        }


        private static async Task<PolicyVersion> GetPolicy()
        {
            var policy = new PolicyVersion();
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync("https://gtfo.splitstats.io/api/Privacy/current");
            if (response.IsSuccessStatusCode)
            {
                //Deserialize the response as a PolicyVersion object
                policy = await response.Content.ReadFromJsonAsync<PolicyVersion>();
                if (policy == null)
                    return new PolicyVersion();
            }
            return policy;

        }

        private static void CreateText(GameObject parent, string message, Vector2 position, int fontSize = 20, TextAnchor alignment = TextAnchor.MiddleCenter, string hexColor = "#FFFFFF", Color? backgroundColor = null)
        {
            var textObj = new GameObject("TextElement");
            var text = textObj.AddComponent<UnityEngine.UI.Text>();
            text.text = message;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = HexToColor(hexColor);

            var textRectTransform = text.GetComponent<RectTransform>();
            textRectTransform.sizeDelta = new Vector2(460, 100); 
            textRectTransform.anchoredPosition = position;
            textRectTransform.SetParent(parent.transform, false);

            if (backgroundColor.HasValue)
            {
                var bgObj = new GameObject("Background");
                var bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
                bgImage.color = backgroundColor.Value;

                var bgRectTransform = bgObj.GetComponent<RectTransform>();
                bgRectTransform.sizeDelta = new Vector2(220, 60); // Size of the button background
                bgRectTransform.anchoredPosition = position;
                bgRectTransform.SetParent(parent.transform, false);

                text.transform.SetParent(bgObj.transform, false);
            }
        }

        private static Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var color))
            {
                return color;
            }
            return Color.white; // Default to white if parsing fails
        }

       
    }

    public static class GlobalInputManager
    {
        private static readonly Dictionary<KeyCode, Action> keyActions = new();

        public static void ListenForKeys(KeyCode key, Action action)
        {
            if (!keyActions.ContainsKey(key))
            {
                keyActions[key] = action;
            }
        }

        public static void StopListeningForKeys(KeyCode key)
        {
            if (keyActions.ContainsKey(key))
            {
                keyActions.Remove(key);
            }
        }

        public static void StopAllListening()
        {
            keyActions.Clear(); // Remove all listeners
        }

        public static void Update()
        {
            if(keyActions.Count == 0)
            {
                return;
            }
            foreach (var keyAction in keyActions)
            {
                if (Input.GetKeyDown(keyAction.Key))
                {
                    keyAction.Value?.Invoke();
                }
            }
        }
    }
}
