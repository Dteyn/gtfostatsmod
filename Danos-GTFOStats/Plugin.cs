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

namespace GTFOStats
{
    [BepInPlugin("danos.GTFOStats", "GTFOStats", DanosStaticStore.ModVersion)]
    public class Plugin : BasePlugin
    {
        public override void Load()
        {
            // Plugin startup logic
            Log.LogInfo("GTFOStats is loaded!");

            var harm =new Harmony("danos.GTFOStats");

            harm.PatchAll(typeof(GameStatePatch));

        }
    }
}
