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
        public GameSession(VillageData village) { Village = village; Village.EnsureProgress(); Village.EnsureTechnology(); Village.EnsureArmy(); }
        public bool Build(BuildingKind kind, int x, int z)
        {
            if (Battle != null) return false;
            if ((int)kind <= 0 || (int)kind >= Rules.Buildings.Length) { Notice = "议事堡只能拥有一座。"; return false; }
            if (Village.AtLimit(kind)) { Notice = Rules.Spec(kind).Name + "已达数量上限 " + Village.Limit(kind) + "。" + ((kind == BuildingKind.Barracks || kind == BuildingKind.TrainingCamp || kind == BuildingKind.Laboratory) ? "该建筑只能建一座。" : "升级议事堡可提高上限。"); return false; }
            if (!Village.CanPlace(kind, x, z, -1)) { Notice = "这里没有足够的空间。请选择绿色区域。"; return false; }
            int cost = Rules.Spec(kind).Cost;
            if (Village.Gold < cost) { Notice = "金币不足，收取产出或完成一次远征。"; return false; }
            Collect(DateTime.UtcNow);
            Village.Gold -= cost;
            Village.Add(kind, x, z);
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
        public bool Upgrade(int id)
        {
            if (Battle != null) return false;
            Building b = Find(id); if (b == null) return false;
            if (b.Level >= 3) { Notice = "已达到原型的最高等级。"; return false; }
            if (b.Kind != BuildingKind.Keep && b.Level >= Village.KeepLevel) { Notice = "请先升级议事堡，解锁建筑等级。"; return false; }
            int gold = UpgradeGold(b), crystal = UpgradeCrystal(b);
            if (Village.Gold < gold || Village.Crystal < crystal) { Notice = "升级资源不足。"; return false; }
            Collect(DateTime.UtcNow);
            Village.Gold -= gold; Village.Crystal -= crystal; b.Level++; b.Health = b.MaxHealth;
            Notice = b.Spec.Name + "已升至 " + b.Level + " 级。";
            return true;
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
        public bool ResearchHeal()
        {
            if (Battle != null) return false;
            if (Village.LaboratoryLevel <= Village.HealLevel) { Notice = "实验室等级不足，先升级实验室。"; return false; }
            int gold = Rules.ResearchGold(Village.HealLevel), crystal = Rules.ResearchCrystal(Village.HealLevel);
            if (Village.Gold < gold || Village.Crystal < crystal) { Notice = "研究资源不足：需要 " + gold + " 金 / " + crystal + " 晶。"; return false; }
            Collect(DateTime.UtcNow); Village.Gold -= gold; Village.Crystal -= crystal;
            Village.HealLevel++;
            Notice = "疗愈之雨研究完成，升至 " + Village.HealLevel + " 级。";
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
            if (Village.TrainingQueue.Count == 0) { Village.TrainingStartedUtcTicks = 0; return false; }
            if (Village.Count(BuildingKind.Barracks) == 0 || Village.Count(BuildingKind.TrainingCamp) == 0) return false;
            if (Village.TrainingStartedUtcTicks <= 0 || Village.TrainingStartedUtcTicks > utcNow.Ticks)
            { Village.TrainingStartedUtcTicks = utcNow.Ticks; return false; }
            long elapsed = utcNow.Ticks - Village.TrainingStartedUtcTicks;
            bool changed = false;
            while (Village.TrainingQueue.Count > 0)
            {
                int kind = Village.TrainingQueue[0];
                TroopSpec spec = Rules.Troops[kind];
                long duration = (long)spec.TrainSeconds * TimeSpan.TicksPerSecond;
                if (elapsed < duration || Village.ArmyHousing + spec.Housing > Village.ArmyCapacity) break;
                elapsed -= duration;
                Village.ArmyCounts[kind]++;
                Village.TrainingQueue.RemoveAt(0);
                changed = true;
            }
            Village.TrainingStartedUtcTicks = Village.TrainingQueue.Count == 0 ? 0 : utcNow.Ticks - elapsed;
            if (changed) Notice = "训练完成，远征队已有 " + Village.ArmyHousing + " / " + Village.ArmyCapacity + " 营位。";
            return changed;
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
        {
            if (Battle != null || (int)kind < 0 || (int)kind >= Rules.Troops.Length) return false;
            AdvanceTraining(utcNow);
            TroopSpec spec = Rules.Spec(kind);
            if (Village.Count(BuildingKind.Barracks) == 0) { Notice = "请先建造兵营。"; return false; }
            if (!Village.IsTroopUnlocked(kind)) { Notice = "先升级训练营，解锁" + spec.Name + "。"; return false; }
            if (Village.ArmyHousing + Village.QueuedHousing + spec.Housing > Village.ArmyCapacity) { Notice = "营位不足。升级或增建兵营，或调整现有编队。"; return false; }
            if (Village.Gold < spec.TrainCost) { Notice = "训练金币不足。"; return false; }
            Village.Gold -= spec.TrainCost;
            if (Village.TrainingQueue.Count == 0) Village.TrainingStartedUtcTicks = utcNow.Ticks;
            Village.TrainingQueue.Add((int)kind);
            Notice = spec.Name + "已加入训练队列 · " + spec.TrainSeconds + "秒。";
            return true;
        }
        public bool CancelLastTraining(DateTime utcNow)
        {
            if (Battle != null) return false;
            AdvanceTraining(utcNow);
            if (Village.TrainingQueue.Count == 0) { Notice = "训练队列为空。"; return false; }
            int last = Village.TrainingQueue.Count - 1, kind = Village.TrainingQueue[last];
            int refund = Rules.Troops[kind].TrainCost;
            if (Village.Gold + refund > Village.Capacity) { Notice = "仓库空间不足，先消费金币再取消并领取完整退款。"; return false; }
            Village.TrainingQueue.RemoveAt(last);
            Village.Gold += refund;
            if (Village.TrainingQueue.Count == 0) Village.TrainingStartedUtcTicks = 0;
            Notice = "已取消队尾的" + Rules.Troops[kind].Name + "并返还金币。";
            return true;
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
            AdvanceTraining(utcNow);
            int[] counts = Rules.FormationCounts[preset], missing = new int[counts.Length];
            int cost = 0, housing = 0, soldiers = 0;
            for (int i = 0; i < counts.Length; i++)
            {
                missing[i] = Math.Max(0, counts[i] - Village.ArmyCounts[i] - Village.QueuedCount((TroopKind)i));
                if (missing[i] > 0 && !Village.IsTroopUnlocked((TroopKind)i)) { Notice = "训练营尚未解锁" + Rules.Troops[i].Name + "；请选择其他编队。"; return false; }
                cost += missing[i] * Rules.Troops[i].TrainCost;
                housing += missing[i] * Rules.Troops[i].Housing;
                soldiers += missing[i];
            }
            if (soldiers == 0) { Notice = Rules.FormationNames[preset] + "已达到目标人数，无需补兵。"; return false; }
            if (Village.ArmyHousing + Village.QueuedHousing + housing > Village.ArmyCapacity)
            { Notice = "补齐" + Rules.FormationNames[preset] + "需要 " + housing + " 营位；请调整现有编队或扩建兵营。"; return false; }
            if (Village.Gold < cost) { Notice = "补齐" + Rules.FormationNames[preset] + "需要 " + cost + " 金币，当前只有 " + Village.Gold + "。"; return false; }
            Village.Gold -= cost;
            if (Village.TrainingQueue.Count == 0) Village.TrainingStartedUtcTicks = utcNow.Ticks;
            for (int i = 0; i < missing.Length; i++) for (int n = 0; n < missing[i]; n++) Village.TrainingQueue.Add(i);
            Notice = Rules.FormationNames[preset] + "已补入 " + soldiers + " 名士兵 · " + housing + " 营位。";
            return true;
        }
        public void BeginBattle()
        {
            if (Battle != null) return;
            if (Village.Count(BuildingKind.Barracks) == 0) { Notice = "请先建造兵营，再率领部队出征。"; return; }
            if (MissionIndex < 0 || MissionIndex >= Missions.Count) MissionIndex = 0;
            if (!Village.IsMissionUnlocked(MissionIndex)) { Notice = "该关卡尚未解锁。先在上一关获得至少1颗星。"; return; }
            AdvanceTraining(DateTime.UtcNow);
            if (Village.ArmyHousing <= 0) { Notice = "远征队为空。打开编队 / 训练，准备士兵后再出发。"; return; }
            int[] army = Village.ArmyCounts.ToArray();
            Battle = new Battle(Missions.Create(MissionIndex), MissionIndex, army, Village.TroopLevels.ToArray(), Village.HealLevel);
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
            bool record = Village.RecordMission(Battle.Mission, Battle.Stars, Battle.Destruction);
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
            foreach (Unit unit in Battle.Units) if (!unit.IsSummon) Village.ArmyCounts[(int)unit.Kind]++;
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
            if (v == null || (v.Version != 1 && v.Version != 2) || v.Buildings == null || v.Buildings.Count > 1200 || v.Wins < 0)
                throw new InvalidDataException("不支持的存档版本或数据。");
            v.EnsureProgress(); v.EnsureTechnology(); v.EnsureArmy();
            if (v.TroopLevels.Count != Rules.Troops.Length || v.HealLevel < 1 || v.HealLevel > 3) throw new InvalidDataException("科技数据无效。");
            foreach (int level in v.TroopLevels) if (level < 1 || level > 3) throw new InvalidDataException("兵种等级无效。");
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
                if (b == null || b.Id <= 0 || !ids.Add(b.Id) || (int)b.Kind < 0 || (int)b.Kind >= Rules.Buildings.Length || b.Level < 1 || b.Level > 3)
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
