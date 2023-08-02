using HarmonyLib;

using UnityEngine;

using CreativePlayers.Functions;

namespace CreativePlayers.Patchers
{
    [HarmonyPatch(typeof(InputDataManager))]
    public class InputDataManagerPatch : MonoBehaviour
    {
		[HarmonyPatch("SetAllControllerRumble", new[] { typeof(float), typeof(float), typeof(bool) })]
		[HarmonyPrefix]
		private static bool SetAllControllerRumble(InputDataManager __instance, float __0, float __1, bool __2 = true)
		{
			if (DataManager.inst.GetSettingBool("ControllerVibrate", true))
			{
				foreach (InputDataManager.CustomPlayer customPlayer in __instance.players)
				{
					if (customPlayer.device != null)
					{
						customPlayer.device.Vibrate(Mathf.Clamp(__0, 0f, 0.5f), Mathf.Clamp(__1, 0f, 0.5f));
					}
				}
			}
			return false;
		}

		[HarmonyPatch("SetControllerRumble", new[] { typeof(int), typeof(float), typeof(float), typeof(bool) })]
		[HarmonyPrefix]
		private static bool SetControllerRumble(InputDataManager __instance, int __0, float __1, float __2, bool __3 = true)
		{
			foreach (var customPlayer in __instance.players)
			{
				if (customPlayer.device != null && customPlayer.GetRTPlayer() != null && customPlayer.GetRTPlayer().playerIndex == __0)
				{
					if (__3 && customPlayer.GetRTPlayer().PlayerAlive)
					{
						customPlayer.device.Vibrate(Mathf.Clamp(__1, 0f, 0.5f), Mathf.Clamp(__2, 0f, 0.5f));
					}
					else
					{
						customPlayer.device.Vibrate(Mathf.Clamp(__1, 0f, 0.5f), Mathf.Clamp(__2, 0f, 0.5f));
					}
				}
			}
			return false;
		}

		[HarmonyPatch("RemovePlayer")]
		[HarmonyPrefix]
		private static bool RemovePlayer(InputDataManager __instance, InputDataManager.CustomPlayer __0)
		{
			int index = __0.index;
			if (__0.GetRTPlayer() != null)
			{
				__instance.StopControllerRumble(__0.index);
				__0.GetRTPlayer().Actions = null;
				if (__0.GetRTPlayer().gameObject != null)
				{
					Destroy(__0.GetRTPlayer().gameObject);
				}
			}
			__instance.StopAllControllerRumble();
			__instance.players.Remove(__0);
			PlayerPlugin.players.Remove(PlayerPlugin.players.Find(x => x.playerIndex == index));
			return false;
		}
	}
}
