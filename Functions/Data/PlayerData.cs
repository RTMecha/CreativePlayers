using System.IO;
using System.Linq;
using System.Collections.Generic;

using SimpleJSON;
using UnityEngine;

namespace CreativePlayers.Functions.Data
{
    public class PlayerData : MonoBehaviour
    {
        public static void SavePlayer(PlayerModelClass.PlayerModel _model, string _name, string _path = "")
        {
            string path = _path;
            if (path == "")
            {
                if (!RTFile.DirectoryExists(RTFile.GetApplicationDirectory() + "beatmaps/players"))
                {
                    Directory.CreateDirectory(RTFile.GetApplicationDirectory() + "beatmaps/players");
                }
                path = RTFile.GetApplicationDirectory() + "beatmaps/players/" + _name.ToLower().Replace(" ", "_") + ".lspl";
            }

            Debug.LogFormat("{0}Saving {1} to {2}", PlayerPlugin.className, _name, path);

            JSONNode jn = null;

            if (RTFile.FileExists(path))
            {
                string json = FileManager.inst.LoadJSONFileRaw(path);
                jn = SavePlayer(_model, JSON.Parse(json));
            }
            else
            {
                jn = SavePlayer(_model, JSON.Parse("{}"));
            }

            RTFile.WriteToFile(path, jn.ToString(3));
        }

        public static JSONNode SavePlayer(PlayerModelClass.PlayerModel _model, JSONNode _jn = null)
        {
            JSONNode jn = null;
            if (jn != null)
            {
                jn = _jn;
            }
            else
            {
                jn = JSON.Parse("{}");
            }

            jn["base"]["name"] = (string)_model.values["Base Name"];
            jn["base"]["id"] = (string)_model.values["Base ID"];
            jn["base"]["health"] = ((int)_model.values["Base Health"]).ToString();

            #region Head
            Debug.LogFormat("{0}Saving Head Shape", PlayerPlugin.className);
            if (((Vector2Int)_model.values["Head Shape"]).x != 0)
                jn["head"]["s"] = ((Vector2Int)_model.values["Head Shape"]).x.ToString();
            if (((Vector2Int)_model.values["Head Shape"]).y != 0)
                jn["head"]["so"] = ((Vector2Int)_model.values["Head Shape"]).y.ToString();
            Debug.LogFormat("{0}Saving Head Position", PlayerPlugin.className);
            jn["head"]["pos"]["x"] = ((Vector2)_model.values["Head Position"]).x.ToString();
            jn["head"]["pos"]["y"] = ((Vector2)_model.values["Head Position"]).y.ToString();
            Debug.LogFormat("{0}Saving Head Scale", PlayerPlugin.className);
            jn["head"]["sca"]["x"] = ((Vector2)_model.values["Head Scale"]).x.ToString();
            jn["head"]["sca"]["y"] = ((Vector2)_model.values["Head Scale"]).y.ToString();
            Debug.LogFormat("{0}Saving Head Rotation", PlayerPlugin.className);
            jn["head"]["rot"]["x"] = ((float)_model.values["Head Rotation"]).ToString();
            #endregion

            #region Head Trail
            Debug.LogFormat("{0}Saving Head Trail Emitting", PlayerPlugin.className);
            jn["head"]["trail"]["em"] = ((bool)_model.values["Head Trail Emitting"]).ToString();
            Debug.LogFormat("{0}Saving Head Trail Time", PlayerPlugin.className);
            jn["head"]["trail"]["t"] = ((float)_model.values["Head Trail Time"]).ToString();
            Debug.LogFormat("{0}Saving Head Trail Start Width", PlayerPlugin.className);
            jn["head"]["trail"]["w"]["start"] = ((float)_model.values["Head Trail Start Width"]).ToString();
            Debug.LogFormat("{0}Saving Head Trail End Width", PlayerPlugin.className);
            jn["head"]["trail"]["w"]["end"] = ((float)_model.values["Head Trail End Width"]).ToString();
            Debug.LogFormat("{0}Saving Head Trail Start Color", PlayerPlugin.className);
            jn["head"]["trail"]["c"]["start"] = ((int)_model.values["Head Trail Start Color"]).ToString();
            Debug.LogFormat("{0}Saving Head Trail End Color", PlayerPlugin.className);
            jn["head"]["trail"]["c"]["end"] = ((int)_model.values["Head Trail End Color"]).ToString();
            Debug.LogFormat("{0}Saving Head Trail Start Opacity", PlayerPlugin.className);
            jn["head"]["trail"]["o"]["start"] = ((float)_model.values["Head Trail Start Opacity"]).ToString();
            Debug.LogFormat("{0}Saving Head Trail End Opacity", PlayerPlugin.className);
            jn["head"]["trail"]["o"]["end"] = ((float)_model.values["Head Trail End Opacity"]).ToString();
            Debug.LogFormat("{0}Saving Head Trail Position Offset", PlayerPlugin.className);
            jn["head"]["trail"]["pos"]["x"] = ((Vector2)_model.values["Head Trail Position Offset"]).x;
            jn["head"]["trail"]["pos"]["y"] = ((Vector2)_model.values["Head Trail Position Offset"]).y;
            #endregion

            #region Head Particles
            jn["head"]["particles"]["em"] = ((bool)_model.values["Head Particles Emitting"]).ToString();
            if (((Vector2Int)_model.values["Head Particles Shape"]).x != 0)
                jn["head"]["particles"]["s"] = ((Vector2Int)_model.values["Head Particles Shape"]).x.ToString();
            if (((Vector2Int)_model.values["Head Particles Shape"]).y != 0)
                jn["head"]["particles"]["so"] = ((Vector2Int)_model.values["Head Particles Shape"]).y.ToString();
            jn["head"]["particles"]["col"] = ((int)_model.values["Head Particles Color"]).ToString();
            jn["head"]["particles"]["opa"]["start"] = ((float)_model.values["Head Particles Start Opacity"]).ToString();
            jn["head"]["particles"]["opa"]["end"] = ((float)_model.values["Head Particles End Opacity"]).ToString();
            jn["head"]["particles"]["sca"]["start"] = ((float)_model.values["Head Particles Start Scale"]).ToString();
            jn["head"]["particles"]["sca"]["end"] = ((float)_model.values["Head Particles End Scale"]).ToString();
            jn["head"]["particles"]["rot"] = ((float)_model.values["Head Particles Rotation"]).ToString();
            jn["head"]["particles"]["lt"] = ((float)_model.values["Head Particles Lifetime"]).ToString();
            jn["head"]["particles"]["sp"] = ((float)_model.values["Head Particles Speed"]).ToString();
            jn["head"]["particles"]["am"] = ((float)_model.values["Head Particles Amount"]).ToString();
            jn["head"]["particles"]["frc"]["x"] = ((Vector2)_model.values["Head Particles Force"]).x.ToString();
            jn["head"]["particles"]["frc"]["y"] = ((Vector2)_model.values["Head Particles Force"]).y.ToString();
            jn["head"]["particles"]["trem"] = ((bool)_model.values["Head Particles Trail Emitting"]).ToString();
            #endregion

            #region Boost
            Debug.LogFormat("{0}Saving Boost Active", PlayerPlugin.className);
            jn["boost"]["active"] = ((bool)_model.values["Boost Active"]).ToString();
            Debug.LogFormat("{0}Saving Boost Shape", PlayerPlugin.className);
            if (((Vector2Int)_model.values["Boost Shape"]).x != 0)
                jn["boost"]["s"] = ((Vector2Int)_model.values["Boost Shape"]).x.ToString();
            if (((Vector2Int)_model.values["Boost Shape"]).y != 0)
                jn["boost"]["so"] = ((Vector2Int)_model.values["Boost Shape"]).y.ToString();
            Debug.LogFormat("{0}Saving Boost Position", PlayerPlugin.className);
            jn["boost"]["pos"]["x"] = ((Vector2)_model.values["Boost Position"]).x.ToString();
            jn["boost"]["pos"]["y"] = ((Vector2)_model.values["Boost Position"]).y.ToString();
            Debug.LogFormat("{0}Saving Boost Scale", PlayerPlugin.className);
            jn["boost"]["sca"]["x"] = ((Vector2)_model.values["Boost Scale"]).x.ToString();
            jn["boost"]["sca"]["y"] = ((Vector2)_model.values["Boost Scale"]).y.ToString();
            Debug.LogFormat("{0}Saving Boost Rotation", PlayerPlugin.className);
            jn["boost"]["rot"]["x"] = ((float)_model.values["Boost Rotation"]).ToString();
            #endregion

            #region Boost Trail
            Debug.LogFormat("{0}Saving Boost Trail Emitting", PlayerPlugin.className);
            jn["boost"]["trail"]["em"] = ((bool)_model.values["Boost Trail Emitting"]).ToString();
            Debug.LogFormat("{0}Saving Boost Trail Time", PlayerPlugin.className);
            jn["boost"]["trail"]["t"] = ((float)_model.values["Boost Trail Time"]).ToString();
            Debug.LogFormat("{0}Saving Boost Trail Start Width", PlayerPlugin.className);
            jn["boost"]["trail"]["w"]["start"] = ((float)_model.values["Boost Trail Start Width"]).ToString();
            Debug.LogFormat("{0}Saving Boost Trail End Width", PlayerPlugin.className);
            jn["boost"]["trail"]["w"]["end"] = ((float)_model.values["Boost Trail End Width"]).ToString();
            Debug.LogFormat("{0}Saving Boost Trail Start Color", PlayerPlugin.className);
            jn["boost"]["trail"]["c"]["start"] = ((int)_model.values["Boost Trail Start Color"]).ToString();
            Debug.LogFormat("{0}Saving Boost Trail End Color", PlayerPlugin.className);
            jn["boost"]["trail"]["c"]["end"] = ((int)_model.values["Boost Trail End Color"]).ToString();
            Debug.LogFormat("{0}Saving Boost Trail Start Opacity", PlayerPlugin.className);
            jn["boost"]["trail"]["o"]["start"] = ((float)_model.values["Boost Trail Start Opacity"]).ToString();
            Debug.LogFormat("{0}Saving Boost Trail End Opacity", PlayerPlugin.className);
            jn["boost"]["trail"]["o"]["end"] = ((float)_model.values["Boost Trail End Opacity"]).ToString();
            #endregion

            #region Boost Particles
            Debug.LogFormat("{0}Saving Boost Particles Emitting", PlayerPlugin.className);
            jn["boost"]["particles"]["em"] = ((bool)_model.values["Boost Particles Emitting"]).ToString();
            Debug.LogFormat("{0}Saving Boost Particles Shape", PlayerPlugin.className);
            if (((Vector2Int)_model.values["Boost Particles Shape"]).x != 0)
                jn["boost"]["particles"]["s"] = ((Vector2Int)_model.values["Boost Particles Shape"]).x.ToString();
            if (((Vector2Int)_model.values["Boost Particles Shape"]).y != 0)
                jn["boost"]["particles"]["so"] = ((Vector2Int)_model.values["Boost Particles Shape"]).y.ToString();
            Debug.LogFormat("{0}Saving Boost Particles Color", PlayerPlugin.className);
            jn["boost"]["particles"]["col"] = ((int)_model.values["Boost Particles Color"]).ToString();
            Debug.LogFormat("{0}Saving Boost Particles Start Opacity", PlayerPlugin.className);
            jn["boost"]["particles"]["opa"]["start"] = ((float)_model.values["Boost Particles Start Opacity"]).ToString();
            Debug.LogFormat("{0}Saving Boost Particles End Opacity", PlayerPlugin.className);
            jn["boost"]["particles"]["opa"]["end"] = ((float)_model.values["Boost Particles End Opacity"]).ToString();
            Debug.LogFormat("{0}Saving Boost Particles Start Scale", PlayerPlugin.className);
            jn["boost"]["particles"]["sca"]["start"] = ((float)_model.values["Boost Particles Start Scale"]).ToString();
            Debug.LogFormat("{0}Saving Boost Particles End Scale", PlayerPlugin.className);
            jn["boost"]["particles"]["sca"]["end"] = ((float)_model.values["Boost Particles End Scale"]).ToString();
            Debug.LogFormat("{0}Saving Boost Particles Rotation", PlayerPlugin.className);
            jn["boost"]["particles"]["rot"] = ((float)_model.values["Boost Particles Rotation"]).ToString();
            Debug.LogFormat("{0}Saving Boost Particles Lifetime", PlayerPlugin.className);
            jn["boost"]["particles"]["lt"] = ((float)_model.values["Boost Particles Lifetime"]).ToString();
            Debug.LogFormat("{0}Saving Boost Particles Speed", PlayerPlugin.className);
            jn["boost"]["particles"]["sp"] = ((float)_model.values["Boost Particles Speed"]).ToString();
            Debug.LogFormat("{0}Saving Boost Particles Amount", PlayerPlugin.className);
            jn["boost"]["particles"]["am"] = ((int)_model.values["Boost Particles Amount"]).ToString();
            Debug.LogFormat("{0}Saving Boost Particles Force X", PlayerPlugin.className);
            jn["boost"]["particles"]["frc"]["x"] = ((Vector2)_model.values["Boost Particles Force"]).x.ToString();
            Debug.LogFormat("{0}Saving Boost Particles Force Y", PlayerPlugin.className);
            jn["boost"]["particles"]["frc"]["y"] = ((Vector2)_model.values["Boost Particles Force"]).y.ToString();
            Debug.LogFormat("{0}Saving Boost Particles Trail Emitting", PlayerPlugin.className);
            jn["boost"]["particles"]["trem"] = ((bool)_model.values["Boost Particles Trail Emitting"]).ToString();
            #endregion

            #region Tail
            Debug.LogFormat("{0}Saving Tail Base", PlayerPlugin.className);
            jn["tail_base"]["distance"] = ((float)_model.values["Tail Base Distance"]).ToString();
            jn["tail_base"]["mode"] = ((int)_model.values["Tail Base Mode"]).ToString();

            for (int i = 1; i < 4; i++)
            {
                Debug.LogFormat("{0}Saving Tail {1} Active", PlayerPlugin.className, i);
                jn["tail"][i - 1]["active"] = ((bool)_model.values[string.Format("Tail {0} Active", i)]).ToString();
                Debug.LogFormat("{0}Saving Tail {1} Shape", PlayerPlugin.className, i);
                if (((Vector2Int)_model.values[string.Format("Tail {0} Shape", i)]).x != 0)
                    jn["tail"][i - 1]["s"] = ((Vector2Int)_model.values[string.Format("Tail {0} Shape", i)]).x.ToString();
                if (((Vector2Int)_model.values[string.Format("Tail {0} Shape", i)]).y != 0)
                    jn["tail"][i - 1]["so"] = ((Vector2Int)_model.values[string.Format("Tail {0} Shape", i)]).y.ToString();
                Debug.LogFormat("{0}Saving Tail {1} Position", PlayerPlugin.className, i);
                jn["tail"][i - 1]["pos"]["x"] = ((Vector2)_model.values[string.Format("Tail {0} Position", i)]).x.ToString();
                jn["tail"][i - 1]["pos"]["y"] = ((Vector2)_model.values[string.Format("Tail {0} Position", i)]).y.ToString();
                Debug.LogFormat("{0}Saving Tail {1} Scale", PlayerPlugin.className, i);
                jn["tail"][i - 1]["sca"]["x"] = ((Vector2)_model.values[string.Format("Tail {0} Scale", i)]).x.ToString();
                jn["tail"][i - 1]["sca"]["y"] = ((Vector2)_model.values[string.Format("Tail {0} Scale", i)]).y.ToString();
                Debug.LogFormat("{0}Saving Tail {1} Rotation", PlayerPlugin.className, i);
                jn["tail"][i - 1]["rot"]["x"] = ((float)_model.values[string.Format("Tail {0} Rotation", i)]).ToString();

                Debug.LogFormat("{0}Saving Tail {1} Trail Emitting", PlayerPlugin.className, i);
                jn["tail"][i - 1]["trail"]["em"] = ((bool)_model.values[string.Format("Tail {0} Trail Emitting", i)]).ToString();
                Debug.LogFormat("{0}Saving Tail {1} Trail Time", PlayerPlugin.className, i);
                jn["tail"][i - 1]["trail"]["t"] = ((float)_model.values[string.Format("Tail {0} Trail Time", i)]).ToString();
                Debug.LogFormat("{0}Saving Tail {1} Trail Start Width", PlayerPlugin.className, i);
                jn["tail"][i - 1]["trail"]["w"]["start"] = ((float)_model.values[string.Format("Tail {0} Trail Start Width", i)]).ToString();
                Debug.LogFormat("{0}Saving Tail {1} Trail End Width", PlayerPlugin.className, i);
                jn["tail"][i - 1]["trail"]["w"]["end"] = ((float)_model.values[string.Format("Tail {0} Trail End Width", i)]).ToString();
                Debug.LogFormat("{0}Saving Tail {1} Trail Start Color", PlayerPlugin.className, i);
                jn["tail"][i - 1]["trail"]["c"]["start"] = ((int)_model.values[string.Format("Tail {0} Trail Start Color", i)]).ToString();
                Debug.LogFormat("{0}Saving Tail {1} Trail End Color", PlayerPlugin.className, i);
                jn["tail"][i - 1]["trail"]["c"]["end"] = ((int)_model.values[string.Format("Tail {0} Trail End Color", i)]).ToString();
                Debug.LogFormat("{0}Saving Tail {1} Trail Start Opacity", PlayerPlugin.className, i);
                jn["tail"][i - 1]["trail"]["o"]["start"] = ((float)_model.values[string.Format("Tail {0} Trail Start Opacity", i)]).ToString();
                Debug.LogFormat("{0}Saving Tail {1} Trail End Opacity", PlayerPlugin.className, i);
                jn["tail"][i - 1]["trail"]["o"]["end"] = ((float)_model.values[string.Format("Tail {0} Trail End Opacity", i)]).ToString();

                jn["tail"][i - 1]["particles"]["em"] = ((bool)_model.values[string.Format("Tail {0} Particles Emitting", i)]).ToString();
                if (((Vector2Int)_model.values[string.Format("Tail {0} Particles Shape", i)]).x != 0)
                    jn["tail"][i - 1]["particles"]["s"] = ((Vector2Int)_model.values[string.Format("Tail {0} Particles Shape", i)]).x.ToString();
                if (((Vector2Int)_model.values[string.Format("Tail {0} Particles Shape", i)]).y != 0)
                    jn["tail"][i - 1]["particles"]["so"] = ((Vector2Int)_model.values[string.Format("Tail {0} Particles Shape", i)]).y.ToString();
                jn["tail"][i - 1]["particles"]["col"] = ((int)_model.values[string.Format("Tail {0} Particles Color", i)]).ToString();
                jn["tail"][i - 1]["particles"]["opa"]["start"] = ((float)_model.values[string.Format("Tail {0} Particles Start Opacity", i)]).ToString();
                jn["tail"][i - 1]["particles"]["opa"]["end"] = ((float)_model.values[string.Format("Tail {0} Particles End Opacity", i)]).ToString();
                jn["tail"][i - 1]["particles"]["sca"]["start"] = ((float)_model.values[string.Format("Tail {0} Particles Start Scale", i)]).ToString();
                jn["tail"][i - 1]["particles"]["sca"]["end"] = ((float)_model.values[string.Format("Tail {0} Particles End Scale", i)]).ToString();
                jn["tail"][i - 1]["particles"]["rot"] = ((float)_model.values[string.Format("Tail {0} Particles Rotation", i)]).ToString();
                jn["tail"][i - 1]["particles"]["lt"] = ((float)_model.values[string.Format("Tail {0} Particles Lifetime", i)]).ToString();
                jn["tail"][i - 1]["particles"]["sp"] = ((float)_model.values[string.Format("Tail {0} Particles Speed", i)]).ToString();
                jn["tail"][i - 1]["particles"]["am"] = ((float)_model.values[string.Format("Tail {0} Particles Amount", i)]).ToString();
                jn["tail"][i - 1]["particles"]["frc"]["x"] = ((Vector2)_model.values[string.Format("Tail {0} Particles Force", i)]).x.ToString();
                jn["tail"][i - 1]["particles"]["frc"]["y"] = ((Vector2)_model.values[string.Format("Tail {0} Particles Force", i)]).y.ToString();
                jn["tail"][i - 1]["particles"]["trem"] = ((bool)_model.values[string.Format("Tail {0} Particles Trail Emitting", i)]).ToString();
            }
            #endregion

            #region Custom Objects
            Dictionary<string, object> dictionary = (Dictionary<string, object>)_model.values["Custom Objects"];
            if (dictionary != null && dictionary.Count > 0)
                for (int i = 0; i < dictionary.Count; i++)
                {
                    var customObj = (Dictionary<string, object>)dictionary.ElementAt(i).Value;

                    jn["custom_objects"][i]["id"] = (string)customObj["ID"];

                    if (((Vector2Int)customObj["Shape"]).x != 0)
                        jn["custom_objects"][i]["s"] = ((Vector2Int)customObj["Shape"]).x.ToString();
                    if (((Vector2Int)customObj["Shape"]).y != 0)
                        jn["custom_objects"][i]["so"] = ((Vector2Int)customObj["Shape"]).y.ToString();
                    jn["custom_objects"][i]["p"] = ((int)customObj["Parent"]).ToString();
                    jn["custom_objects"][i]["d"] = ((float)customObj["Depth"]).ToString();
                    jn["custom_objects"][i]["pos"]["x"] = ((Vector2)customObj["Position"]).x.ToString();
                    jn["custom_objects"][i]["pos"]["y"] = ((Vector2)customObj["Position"]).y.ToString();
                    jn["custom_objects"][i]["sca"]["x"] = ((Vector2)customObj["Scale"]).x.ToString();
                    jn["custom_objects"][i]["sca"]["y"] = ((Vector2)customObj["Scale"]).y.ToString();
                    jn["custom_objects"][i]["rot"]["x"] = ((float)customObj["Rotation"]).ToString();
                    jn["custom_objects"][i]["col"]["x"] = ((int)customObj["Color"]).ToString();
                    if (((float)customObj["Opacity"]) != 1f)
                        jn["custom_objects"][i]["opa"]["x"] = ((float)customObj["Opacity"]).ToString();
                }
            #endregion

            Debug.LogFormat("{0}Done!", PlayerPlugin.className);
            return jn;
        }

        public static PlayerModelClass.PlayerModel LoadPlayer(string _name, string _path = "")
        {
            string path = _path;
            if (path == "")
            {
                path = RTFile.GetApplicationDirectory() + "beatmaps/players/" + _name.ToLower().Replace(" ", "") + ".lspl";
            }

            if (RTFile.FileExists(path))
            {
                string json = FileManager.inst.LoadJSONFileRaw(path);
                JSONNode jn = JSON.Parse(json);
                return LoadPlayer(jn);
            }

            return null;
        }

        public static PlayerModelClass.PlayerModel LoadPlayer(JSONNode jn)
        {
            var model = new PlayerModelClass.PlayerModel();

            model.values["Base Name"] = (string)jn["base"]["name"];
            if (!string.IsNullOrEmpty(jn["base"]["id"]))
            {
                model.values["Base ID"] = (string)jn["base"]["id"];
            }
            else
            {
                model.values["Base ID"] = LSFunctions.LSText.randomNumString(16);
            }
            if (!string.IsNullOrEmpty(jn["base"]["health"]))
            {
                model.values["Base Health"] = int.Parse(jn["base"]["health"]);
            }
            else
            {
                model.values["Base Health"] = 3;
            }

            #region Head
            Debug.LogFormat("{0}Loading Head Shape", PlayerPlugin.className);
            int headS = 0;
            int headSO = 0;
            if (!string.IsNullOrEmpty(jn["head"]["s"]))
            {
                headS = int.Parse(jn["head"]["s"]);
            }
            if (!string.IsNullOrEmpty(jn["head"]["so"]))
            {
                headSO = int.Parse(jn["head"]["so"]);
            }
            model.values["Head Shape"] = new Vector2Int(headS, headSO);
            Debug.LogFormat("{0}Loading Head Position", PlayerPlugin.className);
            model.values["Head Position"] = new Vector2(float.Parse(jn["head"]["pos"]["x"]), float.Parse(jn["head"]["pos"]["y"]));
            Debug.LogFormat("{0}Loading Head Scale", PlayerPlugin.className);
            model.values["Head Scale"] = new Vector2(float.Parse(jn["head"]["sca"]["x"]), float.Parse(jn["head"]["sca"]["y"]));
            Debug.LogFormat("{0}Loading Head Rotation", PlayerPlugin.className);
            model.values["Head Rotation"] = float.Parse(jn["head"]["rot"]["x"]);
            #endregion

            #region Head Trail
            Debug.LogFormat("{0}Loading Head Trail Emitting", PlayerPlugin.className);
            model.values["Head Trail Emitting"] = bool.Parse(jn["head"]["trail"]["em"]);
            Debug.LogFormat("{0}Loading Head Trail Time", PlayerPlugin.className);
            model.values["Head Trail Time"] = float.Parse(jn["head"]["trail"]["t"]);
            Debug.LogFormat("{0}Loading Head Trail Start Width", PlayerPlugin.className);
            model.values["Head Trail Start Width"] = float.Parse(jn["head"]["trail"]["w"]["start"]);
            Debug.LogFormat("{0}Loading Head Trail End Width", PlayerPlugin.className);
            model.values["Head Trail End Width"] = float.Parse(jn["head"]["trail"]["w"]["end"]);
            Debug.LogFormat("{0}Loading Head Trail Start Color", PlayerPlugin.className);
            model.values["Head Trail Start Color"] = int.Parse(jn["head"]["trail"]["c"]["start"]);
            Debug.LogFormat("{0}Loading Head Trail End Color", PlayerPlugin.className);
            model.values["Head Trail End Color"] = int.Parse(jn["head"]["trail"]["c"]["end"]);
            Debug.LogFormat("{0}Loading Head Trail Start Opacity", PlayerPlugin.className);
            model.values["Head Trail Start Opacity"] = float.Parse(jn["head"]["trail"]["o"]["start"]);
            Debug.LogFormat("{0}Loading Head Trail End Opacity", PlayerPlugin.className);
            model.values["Head Trail End Opacity"] = float.Parse(jn["head"]["trail"]["o"]["end"]);

            Debug.LogFormat("{0}Loading Head Trail Position Offset", PlayerPlugin.className);
            float x = 0f;
            float y = 0f;
            if (!string.IsNullOrEmpty(jn["head"]["trail"]["pos"]["x"]))
            {
                x = float.Parse(jn["head"]["trail"]["pos"]["x"]);
            }
            if (!string.IsNullOrEmpty(jn["head"]["trail"]["pos"]["y"]))
            {
                y = float.Parse(jn["head"]["trail"]["pos"]["y"]);
            }

            model.values["Head Trail Position Offset"] = new Vector2(x, y);
            #endregion

            #region Head Particles
            Debug.LogFormat("{0}Loading Head Particles Emitting", PlayerPlugin.className);
            model.values["Head Particles Emitting"] = bool.Parse(jn["head"]["particles"]["em"]);

            Debug.LogFormat("{0}Loading Head Particles Shape", PlayerPlugin.className);
            int headPS = 0;
            int headPSO = 0;
            if (!string.IsNullOrEmpty(jn["head"]["particles"]["s"]))
            {
                headPS = int.Parse(jn["head"]["particles"]["s"]);
            }
            if (!string.IsNullOrEmpty(jn["head"]["particles"]["so"]))
            {
                headPSO = int.Parse(jn["head"]["particles"]["so"]);
            }

            model.values["Head Particles Shape"] = new Vector2Int(headPS, headPSO);
            Debug.LogFormat("{0}Loading Head Particles Color", PlayerPlugin.className);
            model.values["Head Particles Color"] = int.Parse(jn["head"]["particles"]["col"]);
            Debug.LogFormat("{0}Loading Head Particles Start Opacity", PlayerPlugin.className);
            model.values["Head Particles Start Opacity"] = float.Parse(jn["head"]["particles"]["opa"]["start"]);
            Debug.LogFormat("{0}Loading Head Particles End Opacity", PlayerPlugin.className);
            model.values["Head Particles End Opacity"] = float.Parse(jn["head"]["particles"]["opa"]["end"]);
            Debug.LogFormat("{0}Loading Head Particles Start Scale", PlayerPlugin.className);
            model.values["Head Particles Start Scale"] = float.Parse(jn["head"]["particles"]["sca"]["start"]);
            Debug.LogFormat("{0}Loading Head Particles End Scale", PlayerPlugin.className);
            model.values["Head Particles End Scale"] = float.Parse(jn["head"]["particles"]["sca"]["end"]);
            Debug.LogFormat("{0}Loading Head Particles Rotation", PlayerPlugin.className);
            model.values["Head Particles Rotation"] = float.Parse(jn["head"]["particles"]["rot"]);
            Debug.LogFormat("{0}Loading Head Particles Lifetime", PlayerPlugin.className);
            model.values["Head Particles Lifetime"] = float.Parse(jn["head"]["particles"]["lt"]);
            Debug.LogFormat("{0}Loading Head Particles Speed", PlayerPlugin.className);
            model.values["Head Particles Speed"] = float.Parse(jn["head"]["particles"]["sp"]);
            Debug.LogFormat("{0}Loading Head Particles Amount", PlayerPlugin.className);
            model.values["Head Particles Amount"] = float.Parse(jn["head"]["particles"]["am"]);
            Debug.LogFormat("{0}Loading Head Particles Force", PlayerPlugin.className);
            model.values["Head Particles Force"] = new Vector2(float.Parse(jn["head"]["particles"]["frc"]["x"]), float.Parse(jn["head"]["particles"]["frc"]["y"]));
            Debug.LogFormat("{0}Loading Head Particles Trail Emitting", PlayerPlugin.className);
            model.values["Head Particles Trail Emitting"] = bool.Parse(jn["head"]["particles"]["trem"]);
            #endregion

            #region Boost
            Debug.LogFormat("{0}Loading Boost Active", PlayerPlugin.className);
            if (!string.IsNullOrEmpty(jn["boost"]["active"]))
            {
                model.values["Boost Active"] = bool.Parse(jn["boost"]["active"]);
            }
            Debug.LogFormat("{0}Loading Boost Shape", PlayerPlugin.className);
            int boostS = 0;
            int boostSO = 0;
            if (!string.IsNullOrEmpty(jn["boost"]["s"]))
            {
                boostS = int.Parse(jn["boost"]["s"]);
            }
            if (!string.IsNullOrEmpty(jn["boost"]["so"]))
            {
                boostSO = int.Parse(jn["boost"]["so"]);
            }
            model.values["Boost Shape"] = new Vector2Int(boostS, boostSO);
            Debug.LogFormat("{0}Loading Boost Position", PlayerPlugin.className);
            model.values["Boost Position"] = new Vector2(float.Parse(jn["boost"]["pos"]["x"]), float.Parse(jn["boost"]["pos"]["y"]));
            Debug.LogFormat("{0}Loading Boost Scale", PlayerPlugin.className);
            model.values["Boost Scale"] = new Vector2(float.Parse(jn["boost"]["sca"]["x"]), float.Parse(jn["boost"]["sca"]["y"]));
            Debug.LogFormat("{0}Loading Boost Rotation", PlayerPlugin.className);
            model.values["Boost Rotation"] = float.Parse(jn["boost"]["rot"]["x"]);
            #endregion

            #region Boost Trail
            Debug.LogFormat("{0}Loading Boost Trail Emitting", PlayerPlugin.className);
            model.values["Boost Trail Emitting"] = bool.Parse(jn["boost"]["trail"]["em"]);
            Debug.LogFormat("{0}Loading Boost Trail Time", PlayerPlugin.className);
            model.values["Boost Trail Time"] = float.Parse(jn["boost"]["trail"]["t"]);
            Debug.LogFormat("{0}Loading Boost Trail Start Width", PlayerPlugin.className);
            model.values["Boost Trail Start Width"] = float.Parse(jn["boost"]["trail"]["w"]["start"]);
            Debug.LogFormat("{0}Loading Boost Trail End Width", PlayerPlugin.className);
            model.values["Boost Trail End Width"] = float.Parse(jn["boost"]["trail"]["w"]["end"]);
            Debug.LogFormat("{0}Loading Boost Trail Start Color", PlayerPlugin.className);
            model.values["Boost Trail Start Color"] = int.Parse(jn["boost"]["trail"]["c"]["start"]);
            Debug.LogFormat("{0}Loading Boost Trail End Color", PlayerPlugin.className);
            model.values["Boost Trail End Color"] = int.Parse(jn["boost"]["trail"]["c"]["end"]);
            Debug.LogFormat("{0}Loading Boost Trail Start Opacity", PlayerPlugin.className);
            model.values["Boost Trail Start Opacity"] = float.Parse(jn["boost"]["trail"]["o"]["start"]);
            Debug.LogFormat("{0}Loading Boost Trail End Opacity", PlayerPlugin.className);
            model.values["Boost Trail End Opacity"] = float.Parse(jn["boost"]["trail"]["o"]["end"]);
            #endregion

            #region Boost particles
            Debug.LogFormat("{0}Loading Boost Particles Emitting", PlayerPlugin.className);
            model.values["Boost Particles Emitting"] = bool.Parse(jn["boost"]["particles"]["em"]);

            Debug.LogFormat("{0}Loading Boost Particles Shape", PlayerPlugin.className);
            int boostPS = 0;
            int boostPSO = 0;
            if (!string.IsNullOrEmpty(jn["boost"]["particles"]["s"]))
            {
                boostPS = int.Parse(jn["boost"]["particles"]["s"]);
            }
            if (!string.IsNullOrEmpty(jn["boost"]["particles"]["so"]))
            {
                boostPSO = int.Parse(jn["boost"]["particles"]["so"]);
            }

            model.values["Boost Particles Shape"] = new Vector2Int(boostPS, boostPSO);
            Debug.LogFormat("{0}Loading Boost Particles Color", PlayerPlugin.className);
            model.values["Boost Particles Color"] = int.Parse(jn["boost"]["particles"]["col"]);
            Debug.LogFormat("{0}Loading Boost Particles Start Opacity", PlayerPlugin.className);
            model.values["Boost Particles Start Opacity"] = float.Parse(jn["boost"]["particles"]["opa"]["start"]);
            Debug.LogFormat("{0}Loading Boost Particles End Opacity", PlayerPlugin.className);
            model.values["Boost Particles End Opacity"] = float.Parse(jn["boost"]["particles"]["opa"]["end"]);
            Debug.LogFormat("{0}Loading Boost Particles Start Scale", PlayerPlugin.className);
            model.values["Boost Particles Start Scale"] = float.Parse(jn["boost"]["particles"]["sca"]["start"]);
            Debug.LogFormat("{0}Loading Boost Particles End Scale", PlayerPlugin.className);
            model.values["Boost Particles End Scale"] = float.Parse(jn["boost"]["particles"]["sca"]["end"]);
            Debug.LogFormat("{0}Loading Boost Particles Rotation", PlayerPlugin.className);
            model.values["Boost Particles Rotation"] = float.Parse(jn["boost"]["particles"]["rot"]);
            Debug.LogFormat("{0}Loading Boost Particles Lifetime", PlayerPlugin.className);
            model.values["Boost Particles Lifetime"] = float.Parse(jn["boost"]["particles"]["lt"]);
            Debug.LogFormat("{0}Loading Boost Particles Speed", PlayerPlugin.className);
            model.values["Boost Particles Speed"] = float.Parse(jn["boost"]["particles"]["sp"]);
            Debug.LogFormat("{0}Loading Boost Particles Amount", PlayerPlugin.className);
            model.values["Boost Particles Amount"] = int.Parse(jn["boost"]["particles"]["am"]);
            Debug.LogFormat("{0}Loading Boost Particles Force", PlayerPlugin.className);
            model.values["Boost Particles Force"] = new Vector2(float.Parse(jn["boost"]["particles"]["frc"]["x"]), float.Parse(jn["boost"]["particles"]["frc"]["y"]));
            Debug.LogFormat("{0}Loading Boost Particles Trail Emitting", PlayerPlugin.className);
            model.values["Boost Particles Trail Emitting"] = bool.Parse(jn["boost"]["particles"]["trem"]);
            #endregion

            #region Tail
            Debug.LogFormat("{0}Loading Tail Base Distance", PlayerPlugin.className);
            model.values["Tail Base Distance"] = float.Parse(jn["tail_base"]["distance"]);
            Debug.LogFormat("{0}Loading Tail Base Mode", PlayerPlugin.className);
            model.values["Tail Base Mode"] = int.Parse(jn["tail_base"]["mode"]);

            for (int i = 1; i < jn["tail"].Count + 1; i++)
            {
                Debug.LogFormat("{0}Loading Tail {1} Active", PlayerPlugin.className, i);
                if (!string.IsNullOrEmpty(jn["tail"][i - 1]["active"]))
                {
                    model.values[string.Format("Tail {0} Active", i)] = bool.Parse(jn["tail"][i - 1]["active"]);
                }
                Debug.LogFormat("{0}Loading Tail {1} Shape", PlayerPlugin.className, i);
                int tailS = 0;
                int tailSO = 0;
                if (!string.IsNullOrEmpty(jn["tail"][i - 1]["s"]))
                {
                    tailS = int.Parse(jn["tail"][i - 1]["s"]);
                }
                if (!string.IsNullOrEmpty(jn["tail"][i - 1]["so"]))
                {
                    tailSO = int.Parse(jn["tail"][i - 1]["so"]);
                }
                model.values[string.Format("Tail {0} Shape", i)] = new Vector2Int(tailS, tailSO);
                Debug.LogFormat("{0}Loading Tail {1} Position", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Position", i)] = new Vector2(float.Parse(jn["tail"][i - 1]["pos"]["x"]), float.Parse(jn["tail"][i - 1]["pos"]["y"]));
                Debug.LogFormat("{0}Loading Tail {1} Scale", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Scale", i)] = new Vector2(float.Parse(jn["tail"][i - 1]["sca"]["x"]), float.Parse(jn["tail"][i - 1]["sca"]["y"]));
                Debug.LogFormat("{0}Loading Tail {1} Rotation", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Rotation", i)] = float.Parse(jn["tail"][i - 1]["rot"]["x"]);

                Debug.LogFormat("{0}Loading Tail {1} Trail Emitting", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Trail Emitting", i)] = bool.Parse(jn["tail"][i - 1]["trail"]["em"]);
                Debug.LogFormat("{0}Loading Tail {1} Trail Time", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Trail Time", i)] = float.Parse(jn["tail"][i - 1]["trail"]["t"]);
                Debug.LogFormat("{0}Loading Tail {1} Trail Start Width", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Trail Start Width", i)] = float.Parse(jn["tail"][i - 1]["trail"]["w"]["start"]);
                Debug.LogFormat("{0}Loading Tail {1} Trail End Width", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Trail End Width", i)] = float.Parse(jn["tail"][i - 1]["trail"]["w"]["end"]);
                Debug.LogFormat("{0}Loading Tail {1} Trail Start Color", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Trail Start Color", i)] = int.Parse(jn["tail"][i - 1]["trail"]["c"]["start"]);
                Debug.LogFormat("{0}Loading Tail {1} Trail End Color", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Trail End Color", i)] = int.Parse(jn["tail"][i - 1]["trail"]["c"]["end"]);
                Debug.LogFormat("{0}Loading Tail {1} Trail Start Opacity", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Trail Start Opacity", i)] = float.Parse(jn["tail"][i - 1]["trail"]["o"]["start"]);
                Debug.LogFormat("{0}Loading Tail {1} Trail End Opacity", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Trail End Opacity", i)] = float.Parse(jn["tail"][i - 1]["trail"]["o"]["end"]);

                Debug.LogFormat("{0}Loading Tail {1} Particles Emitting", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Particles Emitting", i)] = bool.Parse(jn["tail"][i - 1]["particles"]["em"]);

                Debug.LogFormat("{0}Loading Tail {1} Particles Shape", PlayerPlugin.className, i);
                int tailPS = 0;
                int tailPSO = 0;
                if (!string.IsNullOrEmpty(jn["tail"][i - 1]["particles"]["s"]))
                {
                    tailPS = int.Parse(jn["tail"][i - 1]["particles"]["s"]);
                }
                if (!string.IsNullOrEmpty(jn["tail"][i - 1]["particles"]["so"]))
                {
                    tailPSO = int.Parse(jn["tail"][i - 1]["particles"]["so"]);
                }

                model.values[string.Format("Tail {0} Particles Shape", i)] = new Vector2Int(tailPS, tailPSO);
                Debug.LogFormat("{0}Loading Tail {1} Particles Color", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Particles Color", i)] = int.Parse(jn["tail"][i - 1]["particles"]["col"]);
                Debug.LogFormat("{0}Loading Tail {1} Particles Start Opacity", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Particles Start Opacity", i)] = float.Parse(jn["tail"][i - 1]["particles"]["opa"]["start"]);
                Debug.LogFormat("{0}Loading Tail {1} Particles End Opacity", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Particles End Opacity", i)] = float.Parse(jn["tail"][i - 1]["particles"]["opa"]["end"]);
                Debug.LogFormat("{0}Loading Tail {1} Particles Start Scale", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Particles Start Scale", i)] = float.Parse(jn["tail"][i - 1]["particles"]["sca"]["start"]);
                Debug.LogFormat("{0}Loading Tail {1} Particles End Scale", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Particles End Scale", i)] = float.Parse(jn["tail"][i - 1]["particles"]["sca"]["end"]);
                Debug.LogFormat("{0}Loading Tail {1} Particles Rotation", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Particles Rotation", i)] = float.Parse(jn["tail"][i - 1]["particles"]["rot"]);
                Debug.LogFormat("{0}Loading Tail {1} Particles Lifetime", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Particles Lifetime", i)] = float.Parse(jn["tail"][i - 1]["particles"]["lt"]);
                Debug.LogFormat("{0}Loading Tail {1} Particles Speed", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Particles Speed", i)] = float.Parse(jn["tail"][i - 1]["particles"]["sp"]);
                Debug.LogFormat("{0}Loading Tail {1} Particles Amount", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Particles Amount", i)] = float.Parse(jn["tail"][i - 1]["particles"]["am"]);
                Debug.LogFormat("{0}Loading Tail {1} Particles Force", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Particles Force", i)] = new Vector2(float.Parse(jn["tail"][i - 1]["particles"]["frc"]["x"]), float.Parse(jn["tail"][i - 1]["particles"]["frc"]["y"]));
                Debug.LogFormat("{0}Loading Tail {1} Particles Trail Emitting", PlayerPlugin.className, i);
                model.values[string.Format("Tail {0} Particles Trail Emitting", i)] = bool.Parse(jn["tail"][i - 1]["particles"]["trem"]);
            }
            #endregion

            #region Custom Objects
            var dictionary = (Dictionary<string, object>)model.values["Custom Objects"];
            if (jn["custom_objects"] != null && jn["custom_objects"].Count > 0)
                for (int i = 0; i < jn["custom_objects"].Count; i++)
                {
                    var id = (string)jn["custom_objects"][i]["id"];
                    dictionary.Add(id, new Dictionary<string, object>());

                    ((Dictionary<string, object>)dictionary[id]).Add("ID", id);

                    int tailS = 0;
                    int tailSO = 0;
                    if (!string.IsNullOrEmpty(jn["custom_objects"][i]["s"]))
                    {
                        tailS = int.Parse(jn["custom_objects"][i]["s"]);
                    }
                    if (!string.IsNullOrEmpty(jn["custom_objects"][i]["so"]))
                    {
                        tailSO = int.Parse(jn["custom_objects"][i]["so"]);
                    }

                    ((Dictionary<string, object>)dictionary[id]).Add("Shape", new Vector2Int(tailS, tailSO));
                    ((Dictionary<string, object>)dictionary[id]).Add("Parent", int.Parse(jn["custom_objects"][i]["p"]));
                    ((Dictionary<string, object>)dictionary[id]).Add("Depth", float.Parse(jn["custom_objects"][i]["d"]));
                    ((Dictionary<string, object>)dictionary[id]).Add("Position", new Vector2(float.Parse(jn["custom_objects"][i]["pos"]["x"]), float.Parse(jn["custom_objects"][i]["pos"]["y"])));
                    ((Dictionary<string, object>)dictionary[id]).Add("Scale", new Vector2(float.Parse(jn["custom_objects"][i]["sca"]["x"]), float.Parse(jn["custom_objects"][i]["sca"]["y"])));
                    ((Dictionary<string, object>)dictionary[id]).Add("Rotation", float.Parse(jn["custom_objects"][i]["rot"]["x"]));
                    ((Dictionary<string, object>)dictionary[id]).Add("Color", int.Parse(jn["custom_objects"][i]["col"]["x"]));

                    float opacity = 1f;
                    if (!string.IsNullOrEmpty(jn["custom_objects"][i]["opa"]["x"]))
                    {
                        opacity = float.Parse(jn["custom_objects"][i]["opa"]["x"]);
                    }

                    ((Dictionary<string, object>)dictionary[id]).Add("Opacity", opacity);
                }
            #endregion

            Debug.LogFormat("{0}Done!", PlayerPlugin.className);
            return model;
        }
    }
}
