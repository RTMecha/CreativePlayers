using UnityEngine;
using HarmonyLib;
using InControl;

using CreativePlayers.Functions;

namespace CreativePlayers.Patchers
{

	[HarmonyPatch(typeof(InputDataManager.CustomPlayer))]
	public class CustomPlayerPatch : MonoBehaviour
    {
		[HarmonyPatch("ControllerDisconnected")]
		[HarmonyPrefix]
		private static bool DisconnetControllers(InputDataManager.CustomPlayer __instance, InputDevice __0)
		{
			if (__0.SortOrder == __instance.SortOrder && __instance.GetDeviceModel(__0.Name) == __instance.deviceModel)
			{
				if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Input Select")
				{
					InputManager.OnDeviceAttached -= __instance.ControllerConnected;
					InputManager.OnDeviceDetached -= __instance.ControllerDisconnected;
					InputDataManager.inst.players.RemoveAt(__instance.index);
					int num = 0;
					foreach (var customPlayer in InputDataManager.inst.players)
					{
						InputDataManager.inst.players[num].index = num;
						InputDataManager.inst.players[num].playerIndex = __instance.GetPlayerIndex(InputDataManager.inst.players[num].index);
						num++;
					}
				}
				__instance.controllerConnected = false;
				__instance.device = null;
				if (__instance.GetRTPlayer() != null)
					__instance.GetRTPlayer().Actions = null;
				Debug.LogFormat("{0}playerDisconnectedEvent: {1}", PlayerPlugin.className, __0.Name);
				AccessTools.Field(typeof(InputDataManager), "playerDisconnectedEvent").SetValue(InputDataManager.inst, __instance);
				Debug.LogFormat("{0}Disconnected Controler Was Attached to player. Controller [{1}] [{2}] -/- Player [{3}]", PlayerPlugin.className, __0.Name, __0.SortOrder, __instance.index);
			}
			return false;
		}


		[HarmonyPatch("ControllerConnected")]
		[HarmonyPrefix]
		private static bool ControllerConnected(InputDataManager.CustomPlayer __instance, InputDevice __0)
		{
			if (__0.SortOrder == __instance.SortOrder && __instance.GetDeviceModel(__0.Name) == __instance.deviceModel)
			{
				AccessTools.Method(typeof(InputDataManager), "ThereIsNoPlayerUsingJoystick").Invoke(InputDataManager.inst, new object[] { __0 });
				__instance.controllerConnected = true;
				__instance.device = __0;
				Debug.LogFormat("{0}playerReconnectedEvent: {1}", PlayerPlugin.className, __0.Name);
				AccessTools.Field(typeof(InputDataManager), "playerReconnectedEvent").SetValue(InputDataManager.inst, __instance);
				MyGameActions myGameActions = MyGameActions.CreateWithJoystickBindings();
				myGameActions.Device = __0;
				if (__instance.GetRTPlayer() != null)
					__instance.GetRTPlayer().Actions = myGameActions;
				Debug.LogFormat("{0}Connected Controller Exists in players. Controller [{1}] [{2}] -> Player [{3}]", PlayerPlugin.className, __0.Name, __0.SortOrder, __instance.index);
			}
			return false;
		}

		[HarmonyPatch("ReconnectController")]
		[HarmonyPrefix]
		private static bool ReconnectController(InputDataManager.CustomPlayer __instance, InputDevice __0)
		{
			MyGameActions myGameActions = MyGameActions.CreateWithJoystickBindings();
			myGameActions.Device = __instance.device;
			if (__instance.GetRTPlayer() != null)
				__instance.GetRTPlayer().Actions = myGameActions;
			return false;
		}
	}
}
