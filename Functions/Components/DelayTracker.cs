using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CreativePlayers.Functions.Components
{
    public class DelayTracker : MonoBehaviour
    {
		public RTPlayer player;

		private void Start()
		{
		}

		private void LateUpdate()
		{
			if (player != null && leader != null)
			{
				target = leader.position + offset * leader.transform.right;
				float p = Time.deltaTime * 60f;
				float num = 1f - Mathf.Pow(1f - followSharpness, p);
				if (gameObject.name != "Player" && player.tailMode == 1)
				{
					transform.position += (target - transform.position) * (num * AudioManager.inst.CurrentAudioSource.pitch);
					transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, 0f, leader.transform.rotation.eulerAngles.z), num * AudioManager.inst.CurrentAudioSource.pitch);
				}
			}
		}

		private void OnDrawGizmos()
		{
		}

		public Transform leader;

		public float followSharpness = 0.1f;

		public float offset;

		public Vector3 target;
	}
}
