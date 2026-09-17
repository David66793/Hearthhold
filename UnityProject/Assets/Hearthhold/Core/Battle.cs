using System;
using System.Collections.Generic;
using System.Text;

namespace Hearthhold.Core
{
    public sealed class Battle
    {
        public readonly List<Building> Buildings = new List<Building>();
        public readonly List<Unit> Units = new List<Unit>();
        public readonly List<CombatEffect> Effects = new List<CombatEffect>();
        public readonly List<string> Commands = new List<string>();
        public readonly int[] Available = new int[Rules.Troops.Length];
        public readonly int[] TroopLevels = new int[Rules.Troops.Length];
        public readonly Building[,] Occupied = new Building[Rules.MapSize, Rules.MapSize];
        public int TickNumber, Revision, Mission, SpellCharges = 2, FuryCharges = 1, FreezeCharges = 1, BreachCharges = 1,
            FocusCharges = 2, FocusTargetId = -1, FocusTicks, HealLevel = 1, NextUnitId = 1, InitialHousing;
        public bool Started, Finished, Settled;
        public int GoldReward, CrystalReward;
        public Battle(int mission) : this(Missions.Create(mission), mission) { }
        public Battle(List<Building> buildings, int mission) : this(buildings, mission, null) { }
        public Battle(List<Building> buildings, int mission, int[] army) : this(buildings, mission, army, null, 1) { }
        public Battle(List<Building> buildings, int mission, int[] army, int[] troopLevels, int healLevel)
        {
            Mission = mission;
            HealLevel = Math.Max(1, Math.Min(3, healLevel));
            foreach (Building original in buildings)
            {
                Building b = original.Copy(); b.Cooldown = 0;
                b.BattleHealthScale = Math.Max(1, Math.Min(3, 1 + mission / 2));
                if (b.Kind == BuildingKind.Keep && mission == 8) b.BattleHealthScale = 20;
                if (b.Kind == BuildingKind.Keep && mission >= 9) b.BattleHealthScale = 10;
                b.Health = b.MaxHealth; Buildings.Add(b);
            }
            for (int i = 0; i < Available.Length; i++)
            {
                TroopLevels[i] = troopLevels != null && i < troopLevels.Length ? Math.Max(1, Math.Min(3, troopLevels[i])) : 1;
                Available[i] = army != null && i < army.Length ? Math.Max(0, army[i]) : Rules.Troops[i].Count;
                InitialHousing += Available[i] * Rules.Troops[i].Housing;
            }
            RebuildGrid();
        }
        public int SecondsLeft { get { return Math.Max(0, 180 - TickNumber / Rules.TicksPerSecond); } }
        public int Destruction
        {
            get
            {
                int total = 0, destroyed = 0;
                foreach (Building b in Buildings) if (b.Kind != BuildingKind.Wall) { total++; if (b.Health <= 0) destroyed++; }
                return total == 0 ? 100 : destroyed * 100 / total;
            }
        }
        public int Stars
        {
            get
            {
                int stars = Destruction >= 50 ? 1 : 0;
                foreach (Building b in Buildings) if (b.Kind == BuildingKind.Keep && b.Health <= 0) { stars++; break; }
                if (Destruction == 100) stars++;
                return stars;
            }
        }
        public int AliveCount { get { int count = 0; foreach (Unit u in Units) if (u.Health > 0) count++; return count; } }
        public bool CanDeploy(int x, int z)
        {
            if (Finished || x < 500 || z < 500 || x >= 39500 || z >= 39500) return false;
            // A fixed, clearly rendered outer deployment band keeps scouting predictable.
            if (x > 10500 && x < 30500 && z > 10500 && z < 31500) return false;
            return Occupied[x / 1000, z / 1000] == null;
        }
        public bool Deploy(TroopKind kind, int x, int z)
        {
            if ((int)kind < 0 || (int)kind >= Available.Length || Available[(int)kind] <= 0 || !CanDeploy(x, z)) return false;
            x = x / 1000 * 1000 + 500; z = z / 1000 * 1000 + 500;
            if (!CanDeploy(x, z)) return false;
            Available[(int)kind]--;
            Units.Add(new Unit { Id = NextUnitId++, Kind = kind, X = x, Z = z, Health = MaxHealth(kind) });
            Commands.Add(TickNumber + ":deploy:" + (int)kind + ":" + x + ":" + z);
            Started = true;
            return true;
        }
        public bool NearestDeployment(int x, int z, out int deployX, out int deployZ)
        {
            deployX = deployZ = 0;
            long best = long.MaxValue;
            for (int cellX = 0; cellX < Rules.MapSize; cellX++)
                for (int cellZ = 0; cellZ < Rules.MapSize; cellZ++)
                {
                    int candidateX = cellX * 1000 + 500, candidateZ = cellZ * 1000 + 500;
                    if (!CanDeploy(candidateX, candidateZ)) continue;
                    long dx = candidateX - x, dz = candidateZ - z, distance = dx * dx + dz * dz;
                    if (distance >= best) continue;
                    best = distance; deployX = candidateX; deployZ = candidateZ;
                }
            return best != long.MaxValue;
        }
        public bool DeployNearest(TroopKind kind, int x, int z)
        {
            if ((int)kind < 0 || (int)kind >= Available.Length || Available[(int)kind] <= 0) return false;
            int deployX, deployZ;
            return NearestDeployment(x, z, out deployX, out deployZ) && Deploy(kind, deployX, deployZ);
        }
        public bool CastHeal(int x, int z)
        {
            if (!Started || Finished || SpellCharges <= 0 || x < 0 || z < 0 || x >= 40000 || z >= 40000) return false;
            SpellCharges--;
            foreach (Unit u in Units)
                if (u.Health > 0 && Distance(u.X, u.Z, x, z) <= 5000L * 5000)
                    u.Health = Math.Min(MaxHealth(u.Kind), u.Health + MaxHealth(u.Kind) * (20 + (HealLevel - 1) * 3) / 30);
            Effects.Add(new CombatEffect(x, z, x, z, 24, 3));
            Commands.Add(TickNumber + ":heal:" + x + ":" + z);
            return true;
        }
        public bool CastFury(int x, int z)
        {
            if (!ValidSpellTarget(x, z) || FuryCharges <= 0) return false;
            bool affected = false;
            foreach (Unit u in Units)
                if (u.Health > 0 && Distance(u.X, u.Z, x, z) <= 4500L * 4500)
                { u.FuryTicks = Math.Max(u.FuryTicks, 120); affected = true; }
            if (!affected) return false;
            FuryCharges--; Effects.Add(new CombatEffect(x, z, x, z, 35, 7)); Commands.Add(TickNumber + ":fury:" + x + ":" + z); return true;
        }
        public bool CastFreeze(int x, int z)
        {
            if (!ValidSpellTarget(x, z) || FreezeCharges <= 0) return false;
            bool affected = false;
            foreach (Building b in Buildings)
                if (b.Health > 0 && b.Spec.Damage > 0 && b.DistanceSquared(x, z) <= 4000L * 4000)
                { b.FrozenTicks = Math.Max(b.FrozenTicks, 80); affected = true; }
            if (!affected) return false;
            FreezeCharges--; Effects.Add(new CombatEffect(x, z, x, z, 32, 8)); Commands.Add(TickNumber + ":freeze:" + x + ":" + z); return true;
        }
        public bool CastBreach(int x, int z)
        {
            if (!ValidSpellTarget(x, z) || BreachCharges <= 0) return false;
            bool affected = false;
            foreach (Building b in Buildings)
                if (b.Health > 0 && b.Kind == BuildingKind.Wall && b.DistanceSquared(x, z) <= 3600L * 3600)
                { b.Health = 0; affected = true; }
            if (!affected) return false;
            BreachCharges--; Revision++; RebuildGrid(); Effects.Add(new CombatEffect(x, z, x, z, 30, 9)); Commands.Add(TickNumber + ":breach:" + x + ":" + z); return true;
        }
        private bool ValidSpellTarget(int x, int z) { return Started && !Finished && x >= 0 && z >= 0 && x < 40000 && z < 40000; }
        // A short-lived command that redirects the army without dealing free damage.
        // The player must expose a reachable target and keep troops alive long enough to exploit it.
        public bool CastFocus(int x, int z)
        {
            if (!Started || Finished || FocusCharges <= 0 || x < 0 || z < 0 || x >= 40000 || z >= 40000) return false;
            Building target = null;
            long nearest = 3500L * 3500;
            foreach (Building b in Buildings)
            {
                if (b.Health <= 0 || b.Kind == BuildingKind.Wall) continue;
                long distance = b.DistanceSquared(x, z);
                if (distance >= nearest) continue;
                nearest = distance; target = b;
            }
            if (target == null) return false;
            FocusCharges--; FocusTargetId = target.Id; FocusTicks = 140;
            foreach (Unit unit in Units) { unit.TargetId = -1; unit.PathRevision = -1; }
            Effects.Add(new CombatEffect(target.CenterX, target.CenterZ, target.CenterX, target.CenterZ, 20, 6));
            Commands.Add(TickNumber + ":focus:" + target.Id);
            return true;
        }
        public void Step()
        {
            if (!Started || Finished) return;
            TickNumber++;
            if (FocusTicks > 0 && --FocusTicks == 0) { FocusTargetId = -1; foreach (Unit unit in Units) unit.TargetId = -1; }
            for (int i = Effects.Count - 1; i >= 0; i--) if (--Effects[i].Ticks <= 0) Effects.RemoveAt(i);
            int activeUnits = Units.Count;
            for (int i = 0; i < activeUnits; i++) if (Units[i].Health > 0) StepUnit(Units[i]);
            foreach (Building b in Buildings)
            {
                if (b.Health <= 0 || b.Spec.Damage == 0) continue;
                if (b.FrozenTicks > 0) { b.FrozenTicks--; b.LockedUnitId = -1; b.LockTicks = 0; continue; }
                if (b.Cooldown > 0) { b.Cooldown--; continue; }
                Unit target = null; long nearest = long.MaxValue;
                foreach (Unit u in Units)
                {
                    if (u.Health <= 0) continue;
                    if (u.Spec.Flying ? !b.Spec.TargetsAir : !b.Spec.TargetsGround) continue;
                    long distance = Distance(b.CenterX, b.CenterZ, u.X, u.Z);
                    if (distance <= (long)b.Spec.Range * b.Spec.Range && distance >= (long)b.Spec.MinRange * b.Spec.MinRange && distance < nearest) { nearest = distance; target = u; }
                }
                if (target != null)
                {
                    // Smooth mission scaling: the old integer division gave missions 0-5 nearly identical damage.
                    int defenseDamage = Math.Max(1, b.Spec.Damage * (b.Level + 1) / 2 * (100 + Math.Min(Mission, 8) * 12) / 100);
                    if (b.Spec.RampDamage)
                    {
                        if (b.LockedUnitId == target.Id) b.LockTicks = Math.Min(60, b.LockTicks + b.Spec.Cooldown + 1);
                        else { b.LockedUnitId = target.Id; b.LockTicks = 0; }
                        defenseDamage = defenseDamage * (100 + b.LockTicks * 3) / 100;
                    }
                    target.Health = Math.Max(0, target.Health - defenseDamage);
                    if (b.Spec.SplashRadius > 0)
                        foreach (Unit nearby in Units)
                            if (nearby != target && nearby.Health > 0 && Distance(target.X, target.Z, nearby.X, nearby.Z) <= (long)b.Spec.SplashRadius * b.Spec.SplashRadius)
                                nearby.Health = Math.Max(0, nearby.Health - Math.Max(1, defenseDamage * 2 / 3));
                    b.Cooldown = Math.Max(8, b.Spec.Cooldown - Math.Min(Mission, 7));
                    Effects.Add(new CombatEffect(b.CenterX, b.CenterZ, target.X, target.Z, 7, b.Kind == BuildingKind.Watchtower ? 5 : 1));
                }
            }
            bool reserves = false; foreach (int count in Available) if (count > 0) reserves = true;
            if (Destruction >= 100 || SecondsLeft == 0 || (!reserves && AliveCount == 0)) Finish();
        }
        private void StepUnit(Unit u)
        {
            if (u.FuryTicks > 0) u.FuryTicks--;
            if (u.Cooldown > 0) u.Cooldown--;
            if (u.Spec.HealPower > 0) { StepMedic(u); return; }
            if (u.Spec.SummonCooldown > 0)
            {
                if (u.SummonTicks > 0) u.SummonTicks--;
                else if (SummonCount(u.Id) < 3)
                {
                    Unit shade = new Unit { Id = NextUnitId++, Kind = TroopKind.Vanguard, X = u.X, Z = u.Z, Health = Math.Max(80, MaxHealth(TroopKind.Vanguard) / 2), IsSummon = true, SummonerId = u.Id };
                    Units.Add(shade); u.SummonTicks = u.Spec.SummonCooldown; Effects.Add(new CombatEffect(u.X, u.Z, u.X, u.Z, 18, 10));
                }
            }
            Building target = FindById(u.TargetId);
            if (target == null || target.Health <= 0)
            {
                target = SelectTarget(u);
                if (target == null) return;
                u.TargetId = target.Id; u.PathRevision = -1;
            }
            if (target.DistanceSquared(u.X, u.Z) <= (long)u.Spec.Range * u.Spec.Range)
            { Attack(u, target); return; }
            if (u.Spec.Flying) { MoveDirect(u, target.CenterX, target.CenterZ); return; }
            if (u.PathRevision != Revision || u.PathIndex >= u.Path.Count && TickNumber >= u.RepathTick)
            {
                u.Path = Pathfinder.Find(Occupied, u.X / 1000, u.Z / 1000, target, u.Spec.Range, u.Kind == TroopKind.Sapper);
                u.PathIndex = 0; u.PathRevision = Revision; u.RepathTick = TickNumber + 20;
            }
            if (u.PathIndex >= u.Path.Count) return;
            Cell cell = u.Path[u.PathIndex];
            Building obstacle = Occupied[cell.X, cell.Z];
            if (obstacle != null && obstacle.Health > 0)
            {
                if (obstacle.Kind == BuildingKind.Wall && obstacle.DistanceSquared(u.X, u.Z) <= 1200L * 1200)
                { Attack(u, obstacle); return; }
                if (obstacle.Kind != BuildingKind.Wall) { u.PathRevision = -1; return; }
            }
            int px = cell.X * 1000 + 500, pz = cell.Z * 1000 + 500;
            long dx = px - u.X, dz = pz - u.Z;
            int length = IntegerSqrt(dx * dx + dz * dz);
            if (length <= u.Spec.Speed) { u.X = px; u.Z = pz; u.PathIndex++; }
            else { u.X += (int)(dx * u.Spec.Speed / length); u.Z += (int)(dz * u.Spec.Speed / length); }
        }
        private Building SelectTarget(Unit u)
        {
            if (FocusTicks > 0)
            {
                Building ordered = FindById(FocusTargetId);
                if (ordered != null && ordered.Health > 0) return ordered;
            }
            Building best = null; long bestScore = long.MaxValue;
            foreach (Building b in Buildings)
            {
                if (b.Health <= 0) continue;
                long score = b.DistanceSquared(u.X, u.Z);
                if (b.Kind == BuildingKind.Wall && !u.Spec.PreferWalls) score += 20000000000L;
                if (u.Spec.PreferDefenses && b.Spec.Damage == 0) score += 10000000000L;
                if (u.Spec.PreferWalls && b.Kind != BuildingKind.Wall) score += 10000000000L;
                if (score < bestScore) { bestScore = score; best = b; }
            }
            return best;
        }
        private void Attack(Unit u, Building b)
        {
            if (u.Cooldown > 0) return;
            int damage = Rules.TroopDamage(u.Kind, TroopLevels[(int)u.Kind]);
            if (u.IsSummon) damage = Math.Max(1, damage / 2);
            if (u.FuryTicks > 0) damage = damage * 3 / 2;
            if (u.Spec.PreferWalls && b.Kind == BuildingKind.Wall) damage *= 10;
            b.Health = Math.Max(0, b.Health - damage);
            if (u.Spec.SplashRadius > 0)
                foreach (Building nearby in Buildings)
                    if (nearby != b && nearby.Health > 0 && nearby.Kind != BuildingKind.Wall && Distance(b.CenterX, b.CenterZ, nearby.CenterX, nearby.CenterZ) <= (long)u.Spec.SplashRadius * u.Spec.SplashRadius)
                        nearby.Health = Math.Max(0, nearby.Health - Math.Max(1, damage / 2));
            u.Cooldown = u.Spec.Cooldown;
            Effects.Add(new CombatEffect(u.X, u.Z, b.CenterX, b.CenterZ, 5, u.Kind == TroopKind.Ranger ? 0 : 2));
            if (b.Health == 0)
            {
                Effects.Add(new CombatEffect(b.CenterX, b.CenterZ, b.CenterX, b.CenterZ, 22, 4));
                Revision++; RebuildGrid();
            }
        }
        private void StepMedic(Unit medic)
        {
            Unit target = null; long best = long.MaxValue;
            foreach (Unit ally in Units)
            {
                if (ally == medic || ally.Health <= 0 || ally.Health >= MaxHealth(ally.Kind)) continue;
                long distance = Distance(medic.X, medic.Z, ally.X, ally.Z);
                if (distance < best) { best = distance; target = ally; }
            }
            if (target == null) return;
            if (best <= (long)medic.Spec.Range * medic.Spec.Range && medic.Cooldown <= 0)
            {
                foreach (Unit ally in Units)
                    if (ally.Health > 0 && Distance(target.X, target.Z, ally.X, ally.Z) <= 1800L * 1800)
                        ally.Health = Math.Min(MaxHealth(ally.Kind), ally.Health + medic.Spec.HealPower * (medic.FuryTicks > 0 ? 3 : 2) / 2);
                medic.Cooldown = medic.Spec.Cooldown; Effects.Add(new CombatEffect(medic.X, medic.Z, target.X, target.Z, 8, 11)); return;
            }
            MoveDirect(medic, target.X, target.Z);
        }
        private void MoveDirect(Unit u, int targetX, int targetZ)
        {
            long dx = targetX - u.X, dz = targetZ - u.Z; int length = IntegerSqrt(dx * dx + dz * dz);
            int speed = u.FuryTicks > 0 ? u.Spec.Speed * 13 / 10 : u.Spec.Speed;
            if (length <= speed || length == 0) { u.X = targetX; u.Z = targetZ; }
            else { u.X += (int)(dx * speed / length); u.Z += (int)(dz * speed / length); }
        }
        private int SummonCount(int summonerId)
        {
            int count = 0; foreach (Unit unit in Units) if (unit.IsSummon && unit.SummonerId == summonerId && unit.Health > 0) count++; return count;
        }
        private void RebuildGrid()
        {
            Array.Clear(Occupied, 0, Occupied.Length);
            foreach (Building b in Buildings) if (b.Health > 0)
                for (int x = b.X; x < b.X + b.Spec.Size; x++)
                    for (int z = b.Z; z < b.Z + b.Spec.Size; z++) Occupied[x, z] = b;
        }
        private Building FindById(int id) { foreach (Building b in Buildings) if (b.Id == id) return b; return null; }
        public void Finish()
        {
            if (Finished) return;
            Finished = true;
            GoldReward = Destruction * (4 + Mission * 2) + Stars * 100;
            CrystalReward = Destruction * (2 + Mission) + Stars * 40;
        }
        public string StateFingerprint()
        {
            StringBuilder s = new StringBuilder();
            s.Append(TickNumber).Append('|').Append(Finished).Append('|').Append(SpellCharges).Append('|').Append(FuryCharges).Append('|').Append(FreezeCharges).Append('|').Append(BreachCharges).Append('|').Append(HealLevel)
                .Append('|').Append(FocusCharges).Append('|').Append(FocusTargetId).Append('|').Append(FocusTicks);
            foreach (int level in TroopLevels) s.Append('|').Append(level);
            foreach (int count in Available) s.Append('|').Append(count);
            foreach (Building b in Buildings) s.Append(';').Append(b.Id).Append(',').Append(b.Health).Append(',').Append(b.Cooldown).Append(',').Append(b.FrozenTicks).Append(',').Append(b.LockTicks).Append(',').Append(b.LockedUnitId);
            foreach (Unit u in Units) s.Append(';').Append(u.Id).Append(',').Append(u.X).Append(',').Append(u.Z).Append(',').Append(u.Health).Append(',').Append(u.Cooldown).Append(',').Append(u.TargetId).Append(',').Append(u.FuryTicks).Append(',').Append(u.SummonTicks).Append(',').Append(u.IsSummon).Append(',').Append(u.SummonerId);
            return s.ToString();
        }
        public int MaxHealth(TroopKind kind) { return Rules.TroopHealth(kind, TroopLevels[(int)kind]); }
        private static long Distance(int x1, int z1, int x2, int z2) { long x = x1 - x2, z = z1 - z2; return x * x + z * z; }
        private static int IntegerSqrt(long value)
        {
            long lo = 0, hi = Math.Min(value, 1000000);
            while (lo < hi) { long mid = (lo + hi + 1) / 2; if (mid * mid <= value) lo = mid; else hi = mid - 1; }
            return (int)lo;
        }
    }
}
