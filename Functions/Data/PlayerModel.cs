using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using CreativePlayers.Functions;

namespace CreativePlayers.Functions.Data
{
    public class PlayerModelClass : MonoBehaviour
    {
        public class PlayerModel
        {
            public PlayerModel()
            {
                values = new Dictionary<string, object>();
                RegenValues();
            }

            public PlayerModel(GameObject _gm)
            {
                gm = _gm;
                values = new Dictionary<string, object>();
                RegenValuesGM();
            }

            public void RegenValuesGM()
            {
                values.Clear();

                #region Base

                int isCircle = 0;
                if (gm.name.Contains("circle"))
                {
                    values.Add("Base Name", "Circle");
                    isCircle = 1;
                }
                else
                {
                    values.Add("Base Name", "Regular");
                }

                values.Add("Base ID", LSFunctions.LSText.randomNumString(16));

                values.Add("Base Health", 3);

                values.Add("Base Move Speed", 20f);
                values.Add("Base Boost Speed", 85f);
                values.Add("Base Boost Cooldown", 0.1f);
                values.Add("Base Min Boost Time", 0.07f);
                values.Add("Base Max Boost Time", 0.18f);

                values.Add("Base Rotate Mode", 0);
                values.Add("Base Collision Accurate", false);
                values.Add("Base Sprint Sneak Active", false);

                #endregion

                #region Stretch

                values.Add("Stretch Active", false);
                values.Add("Stretch Amount", 0.4f);
                values.Add("Stretch Easing", 6);

                #endregion

                #region GUI

                values.Add("GUI Health Active", false);
                values.Add("GUI Health Mode", 0);
                values.Add("GUI Health Top Color", 23);
                values.Add("GUI Health Base Color", 4);
                values.Add("GUI Health Top Custom Color", "FFFFFF");
                values.Add("GUI Health Base Custom Color", "FFFFFF");
                values.Add("GUI Health Top Opacity", 1f);
                values.Add("GUI Health Base Opacity", 1f);

                #endregion

                #region Head

                values.Add("Head Shape", new Vector2Int(isCircle, 0));
                values.Add("Head Position", gm.transform.Find("Player/Player").position.ToVector2());
                values.Add("Head Scale", gm.transform.Find("Player/Player").localScale.ToVector2());
                values.Add("Head Rotation", gm.transform.Find("Player/Player").localEulerAngles.z);
                values.Add("Head Color", 23);
                values.Add("Head Custom Color", "FFFFFF");
                values.Add("Head Opacity", 1f);

                #endregion

                #region Head Trail

                values.Add("Head Trail Emitting", false);
                values.Add("Head Trail Time", 1f);
                values.Add("Head Trail Start Width", 1f);
                values.Add("Head Trail End Width", 1f);
                values.Add("Head Trail Start Color", 23);
                values.Add("Head Trail Start Custom Color", "FFFFFF");
                values.Add("Head Trail Start Opacity", 1f);
                values.Add("Head Trail End Color", 23);
                values.Add("Head Trail End Custom Color", "FFFFFF");
                values.Add("Head Trail End Opacity", 0f);
                values.Add("Head Trail Position Offset", Vector2.zero);

                #endregion

                #region Head Particles

                values.Add("Head Particles Emitting", false);
                values.Add("Head Particles Shape", Vector2Int.zero);
                values.Add("Head Particles Color", 23);
                values.Add("Head Particles Custom Color", "FFFFFF");
                values.Add("Head Particles Start Opacity", 1f);
                values.Add("Head Particles End Opacity", 0f);
                values.Add("Head Particles Start Scale", 1f);
                values.Add("Head Particles End Scale", 0f);
                values.Add("Head Particles Rotation", 0f);
                values.Add("Head Particles Lifetime", 5f);
                values.Add("Head Particles Speed", 5f);
                values.Add("Head Particles Amount", 10f);
                values.Add("Head Particles Force", Vector2.zero);
                values.Add("Head Particles Trail Emitting", false);

                #endregion

                values.Add("Face Position", new Vector2(0.3f, 0f));
                values.Add("Face Control Active", true);

                #region Boost

                values.Add("Boost Active", true);
                values.Add("Boost Shape", new Vector2Int(isCircle, 0));
                values.Add("Boost Position", gm.transform.Find("Player/boost").position.ToVector2());
                values.Add("Boost Scale", Vector2.one);
                values.Add("Boost Rotation", gm.transform.Find("Player/boost").localEulerAngles.z);
                values.Add("Boost Color", 4);
                values.Add("Boost Custom Color", "FFFFFF");
                values.Add("Boost Opacity", 1f);

                #endregion

                #region Boost Trail

                values.Add("Boost Trail Emitting", false);
                values.Add("Boost Trail Time", 1f);
                values.Add("Boost Trail Start Width", 1f);
                values.Add("Boost Trail End Width", 1f);
                values.Add("Boost Trail Start Color", 0);
                values.Add("Boost Trail Start Custom Color", "FFFFFF");
                values.Add("Boost Trail Start Opacity", 1f);
                values.Add("Boost Trail End Color", 0);
                values.Add("Boost Trail End Custom Color", "FFFFFF");
                values.Add("Boost Trail End Opacity", 0f);

                #endregion

                #region Boost Particles

                values.Add("Boost Particles Emitting", false);
                values.Add("Boost Particles Shape", Vector2Int.zero);
                values.Add("Boost Particles Color", 0);
                values.Add("Boost Particles Custom Color", "FFFFFF");
                values.Add("Boost Particles Start Opacity", 1f);
                values.Add("Boost Particles End Opacity", 0f);
                values.Add("Boost Particles Start Scale", 1f);
                values.Add("Boost Particles End Scale", 0f);
                values.Add("Boost Particles Rotation", 0f);
                values.Add("Boost Particles Lifetime", 5f);
                values.Add("Boost Particles Speed", 5f);
                values.Add("Boost Particles Amount", 1);
                values.Add("Boost Particles Duration", 1f);
                values.Add("Boost Particles Force", Vector2.zero);
                values.Add("Boost Particles Trail Emitting", false);

                #endregion

                #region Pulse

                values.Add("Pulse Active", false);
                values.Add("Pulse Shape", Vector2Int.zero);
                values.Add("Pulse Rotate to Head", true);
                values.Add("Pulse Start Color", 0);
                values.Add("Pulse Start Custom Color", "FFFFFF");
                values.Add("Pulse End Color", 0);
                values.Add("Pulse End Custom Color", "FFFFFF");
                values.Add("Pulse Easing Color", 4);
                values.Add("Pulse Start Opacity", 0.5f);
                values.Add("Pulse End Opacity", 0f);
                values.Add("Pulse Easing Opacity", 3);
                values.Add("Pulse Depth", 0.1f);
                values.Add("Pulse Start Position", Vector2.zero);
                values.Add("Pulse End Position", Vector2.zero);
                values.Add("Pulse Easing Position", 4);
                values.Add("Pulse Start Scale", Vector2.zero);
                values.Add("Pulse End Scale", new Vector2(12f, 12f));
                values.Add("Pulse Easing Scale", 4);
                values.Add("Pulse Start Rotation", 0f);
                values.Add("Pulse End Rotation", 0f);
                values.Add("Pulse Easing Rotation", 4);
                values.Add("Pulse Duration", 1f);

                #endregion

                #region Tail Base

                values.Add("Tail Base Distance", 2f);
                values.Add("Tail Base Mode", 0);
                values.Add("Tail Base Grows", false);

                #endregion

                #region Tail Boost

                values.Add("Tail Boost Active", false);
                values.Add("Tail Boost Shape", new Vector2Int(isCircle, 0));
                values.Add("Tail Boost Position", Vector2.zero);
                values.Add("Tail Boost Scale", Vector2.one);
                values.Add("Tail Boost Rotation", 45f);
                values.Add("Tail Boost Color", 4);
                values.Add("Tail Boost Custom Color", "FFFFFF");
                values.Add("Tail Boost Opacity", 1f);

                #endregion

                #region Tail

                for (int i = 1; i < 4; i++)
                {
                    values.Add(string.Format("Tail {0} Active", i), true);
                    values.Add(string.Format("Tail {0} Shape", i), new Vector2Int(isCircle, 0));
                    values.Add(string.Format("Tail {0} Position", i), Vector2.zero);
                    values.Add(string.Format("Tail {0} Scale", i), gm.transform.Find("trail/" + i).localScale.ToVector2());
                    values.Add(string.Format("Tail {0} Rotation", i), gm.transform.Find("trail/" + i).localEulerAngles.z);
                    values.Add(string.Format("Tail {0} Color", i), 4);
                    values.Add(string.Format("Tail {0} Custom Color", i), "FFFFFF");
                    values.Add(string.Format("Tail {0} Opacity", i), 1f);

                    var trail = gm.transform.Find("trail/" + i).GetComponent<TrailRenderer>();
                    values.Add(string.Format("Tail {0} Trail Emitting", i), true);
                    values.Add(string.Format("Tail {0} Trail Time", i), trail.time);
                    values.Add(string.Format("Tail {0} Trail Start Width", i), trail.startWidth);
                    values.Add(string.Format("Tail {0} Trail End Width", i), trail.endWidth);
                    values.Add(string.Format("Tail {0} Trail Start Color", i), 4);
                    values.Add(string.Format("Tail {0} Trail Start Custom Color", i), "FFFFFF");
                    values.Add(string.Format("Tail {0} Trail Start Opacity", i), 1f);
                    values.Add(string.Format("Tail {0} Trail End Color", i), 4);
                    values.Add(string.Format("Tail {0} Trail End Custom Color", i), "FFFFFF");
                    values.Add(string.Format("Tail {0} Trail End Opacity", i), 0f);

                    values.Add(string.Format("Tail {0} Particles Emitting", i), false);
                    values.Add(string.Format("Tail {0} Particles Shape", i), Vector2Int.zero);
                    values.Add(string.Format("Tail {0} Particles Color", i), 0);
                    values.Add(string.Format("Tail {0} Particles Custom Color", i), "FFFFFF");
                    values.Add(string.Format("Tail {0} Particles Start Opacity", i), 1f);
                    values.Add(string.Format("Tail {0} Particles End Opacity", i), 0f);
                    values.Add(string.Format("Tail {0} Particles Start Scale", i), 1f);
                    values.Add(string.Format("Tail {0} Particles End Scale", i), 0f);
                    values.Add(string.Format("Tail {0} Particles Rotation", i), 0f);
                    values.Add(string.Format("Tail {0} Particles Lifetime", i), 5f);
                    values.Add(string.Format("Tail {0} Particles Speed", i), 5f);
                    values.Add(string.Format("Tail {0} Particles Amount", i), 10f);
                    values.Add(string.Format("Tail {0} Particles Force", i), Vector2.zero);
                    values.Add(string.Format("Tail {0} Particles Trail Emitting", i), false);
                }

                #endregion

                values.Add("Custom Objects", new Dictionary<string, object>());
            }

            public void RegenValues()
            {
                values = new Dictionary<string, object>();
                values.Clear();

                #region Base

                values.Add("Base Name", "Nanobot");
                values.Add("Base ID", LSFunctions.LSText.randomNumString(16));
                values.Add("Base Health", 3);

                values.Add("Base Move Speed", 20f);
                values.Add("Base Boost Speed", 85f);
                values.Add("Base Boost Cooldown", 0.1f);
                values.Add("Base Min Boost Time", 0.07f);
                values.Add("Base Max Boost Time", 0.18f);

                values.Add("Base Rotate Mode", 0);
                values.Add("Base Collision Accurate", false);
                values.Add("Base Sprint Sneak Active", false);

                #endregion

                #region Stretch

                values.Add("Stretch Active", false);
                values.Add("Stretch Amount", 0.5f);
                values.Add("Stretch Easing", 6);

                #endregion

                #region GUI

                values.Add("GUI Health Active", false);
                values.Add("GUI Health Mode", 0);
                values.Add("GUI Health Top Color", 23);
                values.Add("GUI Health Base Color", 4);
                values.Add("GUI Health Top Custom Color", "FFFFFF");
                values.Add("GUI Health Base Custom Color", "FFFFFF");
                values.Add("GUI Health Top Opacity", 1f);
                values.Add("GUI Health Base Opacity", 1f);

                #endregion

                #region Head

                values.Add("Head Shape", Vector2Int.zero);
                values.Add("Head Position", Vector2.zero);
                values.Add("Head Scale", Vector2.one);
                values.Add("Head Rotation", 0f);
                values.Add("Head Color", 23);
                values.Add("Head Custom Color", "FFFFFF");
                values.Add("Head Opacity", 1f);

                #endregion

                #region Head Trail

                values.Add("Head Trail Emitting", false);
                values.Add("Head Trail Time", 1f);
                values.Add("Head Trail Start Width", 1f);
                values.Add("Head Trail End Width", 1f);
                values.Add("Head Trail Start Color", 0);
                values.Add("Head Trail Start Custom Color", "FFFFFF");
                values.Add("Head Trail Start Opacity", 1f);
                values.Add("Head Trail End Color", 0);
                values.Add("Head Trail End Custom Color", "FFFFFF");
                values.Add("Head Trail End Opacity", 0f);
                values.Add("Head Trail Position Offset", Vector2.zero);

                #endregion

                #region Head Particles

                values.Add("Head Particles Emitting", false);
                values.Add("Head Particles Shape", Vector2Int.zero);
                values.Add("Head Particles Color", 0);
                values.Add("Head Particles Custom Color", "FFFFFF");
                values.Add("Head Particles Start Opacity", 1f);
                values.Add("Head Particles End Opacity", 0f);
                values.Add("Head Particles Start Scale", 1f);
                values.Add("Head Particles End Scale", 0f);
                values.Add("Head Particles Rotation", 0f);
                values.Add("Head Particles Lifetime", 5f);
                values.Add("Head Particles Speed", 5f);
                values.Add("Head Particles Amount", 10f);
                values.Add("Head Particles Force", Vector2.zero);
                values.Add("Head Particles Trail Emitting", false);

                #endregion

                values.Add("Face Position", new Vector2(0.3f, 0f));
                values.Add("Face Control Active", true);

                #region Boost

                values.Add("Boost Active", true);
                values.Add("Boost Shape", Vector2Int.zero);
                values.Add("Boost Position", Vector2.zero);
                values.Add("Boost Scale", Vector2.one);
                values.Add("Boost Rotation", 0f);
                values.Add("Boost Color", 4);
                values.Add("Boost Custom Color", "FFFFFF");
                values.Add("Boost Opacity", 1f);

                #endregion

                #region Boost Trail

                values.Add("Boost Trail Emitting", false);
                values.Add("Boost Trail Time", 1f);
                values.Add("Boost Trail Start Width", 1f);
                values.Add("Boost Trail End Width", 1f);
                values.Add("Boost Trail Start Color", 4);
                values.Add("Boost Trail Start Custom Color", "FFFFFF");
                values.Add("Boost Trail Start Opacity", 1f);
                values.Add("Boost Trail End Color", 4);
                values.Add("Boost Trail End Custom Color", "FFFFFF");
                values.Add("Boost Trail End Opacity", 0f);

                #endregion

                #region Boost Particles

                values.Add("Boost Particles Emitting", false);
                values.Add("Boost Particles Shape", Vector2Int.zero);
                values.Add("Boost Particles Color", 4);
                values.Add("Boost Particles Custom Color", "FFFFFF");
                values.Add("Boost Particles Start Opacity", 1f);
                values.Add("Boost Particles End Opacity", 0f);
                values.Add("Boost Particles Start Scale", 1f);
                values.Add("Boost Particles End Scale", 0f);
                values.Add("Boost Particles Rotation", 0f);
                values.Add("Boost Particles Lifetime", 5f);
                values.Add("Boost Particles Speed", 5f);
                values.Add("Boost Particles Amount", 1);
                values.Add("Boost Particles Duration", 1f);
                values.Add("Boost Particles Force", Vector2.zero);
                values.Add("Boost Particles Trail Emitting", false);

                #endregion

                #region Pulse

                values.Add("Pulse Active", false);
                values.Add("Pulse Shape", Vector2Int.zero);
                values.Add("Pulse Rotate to Head", true);
                values.Add("Pulse Start Color", 0);
                values.Add("Pulse Start Custom Color", "FFFFFF");
                values.Add("Pulse End Color", 23);
                values.Add("Pulse End Custom Color", "FFFFFF");
                values.Add("Pulse Easing Color", 4);
                values.Add("Pulse Start Opacity", 0.5f);
                values.Add("Pulse End Opacity", 0f);
                values.Add("Pulse Easing Opacity", 3);
                values.Add("Pulse Depth", 0.1f);
                values.Add("Pulse Start Position", Vector2.zero);
                values.Add("Pulse End Position", Vector2.zero);
                values.Add("Pulse Easing Position", 4);
                values.Add("Pulse Start Scale", Vector2.zero);
                values.Add("Pulse End Scale", new Vector2(12f, 12f));
                values.Add("Pulse Easing Scale", 4);
                values.Add("Pulse Start Rotation", 0f);
                values.Add("Pulse End Rotation", 0f);
                values.Add("Pulse Easing Rotation", 4);
                values.Add("Pulse Duration", 1f);

                #endregion

                #region Tail Base

                values.Add("Tail Base Distance", 2f);
                values.Add("Tail Base Mode", 0);
                values.Add("Tail Base Grows", false);

                #endregion

                #region Tail Boost

                values.Add("Tail Boost Active", true);
                values.Add("Tail Boost Shape", Vector2Int.zero);
                values.Add("Tail Boost Position", Vector2.zero);
                values.Add("Tail Boost Scale", Vector2.one);
                values.Add("Tail Boost Rotation", 45f);
                values.Add("Tail Boost Color", 4);
                values.Add("Tail Boost Custom Color", "FFFFFF");
                values.Add("Tail Boost Opacity", 1f);

                #endregion

                #region Tail

                for (int i = 1; i < 4; i++)
                {
                    values.Add(string.Format("Tail {0} Active", i), true);
                    values.Add(string.Format("Tail {0} Shape", i), Vector2Int.zero);
                    values.Add(string.Format("Tail {0} Position", i), Vector2.zero);
                    values.Add(string.Format("Tail {0} Scale", i), Vector2.one);
                    values.Add(string.Format("Tail {0} Rotation", i), 0f);
                    values.Add(string.Format("Tail {0} Color", i), 4);
                    values.Add(string.Format("Tail {0} Custom Color", i), "FFFFFF");
                    values.Add(string.Format("Tail {0} Opacity", i), 1f);

                    values.Add(string.Format("Tail {0} Trail Emitting", i), true);
                    values.Add(string.Format("Tail {0} Trail Time", i), 0.2f);
                    values.Add(string.Format("Tail {0} Trail Start Width", i), 0.5f);
                    values.Add(string.Format("Tail {0} Trail End Width", i), 0.2f);
                    values.Add(string.Format("Tail {0} Trail Start Color", i), 4);
                    values.Add(string.Format("Tail {0} Trail Start Custom Color", i), "FFFFFF");
                    values.Add(string.Format("Tail {0} Trail Start Opacity", i), 1f);
                    values.Add(string.Format("Tail {0} Trail End Color", i), 4);
                    values.Add(string.Format("Tail {0} Trail End Custom Color", i), "FFFFFF");
                    values.Add(string.Format("Tail {0} Trail End Opacity", i), 0f);

                    values.Add(string.Format("Tail {0} Particles Emitting", i), false);
                    values.Add(string.Format("Tail {0} Particles Shape", i), Vector2Int.zero);
                    values.Add(string.Format("Tail {0} Particles Color", i), 4);
                    values.Add(string.Format("Tail {0} Particles Custom Color", i), "FFFFFF");
                    values.Add(string.Format("Tail {0} Particles Start Opacity", i), 1f);
                    values.Add(string.Format("Tail {0} Particles End Opacity", i), 0f);
                    values.Add(string.Format("Tail {0} Particles Start Scale", i), 1f);
                    values.Add(string.Format("Tail {0} Particles End Scale", i), 0f);
                    values.Add(string.Format("Tail {0} Particles Rotation", i), 0f);
                    values.Add(string.Format("Tail {0} Particles Lifetime", i), 5f);
                    values.Add(string.Format("Tail {0} Particles Speed", i), 5f);
                    values.Add(string.Format("Tail {0} Particles Amount", i), 10f);
                    values.Add(string.Format("Tail {0} Particles Force", i), Vector2.zero);
                    values.Add(string.Format("Tail {0} Particles Trail Emitting", i), false);
                }

                #endregion

                values.Add("Custom Objects", new Dictionary<string, object>());
            }

            public void CreateCustomObject()
            {
                var dictionary = (Dictionary<string, object>)values["Custom Objects"];

                var id = LSFunctions.LSText.randomNumString(16);

                dictionary.Add(id, new Dictionary<string, object>());

                ((Dictionary<string, object>)dictionary[id]).Add("ID", id);
                ((Dictionary<string, object>)dictionary[id]).Add("Name", "Object Name");

                ((Dictionary<string, object>)dictionary[id]).Add("Shape", new Vector2Int(0, 0));
                ((Dictionary<string, object>)dictionary[id]).Add("Parent", 0);
                ((Dictionary<string, object>)dictionary[id]).Add("Parent Position Offset", 1f);
                ((Dictionary<string, object>)dictionary[id]).Add("Parent Scale Offset", 1f);
                ((Dictionary<string, object>)dictionary[id]).Add("Parent Scale Active", true);
                ((Dictionary<string, object>)dictionary[id]).Add("Parent Rotation Offset", 1f);
                ((Dictionary<string, object>)dictionary[id]).Add("Parent Rotation Active", true);
                ((Dictionary<string, object>)dictionary[id]).Add("Depth", 0f);
                ((Dictionary<string, object>)dictionary[id]).Add("Position", new Vector2(0f, 0f));
                ((Dictionary<string, object>)dictionary[id]).Add("Scale", new Vector2(1f, 1f));
                ((Dictionary<string, object>)dictionary[id]).Add("Rotation", 0f);
                ((Dictionary<string, object>)dictionary[id]).Add("Color", 0);
                ((Dictionary<string, object>)dictionary[id]).Add("Custom Color", "FFFFFF");
                ((Dictionary<string, object>)dictionary[id]).Add("Opacity", 1f);
                ((Dictionary<string, object>)dictionary[id]).Add("Visibility", 0); //0 = Normal / 1 = Boost / 2 = Hit / 3 = Zen Mode / 4 = Specific Health Percentage
                ((Dictionary<string, object>)dictionary[id]).Add("Visibility Value", 100f); //Percentage
                ((Dictionary<string, object>)dictionary[id]).Add("Visibility Not", false);
            }

            public GameObject gm;
            public Dictionary<string, object> values;
            public string filePath;
        }
    }
}
