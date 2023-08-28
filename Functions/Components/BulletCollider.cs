using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using DG.Tweening;

namespace CreativePlayers.Functions.Components
{
    public class BulletCollider : MonoBehaviour
    {
        public RTPlayer.PlayerObject playerObject;
        public RTPlayer player;
        public Rigidbody2D rb;
        public bool kill = false;
        public Tweener tweener;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.transform.parent.parent.name != player.name && kill)
            {
                tweener.Kill();

                player.boosts.Remove(playerObject);
                playerObject = null;
                Destroy(transform.parent.gameObject);
            }
        }
    }
}
