using BepInEx.Unity.IL2CPP;
using BepInEx;
using Dissonance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using GTFOStats.Patches;
using GTFOStats.Data;
using System.Reflection;
using UnityEngine;

namespace GTFOStats
{
    [BepInPlugin("danos.GTFOStats", "GTFOStats", DanosStaticStore.ModVersion)]
    [BepInDependency("Endskill.GTFuckingXP", BepInDependency.DependencyFlags.SoftDependency)]

    public class DanosPlugin : BasePlugin
    {
        public static Harmony HarmonyInstance { get; private set; }
        private static readonly HashSet<Type> AppliedPatches = new HashSet<Type>();
        private static readonly HashSet<string> AppliedDynamicPatches = new HashSet<string>();

        private static bool _additionalPatchesApplied = false;

        public override void Load()
        {
            HarmonyInstance = new Harmony("danos.GTFOStats");
            ApplyPatch(typeof(GameStateManagerPatch)); // Initial patch
            Debug.Log("GTFOStats loaded and initial patches applied.");
        }

        public static void ApplyAdditionalPatches()
        {
            if (_additionalPatchesApplied)
            {
                Debug.Log("Additional patches already applied.");
                return;
            }
            ApplyPatch(typeof(GameStatePatch));
            ApplyPatch(typeof(DanosTerminalCommandPatch));
            ApplyPatch(typeof(DamagePatches));
            Debug.Log("GTFOStats is loaded!");

            // Define patches dynamically
            var patchConfigs = DanosPatchManager.GetPatchConfigurations();

            foreach (var patchConfig in patchConfigs)
            {
                ApplyDynamicPatch(patchConfig);
            }

            _additionalPatchesApplied = true;
        }


        private static void ApplyDynamicPatch(DanosPatchConfiguration patchConfig)
        {
            try
            {
                string patchIdentifier = $"{patchConfig.TargetClass}.{patchConfig.TargetMethod}.{patchConfig.PostfixMethod}";
                if (AppliedDynamicPatches.Contains(patchIdentifier))
                {
                    Debug.Log($"Patch already applied: {patchIdentifier}");
                    return;
                }

                Type targetType = AccessTools.TypeByName(patchConfig.TargetClass);
                if (targetType == null)
                {
                    Debug.Log($"Target class '{patchConfig.TargetClass}' not found. Skipping patch.");
                    return;
                }

                MethodInfo targetMethod = AccessTools.Method(targetType, patchConfig.TargetMethod);
                if (targetMethod == null)
                {
                    Debug.Log($"Target method '{patchConfig.TargetMethod}' not found in class '{patchConfig.TargetClass}'. Skipping patch.");
                    return;
                }

                // Get the postfix method using its full name (Namespace.Class.Method)
                string[] methodParts = patchConfig.PostfixMethod.Split('.');
                string methodName = methodParts.Last();
                string className = string.Join('.', methodParts.Take(methodParts.Length - 1));
                Type postfixClass = Type.GetType(className);
                if (postfixClass == null)
                {
                    Debug.Log($"Postfix class '{className}' not found. Skipping patch.");
                    return;
                }

                MethodInfo postfixMethod = postfixClass.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (postfixMethod == null)
                {
                    Debug.Log($"Postfix method '{patchConfig.PostfixMethod}' not found. Skipping patch.");
                    return;
                }

                HarmonyInstance.Patch(targetMethod, postfix: new HarmonyMethod(postfixMethod));
                AppliedDynamicPatches.Add(patchIdentifier); // Mark this patch as applied
                Debug.Log($"Successfully patched {patchConfig.TargetClass}.{patchConfig.TargetMethod}");
            }
            catch (Exception ex)
            {
                Debug.Log($"Failed to apply patch for {patchConfig.TargetClass}.{patchConfig.TargetMethod}: {ex.Message}");
            }
        }

        private static void ApplyPatch(Type patchType)
        {
            if (!AppliedPatches.Contains(patchType))
            {
                HarmonyInstance.PatchAll(patchType);
                AppliedPatches.Add(patchType);
                Debug.Log($"Patch applied: {patchType.Name}");
            }
            else
            {
                Debug.Log($"Patch already applied: {patchType.Name}");
            }
        }

    }
}
