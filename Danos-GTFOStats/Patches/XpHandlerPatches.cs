using GTFOStats.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GTFOStats.Patches
{
    // This class is used to patch the XpHandler class from the GTFuckingXP mod
    public static class XpHandlerPatches
    {
        private static PropertyInfo xpGainProperty;
        private static PropertyInfo debuffXpProperty;

        public static void AddXpPostfix(object xpData, Vector3 xpTextPosition, bool forceDebuffXp, string xpPopupColor)
        {
            try
            {


                if (xpGainProperty == null)
                    xpGainProperty = xpData.GetType().GetProperty("XpGain");
                if (debuffXpProperty == null)
                    debuffXpProperty = xpData.GetType().GetProperty("DebuffXp");

                uint xpGain = xpGainProperty != null ? (uint)xpGainProperty.GetValue(xpData) : 0;
                uint debuffXp = debuffXpProperty != null ? (uint)debuffXpProperty.GetValue(xpData) : 0;


                if (DanosStaticStore.currentRunDownDataStore != null)
                {
                    DanosStaticStore.currentRunDownDataStore.xp += xpGain;
                }
            }
            catch (Exception e)
            {
                //Do nothing
            }

        }

    }
}
