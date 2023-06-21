using System;
using System.Collections;
using LSFunctions;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Reflection;

namespace CreativePlayers.Functions.Components
{
    public class PlayerSelector : MonoBehaviour
    {
        public int id;
        public void OnMouseDown()
        {
            var playerEditor = GameObject.Find("PlayerEditorManager").GetComponentByName("CreativePlayersEditor");

            playerEditor.GetType().GetMethod("OpenDialog").Invoke(playerEditor, new object[] { });
        }
    }
}
