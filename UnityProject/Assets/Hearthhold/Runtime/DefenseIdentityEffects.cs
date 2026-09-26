using Hearthhold.Core;
using UnityEngine;

namespace Hearthhold.UnityClient
{
    // Defense effects follow attack mechanics: artillery arcs, anti-air bolts,
    // lightning discontinuities and a locked beam must not share one projectile.
    public sealed partial class GameBootstrap
    {
        private void PresentDefenseAttackEffect(CombatEffect effect, Vector3 start, Vector3 end)
        {
            AnimateBuildingAttack(effect.X, effect.Z, end);
            BuildingKind kind = BuildingKind.Cannon;
            foreach (Building building in session.Battle.Buildings)
                if (building.CenterX == effect.X && building.CenterZ == effect.Z)
                { kind = building.Kind; break; }

            switch (kind)
            {
                case BuildingKind.Mortar:
                    SpawnProjectile(start + Vector3.up * 0.85f, end, new Color32(74, 77, 79, 255), 0.43f, 3.2f);
                    SpawnPulse(end + Vector3.down, new Color32(185, 132, 84, 255), 0.3f, 3.7f, 0.45f);
                    SpawnBurst(end, new Color32(192, 149, 106, 255), 12, 2.5f, 0.7f);
                    break;
                case BuildingKind.AirDefense:
                    SpawnArrow(start + new Vector3(-0.28f, 0.95f, 0), end + Vector3.left * 0.10f, Gold, 0.18f, 0.25f);
                    SpawnArrow(start + new Vector3(0.28f, 0.95f, 0), end + Vector3.right * 0.10f, Gold, 0.18f, 0.25f);
                    SpawnBurst(end, Gold, 5, 0.95f, 0.36f);
                    break;
                case BuildingKind.ArcTower:
                    SpawnArcStrike(start + Vector3.up * 1.25f, end);
                    SpawnBurst(end, new Color32(126, 224, 244, 255), 8, 1.45f, 0.43f);
                    break;
                case BuildingKind.BeamTower:
                    SpawnDefenseTrace("Locked beam halo", start + Vector3.up * 1.05f, end, new Color32(234, 173, 95, 255), 0.22f, 0.23f);
                    SpawnDefenseTrace("Locked beam core", start + Vector3.up * 1.05f, end, new Color32(255, 242, 190, 255), 0.085f, 0.28f);
                    SpawnPulse(end + Vector3.down * 0.8f, Gold, 0.12f, 1.1f, 0.24f);
                    break;
                default:
                    SpawnProjectile(start + Vector3.up * 0.65f, end, Ember, 0.28f, 1.35f);
                    SpawnBurst(start + Vector3.up * 0.65f, new Color32(255, 204, 104, 255), 5, 1.25f, 0.28f);
                    SpawnBurst(end, Ember, 8, 1.8f, 0.55f);
                    break;
            }
            FlashUnit(effect.EndX, effect.EndZ, Danger);
        }

        private void SpawnArcStrike(Vector3 start, Vector3 end)
        {
            Vector3 previous = start;
            for (int i = 1; i <= 5; i++)
            {
                Vector3 next = Vector3.Lerp(start, end, i / 5f);
                if (i < 5) next += new Vector3(i % 2 == 0 ? -0.24f : 0.28f, i % 2 == 0 ? -0.15f : 0.17f, i % 2 == 0 ? 0.18f : -0.21f);
                SpawnDefenseTrace("Arc lightning segment", previous, next, new Color32(166, 244, 255, 255), 0.085f, 0.18f);
                previous = next;
            }
        }

        private void SpawnDefenseTrace(string name, Vector3 from, Vector3 to, Color color, float width, float seconds)
        {
            Vector3 direction = to - from;
            GameObject trace = Piece(name, PrimitiveType.Cube, (from + to) * 0.5f, new Vector3(width, width, direction.magnitude), color, effectsRoot);
            trace.transform.rotation = Quaternion.LookRotation(direction);
            trace.AddComponent<TimedWorldEffect>().Hold(seconds);
        }
    }
}
