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

using RTFunctions.Functions;
using RTFunctions.Functions.Components.Player;
using RTFunctions.Functions.Data.Player;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;

namespace CreativePlayers
{
    [BepInPlugin("com.mecha.creativeplayers", "Creative Players", "2.4.0")]
	[BepInDependency("com.mecha.rtfunctions")]
	[BepInProcess("Project Arrhythmia.exe")]
	public class PlayerPlugin : BaseUnityPlugin
    {
        public static PlayerPlugin inst;
        public static string className = $"[<color=#FFD800>CreativePlayers</color>] {VersionNumber}\n";
        private readonly Harmony harmony = new Harmony("CreativePlayers");
		public static string VersionNumber => PluginInfo.PLUGIN_VERSION;

		public static int currentModelIndex = 0;

		public static PlayerModel CurrentModel(int index) 
		{
			var num = index % 4;

			if (PlayerManager.PlayerModelsIndex.ContainsKey(num) && PlayerManager.PlayerModels.ContainsKey(PlayerManager.PlayerModelsIndex[num]) && (!LoadFromGlobalPlayersInArcade.Value))
			{
				return PlayerManager.PlayerModels[PlayerManager.PlayerModelsIndex[num]];
			}
			else if (PlayerManager.PlayerIndexes.Count > num && PlayerManager.PlayerModels.ContainsKey(PlayerManager.PlayerIndexes[num].Value.ToString()) && LoadFromGlobalPlayersInArcade.Value)
			{
				return PlayerManager.PlayerModels[PlayerManager.PlayerIndexes[num].Value.ToString()];
			}
			else
			{
				return PlayerManager.PlayerModels["0"];
			}
		}

		public static Dictionary<string, AudioClip> OriginalSounds = new Dictionary<string, AudioClip>();

		public static ConfigEntry<bool> ZenModeInEditor { get; set; }
		public static ConfigEntry<bool> ZenEditorIncludesSolid { get; set; }
		public static ConfigEntry<bool> PlayerNameTags { get; set; }

		public static ConfigEntry<bool> LoadFromGlobalPlayersInArcade { get; set; }

		public static ConfigEntry<bool> PlaySoundB { get; set; }
		public static ConfigEntry<bool> PlaySoundR { get; set; }
		public static ConfigEntry<RTPlayer.TailUpdateMode> TailUpdateMode { get; set; }

		public static ConfigEntry<InputControlType> PlayerShootControl { get; set; }
		public static ConfigEntry<Key> PlayerShootKey { get; set; }
		public static ConfigEntry<bool> PlayerShootSound { get; set; }
		public static ConfigEntry<bool> AllowPlayersToTakeBulletDamage { get; set; }

		public static ConfigEntry<bool> AssetsGlobal { get; set; }

		public static ConfigEntry<bool> Debugger { get; set; }

		public static ConfigEntry<bool> EvaluateCode { get; set; }

        void Awake()
        {
            inst = this;

			Debugger = Config.Bind("Debug", "CreativePlayers Logs Enabled", false);

			PlayerNameTags = Config.Bind("Game", "Multiplayer NameTags", false, "If enabled and if there's more than one person playing, nametags will show which player is which (WIP).");
			AssetsGlobal = Config.Bind("Game", "Assets Global Source", false, "Assets will use BepInEx/plugins/Assets as the folder instead of the local level folder.");
			LoadFromGlobalPlayersInArcade = Config.Bind("Loading", "Always use global source", false, "Makes the player models always load from beatmaps/players for entering an arcade level. If disabled, players will be loaded from the local players.lspl file.");

			TailUpdateMode = Config.Bind("Player", "Tail Update Mode", RTPlayer.TailUpdateMode.FixedUpdate, "Changes the way the tail updates movement. FixedUpdate is recommended if the game gets laggy, but otherwise Update / LateUpdate is good for a smooth tail.");

			PlaySoundB = Config.Bind("Player", "Play Boost Sound", true, "Plays a little sound when you boost.");
			PlaySoundR = Config.Bind("Player", "Play Boost Recover Sound", false, "Plays a little sound when you can boost again.");

			ZenEditorIncludesSolid = Config.Bind("Player", "Editor Zen Mode includes Solid", false, "Makes Player ignore solid objects in editor.");

			PlayerShootControl = Config.Bind("Player", "Shoot Control", InputControlType.Action3, "Controller button to press to shoot. Requires restart if changed.");
			PlayerShootKey = Config.Bind("Player", "Shoot Key", Key.Z, "Keyboard key to press to shoot. Requires restart if changed.");
			PlayerShootSound = Config.Bind("Player", "Play Shoot Sound", true, "Plays a little sound when you shoot.");
			AllowPlayersToTakeBulletDamage = Config.Bind("Player", "Shots hurt other players", false, "Disable this if you don't want players to kill each other.");
			EvaluateCode = Config.Bind("Player", "Evaluate Code", false, ".cs files from the player folder in the level path will run. E.G. boost.cs will run when the player boosts. Each code includes a stored \"playerIndex\" variable in case you want to check which player is performing the action.");

			Config.SettingChanged += new EventHandler<SettingChangedEventArgs>(UpdateSettings);

			PlayerManager.PlayerIndexes.Add(Config.Bind("Loading", "Player 1 Model", "0", "The player uses this specific model ID."));
			PlayerManager.PlayerIndexes.Add(Config.Bind("Loading", "Player 2 Model", "0", "The player uses this specific model ID."));
			PlayerManager.PlayerIndexes.Add(Config.Bind("Loading", "Player 3 Model", "0", "The player uses this specific model ID."));
			PlayerManager.PlayerIndexes.Add(Config.Bind("Loading", "Player 4 Model", "0", "The player uses this specific model ID."));

			harmony.PatchAll();

            if (!ModCompatibility.mods.ContainsKey("CreativePlayers"))
            {
                var mod = new ModCompatibility.Mod(inst, GetType());
                mod.version = VersionNumber;

                ModCompatibility.mods.Add("CreativePlayers", mod);
            }

			PlayerManager.SaveLocalModels = SaveLocalModels;
			PlayerManager.LoadLocalModels = StartLoadingLocalModels;
			PlayerManager.CreateNewPlayerModel = CreateNewPlayerModel;
			PlayerManager.SaveGlobalModels = SavePlayerModels;
			PlayerManager.LoadGlobalModels = StartLoadingModels;

			PlayerManager.LoadIndexes = LoadIndexes;
			PlayerManager.ClearPlayerModels = ClearPlayerModels;
			PlayerManager.SetPlayerModel = SetPlayerModel;
			PlayerManager.DuplicatePlayerModel = DuplicatePlayerModel;

			SetConfigs();

			Logger.LogInfo("Plugin Creative Players is loaded!");
		}

		static void UpdateSettings(object sender, EventArgs e)
        {
			SetConfigs();
			PlayerManager.UpdatePlayers();
		}

		public static void SetConfigs()
		{
			RTPlayer.UpdateMode = TailUpdateMode.Value;
			RTPlayer.ShowNameTags = PlayerNameTags.Value;
			RTPlayer.AssetsGlobal = AssetsGlobal.Value;
			RTPlayer.PlayBoostSound = PlaySoundB.Value;
			RTPlayer.PlayBoostRecoverSound = PlaySoundR.Value;
			RTPlayer.ZenEditorIncludesSolid = ZenEditorIncludesSolid.Value;

			FaceController.ShootControl = PlayerShootControl.Value;
			FaceController.ShootKey = PlayerShootKey.Value;

			RTPlayer.PlayShootSound = PlayerShootSound.Value;
			RTPlayer.AllowPlayersToTakeBulletDamage = AllowPlayersToTakeBulletDamage.Value;
			RTPlayer.EvaluateCode = EvaluateCode.Value;

			PlayerManager.LoadFromGlobalPlayersInArcade = LoadFromGlobalPlayersInArcade.Value;
		}

		public static void SaveLocalModels()
		{
			string location = RTFile.BasePath + "players.lsb";

			var jn = JSON.Parse("{}");

			for (int i = 0; i < 4; i++)
			{
				jn["indexes"][i] = PlayerManager.PlayerModelsIndex[i];
			}

			if (PlayerManager.PlayerModels.Count > 2)
				for (int i = 2; i < PlayerManager.PlayerModels.Count; i++)
				{
					var current = PlayerManager.PlayerModels.ElementAt(i).Value;
					jn["models"][(i - 2).ToString()] = current.ToJSON();
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
			string location = RTFile.BasePath + "players.lsb";
            if (RTFile.FileExists(location))
			{
				var list = new List<string>();
				for (int i = 0; i < PlayerManager.PlayerModels.Count; i++)
				{
					if (!PlayerModel.DefaultModels.Any(x => x.basePart.id == PlayerManager.PlayerModels.ElementAt(i).Key))
					{
						list.Add(PlayerManager.PlayerModels.ElementAt(i).Key);
					}
				}

				foreach (var str in list)
				{
					PlayerManager.PlayerModels.Remove(str);
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
					PlayerManager.PlayerModelsIndex[i] = jn["indexes"][i];
                }

				for (int i = 0; i < jn["models"].Count; i++)
				{
					var model = PlayerModel.Parse(jn["models"][i]);
					string name = model.basePart.id;
					PlayerManager.PlayerModels.Add(name, model);
				}
			}
			else
            {
				for (int i = 0; i < PlayerManager.PlayerModelsIndex.Count; i++)
                {
					PlayerManager.PlayerModelsIndex[i] = "0";
                }
            }
			yield break;
		}

		public static void CreateNewPlayerModel()
		{
			var model = new PlayerModel(true);
			model.basePart.name = "New Model";

			PlayerManager.PlayerModels.Add((string)model.basePart.id, model);
		}

		public static void SavePlayerModels()
        {
			if (EditorManager.inst != null)
				EditorManager.inst.DisplayNotification("Saving Player Models...", 1f, EditorManager.NotificationType.Warning);
			foreach (var model in PlayerManager.PlayerModels)
			{
				if (!PlayerModel.DefaultModels.Any(x => x.basePart.id == model.Key))
                {
					RTFile.WriteToFile(RTFile.ApplicationDirectory + "beatmaps/players/" + model.Value.basePart.name, model.Value.ToJSON());
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
				var list = new List<string>();
				for (int i = 0; i < PlayerManager.PlayerModels.Count; i++)
				{
					if (!PlayerModel.DefaultModels.Any(x => x.basePart.id == PlayerManager.PlayerModels.ElementAt(i).Key))
					{
						list.Add(PlayerManager.PlayerModels.ElementAt(i).Key);

					}
                }

				foreach (var str in list)
                {
					PlayerManager.PlayerModels.Remove(str);
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

						var model = PlayerModel.Parse(JSON.Parse(RTFile.ReadFromFile(file)));
						string id = model.basePart.id;
						if (!PlayerManager.PlayerModels.ContainsKey(id))
						{
							PlayerManager.PlayerModels.Add(id, model);
						}
					}
				}

				if (EditorManager.inst == null && !LoadFromGlobalPlayersInArcade.Value)
					LoadIndexes();
				else if (LoadFromGlobalPlayersInArcade.Value)
                {
					for (int i = 0; i < PlayerManager.PlayerModelsIndex.Count; i++)
                    {
						PlayerManager.PlayerModelsIndex[i] = PlayerManager.PlayerIndexes[i].Value;
					}
                }
			}

			if (LoadFromGlobalPlayersInArcade.Value)
				PlayerManager.AssignPlayerModels();

			yield break;
        }

		public static void LoadIndexes()
		{
			inst.StartCoroutine(SoundLibraryPatch.SetAudioClips());
			string location = RTFile.BasePath + "players.lsb";

			if (RTFile.FileExists(location))
			{
				var json = FileManager.inst.LoadJSONFileRaw(location);
				var jn = JSON.Parse(json);

				for (int i = 0; i < jn["indexes"].Count; i++)
				{
					if (PlayerManager.PlayerModels.ContainsKey(jn["indexes"][i]))
					{
						PlayerManager.PlayerModelsIndex[i] = jn["indexes"][i];
						Debug.LogFormat("{0}Loaded PlayerModel Index: {1}", className, jn["indexes"][i]);
					}
					else
                    {
						Debug.LogErrorFormat("{0}Failed to load PlayerModel Index: {1}\nPlayer with that ID does not exist", className, jn["indexes"][i]);
					}
				}
			}
			//else if (!LoadFromGlobalPlayersInArcade.Value && EditorManager.inst == null)
			else if (!LoadFromGlobalPlayersInArcade.Value)
			{
				Debug.LogErrorFormat("{0}player.lspl file does not exist:, setting to default player", className);
				for (int i = 0; i < PlayerManager.PlayerModelsIndex.Count; i++)
                {
					PlayerManager.PlayerModelsIndex[i] = "0";
                }
            }

			PlayerManager.AssignPlayerModels();
		}

		public static void ClearPlayerModels()
		{
			if (PlayerManager.PlayerModels.Count > 2)
			{
				for (int i = 2; i < PlayerManager.PlayerModels.Count; i++)
				{
					PlayerManager.PlayerModels.Remove(PlayerManager.PlayerModels.ElementAt(i).Key);
				}

				var list = new List<string>();
				foreach (var keyValue in PlayerManager.PlayerModels)
				{
					var key = keyValue.Key;
					if (!PlayerModel.DefaultModels.Any(x => x.basePart.id == key))
					{
						list.Add(keyValue.Key);
                    }
                }

				foreach (var key in list)
                {
					PlayerManager.PlayerModels.Remove(key);
				}
			}

			StartLoadingModels();
		}

		public static void SetPlayerModel(int index, string id)
        {
			if (PlayerManager.PlayerModels.ContainsKey(id))
            {
				PlayerManager.PlayerModelsIndex[index] = id;
				if (PlayerManager.Players.Count > index && PlayerManager.Players[index])
				{
					PlayerManager.Players[index].CurrentPlayerModel = id;
					PlayerManager.Players[index].Player?.UpdatePlayer();
				}
			}
        }

		public static void DuplicatePlayerModel(string id)
        {
			if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/players"))
			{
				Directory.CreateDirectory(RTFile.ApplicationDirectory + "beatmaps/players");
			}

			var files = Directory.GetFiles(RTFile.ApplicationDirectory + "beatmaps/players");

			if (files.Length > 0)
			{
				foreach (var file in files)
				{
					if (Path.GetFileName(file).Contains(".lspl") && Path.GetFileName(file) != "regular.lspl" && Path.GetFileName(file) != "circle.lspl")
					{
						if (RTFile.FileExists(file))
						{
							string json = FileManager.inst.LoadJSONFileRaw(file);
							var jn = JSON.Parse(json);

							if ((string)jn["base"]["id"] == id)
							{
								var model = PlayerModel.Parse(jn);

								var filePath = file.Replace(".lspl", "_clone.lspl");

								model.basePart.name += " Clone";
								model.basePart.id = LSFunctions.LSText.randomNumString(16);

								RTFile.WriteToFile(filePath, model.ToJSON());
							}
						}
					}
				}
			}

			StartLoadingModels();
		}
	}
}
