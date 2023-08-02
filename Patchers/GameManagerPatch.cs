using System.Linq;
using System.Collections.Generic;
using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;

using CreativePlayers.Functions;
using CreativePlayers.Functions.Data;

namespace CreativePlayers.Patchers
{
    [HarmonyPatch(typeof(GameManager))]
    public class GameManagerPatch : MonoBehaviour
	{
		[HarmonyPatch("Start")]
		[HarmonyPostfix]
		private static void StartPostfix(GameManager __instance)
        {
			PlayerPlugin.playerModels = new Dictionary<string, PlayerModelClass.PlayerModel>();
			var pm1 = new PlayerModelClass.PlayerModel(__instance.PlayerPrefabs[0]);
			var pm2 = new PlayerModelClass.PlayerModel(__instance.PlayerPrefabs[1]);

			pm1.values["Base ID"] = "0";
			pm2.values["Base ID"] = "1";

			PlayerPlugin.playerModels.Add("0", pm1);
			PlayerPlugin.playerModels.Add("1", pm2);

			if (EditorManager.inst != null)
				PlayerPlugin.StartLoadingModels();
			else
				PlayerPlugin.StartLoadingLocalModels();

			var health = __instance.playerGUI.transform.Find("Health");
			health.gameObject.SetActive(true);
			health.GetChild(0).gameObject.SetActive(true);
			for (int i = 1; i < 4; i++)
			{
				Destroy(health.GetChild(i).gameObject);
			}

			for (int i = 3; i < 5; i++)
			{
				Destroy(health.GetChild(0).GetChild(i).gameObject);
			}
			var gm = health.GetChild(0).gameObject;
			PlayerPlugin.healthImages = gm;
			var text = gm.AddComponent<Text>();

			text.alignment = TextAnchor.MiddleCenter;
			text.font = Font.GetDefault();
			text.enabled = false;

			if (gm.transform.Find("Image"))
            {
				PlayerPlugin.healthSprite = gm.transform.Find("Image").GetComponent<Image>().sprite;
			}

			gm.transform.SetParent(null);
			PlayerPlugin.healthParent = health;
		}

		[HarmonyPatch("Update")]
		[HarmonyPostfix]
		private static void GameUpdatePostfix(GameManager __instance)
		{
			if (__instance.gameState == GameManager.State.Playing)
			{
				if (EditorManager.inst == null)
				{
					foreach (InputDataManager.CustomPlayer customPlayer in InputDataManager.inst.players)
					{
						if (customPlayer.GetRTPlayer() && customPlayer.GetRTPlayer().Actions.Pause.WasPressed)
						{
							__instance.Pause();
						}
					}
				}
			}
		}

		[HarmonyPatch("SpawnPlayers")]
		[HarmonyPrefix]
		private static bool SpawnPlayersPrefix(GameManager __instance, Vector3 __0)
		{
			foreach (InputDataManager.CustomPlayer customPlayer in InputDataManager.inst.players)
			{
				if (customPlayer.GetRTPlayer() == null)
				{
					Debug.LogFormat("{0}Player [{1}] Pos Spawn: [{2}, {3}]", PlayerPlugin.className, customPlayer.index, __0.x, __0.y);

					var player = PlayerPlugin.AssignPlayer(customPlayer, __0, GameManager.inst.LiveTheme.playerColors[customPlayer.index % 4]);

					PlayerPlugin.players.Add(player);

					player.transform.SetParent(__instance.players.transform);
					player.transform.Find("Player").localPosition = new Vector3(__0.x, __0.y, 0f);
					player.transform.localScale = Vector3.one;

					if (EditorManager.inst == null)
					{
						if (DataManager.inst.GetSettingInt("ArcadeDifficulty", 0) == 3 || DataManager.inst.GetSettingInt("ArcadeDifficulty", 0) == 2)
						{
							player.playerDeathEvent += delegate (Vector3 _val)
							{
								if (InputDataManager.inst.players.All(x => !x.GetRTPlayer().PlayerAlive))
								{
									__instance.lastCheckpointState = -1;
									__instance.ResetCheckpoints();
									__instance.hits.Clear();
									__instance.deaths.Clear();
									__instance.gameState = GameManager.State.Reversing;
								}
							};
						}
						else
						{
							player.playerDeathEvent += delegate (Vector3 _val)
							{
								if (InputDataManager.inst.players.All(x => !x.GetRTPlayer().PlayerAlive))
								{
									__instance.gameState = GameManager.State.Reversing;
								}
							};
						}
						if (player.playerIndex == 0)
						{
							player.playerDeathEvent += delegate (Vector3 _val)
							{
								__instance.deaths.Add(new SaveManager.SaveGroup.Save.PlayerDataPoint(_val, __instance.UpcomingCheckpointIndex, AudioManager.inst.CurrentAudioSource.time));
							};
							player.playerHitEvent += delegate (int _health, Vector3 _val)
							{
								__instance.hits.Add(new SaveManager.SaveGroup.Save.PlayerDataPoint(_val, __instance.UpcomingCheckpointIndex, AudioManager.inst.CurrentAudioSource.time));
							};
						}
					}
					else
					{
						player.playerDeathEvent += delegate (Vector3 _val)
						{
							if (InputDataManager.inst.players.All(x => !x.GetRTPlayer().PlayerAlive))
							{
								//__instance.ResetCheckpoints();
								__instance.gameState = GameManager.State.Reversing;
							}
						};
					}
				}
				else
                {
					Debug.LogFormat("{0}Player {1} already exists!", PlayerPlugin.className, customPlayer.index);
                }
			}
			return false;
		}

		[HarmonyPatch("Pause")]
		[HarmonyPrefix]
		private static bool PausePrefix(GameManager __instance)
		{
			if (__instance.gameState == GameManager.State.Playing)
			{
				__instance.menuUI.GetComponent<InterfaceController>().SwitchBranch("main");
				__instance.menuUI.GetComponentInChildren<Image>().enabled = true;
				AudioManager.inst.CurrentAudioSource.Pause();
				InputDataManager.inst.SetAllControllerRumble(0f);
				__instance.gameState = GameManager.State.Paused;
				foreach (InputDataManager.CustomPlayer customPlayer in InputDataManager.inst.players)
				{
					if (customPlayer.GetRTPlayer() != null)
						((Animator)customPlayer.GetRTPlayer().playerObjects["Base"].values["Animator"]).speed = 0f;
				}
			}
			return false;
		}

		[HarmonyPatch("UnPause")]
		[HarmonyPrefix]
		private static bool UnPausePrefix(GameManager __instance)
		{
			if (__instance.gameState == GameManager.State.Paused)
			{
				__instance.menuUI.GetComponent<InterfaceController>().SwitchBranch("empty");
				__instance.menuUI.GetComponentInChildren<Image>().enabled = false;
				AudioManager.inst.CurrentAudioSource.UnPause();
				__instance.gameState = GameManager.State.Playing;
				foreach (InputDataManager.CustomPlayer customPlayer in InputDataManager.inst.players)
				{
					if (customPlayer.GetRTPlayer() != null)
						((Animator)customPlayer.GetRTPlayer().playerObjects["Base"].values["Animator"]).speed = 1f;
				}
			}
			return false;
		}

		[HarmonyPatch("UpdateTheme")]
		[HarmonyPostfix]
		private static void UpdateThemePostfix(GameManager __instance)
		{
			DataManager.BeatmapTheme beatmapTheme = __instance.LiveTheme;
			if (EditorManager.inst != null && EventEditor.inst.showTheme)
			{
				beatmapTheme = EventEditor.inst.previewTheme;
			}
			int num = 0;
			foreach (InputDataManager.CustomPlayer customPlayer in InputDataManager.inst.players)
			{
				if (customPlayer != null && customPlayer.GetRTPlayer() != null)
				{
					customPlayer.GetRTPlayer().SetColor(beatmapTheme.GetPlayerColor(num % 4), beatmapTheme.guiColor);
				}
				num++;
			}
		}

		[HarmonyPatch("QuitToArcade")]
		[HarmonyPostfix]
		private static void QuitToArcadePostfix()
        {
			PlayerPlugin.players.Clear();
		}
	}
}
