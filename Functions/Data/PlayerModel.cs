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

                values.Add("Head Shape", new Vector2Int(isCircle, 0));
                values.Add("Head Position", gm.transform.Find("Player/Player").position.ToVector2());
                values.Add("Head Scale", gm.transform.Find("Player/Player").localScale.ToVector2());
                values.Add("Head Rotation", gm.transform.Find("Player/Player").localEulerAngles.z);

                values.Add("Head Trail Emitting", false);
                values.Add("Head Trail Time", 1f);
                values.Add("Head Trail Start Width", 1f);
                values.Add("Head Trail End Width", 1f);
                values.Add("Head Trail Start Color", 0);
                values.Add("Head Trail Start Opacity", 1f);
                values.Add("Head Trail End Color", 0);
                values.Add("Head Trail End Opacity", 0f);
                values.Add("Head Trail Position Offset", Vector2.zero);

                values.Add("Head Particles Emitting", false);
                values.Add("Head Particles Shape", Vector2Int.zero);
                values.Add("Head Particles Color", 0);
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

                values.Add("Boost Active", true);
                values.Add("Boost Shape", new Vector2Int(isCircle, 0));
                values.Add("Boost Position", gm.transform.Find("Player/boost").position.ToVector2());
                values.Add("Boost Scale", Vector2.one);
                values.Add("Boost Rotation", gm.transform.Find("Player/boost").localEulerAngles.z);

                values.Add("Boost Trail Emitting", false);
                values.Add("Boost Trail Time", 1f);
                values.Add("Boost Trail Start Width", 1f);
                values.Add("Boost Trail End Width", 1f);
                values.Add("Boost Trail Start Color", 0);
                values.Add("Boost Trail Start Opacity", 1f);
                values.Add("Boost Trail End Color", 0);
                values.Add("Boost Trail End Opacity", 0f);

                values.Add("Boost Particles Emitting", false);
                values.Add("Boost Particles Shape", Vector2Int.zero);
                values.Add("Boost Particles Color", 0);
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

                values.Add("Tail Base Distance", 2f);
                values.Add("Tail Base Mode", 0);

                for (int i = 1; i < 4; i++)
                {
                    values.Add(string.Format("Tail {0} Active", i), true);
                    values.Add(string.Format("Tail {0} Shape", i), new Vector2Int(isCircle, 0));
                    values.Add(string.Format("Tail {0} Position", i), Vector2.zero);
                    values.Add(string.Format("Tail {0} Scale", i), gm.transform.Find("trail/" + i).localScale.ToVector2());
                    values.Add(string.Format("Tail {0} Rotation", i), gm.transform.Find("trail/" + i).localEulerAngles.z);

                    var trail = gm.transform.Find("trail/" + i).GetComponent<TrailRenderer>();
                    values.Add(string.Format("Tail {0} Trail Emitting", i), true);
                    values.Add(string.Format("Tail {0} Trail Time", i), trail.time);
                    values.Add(string.Format("Tail {0} Trail Start Width", i), trail.startWidth);
                    values.Add(string.Format("Tail {0} Trail End Width", i), trail.endWidth);
                    values.Add(string.Format("Tail {0} Trail Start Color", i), 4);
                    values.Add(string.Format("Tail {0} Trail Start Opacity", i), 1f);
                    values.Add(string.Format("Tail {0} Trail End Color", i), 4);
                    values.Add(string.Format("Tail {0} Trail End Opacity", i), 0f);

                    values.Add(string.Format("Tail {0} Particles Emitting", i), false);
                    values.Add(string.Format("Tail {0} Particles Shape", i), Vector2Int.zero);
                    values.Add(string.Format("Tail {0} Particles Color", i), 0);
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

                values.Add("Custom Objects", new Dictionary<string, object>());
            }

            public void RegenValues()
            {
                values = new Dictionary<string, object>();
                values.Clear();

                values.Add("Base Name", "Nanobot");
                values.Add("Base ID", LSFunctions.LSText.randomNumString(16));
                values.Add("Base Health", 3);

                values.Add("Head Shape", Vector2Int.zero);
                values.Add("Head Position", Vector2.zero);
                values.Add("Head Scale", Vector2.one);
                values.Add("Head Rotation", 0f);

                values.Add("Head Trail Emitting", false);
                values.Add("Head Trail Time", 1f);
                values.Add("Head Trail Start Width", 1f);
                values.Add("Head Trail End Width", 1f);
                values.Add("Head Trail Start Color", 0);
                values.Add("Head Trail Start Opacity", 1f);
                values.Add("Head Trail End Color", 0);
                values.Add("Head Trail End Opacity", 0f);
                values.Add("Head Trail Position Offset", Vector2.zero);

                values.Add("Head Particles Emitting", false);
                values.Add("Head Particles Shape", Vector2Int.zero);
                values.Add("Head Particles Color", 0);
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

                values.Add("Boost Active", true);
                values.Add("Boost Shape", Vector2Int.zero);
                values.Add("Boost Position", Vector2.zero);
                values.Add("Boost Scale", Vector2.one);
                values.Add("Boost Rotation", 0f);

                values.Add("Boost Trail Emitting", false);
                values.Add("Boost Trail Time", 1f);
                values.Add("Boost Trail Start Width", 1f);
                values.Add("Boost Trail End Width", 1f);
                values.Add("Boost Trail Start Color", 0);
                values.Add("Boost Trail Start Opacity", 1f);
                values.Add("Boost Trail End Color", 0);
                values.Add("Boost Trail End Opacity", 0f);

                values.Add("Boost Particles Emitting", false);
                values.Add("Boost Particles Shape", Vector2Int.zero);
                values.Add("Boost Particles Color", 0);
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

                values.Add("Tail Base Distance", 2f);
                values.Add("Tail Base Mode", 0);

                for (int i = 1; i < 4; i++)
                {
                    values.Add(string.Format("Tail {0} Active", i), true);
                    values.Add(string.Format("Tail {0} Shape", i), Vector2Int.zero);
                    values.Add(string.Format("Tail {0} Position", i), Vector2.zero);
                    values.Add(string.Format("Tail {0} Scale", i), Vector2.one);
                    values.Add(string.Format("Tail {0} Rotation", i), 0f);

                    values.Add(string.Format("Tail {0} Trail Emitting", i), true);
                    values.Add(string.Format("Tail {0} Trail Time", i), 0.2f);
                    values.Add(string.Format("Tail {0} Trail Start Width", i), 0.5f);
                    values.Add(string.Format("Tail {0} Trail End Width", i), 0.2f);
                    values.Add(string.Format("Tail {0} Trail Start Color", i), 0);
                    values.Add(string.Format("Tail {0} Trail Start Opacity", i), 1f);
                    values.Add(string.Format("Tail {0} Trail End Color", i), 0);
                    values.Add(string.Format("Tail {0} Trail End Opacity", i), 0f);

                    values.Add(string.Format("Tail {0} Particles Emitting", i), false);
                    values.Add(string.Format("Tail {0} Particles Shape", i), Vector2Int.zero);
                    values.Add(string.Format("Tail {0} Particles Color", i), 0);
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

                values.Add("Custom Objects", new Dictionary<string, object>());
            }

            //Add a custom function property (e.g. only appears when boosting / zen mode is enabled, etc)
            public void CreateCustomObject()
            {
                var dictionary = (Dictionary<string, object>)values["Custom Objects"];

                var id = LSFunctions.LSText.randomNumString(16);

                dictionary.Add(id, new Dictionary<string, object>());

                ((Dictionary<string, object>)dictionary[id]).Add("ID", id);

                ((Dictionary<string, object>)dictionary[id]).Add("Shape", new Vector2Int(0, 0));
                ((Dictionary<string, object>)dictionary[id]).Add("Parent", 0);
                ((Dictionary<string, object>)dictionary[id]).Add("Depth", 0f);
                ((Dictionary<string, object>)dictionary[id]).Add("Position", new Vector2(0f, 0f));
                ((Dictionary<string, object>)dictionary[id]).Add("Scale", new Vector2(1f, 1f));
                ((Dictionary<string, object>)dictionary[id]).Add("Rotation", 0f);
                ((Dictionary<string, object>)dictionary[id]).Add("Color", 0);
                ((Dictionary<string, object>)dictionary[id]).Add("Opacity", 1f);
            }

            public GameObject gm;
            public Dictionary<string, object> values;
            public string filePath;
        }
    }
}
