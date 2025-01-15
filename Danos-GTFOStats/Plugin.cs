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

namespace GTFOStats
{
    [BepInPlugin("danos.GTFOStats", "GTFOStats", DanosStaticStore.ModVersion)]
    [BepInDependency("Endskill.GTFuckingXP", BepInDependency.DependencyFlags.SoftDependency)]

    public class DanosPlugin : BasePlugin
    {
        private Harmony _harmony;

        public override void Load()
        {
            

            _harmony = new Harmony("danos.GTFOStats");
            _harmony.PatchAll(typeof(GameStatePatch));
            _harmony.PatchAll(typeof(DanosTerminalCommandPatch));
            _harmony.PatchAll(typeof(DamagePatches));
            Log.LogInfo("GTFOStats is loaded!");

            // Define patches dynamically
            var patchConfigs = DanosPatchManager.GetPatchConfigurations();

            foreach (var patchConfig in patchConfigs)
            {
                ApplyDynamicPatch(patchConfig);
            }

        }


        // Dynamic patching to keep the project lightweight and avoid dependencies on other mods.
        private void ApplyDynamicPatch(DanosPatchConfiguration patchConfig)
        {
            try
            {
                Type targetType = AccessTools.TypeByName(patchConfig.TargetClass);
                if (targetType == null)
                {
                    Log.LogWarning($"Target class '{patchConfig.TargetClass}' not found. Skipping patch.");
                    return;
                }

                MethodInfo targetMethod = AccessTools.Method(targetType, patchConfig.TargetMethod);
                if (targetMethod == null)
                {
                    Log.LogWarning($"Target method '{patchConfig.TargetMethod}' not found in class '{patchConfig.TargetClass}'. Skipping patch.");
                    return;
                }

                // Get the postfix method using its full name (Namespace.Class.Method)
                string[] methodParts = patchConfig.PostfixMethod.Split('.');
                string methodName = methodParts.Last();
                string className = string.Join('.', methodParts.Take(methodParts.Length - 1));
                Type postfixClass = Type.GetType(className);
                if (postfixClass == null)
                {
                    Log.LogWarning($"Postfix class '{className}' not found. Skipping patch.");
                    return;
                }

                MethodInfo postfixMethod = postfixClass.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (postfixMethod == null)
                {
                    Log.LogWarning($"Postfix method '{patchConfig.PostfixMethod}' not found. Skipping patch.");
                    return;
                }

                _harmony.Patch(targetMethod, postfix: new HarmonyMethod(postfixMethod));
                Log.LogInfo($"Successfully patched {patchConfig.TargetClass}.{patchConfig.TargetMethod}");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to apply patch for {patchConfig.TargetClass}.{patchConfig.TargetMethod}: {ex.Message}");
            }
        }

    }
}
