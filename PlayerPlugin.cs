using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.IO;

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;
using InControl;

using SimpleJSON;

using CreativePlayers.Patchers;
using CreativePlayers.Functions;
using CreativePlayers.Functions.Data;

namespace CreativePlayers
{
    [BepInPlugin("com.mecha.creativeplayers", "Creative Players", "1.0.2")]
	[BepInIncompatibility("com.mecha.playereditor")]
    public class PlayerPlugin : BaseUnityPlugin
    {
        public static PlayerPlugin inst;
        public static string className = "[<color=#FFD800>CreativePlayers</color>]\n";
        public static Harmony harmony = new Harmony("CreativePlayers");

		public static List<RTPlayer> players = new List<RTPlayer>();

		public static int currentModelIndex = 0;

		public static Dictionary<int, string> playerModelsIndex = new Dictionary<int, string>();

		public static Dictionary<string, PlayerModelClass.PlayerModel> playerModels = new Dictionary<string, PlayerModelClass.PlayerModel>();

		public static ConfigEntry<bool> zenModeInEditor { get; set; }

        private void Awake()
        {
            inst = this;
            // Plugin startup logic
            Logger.LogInfo("Plugin Creative Players is loaded!");

			zenModeInEditor = Config.Bind("Game", "Zen Mode", false, "If enabled, the player will not take damage in the editor.");

			harmony.PatchAll(typeof(PlayerPlugin));
            harmony.PatchAll(typeof(InputDataManagerPatch));
            harmony.PatchAll(typeof(GameManagerPatch));
            harmony.PatchAll(typeof(CustomPlayerPatch));
            harmony.PatchAll(typeof(EditorManagerPatch));
            harmony.PatchAll(typeof(OnTriggerEnterPassPatch));
            harmony.PatchAll(typeof(PlayerPatch));

			GameObject spr = new GameObject("SpriteManager for Player");
			DontDestroyOnLoad(spr);
			spr.AddComponent<SpriteManager>();

			playerModelsIndex.Add(0, "0");
			playerModelsIndex.Add(1, "0");
			playerModelsIndex.Add(2, "0");
			playerModelsIndex.Add(3, "0");
		}

		[HarmonyPatch(typeof(InputDataManager), "AlivePlayers", MethodType.Getter)]
		[HarmonyPrefix]
		public static bool GetAlivePlayers(InputDataManager __instance)
        {
			__instance.GetType().GetProperty("AlivePlayers").SetValue(__instance, __instance.players.FindAll(x => x.GetRTPlayer().PlayerAlive));
			return false;
        }

		public static RTPlayer AssignPlayer(InputDataManager.CustomPlayer _player, Vector3 _pos, Color _col)
		{
			Debug.Log("Creating New Player");

			GameObject objAssign;
			if (playerModels.ContainsKey(playerModelsIndex[_player.index]) && playerModels[playerModelsIndex[_player.index]].gm != null)
			{
				objAssign = playerModels[playerModelsIndex[_player.index]].gm;
			}
			else
            {
				objAssign = playerModels["0"].gm;
            }

			GameObject gameObject = Instantiate(objAssign, new Vector3(0, 0, -6f), Quaternion.identity);
			gameObject.layer = 8;
			gameObject.name = "Player " + (_player.index + 1);
			gameObject.SetActive(true);

			if (gameObject.GetComponent<Player>())
            {
				Destroy(gameObject.GetComponent<Player>());
            }
			if (gameObject.GetComponentInChildren<PlayerTrail>())
            {
				Destroy(gameObject.GetComponentInChildren<PlayerTrail>());
            }

			if (!gameObject.GetComponent<RTPlayer>())
            {
				gameObject.AddComponent<RTPlayer>();
            }

			var player = gameObject.GetComponent<RTPlayer>();

			if (_player.device == null)
            {
				player.Actions = (MyGameActions)AccessTools.Field(typeof(InputDataManager), "keyboardListener").GetValue(InputDataManager.inst);
			}
			else
			{
				MyGameActions myGameActions = MyGameActions.CreateWithJoystickBindings();
				myGameActions.Device = _player.device;
				player.Actions = myGameActions;
			}

			_player.active = true;
			player.playerIndex = _player.index;
			Debug.LogFormat("{0}Created new player [{1}]", className, player.playerIndex);
			return player;
		}

		public static void StartRespawnPlayers()
        {
			inst.StartCoroutine(RespawnPlayers());
        }

		public static IEnumerator RespawnPlayers()
        {
			foreach (var player in players)
            {
				Destroy(player.gameObject);
            }
			players.Clear();
			yield return new WaitForSeconds(0.1f);

			GameManager.inst.SpawnPlayers(Vector3.zero);
			yield break;
        }

		public static void SaveLocalModels()
		{
			string location = RTFile.GetApplicationDirectory() + RTExtensions.basePath + "players.lsb";

			var jn = JSON.Parse("{}");

			for (int i = 0; i < 4; i++)
			{
				jn["indexes"][i] = playerModelsIndex[i];
			}

			if (playerModels.Count > 2)
				for (int i = 2; i < playerModels.Count; i++)
				{
					var current = playerModels.ElementAt(i).Value;

					jn["models"].Add((i - 2).ToString(), PlayerData.SavePlayer(current));
				}

			RTFile.WriteToFile(location, jn.ToString());
		}

		public static void StartLoadingLocalModels()
        {
			inst.StartCoroutine(LoadLocalModels());
        }

		public static IEnumerator LoadLocalModels()
        {
			string location = RTFile.GetApplicationDirectory() + RTExtensions.basePath + "players.lsb";
            if (RTFile.FileExists(location))
			{
				for (int i = 0; i < playerModels.Count; i++)
				{
					if (playerModels.ElementAt(i).Key != "0" && playerModels.ElementAt(i).Key != "1")
					{
						Destroy(playerModels.ElementAt(i).Value.gm);
						playerModels.Remove(playerModels.ElementAt(i).Key);
					}
				}

				for (int i = 0; i < GameManager.inst.PlayerPrefabs.Length; i++)
				{
					if (GameManager.inst.PlayerPrefabs[i].name.Contains("Clone"))
					{
						Destroy(GameManager.inst.PlayerPrefabs[i]);
					}
				}

				var json = FileManager.inst.LoadJSONFileRaw(location);
				var jn = JSON.Parse(json);

				for (int i = 0; i < jn["indexes"].Count; i++)
                {
					playerModelsIndex[i] = jn["indexes"][i];
                }

				for (int i = 0; i < jn["models"].Count; i++)
				{
					var model = PlayerData.LoadPlayer(jn["models"][i]);
					string name = (string)model.values["Base ID"];
					playerModels.Add(name, model);

					var newPrefab = Instantiate(GameManager.inst.PlayerPrefabs[0]);
					newPrefab.SetActive(false);
					GameManager.inst.PlayerPrefabs.AddItem(newPrefab);

					model.gm = newPrefab;
				}

				InputDataManager.inst.PlayerPrefabs = GameManager.inst.PlayerPrefabs;
			}
			else
            {
				for (int i = 0; i < playerModelsIndex.Count; i++)
                {
					playerModelsIndex[i] = "0";
                }
            }
			yield break;
		}

		public static void CreateNewPlayerModel()
		{
			var newPrefab = Instantiate(GameManager.inst.PlayerPrefabs[0]);
			var model = new PlayerModelClass.PlayerModel(newPrefab);
			model.values["Base Name"] = "New Model";

			playerModels.Add((string)model.values["Base ID"], model);

			newPrefab.SetActive(false);
			GameManager.inst.PlayerPrefabs.AddItem(newPrefab);
			model.gm = newPrefab;
		}

		public static void SavePlayerModels()
        {
			foreach (var model in playerModels)
            {
				if (model.Key != "0" && model.Key != "1")
                {
					PlayerData.SavePlayer(model.Value, (string)model.Value.values["Base Name"]);
                }
            }
        }

		public static void StartLoadingModels()
        {
			inst.StartCoroutine(LoadPlayerModels());
        }

		public static IEnumerator LoadPlayerModels()
		{
			var files = Directory.GetFiles(RTFile.GetApplicationDirectory() + "beatmaps/players");

			if (files.Length > 0)
			{
				for (int i = 0; i < playerModels.Count; i++)
				{
					if (playerModels.ElementAt(i).Key != "0" && playerModels.ElementAt(i).Key != "1")
					{
						Destroy(playerModels.ElementAt(i).Value.gm);
						playerModels.Remove(playerModels.ElementAt(i).Key);
					}
				}

				for (int i = 0; i < GameManager.inst.PlayerPrefabs.Length; i++)
				{
					if (GameManager.inst.PlayerPrefabs[i].name.Contains("Clone"))
					{
						Destroy(GameManager.inst.PlayerPrefabs[i]);
					}
				}

				foreach (var file in files)
				{
					if (Path.GetFileName(file).Contains(".lspl") && Path.GetFileName(file) != "regular.lspl" && Path.GetFileName(file) != "circle.lspl")
					{
						var filename = Path.GetFileName(file).Replace(".lspl", "");

						var model = PlayerData.LoadPlayer(filename);
						string name = (string)model.values["Base ID"];
						playerModels.Add(name, model);

						var newPrefab = Instantiate(GameManager.inst.PlayerPrefabs[0]);
						newPrefab.SetActive(false);
						GameManager.inst.PlayerPrefabs.AddItem(newPrefab);
						model.gm = newPrefab;
					}
				}

				InputDataManager.inst.PlayerPrefabs = GameManager.inst.PlayerPrefabs;
			}

			yield break;
        }

		public static void LoadIndexes()
        {
			string location = RTFile.GetApplicationDirectory() + RTExtensions.basePath + "players.lsb";

			if (RTFile.FileExists(location))
			{
				var json = FileManager.inst.LoadJSONFileRaw(location);
				var jn = JSON.Parse(json);

				for (int i = 0; i < jn["indexes"].Count; i++)
				{
					if (playerModels.ContainsKey(jn["indexes"][i]))
					{
						playerModelsIndex[i] = jn["indexes"][i];
						Debug.LogFormat("{0}Loaded PlayerModel Index: {1}", className, jn["indexes"][i]);
					}
					else
                    {
						Debug.LogErrorFormat("{0}Failed to load PlayerModel Index: {1}\nPlayer with that ID does not exist", className, jn["indexes"][i]);
					}
				}
			}
		}
	}
}
