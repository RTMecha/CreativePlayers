using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using CreativePlayers.Functions;

namespace CreativePlayers.Patchers
{
    [HarmonyPatch(typeof(EditorManager))]
    public class EditorManagerPatch
	{
		[HarmonyPatch("Update")]
		[HarmonyPostfix]
		private static void EditorUpdatePostfix(EditorManager __instance)
		{
			foreach (InputDataManager.CustomPlayer customPlayer in InputDataManager.inst.players)
			{
				if (customPlayer.GetRTPlayer() && customPlayer.GetRTPlayer().Actions.Pause.WasPressed && !__instance.isEditing)
				{
					__instance.ToggleEditor();
				}
			}
		}
	}
}
