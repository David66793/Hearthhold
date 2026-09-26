using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Hearthhold.Core
{
    public sealed class GameSession
    {
        public VillageData Village;
        public Battle Battle;
        public int MissionIndex;
        public string Notice = "欢迎来到篝火堡垒。建设聚落，带领远征队出发。";
        private readonly Stack<LayoutMove> undo = new Stack<LayoutMove>();
        private readonly Stack<LayoutMove> redo = new Stack<LayoutMove>();
        private struct LayoutMove { public int Id, OldX, OldZ, NewX, NewZ; }
        public GameSession(VillageData village)
        {
            Village = village; Village.EnsureProgress(); Village.EnsureTechnology(); Village.EnsureHeroes(); Village.EnsureEquipment(); Village.EnsureArmy();
            CompleteLegacyTraining();
        }
        private bool CompleteLegacyTraining()
        {
            if (Village.TrainingQueue.Count == 0) { Village.TrainingStartedUtcTicks = 0; return false; }
            bool changed = false; int completed = 0, refunded = 0;
            foreach (int kind in Village.TrainingQueue)
            {
                if (kind < 0 || kind >= Rules.Troops.Length) continue;
                if (Village.ArmyHousing + Rules.Troops[kind].Housing <= Village.ArmyCapacity)
                { Village.ArmyCounts[kind]++; completed++; changed = true; }
                else
                {
                    int refund = Math.Min(Rules.Troops[kind].TrainCost, Village.Capacity - Village.Gold);
                    Village.Gold += refund; refunded += refund; changed = true;
                }
            }
            Village.TrainingQueue.Clear(); Village.TrainingStartedUtcTicks = 0;
            if (changed) Notice = "旧版训练队列已处理：" + completed + " 名立即就绪"
                + (refunded > 0 ? "，超出营位的项目返还 " + refunded + " 金币" : "") + "。当前版本造兵无需等待。";
            return changed;
        }
        public bool Build(BuildingKind kind, int x, int z)
        {
            if (Battle != null) return false;
            if ((int)kind <= 0 || (int)kind >= Rules.Buildings.Length) { Notice = "议事堡只能拥有一座。"; return false; }
            if (Village.AtLimit(kind)) { Notice = Rules.Spec(kind).Name + "已达数量上限 " + Village.Limit(kind) + "。" + ((kind == BuildingKind.Barracks || kind == BuildingKind.TrainingCamp || kind == BuildingKind.Laboratory) ? "该建筑只能建一座。" : "升级议事堡可提高上限。"); return false; }
            if (!Village.CanPlace(kind, x, z, -1)) { Notice = "这里没有足够的空间。请选择绿色区域。"; return false; }
            if (kind == BuildingKind.HeroHall && !Village.HeroHallPermit && Village.CoreSigils < 1) { Notice = "建造英雄殿堂需要一枚炉心印记；首次通关第 1 关可获得。"; return false; }
            int cost = Rules.Spec(kind).Cost;
            if (Village.Gold < cost) { Notice = "金币不足，收取产出或完成一次远征。"; return false; }
            Collect(DateTime.UtcNow);
            Village.Gold -= cost;
            Village.Add(kind, x, z);
            if (kind == BuildingKind.HeroHall)
            {
                Village.EnsureHeroes();
                Village.EnsureEquipment();
                if (!Village.HeroHallPermit) { Village.CoreSigils--; Village.HeroHallPermit = true; }
                for (int i = 0; i < 2; i++) Village.EquipmentLevels[i] = Math.Max(1, Village.EquipmentLevels[i]);
                if (Village.HeroEquipmentSlots[0] < 0 && Village.HeroEquipmentSlots[1] < 0)
                { Village.HeroEquipmentSlots[0] = 0; Village.HeroEquipmentSlots[1] = 1; }
                Village.HeroLevels[(int)HeroKind.EmberWarden] = Math.Max(1, Village.HeroLevels[(int)HeroKind.EmberWarden]);
            }
            if (kind == BuildingKind.PetLodge)
            {
                Village.EnsureHeroes(); Village.PetLevels[(int)PetKind.CinderFox] = Math.Max(1, Village.PetLevels[(int)PetKind.CinderFox]);
                if (Village.HeroPetAssignments[(int)HeroKind.EmberWarden] < 0) Village.HeroPetAssignments[(int)HeroKind.EmberWarden] = (int)PetKind.CinderFox;
            }
            undo.Clear(); redo.Clear();
            Notice = Rules.Spec(kind).Name + "建造完成。";
            return true;
        }
        public bool Move(int id, int x, int z)
        {
            if (Battle != null) return false;
            Building b = Find(id);
            if (b == null || !Village.CanPlace(b.Kind, x, z, id)) { Notice = "建筑移动失败：目标位置被占用。"; return false; }
            if (b.X == x && b.Z == z) return false;
            undo.Push(new LayoutMove { Id = id, OldX = b.X, OldZ = b.Z, NewX = x, NewZ = z });
            redo.Clear(); b.X = x; b.Z = z;
            Notice = "布局已更新 · Ctrl+Z 可撤销移动。";
            return true;
        }
        public bool Undo(bool forward)
        {
            if (Battle != null) return false;
            Stack<LayoutMove> from = forward ? redo : undo, to = forward ? undo : redo;
            if (from.Count == 0) { Notice = "没有可" + (forward ? "重做" : "撤销") + "的移动。"; return false; }
            LayoutMove move = from.Peek();
            Building b = Find(move.Id);
            int x = forward ? move.NewX : move.OldX, z = forward ? move.NewZ : move.OldZ;
            if (b == null || !Village.CanPlace(b.Kind, x, z, b.Id)) return false;
            from.Pop(); to.Push(move); b.X = x; b.Z = z;
            Notice = forward ? "已重做建筑移动。" : "已撤销建筑移动。";
            return true;
        }
        public Building Find(int id) { foreach (Building b in Village.Buildings) if (b.Id == id) return b; return null; }
        public void DemolitionRefund(Building b, out int gold, out int crystal)
        {
            gold = crystal = 0;
            if (b == null || b.Kind == BuildingKind.Keep) return;
            int upgradeTiers = b.Level * (b.Level - 1) / 2;
            gold = (b.Spec.Cost + Math.Max(80, b.Spec.Cost) * upgradeTiers) / 2;
            crystal = 55 * upgradeTiers / 2;
        }
        public bool Demolish(int id)
        {
            if (Battle != null) { Notice = "远征期间不能拆除基地建筑。"; return false; }
            Building b = Find(id);
            if (b == null) { Notice = "建筑不存在或已拆除。"; return false; }
            if (b.Kind == BuildingKind.Keep) { Notice = "议事堡是聚落核心，不能拆除。"; return false; }
            int gold, crystal; DemolitionRefund(b, out gold, out crystal);
            Collect(DateTime.UtcNow);
            int actualGold = Math.Min(gold, Village.Capacity - Village.Gold), actualCrystal = Math.Min(crystal, Village.Capacity - Village.Crystal);
            Village.Buildings.Remove(b);
            Village.Gold += actualGold; Village.Crystal += actualCrystal;
            undo.Clear(); redo.Clear();
            Notice = "已拆除" + b.Spec.Name + "，返还 " + actualGold + " 金币、" + actualCrystal + " 晶露。";
            return true;
        }
        public int UpgradeGold(Building b) { return b.Kind == BuildingKind.Keep ? 650 * b.Level : Math.Max(80, b.Spec.Cost) * b.Level; }
        public int UpgradeCrystal(Building b) { return (b.Kind == BuildingKind.Keep ? 180 : 55) * b.Level; }
        public List<Building> WallRow(int id)
        {
            Building origin = Find(id);
            List<Building> result = new List<Building>();
            if (origin == null || origin.Kind != BuildingKind.Wall) return result;
            List<Building> horizontal = ConnectedWalls(origin, 1, 0);
            List<Building> vertical = ConnectedWalls(origin, 0, 1);
            return horizontal.Count >= vertical.Count ? horizontal : vertical;
        }
        private List<Building> ConnectedWalls(Building origin, int dx, int dz)
        {
            List<Building> result = new List<Building>();
            for (int direction = -1; direction <= 1; direction += 2)
            {
                int x = origin.X + dx * direction, z = origin.Z + dz * direction;
                while (true)
                {
                    Building wall = Village.At(x, z);
                    if (wall == null || wall.Kind != BuildingKind.Wall) break;
                    if (direction < 0) result.Insert(0, wall); else result.Add(wall);
                    x += dx * direction; z += dz * direction;
                }
            }
            int insert = 0;
            while (insert < result.Count && (dx != 0 ? result[insert].X : result[insert].Z) < (dx != 0 ? origin.X : origin.Z)) insert++;
            result.Insert(insert, origin);
            return result;
        }
        public bool ExtendWallStroke(IList<int> selectedIds, int targetId)
        {
            if (Battle != null || selectedIds == null || selectedIds.Count == 0) return false;
            Building target = Find(targetId);
            if (target == null || target.Kind != BuildingKind.Wall) return false;
            List<Building> walls = new List<Building>();
            HashSet<int> seen = new HashSet<int>();
            foreach (int id in selectedIds)
            {
                Building wall = Find(id);
                if (wall == null || wall.Kind != BuildingKind.Wall || !seen.Add(id)) return false;
                walls.Add(wall);
            }
            if (seen.Contains(targetId)) return false;
            bool alongX;
            if (walls.Count == 1)
            {
                if (target.Z == walls[0].Z && target.X != walls[0].X) alongX = true;
                else if (target.X == walls[0].X && target.Z != walls[0].Z) alongX = false;
                else return false;
            }
            else
            {
                alongX = walls[0].Z == walls[1].Z;
                int fixedCoordinate = alongX ? walls[0].Z : walls[0].X;
                foreach (Building wall in walls)
                    if ((alongX ? wall.Z : wall.X) != fixedCoordinate) return false;
                if ((alongX ? target.Z : target.X) != fixedCoordinate) return false;
            }
            walls.Sort((a, b) => (alongX ? a.X : a.Z).CompareTo(alongX ? b.X : b.Z));
            for (int i = 1; i < walls.Count; i++)
                if ((alongX ? walls[i].X - walls[i - 1].X : walls[i].Z - walls[i - 1].Z) != 1) return false;
            int minimum = alongX ? walls[0].X : walls[0].Z;
            int maximum = alongX ? walls[walls.Count - 1].X : walls[walls.Count - 1].Z;
            int targetCoordinate = alongX ? target.X : target.Z;
            if (targetCoordinate >= minimum && targetCoordinate <= maximum) return false;
            int fixedAxis = alongX ? walls[0].Z : walls[0].X;
            int start = targetCoordinate < minimum ? targetCoordinate : maximum + 1;
            int end = targetCoordinate < minimum ? minimum - 1 : targetCoordinate;
            List<Building> extension = new List<Building>();
            for (int coordinate = start; coordinate <= end; coordinate++)
            {
                Building wall = Village.At(alongX ? coordinate : fixedAxis, alongX ? fixedAxis : coordinate);
                if (wall == null || wall.Kind != BuildingKind.Wall || seen.Contains(wall.Id)) return false;
                extension.Add(wall);
            }
            if (targetCoordinate < minimum)
                for (int i = extension.Count - 1; i >= 0; i--) selectedIds.Insert(0, extension[i].Id);
            else foreach (Building wall in extension) selectedIds.Add(wall.Id);
            return true;
        }
        public bool UpgradeWallRow(IList<int> ids)
        {
            if (Battle != null || ids == null || ids.Count == 0) return false;
            List<Building> walls = new List<Building>(); HashSet<int> seen = new HashSet<int>();
            int gold = 0, crystal = 0;
            foreach (int id in ids)
            {
                Building wall = Find(id);
                if (wall == null || wall.Kind != BuildingKind.Wall || !seen.Add(id)) { Notice = "城墙排选择已失效，请重新选择。"; return false; }
                if (wall.Level >= Rules.MaxBuildingLevel(wall.Kind)) { Notice = "所选城墙中有墙段已达到最高等级。"; return false; }
                if (wall.Level >= Village.KeepLevel) { Notice = "请先升级议事堡，解锁城墙等级。"; return false; }
                walls.Add(wall); gold += UpgradeGold(wall); crystal += UpgradeCrystal(wall);
            }
            Collect(DateTime.UtcNow);
            if (Village.Gold < gold || Village.Crystal < crystal) { Notice = "整排升级需要 " + gold + " 金币 / " + crystal + " 晶露。"; return false; }
            Village.Gold -= gold; Village.Crystal -= crystal;
            foreach (Building wall in walls) { wall.Level++; wall.Health = wall.MaxHealth; }
            Notice = "已将连续城墙 ×" + walls.Count + " 全部升级。";
            return true;
        }
        public bool CanMoveWallRow(IList<int> ids, int anchorId, int targetX, int targetZ)
        {
            if (Battle != null || ids == null || ids.Count < 2) return false;
            Building anchor = Find(anchorId);
            if (anchor == null || anchor.Kind != BuildingKind.Wall) return false;
            HashSet<int> movingIds = new HashSet<int>();
            foreach (int id in ids)
            {
                Building wall = Find(id);
                if (wall == null || wall.Kind != BuildingKind.Wall || !movingIds.Add(id)) return false;
            }
            int dx = targetX - anchor.X, dz = targetZ - anchor.Z;
            foreach (int id in ids)
            {
                Building wall = Find(id); int x = wall.X + dx, z = wall.Z + dz;
                if (x < 0 || z < 0 || x >= Rules.MapSize || z >= Rules.MapSize) return false;
                foreach (Building other in Village.Buildings)
                    if (!movingIds.Contains(other.Id) && other.Contains(x, z)) return false;
            }
            return true;
        }
        public bool MoveWallRow(IList<int> ids, int anchorId, int targetX, int targetZ)
        {
            if (!CanMoveWallRow(ids, anchorId, targetX, targetZ)) { Notice = "整排城墙无法放在这里：目标格越界或被占用。"; return false; }
            Building anchor = Find(anchorId); int dx = targetX - anchor.X, dz = targetZ - anchor.Z;
            foreach (int id in ids) { Building wall = Find(id); wall.X += dx; wall.Z += dz; }
            undo.Clear(); redo.Clear();
            Notice = "已整体移动连续城墙 ×" + ids.Count + "。";
            return true;
        }
        public bool CanMoveGroup(IList<int> ids, int dx, int dz)
        {
            if (Battle != null || ids == null || ids.Count == 0) return false;
            HashSet<int> movingIds = new HashSet<int>();
            foreach (int id in ids) if (Find(id) == null || !movingIds.Add(id)) return false;
            foreach (int id in ids)
            {
                Building movingBuilding = Find(id);
                int x = movingBuilding.X + dx, z = movingBuilding.Z + dz, size = movingBuilding.Spec.Size;
                if (x < 2 || z < 2 || x + size > Rules.MapSize - 2 || z + size > Rules.MapSize - 2) return false;
                foreach (Building other in Village.Buildings)
                    if (!movingIds.Contains(other.Id) && x < other.X + other.Spec.Size && x + size > other.X
                        && z < other.Z + other.Spec.Size && z + size > other.Z) return false;
            }
            return true;
        }
        public bool MoveGroup(IList<int> ids, int dx, int dz)
        {
            if (!CanMoveGroup(ids, dx, dz)) { Notice = "整体无法移动到这里：有建筑越界或与未选建筑重叠。"; return false; }
            foreach (int id in ids) { Building building = Find(id); building.X += dx; building.Z += dz; }
            undo.Clear(); redo.Clear();
            Notice = "已整体移动 " + ids.Count + " 座建筑。";
            return true;
        }
        public bool Upgrade(int id)
        {
            if (Battle != null) return false;
            Building b = Find(id); if (b == null) return false;
            if (b.Level >= Rules.MaxBuildingLevel(b.Kind)) { Notice = "已达到当前版本的最高等级。"; return false; }
            if (b.Kind != BuildingKind.Keep && b.Level >= Village.KeepLevel) { Notice = "请先升级议事堡，解锁建筑等级。"; return false; }
            int gold = UpgradeGold(b), crystal = UpgradeCrystal(b);
            if (Village.Gold < gold || Village.Crystal < crystal) { Notice = "升级资源不足。"; return false; }
            Collect(DateTime.UtcNow);
            Village.Gold -= gold; Village.Crystal -= crystal; b.Level++; b.Health = b.MaxHealth;
            Notice = b.Spec.Name + "已升至 " + b.Level + " 级。";
            return true;
        }
        public bool ImproveEquipment(EquipmentKind kind)
        {
            if (Battle != null || (int)kind < 0 || (int)kind >= Rules.EquipmentNames.Length) return false;
            Village.EnsureEquipment(); int index = (int)kind, level = Village.EquipmentLevels[index];
            if (Village.HeroHallLevel == 0) { Notice = "需要英雄殿堂。"; return false; }
            if (level >= 3) { Notice = "装备已达最高等级。"; return false; }
            if (level > 0 && Village.HeroHallLevel <= level) { Notice = "先升级英雄殿堂。"; return false; }
            int gold = Rules.EquipmentGoldCost(level), dust = Rules.EquipmentDustCost(level);
            if (Village.Gold < gold || Village.Stardust < dust) { Notice = "锻造材料不足：需要 " + gold + " 金币与 " + dust + " 星辉粉尘。"; return false; }
            Village.Gold -= gold; Village.Stardust -= dust; Village.EquipmentLevels[index]++;
            Notice = Rules.EquipmentNames[index] + "提升至 " + Village.EquipmentLevels[index] + " 级。"; return true;
        }
        public bool EquipHero(HeroKind hero, int slot, EquipmentKind? kind)
        {
            if (Battle != null || slot < 0 || slot > 1 || (int)hero < 0 || (int)hero >= Rules.Heroes.Length) return false;
            Village.EnsureEquipment();
            if (!Village.IsHeroUnlocked(hero)) return false;
            int index = kind.HasValue ? (int)kind.Value : -1;
            if (index >= Rules.EquipmentNames.Length || index < -1 || index >= 0 && Village.EquipmentLevels[index] <= 0) return false;
            int target = (int)hero * 2 + slot, other = (int)hero * 2 + 1 - slot;
            if (index >= 0 && Village.HeroEquipmentSlots[other] == index) return false;
            if (Village.HeroEquipmentSlots[target] == index) return false;
            Village.HeroEquipmentSlots[target] = index;
            Notice = index < 0 ? "装备已卸下。" : Rules.EquipmentNames[index] + "已装配。"; return true;
        }
        public bool UpgradeHero(HeroKind kind)
        {
            if (Battle != null || (int)kind < 0 || (int)kind >= Rules.Heroes.Length) return false;
            Village.EnsureHeroes(); int level = Village.HeroLevels[(int)kind];
            if (!Village.IsHeroUnlocked(kind)) { Notice = "先将议事堡升至4级并建造英雄殿堂。"; return false; }
            if (level >= Village.HeroHallLevel || level >= 4) { Notice = level >= 4 ? "英雄已达到当前最高等级。" : "先升级英雄殿堂，提高英雄等级上限。"; return false; }
            int gold = Rules.HeroUpgradeGold(level), crystal = Rules.HeroUpgradeCrystal(level);
            if (Village.Gold < gold || Village.Crystal < crystal) { Notice = "英雄升级需要 " + gold + " 金币 / " + crystal + " 晶露。"; return false; }
            Collect(DateTime.UtcNow); Village.Gold -= gold; Village.Crystal -= crystal; Village.HeroLevels[(int)kind]++;
            Notice = Rules.Spec(kind).Name + "已升至 " + Village.HeroLevels[(int)kind] + " 级。"; return true;
        }
        public bool UpgradePet(PetKind kind)
        {
            if (Battle != null || (int)kind < 0 || (int)kind >= Rules.Pets.Length) return false;
            Village.EnsureHeroes(); int level = Village.PetLevels[(int)kind];
            if (!Village.IsPetUnlocked(kind)) { Notice = "先建造战宠小屋并解锁该战宠。"; return false; }
            if (level >= Village.PetLodgeLevel || level >= 4) { Notice = level >= 4 ? "战宠已达到当前最高等级。" : "先升级战宠小屋，提高战宠等级上限。"; return false; }
            int crystal = Rules.PetUpgradeCrystal(level);
            if (Village.Crystal < crystal) { Notice = "战宠升级需要 " + crystal + " 晶露。"; return false; }
            Collect(DateTime.UtcNow); Village.Crystal -= crystal; Village.PetLevels[(int)kind]++;
            Notice = Rules.Spec(kind).Name + "已升至 " + Village.PetLevels[(int)kind] + " 级。"; return true;
        }
        public bool AssignPet(HeroKind hero, PetKind? pet)
        {
            if (Battle != null || (int)hero < 0 || (int)hero >= Rules.Heroes.Length) return false;
            Village.EnsureHeroes(); int petIndex = pet.HasValue ? (int)pet.Value : -1;
            if (!Village.IsHeroUnlocked(hero) || petIndex >= 0 && (petIndex >= Rules.Pets.Length || !Village.IsPetUnlocked(pet.Value))) { Notice = "英雄或战宠尚未解锁。"; return false; }
            if (petIndex >= 0) for (int i = 0; i < Village.HeroPetAssignments.Count; i++) if (i != (int)hero && Village.HeroPetAssignments[i] == petIndex) Village.HeroPetAssignments[i] = -1;
            Village.HeroPetAssignments[(int)hero] = petIndex;
            Notice = petIndex < 0 ? Rules.Spec(hero).Name + "已取消战宠绑定。" : Rules.Spec(pet.Value).Name + "已绑定" + Rules.Spec(hero).Name + "。"; return true;
        }
        public bool ResearchTroop(TroopKind kind)
        {
            if (Battle != null || (int)kind < 0 || (int)kind >= Rules.Troops.Length) return false;
            Village.EnsureTechnology();
            int level = Village.TroopLevels[(int)kind];
            if (Village.LaboratoryLevel <= level) { Notice = "实验室等级不足，先升级实验室。"; return false; }
            if (!Village.IsTroopUnlocked(kind)) { Notice = "该兵种尚未由训练营解锁。"; return false; }
            int gold = Rules.ResearchGold(level), crystal = Rules.ResearchCrystal(level);
            if (Village.Gold < gold || Village.Crystal < crystal) { Notice = "研究资源不足：需要 " + gold + " 金 / " + crystal + " 晶。"; return false; }
            Collect(DateTime.UtcNow); Village.Gold -= gold; Village.Crystal -= crystal;
            Village.TroopLevels[(int)kind]++;
            Notice = Rules.Spec(kind).Name + "研究完成，升至 " + Village.TroopLevels[(int)kind] + " 级。";
            return true;
        }
        public bool ResearchHeal() { return ResearchSpell(SpellKind.Heal); }
        public bool ResearchSpell(SpellKind kind)
        {
            if (Battle != null || (int)kind < 0 || (int)kind >= Rules.SpellNames.Length) return false;
            Village.EnsureTechnology();
            int level = Village.SpellLevels[(int)kind], unlock = Rules.SpellUnlockKeepLevel(kind);
            if (Village.KeepLevel < unlock) { Notice = Rules.SpellNames[(int)kind] + "需要议事堡 " + unlock + " 级。"; return false; }
            if (Village.LaboratoryLevel < unlock) { Notice = "先将实验室升至 " + unlock + " 级，解锁" + Rules.SpellNames[(int)kind] + "。"; return false; }
            if (level >= 3 || level > 0 && Village.LaboratoryLevel <= level) { Notice = level >= 3 ? "该法术已达到最高等级。" : "实验室等级不足，先升级实验室。"; return false; }
            int nextCostLevel = Math.Max(1, level), gold = Rules.ResearchGold(nextCostLevel), crystal = Rules.ResearchCrystal(nextCostLevel);
            if (Village.Gold < gold || Village.Crystal < crystal) { Notice = "研究资源不足：需要 " + gold + " 金 / " + crystal + " 晶。"; return false; }
            Collect(DateTime.UtcNow); Village.Gold -= gold; Village.Crystal -= crystal;
            Village.SpellLevels[(int)kind] = level + 1;
            Village.HealLevel = Village.SpellLevels[(int)SpellKind.Heal];
            Notice = Rules.SpellNames[(int)kind] + (level == 0 ? "已解锁" : "研究完成，升至 " + (level + 1) + " 级") + "。";
            return true;
        }
        public void Income(DateTime utcNow, out int gold, out int crystal)
        {
            long elapsed = Math.Max(0, utcNow.Ticks - Village.LastIncomeUtcTicks);
            long seconds = Math.Min(8 * 3600, elapsed / TimeSpan.TicksPerSecond);
            int goldRate = 0, crystalRate = 0;
            foreach (Building b in Village.Buildings)
            {
                if (b.Kind == BuildingKind.Mine) goldRate += 24 * b.Level;
                if (b.Kind == BuildingKind.Reservoir) crystalRate += 18 * b.Level;
            }
            gold = (int)Math.Min(Village.Capacity - Village.Gold, seconds * goldRate / 60);
            crystal = (int)Math.Min(Village.Capacity - Village.Crystal, seconds * crystalRate / 60);
        }
        public void Collect(DateTime utcNow)
        {
            if (Battle != null) return;
            int gold, crystal; Income(utcNow, out gold, out crystal);
            Village.Gold += gold; Village.Crystal += crystal;
            // Local demo clock, intentionally not an online-economy trust boundary.
            Village.LastIncomeUtcTicks = utcNow.Ticks;
            Notice = "收取 " + gold + " 金币、" + crystal + " 晶露。";
        }
        public bool AdvanceTraining(DateTime utcNow)
        {
            Village.EnsureArmy();
            if (Battle != null) return false;
            return CompleteLegacyTraining();
        }
        public int TrainingSecondsLeft(DateTime utcNow)
        {
            Village.EnsureArmy();
            if (Village.TrainingQueue.Count == 0) return 0;
            TroopSpec spec = Rules.Troops[Village.TrainingQueue[0]];
            if (Village.TrainingStartedUtcTicks <= 0 || Village.TrainingStartedUtcTicks > utcNow.Ticks) return spec.TrainSeconds;
            long elapsed = utcNow.Ticks - Village.TrainingStartedUtcTicks;
            return Math.Max(0, spec.TrainSeconds - (int)(elapsed / TimeSpan.TicksPerSecond));
        }
        public bool QueueTroop(TroopKind kind, DateTime utcNow)
        { return QueueTroops(kind, 1, utcNow); }
        public bool QueueTroops(TroopKind kind, int count, DateTime utcNow)
        {
            if (Battle != null || (int)kind < 0 || (int)kind >= Rules.Troops.Length || count < 1 || count > 20) return false;
            TroopSpec spec = Rules.Spec(kind);
            if (Village.Count(BuildingKind.Barracks) == 0) { Notice = "请先建造兵营。"; return false; }
            if (Village.Count(BuildingKind.TrainingCamp) == 0) { Notice = "请先建造训练营。"; return false; }
            if (!Village.IsTroopUnlocked(kind)) { Notice = "先升级训练营，解锁" + spec.Name + "。"; return false; }
            if (Village.ArmyHousing + (long)spec.Housing * count > Village.ArmyCapacity)
            { Notice = "需要 " + spec.Housing * count + " 营位；请扩建兵营或调整现有编队。"; return false; }
            if (Village.Gold < (long)spec.TrainCost * count)
            { Notice = "训练 " + count + " 名" + spec.Name + "需要 " + spec.TrainCost * count + " 金币。"; return false; }
            Village.Gold -= spec.TrainCost * count;
            Village.ArmyCounts[(int)kind] += count;
            Notice = count + " 名" + spec.Name + "已立即就绪 · 共 " + spec.Housing * count + " 营位。";
            return true;
        }
        public bool CancelLastTraining(DateTime utcNow)
        {
            if (Battle != null) return false;
            Notice = "当前版本造兵立即完成，没有可取消的训练队列。"; return false;
        }
        public bool DismissTroop(TroopKind kind)
        {
            if (Battle != null || (int)kind < 0 || (int)kind >= Rules.Troops.Length) return false;
            Village.EnsureArmy(); int index = (int)kind;
            if (Village.ArmyCounts[index] <= 0) { Notice = "没有可遣散的" + Rules.Spec(kind).Name + "。"; return false; }
            Village.ArmyCounts[index]--;
            Notice = "已遣散1名" + Rules.Spec(kind).Name + "，腾出" + Rules.Spec(kind).Housing + "营位。";
            return true;
        }
        public bool QueueFormation(int preset, DateTime utcNow)
        {
            if (Battle != null || preset < 0 || preset >= Rules.FormationCounts.Length) return false;
            int[] counts = Rules.FormationCounts[preset], missing = new int[counts.Length];
            int cost = 0, housing = 0, soldiers = 0;
            for (int i = 0; i < counts.Length; i++)
            {
                missing[i] = Math.Max(0, counts[i] - Village.ArmyCounts[i]);
                if (missing[i] > 0 && !Village.IsTroopUnlocked((TroopKind)i)) { Notice = "训练营尚未解锁" + Rules.Troops[i].Name + "；请选择其他编队。"; return false; }
                cost += missing[i] * Rules.Troops[i].TrainCost;
                housing += missing[i] * Rules.Troops[i].Housing;
                soldiers += missing[i];
            }
            if (soldiers == 0) { Notice = Rules.FormationNames[preset] + "已达到目标人数，无需补兵。"; return false; }
            if (Village.ArmyHousing + housing > Village.ArmyCapacity)
            { Notice = "补齐" + Rules.FormationNames[preset] + "需要 " + housing + " 营位；请调整现有编队或扩建兵营。"; return false; }
            if (Village.Gold < cost) { Notice = "补齐" + Rules.FormationNames[preset] + "需要 " + cost + " 金币，当前只有 " + Village.Gold + "。"; return false; }
            Village.Gold -= cost;
            for (int i = 0; i < missing.Length; i++) Village.ArmyCounts[i] += missing[i];
            Notice = Rules.FormationNames[preset] + "已立即补齐 " + soldiers + " 名士兵 · " + housing + " 营位。";
            return true;
        }
        public void BeginBattle()
        {
            if (Battle != null) return;
            if (Village.Count(BuildingKind.Barracks) == 0) { Notice = "请先建造兵营，再率领部队出征。"; return; }
            if (MissionIndex < 0 || MissionIndex >= Missions.Count) MissionIndex = 0;
            if (!Village.IsMissionUnlocked(MissionIndex)) { Notice = "该关卡尚未解锁。先在上一关获得至少1颗星。"; return; }
            AdvanceTraining(DateTime.UtcNow);
            bool heroReady = Village.IsHeroUnlocked(HeroKind.EmberWarden);
            if (Village.ArmyHousing <= 0 && !heroReady) { Notice = "远征队为空。打开编队 / 训练，准备士兵后再出发。"; return; }
            int[] army = Village.ArmyCounts.ToArray();
            Battle = new Battle(Missions.Create(MissionIndex), MissionIndex, army, Village.TroopLevels.ToArray(), Village.SpellLevels.ToArray(), heroReady ? Village.HeroLevels.ToArray() : null, Village.PetLevels.ToArray(), Village.HeroPetAssignments.ToArray(), Village.EquipmentLevels.ToArray(), Village.HeroEquipmentSlots.ToArray());
            for (int i = 0; i < Village.ArmyCounts.Count; i++) Village.ArmyCounts[i] = 0;
            Notice = "侦察阶段 · 本次编队 " + Battle.InitialHousing + " 营位；投下第一名士兵后开始计时。";
        }
        public void CycleMission(int direction)
        {
            MissionIndex = (MissionIndex + direction % Missions.Count + Missions.Count) % Missions.Count;
            Notice = Village.IsMissionUnlocked(MissionIndex) ? Missions.Names[MissionIndex] + " · 最佳 " + Village.CampaignStars[MissionIndex] + "星 / " + Village.CampaignBest[MissionIndex] + "%" : Missions.Names[MissionIndex] + "尚未解锁。";
        }
        public bool ClaimAchievement(string id)
        {
            AchievementSpec spec = Achievements.Find(id);
            if (spec == null || Village.HasClaimed(id)) { Notice = "该成就奖励已经领取或不存在。"; return false; }
            int progress = Achievements.Progress(Village, spec);
            if (progress < spec.Target) { Notice = "成就尚未完成：" + progress + " / " + spec.Target + "。"; return false; }
            if (Village.Gold + spec.GoldReward > Village.Capacity || Village.Crystal + spec.CrystalReward > Village.Capacity)
            { Notice = "仓库空间不足，先消费资源再领取完整奖励。"; return false; }
            Village.Gold += spec.GoldReward; Village.Crystal += spec.CrystalReward;
            Village.ClaimedAchievements.Add(spec.Id);
            Notice = "成就达成：" + spec.Name + "，领取 " + spec.GoldReward + " 金 / " + spec.CrystalReward + " 晶。";
            return true;
        }
        public bool Settle()
        {
            if (Battle == null || !Battle.Finished || Battle.Settled) return false;
            Battle.Settled = true;
            for (int i = 0; i < Battle.Available.Length; i++)
            {
                Village.ArmyCounts[i] += Battle.Available[i];
                Battle.Available[i] = 0;
            }
            Village.Gold = Math.Min(Village.Capacity, Village.Gold + Battle.GoldReward);
            Village.Crystal = Math.Min(Village.Capacity, Village.Crystal + Battle.CrystalReward);
            int emergencyGold = 0;
            if (Battle.Started && Village.ArmyHousing == 0 && Village.QueuedHousing == 0 && Village.Gold < 80)
            {
                emergencyGold = 80 - Village.Gold;
                Village.Gold += emergencyGold;
            }
            if (Battle.Stars > 0) Village.Wins++;
            int oldSigils = Village.CoreSigils, oldDust = Village.Stardust;
            bool record = Village.RecordMission(Battle.Mission, Battle.Stars, Battle.Destruction);
            Battle.SigilReward = Village.CoreSigils - oldSigils; Battle.StardustReward = Village.Stardust - oldDust;
            Notice = "远征结束：" + Battle.Stars + " 星，获得 " + Battle.GoldReward + " 金币。"
                + (emergencyGold > 0 ? " 营地应急补给 " + emergencyGold + " 金币用于重新训练。" : "")
                + (record ? " 新的战役纪录已保存。" : "");
            return true;
        }
        public void ReturnHome()
        {
            if (Battle == null) return;
            Battle.Finish(); Settle(); Battle = null;
        }
        public bool AbandonBattle()
        {
            if (Battle == null || Battle.Settled) return false;
            for (int i = 0; i < Battle.Available.Length; i++) Village.ArmyCounts[i] += Battle.Available[i];
            foreach (Unit unit in Battle.Units) if (!unit.IsSummon && !unit.IsHero && !unit.IsPet) Village.ArmyCounts[(int)unit.Kind]++;
            Battle = null;
            Notice = "未结算的远征已放弃，出征阵容已返回营地。";
            return true;
        }
        public VillageData SnapshotForSave()
        {
            if (Battle == null || Battle.Settled) return Village;
            // An interrupted expedition is not resumed; preserve its full pre-deployment roster.
            VillageData snapshot = Village.CopyForSave();
            for (int i = 0; i < Battle.Available.Length; i++) snapshot.ArmyCounts[i] += Battle.Available[i];
            foreach (Unit unit in Battle.Units) if (!unit.IsSummon) snapshot.ArmyCounts[(int)unit.Kind]++;
            return snapshot;
        }
    }

    public static class SaveStore
    {
        public static VillageData Load(string path, out string message)
        {
            message = "";
            if (!File.Exists(path) && !File.Exists(path + ".bak")) return VillageData.Create();
            foreach (string candidate in new[] { path, path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    VillageData v;
                    using (FileStream file = File.OpenRead(candidate)) v = (VillageData)new XmlSerializer(typeof(VillageData)).Deserialize(file);
                    bool needsStarterArmy = v != null && !v.ArmyInitialized && (v.ArmyCounts == null || v.ArmyCounts.Count == 0);
                    bool balancedHealthMigrated = MigrateBalancedHealth(v);
                    Validate(v);
                    if (v.Version == 1)
                    {
                        v.MigrateLegacy();
                        if (needsStarterArmy) { v.ArmyInitialized = false; v.EnsureArmy(); }
                        message = "旧存档已升级：保留原部队，并补建训练营与实验室。";
                    }
                    if (v.Version == 2) v.MigrateEquipment();
                    if (balancedHealthMigrated) message = "旧版风暴塔生命值已适配新版平衡，建筑和进度均已保留。";
                    if (candidate != path) message = "主存档无法读取，已从备份恢复。" + message;
                    return v;
                }
                catch (Exception ex)
                {
                    if (!(ex is IOException || ex is InvalidOperationException || ex is InvalidDataException || ex is UnauthorizedAccessException)) throw;
                    message = "存档无法读取：" + ex.Message;
                }
            }
            // Never silently replace an existing corrupt or newer-version save.
            throw new InvalidDataException(message + " 原文件已保留，请备份后检查。");
        }
        private static bool MigrateBalancedHealth(VillageData village)
        {
            if (village == null || village.Version != 2 || village.Buildings == null) return false;
            bool migrated = false;
            // Version 2 originally shipped with a 760-HP arc tower. Home buildings are saved at full health;
            // translate only that exact historical maximum, leaving arbitrary damaged/corrupt values invalid.
            foreach (Building building in village.Buildings)
            {
                if (building == null || building.Kind != BuildingKind.ArcTower || building.Level < 1 || building.Level > 3) continue;
                int historicalFullHealth = 760 * (building.Level + 1) / 2;
                if (building.Health != historicalFullHealth || building.Health == building.MaxHealth) continue;
                building.Health = building.MaxHealth;
                migrated = true;
            }
            return migrated;
        }
        public static void Save(string path, VillageData village)
        {
            Validate(village);
            string directory = Path.GetDirectoryName(Path.GetFullPath(path));
            Directory.CreateDirectory(directory);
            string temporary = path + ".tmp";
            using (FileStream file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                new XmlSerializer(typeof(VillageData)).Serialize(file, village);
                file.Flush(true);
            }
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
            else File.Move(temporary, path);
        }
        public static void Validate(VillageData v)
        {
            if (v == null || (v.Version != 1 && v.Version != 2 && v.Version != 3) || v.Buildings == null || v.Buildings.Count > 1200 || v.Wins < 0)
                throw new InvalidDataException("不支持的存档版本或数据。");
            v.EnsureProgress(); v.EnsureTechnology(); v.EnsureHeroes(); v.EnsureEquipment(); v.EnsureArmy();
            if (v.Version == 3)
            {
                if (v.CoreSigils < 0 || v.CoreSigils > 12 || v.Stardust < 0 || v.Stardust > 1000 || v.EquipmentLevels.Count != Rules.EquipmentNames.Length || v.HeroEquipmentSlots.Count != Rules.Heroes.Length * 2)
                    throw new InvalidDataException("英雄装备资源无效。");
                foreach (int level in v.EquipmentLevels) if (level < 0 || level > 3) throw new InvalidDataException("英雄装备等级无效。");
                for (int i = 0; i < v.HeroEquipmentSlots.Count; i++)
                {
                    int gear = v.HeroEquipmentSlots[i];
                    if (gear < -1 || gear >= Rules.EquipmentNames.Length || gear >= 0 && v.EquipmentLevels[gear] == 0 || i % 2 == 1 && gear >= 0 && gear == v.HeroEquipmentSlots[i - 1])
                        throw new InvalidDataException("英雄装备装配无效。");
                }
            }
            if (v.TroopLevels.Count != Rules.Troops.Length || v.SpellLevels.Count != Rules.SpellNames.Length || v.HealLevel < 1 || v.HealLevel > 3) throw new InvalidDataException("科技数据无效。");
            foreach (int level in v.TroopLevels) if (level < 1 || level > 3) throw new InvalidDataException("兵种等级无效。");
            for (int i = 0; i < v.SpellLevels.Count; i++) if (v.SpellLevels[i] < 0 || v.SpellLevels[i] > 3 || i == 0 && v.SpellLevels[i] < 1) throw new InvalidDataException("法术科技数据无效。");
            if (v.HeroLevels.Count != Rules.Heroes.Length || v.PetLevels.Count != Rules.Pets.Length || v.HeroPetAssignments.Count != Rules.Heroes.Length) throw new InvalidDataException("英雄或战宠数据无效。");
            foreach (int level in v.HeroLevels) if (level < 0 || level > 4) throw new InvalidDataException("英雄等级无效。");
            foreach (int level in v.PetLevels) if (level < 0 || level > 4) throw new InvalidDataException("战宠等级无效。");
            HashSet<int> assignedPets = new HashSet<int>(); foreach (int pet in v.HeroPetAssignments) if (pet < -1 || pet >= Rules.Pets.Length || pet >= 0 && !assignedPets.Add(pet)) throw new InvalidDataException("战宠绑定数据无效。");
            if (v.CampaignStars.Count != Missions.Count || v.CampaignBest.Count != Missions.Count || v.ClaimedAchievements.Count > Achievements.Specs.Length)
                throw new InvalidDataException("战役进度数据无效。");
            for (int i = 0; i < Missions.Count; i++) if (v.CampaignStars[i] < 0 || v.CampaignStars[i] > 3 || v.CampaignBest[i] < 0 || v.CampaignBest[i] > 100)
                throw new InvalidDataException("战役成绩数据无效。");
            HashSet<string> claimed = new HashSet<string>();
            foreach (string id in v.ClaimedAchievements) if (string.IsNullOrEmpty(id) || Achievements.Find(id) == null || !claimed.Add(id))
                throw new InvalidDataException("成就领取数据无效。");
            if (v.ArmyCounts.Count != Rules.Troops.Length || v.TrainingQueue.Count > 1200 || v.TrainingStartedUtcTicks < 0 || v.TrainingStartedUtcTicks > DateTime.MaxValue.Ticks)
                throw new InvalidDataException("远征编队数据无效。");
            foreach (int count in v.ArmyCounts) if (count < 0 || count > 1200) throw new InvalidDataException("远征兵力数据无效。");
            foreach (int kind in v.TrainingQueue) if (kind < 0 || kind >= Rules.Troops.Length) throw new InvalidDataException("训练队列数据无效。");
            HashSet<int> ids = new HashSet<int>();
            int keepCount = 0, maxId = 0;
            foreach (Building b in v.Buildings)
            {
                if (b == null || b.Id <= 0 || !ids.Add(b.Id) || (int)b.Kind < 0 || (int)b.Kind >= Rules.Buildings.Length || b.Level < 1 || b.Level > Rules.MaxBuildingLevel(b.Kind))
                    throw new InvalidDataException("建筑数据无效。");
                maxId = Math.Max(maxId, b.Id);
                if (b.Kind == BuildingKind.Keep) keepCount++;
            }
            if (keepCount != 1 || v.NextId <= maxId || v.Gold < 0 || v.Crystal < 0 || v.Gold > v.Capacity || v.Crystal > v.Capacity || v.LastIncomeUtcTicks <= 0 || v.LastIncomeUtcTicks > DateTime.MaxValue.Ticks)
                throw new InvalidDataException("资源或主城数据无效。");
            foreach (Building b in v.Buildings)
                if (b.Health != b.MaxHealth || !v.CanPlace(b.Kind, b.X, b.Z, b.Id)) throw new InvalidDataException("建筑位置或生命值无效。");
        }
    }
}
