using GHPC;
using GHPC.PhysicsHelpers;
using GHPC.Weapons;
using UnityEngine;

namespace M1A1Abrams
{
    /// <summary>
    /// Proximity sensing shared by the XM1147 AMP round and the MPAT airburst.
    /// </summary>
    internal static class ProximityFuze
    {
        /// <summary>Closest the fuze may function to its own muzzle, in metres.</summary>
        internal const float MinClearance = 3f;

        // Shared so the per-frame sweep allocates nothing; this runs inside the round flight loop.
        private static readonly RaycastHit[] Hits = new RaycastHit[64];


        /// <summary>
        /// Sweeps the fuze envelope over one flight step and reports where the warhead has to
        /// function.
        /// </summary>
        /// <remarks>
        /// The flight loop moves a live round once per frame, by an entire frame of travel: at
        /// 1410 m/s and 60 fps that is about 23 m, several times the burst radius. Testing only
        /// the round's position at the end of the frame therefore steps straight over a target it
        /// flew past, so the envelope has to be swept across the whole step instead. The sweep
        /// must not be extended past the step either: a contact reported beyond it belongs to a
        /// target the round has not reached yet, and detonating on it would put the burst a whole
        /// frame early.
        ///
        /// <paramref name="burstPoint"/> is the first touch of the swept ball, so the round can be
        /// committed there and the burst lands exactly <paramref name="radius"/> from the target's
        /// surface instead of wherever the frame happened to end - the offset reported as the
        /// round exploding "some distance away" from the target.
        /// </remarks>
        internal static bool TryGetBurstPoint(LiveRound round, Vector3 launchPosition, float radius,
            Vector3 from, Vector3 to, out Vector3 burstPoint)
        {
            burstPoint = from;
            if (round == null || round.IsDestroyed || round.Info == null) return false;

            ConstantsAndInfoManager constants = ConstantsAndInfoManager.Instance;
            if (constants == null) return false;

            // Do not let the fuze function on the hull that launched it.
            float clearance = Mathf.Max(MinClearance, round.Info.ArmingDistance);
            Vector3 fromLaunch = to - launchPosition;
            if (fromLaunch.sqrMagnitude < clearance * clearance) return false;

            float fuzeRadius = Mathf.Max(radius, 0.1f);

            Vector3 direction = to - from;
            float travel = direction.magnitude;
            if (travel > 1e-4f)
            {
                direction /= travel;
            }
            else
            {
                direction = round.transform.forward;
                travel = 0f;
            }
            if (direction.sqrMagnitude < 0.5f) return false;

            // A sphere cast of `fuzeRadius` already covers everything within that radius of the
            // segment, so the bound is the step itself. A stationary round still gets its initial
            // overlap tested through the small floor.
            float reach = Mathf.Max(travel, 0.01f);

            int mask = constants.LaserRangefinderLayerMask;
            Ray sweep = new Ray(from, direction);

            // The game's own query wrapper switches on collider trees the engine has lazily
            // disabled along the path; a raw Physics call sails straight through those objects.
            // It only takes that path when the raycast-only layer is part of the mask, so warm
            // with it forced in. The result is discarded - this is the activation pass.
            RaycastColliderUtils.SphereCast(sweep, fuzeRadius, reach,
                mask | RaycastColliderUtils.RAYCAST_LAYER_MASK);

            int count = Physics.SphereCastNonAlloc(sweep, fuzeRadius, Hits, reach,
                mask & ~RaycastColliderUtils.GROUP_LAYER_MASK);

            // Nearest sensed object wins. The first hit alone is not enough: the terrain below a
            // low-flying round is hit first and would mask every target behind it.
            float contact = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider collider = Hits[i].collider;
                if (collider == null || Hits[i].distance >= contact) continue;
                if (!IsTarget(collider, round)) continue;
                contact = Hits[i].distance;
            }
            if (contact == float.MaxValue) return false;

            if (contact < 0f) contact = 0f;
            burstPoint = from + direction * contact;
            return true;
        }

        internal static bool IsTarget(Collider collider, LiveRound round)
        {
            if (collider == null || !collider.CompareTag("Penetrable")) return false;
            IArmor armor = collider.GetComponent<IArmor>();
            if (armor != null && ReferenceEquals(armor.Unit, round.Shooter)) return false;
            Unit unit = collider.GetComponentInParent<Unit>();
            return unit == null || !ReferenceEquals(unit, round.Shooter);
        }
    }
}
