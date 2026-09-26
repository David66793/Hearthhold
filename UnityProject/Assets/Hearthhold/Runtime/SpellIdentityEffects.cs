using Hearthhold.Core;
using UnityEngine;

namespace Hearthhold.UnityClient
{
    // A separate entry point for each spell's combat presentation. Shared field
    // plumbing does not decide its geometry, affected target, or event rhythm.
    public sealed partial class GameBootstrap
    {
        private void PresentHealEffect(CombatEffect effect)
        {
            Vector3 center = new Vector3(effect.X / 1000f, 0.12f, effect.Z / 1000f);
            Color glow = new Color32(188, 255, 213, 255);
            SpawnSpellField(center, Mint, glow, Rules.SpellRadius(SpellKind.Heal, session.Battle.SpellLevels[(int)SpellKind.Heal]) / 1000f, effect, 0);
            SpawnPulse(center, Mint, 0.5f, 8.5f, 0.6f);
            SpawnMotes(center, glow, 20);
            foreach (Unit unit in session.Battle.Units)
            {
                long dx = unit.X - effect.X, dz = unit.Z - effect.Z;
                if (unit.Health > 0 && dx * dx + dz * dz <= 25000000L)
                    Flash(unitViews.ContainsKey(unit.Id) ? unitViews[unit.Id] : null, Mint);
            }
        }

        private void PresentFuryEffect(CombatEffect effect)
        {
            Vector3 center = new Vector3(effect.X / 1000f, 0.13f, effect.Z / 1000f);
            SpawnSpellField(center, Ember, Gold, Rules.SpellRadius(SpellKind.Fury, session.Battle.SpellLevels[(int)SpellKind.Fury]) / 1000f, effect, 1);
            SpawnPulse(center, Gold, 0.4f, 9f, 1f);
            SpawnMotes(center, Ember, 20);
        }

        private void PresentFreezeEffect(CombatEffect effect)
        {
            Vector3 center = new Vector3(effect.X / 1000f, 0.13f, effect.Z / 1000f);
            Color ice = new Color32(122, 218, 255, 255);
            SpawnSpellField(center, ice, new Color32(208, 247, 255, 255), Rules.SpellRadius(SpellKind.Freeze, session.Battle.SpellLevels[(int)SpellKind.Freeze]) / 1000f, effect, 2);
            SpawnPulse(center, ice, 0.4f, 8f, 1f);
            SpawnMotes(center, ice, 18);
            foreach (Building building in session.Battle.Buildings)
                if (building.FrozenTicks > 0 && buildingViews.ContainsKey(building.Id))
                    MarkFrozenDefense(building, buildingViews[building.Id]);
        }

        private void PresentBreachEffect(CombatEffect effect)
        {
            Vector3 center = new Vector3(effect.X / 1000f, 0.16f, effect.Z / 1000f);
            SpawnPulse(center, new Color32(219, 159, 88, 255), 0.5f, 9f, 1f);
            SpawnPulse(center + Vector3.up * 0.04f, Ember, 0.25f, 6f, 0.8f);
            SpawnBurst(center, Ember, 24, 3f, 1.15f);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI * 2 / 12;
                Vector3 shardPosition = center + new Vector3(Mathf.Cos(angle) * (0.6f + i % 3 * 0.28f), 0.25f, Mathf.Sin(angle) * (0.6f + i % 3 * 0.28f));
                GameObject shard = Piece("Rift stone", PrimitiveType.Cube, shardPosition, new Vector3(0.22f, 0.35f, 0.32f), i % 2 == 0 ? Ember : Gold, effectsRoot);
                shard.AddComponent<TimedWorldEffect>().Debris(new Vector3(Mathf.Cos(angle) * 2.4f, 2.7f + i % 3 * 0.4f, Mathf.Sin(angle) * 2.4f), 1f);
            }
        }
    }
}
