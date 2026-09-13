using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Hearthhold.Core
{
    public enum BuildingKind { Keep, Mine, Reservoir, Barracks, Cannon, Watchtower, Wall }
    public enum TroopKind { Vanguard, Ranger, Guardian, Sapper }

    public sealed class BuildingSpec
    {
        public string Name, Description;
        public int Size, Health, Cost, Damage, Range, Cooldown;
        public BuildingSpec(string name, string description, int size, int health, int cost, int damage, int range, int cooldown)
        { Name = name; Description = description; Size = size; Health = health; Cost = cost; Damage = damage; Range = range; Cooldown = cooldown; }
    }

    public sealed class TroopSpec
    {
        public string Name, Role;
        public int Health, Damage, Range, Speed, Cooldown, Count, Housing, TrainCost, TrainSeconds;
        public TroopSpec(string name, string role, int health, int damage, int range, int speed, int cooldown, int count, int housing, int trainCost, int trainSeconds)
        { Name = name; Role = role; Health = health; Damage = damage; Range = range; Speed = speed; Cooldown = cooldown; Count = count; Housing = housing; TrainCost = trainCost; TrainSeconds = trainSeconds; }
        public string Description, Tactics, Weakness;
    }

    public sealed class AchievementSpec
    {
        public string Id, Name, Description;
        public int Target, GoldReward, CrystalReward;
        public AchievementSpec(string id, string name, string description, int target, int goldReward, int crystalReward)
        { Id = id; Name = name; Description = description; Target = target; GoldReward = goldReward; CrystalReward = crystalReward; }
    }

    public static class Achievements
    {
        public static readonly AchievementSpec[] Specs = {
            new AchievementSpec("builder", "营火初盛", "聚落拥有18座建筑", 18, 220, 40),
            new AchievementSpec("keep_two", "石垒新城", "将议事堡升到2级", 2, 320, 90),
            new AchievementSpec("first_victory", "远征初捷", "赢得1次远征", 1, 260, 80),
            new AchievementSpec("ten_stars", "星火征途", "战役累计获得10颗星", 10, 520, 160),
            new AchievementSpec("five_missions", "开疆五站", "攻克5个不同关卡", 5, 760, 240)
        };
        public static AchievementSpec Find(string id)
        { foreach (AchievementSpec spec in Specs) if (spec.Id == id) return spec; return null; }
        public static int Progress(VillageData village, AchievementSpec spec)
        {
            if (spec.Id == "builder") return village.Buildings.Count;
            if (spec.Id == "keep_two") return village.KeepLevel;
            if (spec.Id == "first_victory") return village.Wins;
            if (spec.Id == "ten_stars") return village.TotalStars;
            if (spec.Id == "five_missions") return village.CompletedMissions;
            return 0;
        }
    }

    public static class Rules
    {
        public const int MapSize = 40;
        public const int Scale = 1000;
        public const int TicksPerSecond = 20;
        public static readonly BuildingSpec[] Buildings = {
            new BuildingSpec("议事堡", "聚落的中心。升级后提高其他建筑的等级上限。", 4, 2200, 0, 0, 0, 0),
            new BuildingSpec("金矿", "持续产出金币。点击收取，将离线收益收入仓库。", 3, 650, 180, 0, 0, 0),
            new BuildingSpec("晶露池", "收集用于建筑升级的晶露。", 3, 600, 160, 0, 0, 0),
            new BuildingSpec("远征营", "训练远征士兵并提供营位；升级可提高编队容量。", 3, 800, 240, 0, 0, 0),
            new BuildingSpec("重弩炮", "强力单体防御，擅长击退重甲目标。", 2, 850, 220, 38, 6500, 24),
            new BuildingSpec("哨塔", "视野开阔、射程更远的防御塔。", 2, 650, 200, 19, 8000, 15),
            new BuildingSpec("石墙", "阻挡地面部队，迫使对手绕行或破墙。", 1, 430, 20, 0, 0, 0)
        };
        public static readonly TroopSpec[] Troops = {
            new TroopSpec("先锋", "近战 · 均衡", 270, 42, 1050, 150, 15, 12, 1, 15, 2),
            new TroopSpec("游侠", "远程 · 跨墙射击", 130, 32, 4500, 125, 17, 10, 1, 20, 2),
            new TroopSpec("铁卫", "重甲 · 优先防御", 1300, 72, 1100, 85, 26, 3, 5, 65, 6),
            new TroopSpec("破城手", "攻城 · 城墙特攻", 180, 36, 1100, 165, 18, 4, 2, 35, 3)
        };
        public static readonly string[] FormationNames = { "均衡远征", "重甲破阵", "远程压制" };
        public static readonly int[][] FormationCounts = {
            new[] { 12, 10, 3, 4 },
            new[] { 7, 6, 4, 6 },
            new[] { 7, 18, 2, 5 }
        };
        public static BuildingSpec Spec(BuildingKind kind) { return Buildings[(int)kind]; }
        public static TroopSpec Spec(TroopKind kind) { return Troops[(int)kind]; }
        static Rules()
        {
            Troops[0].Description = "持剑近战步兵，攻击距离约1格。自动选择附近建筑，遇到挡路城墙会破墙。";
            Troops[0].Tactics = "在铁卫吸引火力、破城手打开缺口后成组投放，清理资源建筑。";
            Troops[0].Weakness = "不能隔墙攻击；单兵冲进防御塔火力区容易阵亡。";
            Troops[1].Description = "持弓远程射手，射程4.5格。可以隔着城墙攻击建筑，但自身生命较低。";
            Troops[1].Tactics = "放在铁卫和先锋后方；利用射程先打掉靠外的建筑。";
            Troops[1].Weakness = "生命只有130，别让游侠第一个吸引防御塔火力。";
            Troops[2].Description = "持盾重甲卫士，生命1300。优先攻击重弩炮、哨塔等防御建筑。";
            Troops[2].Tactics = "优先投放2—3名铁卫吸引火力，再跟进破城手和其他兵种。";
            Troops[2].Weakness = "移动慢、攻击间隔长。需要输出部队配合，不能只靠铁卫。";
            Troops[3].Description = "携带爆破器材的攻城手，优先攻击城墙；对城墙每次造成10倍伤害。";
            Troops[3].Tactics = "紧跟铁卫，从同一侧投下，打开缺口让近战部队进入基地。";
            Troops[3].Weakness = "生命较低；城墙拆完后的普通伤害有限，别作为主力输出。";
        }
        public static int BuildLimit(BuildingKind kind, int keepLevel)
        {
            int tier = Math.Max(1, Math.Min(3, keepLevel));
            switch (kind)
            {
                case BuildingKind.Keep: return 1;
                case BuildingKind.Mine: return 2 + tier;
                case BuildingKind.Reservoir: return 1 + tier;
                case BuildingKind.Barracks: return tier;
                case BuildingKind.Cannon: case BuildingKind.Watchtower: return 1 + tier;
                case BuildingKind.Wall: return 10 + tier * 30;
                default: return 0;
            }
        }
    }

    public sealed class Building
    {
        public int Id, X, Z, Level = 1, Health, Cooldown;
        public BuildingKind Kind;
        [XmlIgnore] public BuildingSpec Spec { get { return Rules.Spec(Kind); } }
        [XmlIgnore] public int MaxHealth { get { return Spec.Health * (Level + 1) / 2; } }
        [XmlIgnore] public int CenterX { get { return X * 1000 + Spec.Size * 500; } }
        [XmlIgnore] public int CenterZ { get { return Z * 1000 + Spec.Size * 500; } }
        public Building Copy() { return (Building)MemberwiseClone(); }
        public bool Contains(int x, int z) { return x >= X && z >= Z && x < X + Spec.Size && z < Z + Spec.Size; }
        public long DistanceSquared(int x, int z)
        {
            long dx = Math.Max(X * 1000 - x, Math.Max(0, x - (X + Spec.Size) * 1000));
            long dz = Math.Max(Z * 1000 - z, Math.Max(0, z - (Z + Spec.Size) * 1000));
            return dx * dx + dz * dz;
        }
    }

    public struct Cell
    {
        public int X, Z;
        public Cell(int x, int z) { X = x; Z = z; }
    }

    public sealed class Unit
    {
        public int Id, X, Z, Health, Cooldown, TargetId = -1, PathRevision = -1, RepathTick;
        public TroopKind Kind;
        public List<Cell> Path = new List<Cell>();
        public int PathIndex;
        public TroopSpec Spec { get { return Rules.Spec(Kind); } }
    }

    public sealed class CombatEffect
    {
        public int X, Z, EndX, EndZ, Ticks, Kind;
        public CombatEffect(int x, int z, int endX, int endZ, int ticks, int kind)
        { X = x; Z = z; EndX = endX; EndZ = endZ; Ticks = ticks; Kind = kind; }
    }

    public sealed class VillageData
    {
        public int Version = 1, Gold = 1600, Crystal = 700, NextId = 1, Wins;
        public long LastIncomeUtcTicks;
        public List<Building> Buildings = new List<Building>();
        public List<int> CampaignStars = new List<int>();
        public List<int> CampaignBest = new List<int>();
        public List<string> ClaimedAchievements = new List<string>();
        public bool ArmyInitialized;
        public List<int> ArmyCounts = new List<int>();
        public List<int> TrainingQueue = new List<int>();
        public long TrainingStartedUtcTicks;
        public VillageData CopyForSave()
        {
            VillageData copy = (VillageData)MemberwiseClone();
            copy.ArmyCounts = new List<int>(ArmyCounts);
            return copy;
        }
        public static VillageData Create()
        {
            VillageData v = new VillageData();
            v.LastIncomeUtcTicks = DateTime.UtcNow.AddMinutes(-2).Ticks;
            v.Add(BuildingKind.Keep, 18, 16);
            v.Add(BuildingKind.Mine, 12, 18);
            v.Add(BuildingKind.Mine, 26, 20);
            v.Add(BuildingKind.Reservoir, 23, 14);
            v.Add(BuildingKind.Barracks, 16, 23);
            v.Add(BuildingKind.Cannon, 15, 15);
            v.Add(BuildingKind.Watchtower, 23, 24);
            for (int x = 15; x <= 24; x++) v.Add(BuildingKind.Wall, x, 21);
            v.EnsureProgress(); v.EnsureArmy();
            return v;
        }
        public Building Add(BuildingKind kind, int x, int z)
        {
            Building b = new Building { Id = NextId++, Kind = kind, X = x, Z = z };
            b.Health = b.MaxHealth;
            Buildings.Add(b);
            return b;
        }
        public int Capacity { get { return 5000 + (KeepLevel - 1) * 5000; } }
        public int KeepLevel
        {
            get { foreach (Building b in Buildings) if (b.Kind == BuildingKind.Keep) return b.Level; return 1; }
        }
        public Building At(int x, int z)
        { foreach (Building b in Buildings) if (b.Contains(x, z)) return b; return null; }
        public int Count(BuildingKind kind) { int count = 0; foreach (Building b in Buildings) if (b.Kind == kind) count++; return count; }
        public int Limit(BuildingKind kind) { return Rules.BuildLimit(kind, KeepLevel); }
        public bool AtLimit(BuildingKind kind) { return Count(kind) >= Limit(kind); }
        [XmlIgnore] public int ArmyCapacity
        {
            get
            {
                int capacity = 0;
                foreach (Building b in Buildings) if (b.Kind == BuildingKind.Barracks) capacity += 45 + (b.Level - 1) * 15;
                return capacity;
            }
        }
        [XmlIgnore] public int ArmyHousing
        {
            get { EnsureArmy(); int total = 0; for (int i = 0; i < Rules.Troops.Length; i++) total += ArmyCounts[i] * Rules.Troops[i].Housing; return total; }
        }
        [XmlIgnore] public int QueuedHousing
        {
            get { EnsureArmy(); int total = 0; foreach (int kind in TrainingQueue) if (kind >= 0 && kind < Rules.Troops.Length) total += Rules.Troops[kind].Housing; return total; }
        }
        [XmlIgnore] public int TotalStars { get { EnsureProgress(); int total = 0; foreach (int stars in CampaignStars) total += stars; return total; } }
        [XmlIgnore] public int CompletedMissions { get { EnsureProgress(); int count = 0; foreach (int stars in CampaignStars) if (stars > 0) count++; return count; } }
        [XmlIgnore] public int UnlockedMissionCount
        {
            get
            {
                EnsureProgress(); int count = 1;
                while (count < Missions.Count && CampaignStars[count - 1] > 0) count++;
                return count;
            }
        }
        public void EnsureProgress()
        {
            if (CampaignStars == null) CampaignStars = new List<int>();
            if (CampaignBest == null) CampaignBest = new List<int>();
            if (ClaimedAchievements == null) ClaimedAchievements = new List<string>();
            while (CampaignStars.Count < Missions.Count) CampaignStars.Add(0);
            while (CampaignBest.Count < Missions.Count) CampaignBest.Add(0);
        }
        public void EnsureArmy()
        {
            if (ArmyCounts == null) ArmyCounts = new List<int>();
            if (TrainingQueue == null) TrainingQueue = new List<int>();
            while (ArmyCounts.Count < Rules.Troops.Length) ArmyCounts.Add(0);
            if (!ArmyInitialized)
            {
                int used = 0;
                for (int i = 0; i < Rules.Troops.Length; i++)
                {
                    int room = Math.Max(0, ArmyCapacity - used) / Rules.Troops[i].Housing;
                    ArmyCounts[i] = Math.Min(Rules.Troops[i].Count, room);
                    used += ArmyCounts[i] * Rules.Troops[i].Housing;
                }
                ArmyInitialized = true;
            }
        }
        public int QueuedCount(TroopKind kind)
        { EnsureArmy(); int count = 0; foreach (int queued in TrainingQueue) if (queued == (int)kind) count++; return count; }
        public bool IsMissionUnlocked(int mission) { return mission >= 0 && mission < UnlockedMissionCount; }
        public bool RecordMission(int mission, int stars, int destruction)
        {
            EnsureProgress();
            if (mission < 0 || mission >= Missions.Count) return false;
            int oldStars = CampaignStars[mission], oldBest = CampaignBest[mission];
            CampaignStars[mission] = Math.Max(oldStars, Math.Max(0, Math.Min(3, stars)));
            CampaignBest[mission] = Math.Max(oldBest, Math.Max(0, Math.Min(100, destruction)));
            return CampaignStars[mission] != oldStars || CampaignBest[mission] != oldBest;
        }
        public bool HasClaimed(string achievementId) { return ClaimedAchievements != null && ClaimedAchievements.Contains(achievementId); }
        public bool CanPlace(BuildingKind kind, int x, int z, int ignoreId)
        {
            int size = Rules.Spec(kind).Size;
            if (x < 2 || z < 2 || x + size > Rules.MapSize - 2 || z + size > Rules.MapSize - 2) return false;
            foreach (Building b in Buildings)
                if (b.Id != ignoreId && x < b.X + b.Spec.Size && x + size > b.X && z < b.Z + b.Spec.Size && z + size > b.Z) return false;
            return true;
        }
    }

    public static class Missions
    {
        public static readonly string[] Names = { "松林前哨", "河谷营地", "灰岩要塞", "双桥关", "霜木环堡", "赤土兵站", "风暴高台", "月湾城寨", "黑松迷阵", "晨火王庭" };
        public static readonly string[] Descriptions = {
            "西侧栅线留有缺口，适合练习首次投兵。", "单一缺口由双重弩火覆盖，需要铁卫先行。", "完整外墙考验破城手与后排配合。", "横向隔墙把守军分成两个庭院。", "纵向隔墙迫使部队选择突破方向。", "十字内墙和更多资源点延长清扫路线。", "内外双环保护核心，先集中打开一侧。", "双营地与多座防御塔组成持久战。", "多重隔墙考验剩余兵力和治疗时机。", "三级王庭是当前战役终点，集中火力突破内环。"
        };
        public static int Count { get { return Names.Length; } }
        public static List<Building> Create(int index)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException("index");
            VillageData v = new VillageData();
            int level = Math.Min(3, 1 + index / 4);
            Add(v, BuildingKind.Keep, 18, 18, level);
            Add(v, BuildingKind.Mine, 14, 14, level);
            Add(v, BuildingKind.Reservoir, 24, 19, level);
            Add(v, BuildingKind.Barracks, 17, 24, level);
            Add(v, BuildingKind.Cannon, 14, 20, level);
            Add(v, BuildingKind.Watchtower, 24, 15, level);
            if (index >= 1) Add(v, BuildingKind.Cannon, 22, 24, level);
            if (index >= 2) Add(v, BuildingKind.Watchtower, 27, 21, level);
            if (index >= 3) Add(v, BuildingKind.Mine, 24, 25, level);
            if (index >= 4) Add(v, BuildingKind.Reservoir, 13, 24, level);
            if (index >= 5) Add(v, BuildingKind.Cannon, 20, 27, level);
            if (index >= 6) Add(v, BuildingKind.Watchtower, 17, 14, level);
            if (index >= 7) Add(v, BuildingKind.Barracks, 25, 12, level);
            if (index >= 8) Add(v, BuildingKind.Cannon, 27, 17, level);
            if (index >= 9) Add(v, BuildingKind.Watchtower, 12, 18, level);

            for (int x = 11; x <= 29; x++) { Wall(v, x, 11, level); Wall(v, x, 30, level); }
            for (int z = 12; z <= 29; z++)
            {
                if (!((index == 0 && z >= 18 && z <= 22) || (index == 1 && z == 20))) Wall(v, 11, z, level);
                Wall(v, 29, z, level);
            }
            if (index == 3 || index == 5 || index == 9) for (int x = 13; x <= 27; x++) Wall(v, x, 23, level);
            if (index == 4 || index == 5 || index == 8 || index == 9) for (int z = 13; z <= 28; z++) Wall(v, 21, z, level);
            if (index == 6 || index == 9)
            {
                for (int x = 16; x <= 24; x++) { Wall(v, x, 16, level); Wall(v, x, 25, level); }
                for (int z = 17; z <= 24; z++) { Wall(v, 16, z, level); Wall(v, 24, z, level); }
            }
            if (index == 7 || index == 8)
                for (int z = 13; z <= 28; z++) { if (z != 19) Wall(v, 17, z, level); if (z != 24) Wall(v, 23, z, level); }
            return v.Buildings;
        }
        private static void Add(VillageData village, BuildingKind kind, int x, int z, int level)
        {
            if (!village.CanPlace(kind, x, z, -1)) throw new InvalidOperationException("Mission layout overlap: " + kind + " at " + x + "," + z);
            Building building = village.Add(kind, x, z); building.Level = level; building.Health = building.MaxHealth;
        }
        private static void Wall(VillageData village, int x, int z, int level)
        {
            if (!village.CanPlace(BuildingKind.Wall, x, z, -1)) return;
            Building wall = village.Add(BuildingKind.Wall, x, z); wall.Level = level; wall.Health = wall.MaxHealth;
        }
    }
}
