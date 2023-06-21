using HarmonyLib;

using UnityEngine;

namespace CreativePlayers.Patchers
{
    [HarmonyPatch(typeof(Player))]
    public class PlayerPatch
    {
        [HarmonyPatch("StopMovement")]
        [HarmonyPrefix]
        private static bool StopMovementPrefix(Player __instance)
        {
            Debug.LogFormat("{0}StopMovement() Method Invoked", PlayerPlugin.className);
            return false;
        }

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        private static bool StartPrefix(Player __instance)
        {
            Debug.LogFormat("{0}Start() Method Invoked", PlayerPlugin.className);
            var player = __instance.transform.Find("Player").gameObject;

            __instance.GetType().GetField("psSpawn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(__instance, player.transform.Find("Player/spawn-implosion").GetComponent<ParticleSystem>());
            __instance.GetType().GetField("psBurst", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(__instance, player.transform.Find("Player/burst-explosion").GetComponent<ParticleSystem>());
            __instance.GetType().GetField("psDeath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(__instance, player.transform.Find("Player/death-explosion").GetComponent<ParticleSystem>());
            return false;
        }

        [HarmonyPatch("SetColor")]
        [HarmonyPrefix]
        private static bool SetColorPrefix()
        {
            return false;
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        private static bool UpdatePrefix()
        {
            return false;
        }

        [HarmonyPatch("LateUpdate")]
        [HarmonyPrefix]
        private static bool LateUpdatePrefix()
        {
            return false;
        }

        [HarmonyPatch("FixedUpdate")]
        [HarmonyPrefix]
        private static bool FixedUpdatePrefix()
        {
            return false;
        }

        [HarmonyPatch("OnChildTriggerEnter")]
        [HarmonyPrefix]
        private static bool OnChildTriggerEnterPrefix()
        {
            return false;
        }

        [HarmonyPatch("OnChildTriggerEnterMesh")]
        [HarmonyPrefix]
        private static bool OnChildTriggerEnterMeshPrefix()
        {
            return false;
        }

        [HarmonyPatch("OnChildTriggerStay")]
        [HarmonyPrefix]
        private static bool OnChildTriggerStayPrefix()
        {
            return false;
        }

        [HarmonyPatch("OnChildTriggerStayMesh")]
        [HarmonyPrefix]
        private static bool OnChildTriggerStayMeshPrefix()
        {
            return false;
        }

        [HarmonyPatch("BoostCooldownLoop")]
        [HarmonyPrefix]
        private static bool BoostCooldownLoopPrefix()
        {
            Debug.LogFormat("{0}BoostCooldownLoop() Method Invoked", PlayerPlugin.className);
            return false;
        }

        [HarmonyPatch("PlayerHit")]
        [HarmonyPrefix]
        private static bool PlayerHitPrefix()
        {
            Debug.LogFormat("{0}PlayerHit() Method Invoked", PlayerPlugin.className);
            return false;
        }

        [HarmonyPatch("Kill")]
        [HarmonyPrefix]
        private static bool KillPrefix()
        {
            Debug.LogFormat("{0}Kill() Method Invoked", PlayerPlugin.className);
            return false;
        }

        [HarmonyPatch("Spawn")]
        [HarmonyPrefix]
        private static bool SpawnPrefix()
        {
            Debug.LogFormat("{0}Spawn() Method Invoked", PlayerPlugin.className);
            return false;
        }
    }
}
