using Agents;
using Dissonance;
using Enemies;
using GameData;
using GTFOStats.Data;
using HarmonyLib;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GTFOStats.Patches
{
    [HarmonyPatch]
    public class DamagePatches
    {
        // Track enemies we've already counted
        private static readonly HashSet<int> countedEnemies = new();
        
        [HarmonyPatch(typeof(EnemyAgent), "OnDead")]
        [HarmonyPrefix]
        public static bool OnDeadPrefix(EnemyAgent __instance)
        {
            //Make sure the enemy is not null
            if (__instance == null)
            {
                return true;
            }

            if(DanosStaticStore.currentRunDownDataStore == null)
            {
                return true;
            }

            //Get the enemies balancing data
            var enemyBalancingDataBlock = __instance.EnemyBalancingData;
            if (enemyBalancingDataBlock == null)
            {
                return true;
            }


            //Get the enemy name
            string enemyName = enemyBalancingDataBlock.name;
            if (string.IsNullOrEmpty(enemyName))
            {
                return true;
            }

            // Each enemy agent should only be counted once
            int id = __instance.gameObject != null ? __instance.gameObject.GetInstanceID() : __instance.GetInstanceID();
            if (!countedEnemies.Add(id))
            {
                // Already counted this instance, skip
                return true;
            }
            
            DanosStaticStore.currentRunDownDataStore.AddEnemyDeathCount(enemyName);

            return true;

        }

            [HarmonyPatch(typeof(Dam_PlayerDamageBase), "OnIncomingDamage")]
        [HarmonyPrefix]
        public static bool OnIncomingDamagePrefix(Dam_PlayerDamageBase __instance, ref float damage, ref float originalDamage, ref Agent sourceAgent)
        {
            try
            {
                PlayerAgent damagedAgent = __instance.Owner;
                if (damagedAgent == null)
                {
                    return true;
                }

                // Determine the type of the sourceAgent
                string sourceType = "Unknown";
                if (sourceAgent != null)
                {
                    if (sourceAgent.GetIl2CppType().Name == typeof(PlayerAgent).Name)
                    {
                        sourceType = "Player";
                    }
                    else if (sourceAgent.GetIl2CppType().Name == typeof(LocalPlayerAgent).Name)
                    {
                        sourceType = "Player";
                    }
                    else if (sourceAgent.GetIl2CppType().Name == typeof(EnemyAgent).Name)
                    {
                        sourceType = "Enemy";
                        try
                        {
                            // Try to get more details about the enemy
                            var enemyAgent = sourceAgent.Cast<EnemyAgent>();
                            if (enemyAgent != null)
                            {
                                var enemyBalancingDataBlock = enemyAgent.EnemyBalancingData;
                                if (enemyBalancingDataBlock != null)
                                {
                                    sourceType = enemyBalancingDataBlock.name;
                                }
                                else
                                {
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogError($"Error retrieving EnemyAgent details: {ex}");
                        }
                    }
                    else
                    {
                        sourceType = "Unknown";
                    }
                }
                else
                {
                    sourceType = "Likely Projectile";
                }


                if (damagedAgent != null)
                {
                    DanosDamageInfoStore.UpdateDamage(damagedAgent, damage, sourceAgent, sourceType);
                }



            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error in OnIncomingDamagePrefix: {ex}");
            }

            return true;
        }

        public static void ClearCountedEnemies()
        {
            countedEnemies.Clear();
        }
        
    }



}
