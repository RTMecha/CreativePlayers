﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using HarmonyLib;

namespace CreativePlayers.Patchers
{
	[HarmonyPatch(typeof(OnTriggerEnterPass))]
	public class OnTriggerEnterPassPatch : MonoBehaviour
	{
		[HarmonyPatch("Start")]
		[HarmonyPrefix]
		private static bool TriggerPassPrefix(OnTriggerEnterPass __instance)
		{
			return false;
		}

		[HarmonyPatch("OnTriggerEnter2D")]
		[HarmonyPrefix]
		private static bool OnTriggerEnter2DPrefix(OnTriggerEnterPass __instance, Collider2D __0)
		{
			if (__instance.transform.parent.GetComponent<RTPlayer>())
				__instance.transform.parent.GetComponent<RTPlayer>().OnChildTriggerEnter(__0);
			return false;
		}

		[HarmonyPatch("OnTriggerEnter")]
		[HarmonyPrefix]
		private static bool OnTriggerEnterPrefix(OnTriggerEnterPass __instance, Collider __0)
		{
			__instance.transform.parent.GetComponent<RTPlayer>().OnChildTriggerStayMesh(__0);
			return false;
		}

		[HarmonyPatch("OnTriggerStay2D")]
		[HarmonyPrefix]
		private static bool OnTriggerStay2DPrefix(OnTriggerEnterPass __instance, Collider2D __0)
		{
			__instance.transform.parent.GetComponent<RTPlayer>().OnChildTriggerStay(__0);
			return false;
		}

		[HarmonyPatch("OnTriggerStay")]
		[HarmonyPrefix]
		private static bool OnTriggerStayPrefix(OnTriggerEnterPass __instance, Collider __0)
		{
			__instance.transform.parent.GetComponent<RTPlayer>().OnChildTriggerStayMesh(__0);
			return false;
		}
	}
}