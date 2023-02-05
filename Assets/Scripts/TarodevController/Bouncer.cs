using System;
using UnityEngine;

namespace TarodevController {
    public class Bouncer : MonoBehaviour {
        [SerializeField] private float _bounceForce = 60f;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.TryGetComponent(out IPlayerController controller)) {
                var incomingSpeedNormal = Vector3.Project(controller.Speed, transform.up); // vertical speed in direction of Bouncer
                controller.ApplyVelocity(-incomingSpeedNormal, PlayerForce.Burst); // cancel current vertical speed for more consistent heights
                controller.SetVelocity(transform.up * _bounceForce, PlayerForce.Decay);
            }
        }
    }
}