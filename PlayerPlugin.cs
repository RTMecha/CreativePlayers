using System;
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

using RTFunctions.Functions;

namespace CreativePlayers
{
    [BepInPlugin("com.mecha.creativeplayers", "Creative Players", "1.1.8")]
	[BepInIncompatibility("com.mecha.playereditor")]
	[BepInDependency("com.mecha.rtfunctions")]
	[BepInProcess("Project Arrhythmia.exe")]
	public class PlayerPlugin : BaseUnityPlugin
    {
		//Updates:

		public static string VersionNumber
        {
			get
            {
				return PluginInfo.PLUGIN_VERSION;
            }
        }

        public static PlayerPlugin inst;
        public static string className = "[<color=#FFD800>CreativePlayers</color>] " + PluginInfo.PLUGIN_VERSION + "\n";
        private readonly Harmony harmony = new Harmony("CreativePlayers");

		public static List<RTPlayer> players = new List<RTPlayer>();

		public static int currentModelIndex = 0;

		public static Dictionary<int, string> playerModelsIndex = new Dictionary<int, string>();

		public static Dictionary<string, PlayerModelClass.PlayerModel> playerModels = new Dictionary<string, PlayerModelClass.PlayerModel>();

		public static GameObject healthImages;
		public static Transform healthParent;
		public static Sprite healthSprite;

		public static PlayerModelClass.PlayerModel CurrentModel(int index) 
		{
			var num = index % 4;

			if (playerModelsIndex.ContainsKey(num) && playerModels.ContainsKey(playerModelsIndex[num]) && playerModels[playerModelsIndex[num]].gm != null && (!LoadFromGlobalPlayersInArcade.Value))
			{
				return playerModels[playerModelsIndex[num]];
			}
			else if (PlayerIndexes.Count > num && playerModels.ContainsKey(PlayerIndexes[num].Value.ToString()) && playerModels[PlayerIndexes[num].Value.ToString()].gm != null && LoadFromGlobalPlayersInArcade.Value)
			{
				return playerModels[PlayerIndexes[num].Value.ToString()];
			}
			else
			{
				return playerModels["0"];
			}

			//return playerModels[playerModelsIndex[index % 4]];
		}

		public static Dictionary<string, AudioClip> OriginalSounds = new Dictionary<string, AudioClip>();

		public static ConfigEntry<bool> ZenModeInEditor { get; set; }
		public static ConfigEntry<bool> ZenEditorIncludesSolid { get; set; }
		public static ConfigEntry<bool> PlayerNameTags { get; set; }

		public static ConfigEntry<bool> LoadFromGlobalPlayersInArcade { get; set; }

		public static ConfigEntry<string> Player1Index { get; set; }
		public static ConfigEntry<string> Player2Index { get; set; }
		public static ConfigEntry<string> Player3Index { get; set; }
		public static ConfigEntry<string> Player4Index { get; set; }

		public static List<ConfigEntry<string>> PlayerIndexes = new List<ConfigEntry<string>>();

		public static ConfigEntry<bool> PlaySoundB { get; set; }
		public static ConfigEntry<bool> PlaySoundR { get; set; }
		public static ConfigEntry<RTPlayer.TailUpdateMode> TailUpdateMode { get; set; }

        private void Awake()
        {
            inst = this;
            // Plugin startup logic
            Logger.LogInfo("Plugin Creative Players is loaded!");

			ZenModeInEditor = Config.Bind("Game", "Zen Mode", false, "If enabled, the player will not take damage in the editor.");
			PlayerNameTags = Config.Bind("Game", "Multiplayer NameTags", false, "If enabled and if there's more than one person playing, nametags will show which player is which (WIP).");
			LoadFromGlobalPlayersInArcade = Config.Bind("Loading", "Always use global source", false, "Makes the player models always load from beatmaps/players for entering an arcade level. If disabled, players will be loaded from the local players.lspl file.");

			TailUpdateMode = Config.Bind("Player", "Tail Update Mode", RTPlayer.TailUpdateMode.FixedUpdate, "Changes the way the tail updates movement. FixedUpdate is recommended if the game gets laggy, but otherwise Update / LateUpdate is good for a smooth tail.");

			PlaySoundB = Config.Bind("Player", "Play Boost Sound", true, "Plays a little sound when you boost.");
			PlaySoundR = Config.Bind("Player", "Play Boost Recover Sound", false, "Plays a little sound when you can boost again.");

			ZenEditorIncludesSolid = Config.Bind("Player", "Editor Zen Mode includes Solid", false, "Makes Player ignore solid objects in editor.");

			Player1Index = Config.Bind("Loading", "Player 1 Model", "0", "The player uses this specific model ID.");
			Player2Index = Config.Bind("Loading", "Player 2 Model", "0", "The player uses this specific model ID.");
			Player3Index = Config.Bind("Loading", "Player 3 Model", "0", "The player uses this specific model ID.");
			Player4Index = Config.Bind("Loading", "Player 4 Model", "0", "The player uses this specific model ID.");

			Config.SettingChanged += new EventHandler<SettingChangedEventArgs>(UpdateSettings);

			PlayerIndexes.Add(Player1Index);
			PlayerIndexes.Add(Player2Index);
			PlayerIndexes.Add(Player3Index);
			PlayerIndexes.Add(Player4Index);

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
		private static void UpdateSettings(object sender, EventArgs e)
        {
			if (players.Count > 0)
            {
				foreach (var player in players)
                {
					player.updatePlayer();
				}
            }
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

			GameObject objAssign = CurrentModel(_player.index % 4).gm;

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

			player.playerIndex = _player.index;
			player.updateMode = TailUpdateMode.Value;

			player.updatePlayer();

			if (_player.device == null)
            {
				player.Actions = (MyGameActions)AccessTools.Field(typeof(InputDataManager), "keyboardListener").GetValue(InputDataManager.inst);
				player.isKeyboard = true;

				if (EditorManager.inst != null && InputDataManager.inst.players.Count == 1)
                {
					player.faceController = FaceController.CreateWithBothBindings();
                }
				else
				{
					player.faceController = FaceController.CreateWithKeyboardBindings();
				}
			}
			else
			{
				MyGameActions myGameActions = MyGameActions.CreateWithJoystickBindings();
				myGameActions.Device = _player.device;
				player.Actions = myGameActions;
				player.isKeyboard = false;

				var faceController = FaceController.CreateWithJoystickBindings();
				faceController.Device = _player.device;
				player.faceController = faceController;
			}

			_player.active = true;
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
				Destroy(player.health);
				Destroy(player.gameObject);
            }
			players.Clear();
			yield return new WaitForSeconds(0.1f);

			GameManager.inst.SpawnPlayers(Vector3.zero);
			yield break;
        }

		public static IEnumerator RespawnPlayer(int index)
		{
			Destroy(players[index].health);
			Destroy(players[index].gameObject);

			players.RemoveAt(index);

			yield return new WaitForSeconds(0.1f);

            var nextIndex = DataManager.inst.gameData.beatmapData.checkpoints.FindIndex(x => x.time > AudioManager.inst.CurrentAudioSource.time);
			var prevIndex = nextIndex - 1;
			if (prevIndex < 0)
				prevIndex = 0;

            if (DataManager.inst.gameData.beatmapData.checkpoints.Count > prevIndex && DataManager.inst.gameData.beatmapData.checkpoints[prevIndex] != null)
				GameManager.inst.SpawnPlayers(DataManager.inst.gameData.beatmapData.checkpoints[prevIndex].pos);
			else
				GameManager.inst.SpawnPlayers(Vector3.zero);
			yield break;
		}

		public static void SaveLocalModels()
		{
			string location = RTFile.ApplicationDirectory + RTFile.basePath + "players.lsb";

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
			if (!LoadFromGlobalPlayersInArcade.Value)
				inst.StartCoroutine(LoadLocalModels());
			else
				inst.StartCoroutine(LoadPlayerModels());
        }

		public static IEnumerator LoadLocalModels()
        {
			string location = RTFile.ApplicationDirectory + RTFile.basePath + "players.lsb";
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
			if (EditorManager.inst != null)
				EditorManager.inst.DisplayNotification("Saving Player Models...", 1f, EditorManager.NotificationType.Warning);
			foreach (var model in playerModels)
            {
				if (model.Key != "0" && model.Key != "1")
                {
					PlayerData.SavePlayer(model.Value, (string)model.Value.values["Base Name"]);
                }
			}
			if (EditorManager.inst != null)
				EditorManager.inst.DisplayNotification("Saved Player Models!", 1f, EditorManager.NotificationType.Success);
		}

		public static void StartLoadingModels()
        {
			inst.StartCoroutine(LoadPlayerModels());
        }

		public static IEnumerator LoadPlayerModels()
		{
			if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/players"))
            {
				Directory.CreateDirectory(RTFile.ApplicationDirectory + "beatmaps/players");
            }

			var files = Directory.GetFiles(RTFile.ApplicationDirectory + "beatmaps/players");

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
						model.filePath = file;
						playerModels.Add(name, model);

						var newPrefab = Instantiate(GameManager.inst.PlayerPrefabs[0]);
						newPrefab.SetActive(false);
						GameManager.inst.PlayerPrefabs.AddItem(newPrefab);
						model.gm = newPrefab;
					}
				}

				InputDataManager.inst.PlayerPrefabs = GameManager.inst.PlayerPrefabs;

				if (EditorManager.inst == null && !LoadFromGlobalPlayersInArcade.Value)
					LoadIndexes();
				else if (LoadFromGlobalPlayersInArcade.Value)
                {
					playerModelsIndex[0] = Player1Index.Value;
					playerModelsIndex[1] = Player2Index.Value;
					playerModelsIndex[2] = Player3Index.Value;
					playerModelsIndex[3] = Player4Index.Value;
                }
			}

			yield break;
        }

		public static void LoadIndexes()
        {
			string location = RTFile.ApplicationDirectory + RTFile.basePath + "players.lsb";

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
			else if (!LoadFromGlobalPlayersInArcade.Value && EditorManager.inst == null)
			{
				Debug.LogErrorFormat("{0}player.lspl file does not exist:, setting to default player", className);
				for (int i = 0; i < playerModelsIndex.Count; i++)
                {
					playerModelsIndex[i] = "0";
                }
            }
		}

		public static void ClearPlayerModels()
		{
			if (playerModels.Count > 2)
			{
				for (int i = 2; i < playerModels.Count; i++)
				{
					playerModels.Remove(playerModels.ElementAt(i).Key);
				}

				foreach (var keyValue in playerModels)
                {
					if (keyValue.Key != "0" && keyValue.Key != "1")
                    {
						var key = keyValue.Key;
						var model = keyValue.Value;

						Destroy(model.gm);
						playerModels.Remove(key);
                    }
                }
			}

			StartLoadingModels();
		}

		public static void SetPlayerModel(int index, string id)
        {
			if (playerModels.ContainsKey(id))
            {
				playerModelsIndex[index] = id;
				inst.StartCoroutine(RespawnPlayer(index));
			}
        }

		public static DataManager.GameData.BeatmapData.Checkpoint GetClosestIndex(GameManager __instance, List<DataManager.GameData.BeatmapData.Checkpoint> checkpoints, float time)
        {
			return (DataManager.GameData.BeatmapData.Checkpoint)__instance.GetType().GetMethod("GetClosestIndex", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { checkpoints, time });
        }

		public static IEnumerator ReverseToCheckpointLoop(GameManager __instance)
		{
			if (!__instance.isReversing)
			{
				__instance.playingCheckpointAnimation = true;
				__instance.isReversing = true;
				DataManager.GameData.BeatmapData.Checkpoint checkpoint = GetClosestIndex(__instance, DataManager.inst.gameData.beatmapData.checkpoints, AudioManager.inst.CurrentAudioSource.time);
				Debug.Log(string.Concat(new object[]
				{
					"Debug Checkpoint: ",
					checkpoint.time,
					" = ",
					AudioManager.inst.CurrentAudioSource.time
				}));
				AudioManager.inst.SetPitch(-1.5f);
				float time = AudioManager.inst.CurrentAudioSource.time;
				float seconds = 2f;
				AudioManager.inst.PlaySound("rewind");
				//if (GameManager.UpdatedAudioPos != null)
				//{
				//	GameManager.UpdatedAudioPos(AudioManager.inst.CurrentAudioSource.isPlaying, AudioManager.inst.CurrentAudioSource.time, AudioManager.inst.CurrentAudioSource.pitch);
				//}
				DataManager.inst.gameData.beatmapData.GetWhichCheckpointBasedOnTime(AudioManager.inst.CurrentAudioSource.time);
				yield return new WaitForSeconds(seconds);
				float time2 = Mathf.Clamp(checkpoint.time + 0.01f, 0.1f, AudioManager.inst.CurrentAudioSource.clip.length);

				if (EditorManager.inst == null && (DataManager.inst.GetSettingInt("ArcadeDifficulty", 0) == 2 || DataManager.inst.GetSettingInt("ArcadeDifficulty", 0) == 3))
				{
					time2 = 0.1f;
				}
				AudioManager.inst.CurrentAudioSource.time = time2;
				__instance.gameState = GameManager.State.Playing;
				AudioManager.inst.CurrentAudioSource.Play();
				AudioManager.inst.SetPitch(__instance.getPitch());
				//foreach (DataManager.GameData.BeatmapObject beatmapObject in DataManager.inst.gameData.beatmapObjects)
				//{
				//	if (ObjectManager.inst.beatmapGameObjects.ContainsKey(beatmapObject.id))
				//	{
				//		ObjectManager.GameObjectRef gameObjectRef = ObjectManager.inst.beatmapGameObjects[beatmapObject.id];
				//		gameObjectRef.sequence.all.Goto(AudioManager.inst.CurrentAudioSource.time, false);
				//		gameObjectRef.sequence.col.Goto(AudioManager.inst.CurrentAudioSource.time, false);
				//	}
				//}

				__instance.UpdateEventSequenceTime();
				__instance.isReversing = false;
				yield return new WaitForSeconds(0.1f);

				//if (GameManager.UpdatedAudioPos != null)
				//{
				//	GameManager.UpdatedAudioPos(AudioManager.inst.CurrentAudioSource.isPlaying, AudioManager.inst.CurrentAudioSource.time, AudioManager.inst.CurrentAudioSource.pitch);
				//}

				__instance.SpawnPlayers(checkpoint.pos);
				__instance.playingCheckpointAnimation = false;
				checkpoint = null;
			}
			yield break;
		}
	}
}
