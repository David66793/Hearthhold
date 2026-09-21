using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Hearthhold.Core
{
    public enum BuildingKind { Keep, Mine, Reservoir, Barracks, Cannon, Watchtower, Wall, TrainingCamp, Laboratory, Mortar, AirDefense, ArcTower, BeamTower, HeroHall, PetLodge }
    public enum TroopKind { Vanguard, Ranger, Guardian, Sapper, SkyRider, Alchemist, Medic, Summoner }
    public enum SpellKind { Heal, Fury, Freeze, Breach }
    public enum HeroKind { EmberWarden }
    public enum PetKind { CinderFox, Mossback }

    public sealed class BuildingSpec
    {
        public string Name, Description;
        public int Size, Health, Cost, Damage, Range, Cooldown, MinRange, SplashRadius;
        public bool TargetsGround = true, TargetsAir, RampDamage;
        public BuildingSpec(string name, string description, int size, int health, int cost, int damage, int range, int cooldown,
            bool targetsGround = true, bool targetsAir = false, int minRange = 0, int splashRadius = 0, bool rampDamage = false)
        { Name = name; Description = description; Size = size; Health = health; Cost = cost; Damage = damage; Range = range; Cooldown = cooldown; TargetsGround = targetsGround; TargetsAir = targetsAir; MinRange = minRange; SplashRadius = splashRadius; RampDamage = rampDamage; }
    }

    public sealed class TroopSpec
    {
        public string Name, Role;
        public int Health, Damage, Range, Speed, Cooldown, Count, Housing, TrainCost, TrainSeconds, SplashRadius, HealPower, SummonCooldown;
        public bool Flying, PreferDefenses, PreferWalls;
        public TroopSpec(string name, string role, int health, int damage, int range, int speed, int cooldown, int count, int housing, int trainCost, int trainSeconds,
            bool flying = false, bool preferDefenses = false, bool preferWalls = false, int splashRadius = 0, int healPower = 0, int summonCooldown = 0)
        { Name = name; Role = role; Health = health; Damage = damage; Range = range; Speed = speed; Cooldown = cooldown; Count = count; Housing = housing; TrainCost = trainCost; TrainSeconds = trainSeconds; Flying = flying; PreferDefenses = preferDefenses; PreferWalls = preferWalls; SplashRadius = splashRadius; HealPower = healPower; SummonCooldown = summonCooldown; }
        public string Description, Tactics, Weakness;
    }

    public sealed class HeroSpec
    {
        public string Name, Role, Description, AbilityName, AbilityDescription;
        public TroopSpec Combat;
        public HeroSpec(string name, string role, string description, string abilityName, string abilityDescription, TroopSpec combat)
        { Name = name; Role = role; Description = description; AbilityName = abilityName; AbilityDescription = abilityDescription; Combat = combat; }
    }
    public sealed class PetSpec
    {
        public string Name, Role, Description;
        public TroopSpec Combat;
        public int FollowRange;
        public PetSpec(string name, string role, string description, TroopSpec combat, int followRange)
        { Name = name; Role = role; Description = description; Combat = combat; FollowRange = followRange; }
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
            new AchievementSpec("builder", "营火初盛", "聚落拥有20座建筑", 20, 220, 40),
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
            new BuildingSpec("兵营", "提供部队营位；每座1级兵营提供45营位，每升级增加15营位。", 3, 800, 240, 0, 0, 0),
            new BuildingSpec("重弩炮", "地面单体重击；克制高血量前排，但无法攻击空军。", 2, 850, 220, 44, 6500, 24),
            new BuildingSpec("哨塔", "地空通用远射；火力稳定但缺少群体伤害。", 2, 650, 200, 19, 8000, 15, true, true),
            new BuildingSpec("石墙", "阻挡地面部队，迫使对手绕行或破墙。", 1, 430, 20, 0, 0, 0),
            new BuildingSpec("训练营", "负责即时造兵与兵种解锁：1级先锋/游侠，2级铁卫，3级破城手。", 3, 700, 300, 0, 0, 0),
            new BuildingSpec("实验室", "研究兵种与四类战术法术；科技等级不超过实验室等级。", 3, 700, 320, 0, 0, 0),
            new BuildingSpec("投石台", "超远地面范围攻击，有3格近距盲区。", 3, 780, 360, 70, 10500, 42, true, false, 3000, 2100),
            new BuildingSpec("猎空弩", "只攻击空中目标，单次伤害很高。", 2, 720, 340, 105, 8500, 25, false, true),
            new BuildingSpec("风暴塔", "中程地空范围电弧，惩罚密集军团。", 2, 900, 380, 42, 6500, 18, true, true, 0, 2200),
            new BuildingSpec("灼光塔", "持续锁定同一目标时伤害逐步升高。", 2, 900, 440, 68, 7000, 8, true, true, 0, 0, true),
            new BuildingSpec("英雄殿堂", "解锁、升级并管理英雄。英雄不占兵营营位，也不由实验室研究。", 3, 1500, 1000, 0, 0, 0),
            new BuildingSpec("战宠小屋", "解锁、升级并配置英雄战宠。战宠随英雄部署，英雄阵亡后继续独立作战。", 3, 1050, 850, 0, 0, 0)
        };
        public static readonly TroopSpec[] Troops = {
            new TroopSpec("先锋", "近战 · 均衡", 250, 38, 1050, 150, 15, 12, 1, 15, 0),
            new TroopSpec("游侠", "远程 · 跨墙射击", 120, 29, 4500, 125, 17, 10, 1, 20, 0),
            new TroopSpec("铁卫", "重甲 · 优先防御", 2800, 110, 1100, 85, 24, 3, 5, 85, 0, false, true),
            new TroopSpec("破城手", "攻城 · 城墙特攻", 210, 43, 1100, 165, 18, 4, 2, 35, 0, false, false, true),
            new TroopSpec("翼骑", "空军 · 优先防御", 780, 100, 1250, 145, 24, 4, 4, 90, 0, true, true),
            new TroopSpec("炼金师", "远程 · 范围轰击", 380, 75, 5200, 105, 28, 4, 3, 70, 0, false, false, false, 1700),
            new TroopSpec("医师", "支援 · 治疗友军", 560, 0, 3800, 115, 20, 3, 4, 80, 0, false, false, false, 0, 100),
            new TroopSpec("唤灵师", "召唤 · 数量压力", 470, 45, 4300, 100, 24, 3, 4, 95, 0, false, false, false, 0, 0, 90)
        };
        public static readonly HeroSpec[] Heroes = {
            new HeroSpec("烬卫", "英雄 · 重装突进", "披覆炉心重甲的聚落守护者。每场可投放一次，不占营位，死亡不会永久损失。", "炉心号令", "恢复30%最大生命，并令自己与5格内友军强化8秒。",
                new TroopSpec("烬卫", "英雄 · 重装突进", 5200, 260, 1400, 115, 18, 1, 0, 0, 0, false, true))
        };
        public static readonly PetSpec[] Pets = {
            new PetSpec("燧爪", "战宠 · 近战协攻", "忠诚的炉火猎兽。跟随英雄攻击同一目标；英雄倒下后会继续独立作战。", new TroopSpec("燧爪", "战宠 · 近战协攻", 950, 85, 1100, 175, 15, 1, 0, 0, 0), 3200),
            new PetSpec("苔背", "战宠 · 守护", "尚未开放的防护型战宠。", new TroopSpec("苔背", "战宠 · 守护", 1500, 45, 1100, 110, 20, 1, 0, 0, 0), 2800)
        };
        public static readonly string[] FormationNames = { "新兵集结", "均衡远征", "重甲破阵", "远程压制", "王庭突击·75" };
        public static readonly int[][] FormationCounts = {
            new[] { 22, 23, 0, 0, 0, 0, 0, 0 },
            new[] { 8, 7, 3, 4, 1, 1, 0, 0 },
            new[] { 7, 3, 4, 2, 1, 1, 1, 0 },
            new[] { 5, 13, 2, 2, 1, 3, 0, 0 },
            new[] { 8, 7, 5, 3, 3, 3, 1, 1 }
        };
        public static BuildingSpec Spec(BuildingKind kind) { return Buildings[(int)kind]; }
        public static TroopSpec Spec(TroopKind kind) { return Troops[(int)kind]; }
        public static HeroSpec Spec(HeroKind kind) { return Heroes[(int)kind]; }
        public static PetSpec Spec(PetKind kind) { return Pets[(int)kind]; }
        public static int HeroHealth(HeroKind kind, int level) { return Spec(kind).Combat.Health * (100 + Math.Max(0, level - 1) * 15) / 100; }
        public static int HeroDamage(HeroKind kind, int level) { return Spec(kind).Combat.Damage * (100 + Math.Max(0, level - 1) * 15) / 100; }
        public static int HeroUpgradeGold(int level) { return 500 * level; }
        public static int HeroUpgradeCrystal(int level) { return 350 * level; }
        public static int PetHealth(PetKind kind, int level) { return Spec(kind).Combat.Health * (100 + Math.Max(0, level - 1) * 12) / 100; }
        public static int PetDamage(PetKind kind, int level) { return Spec(kind).Combat.Damage * (100 + Math.Max(0, level - 1) * 12) / 100; }
        public static int PetUpgradeCrystal(int level) { return 280 * level; }
        public static int MaxBuildingLevel(BuildingKind kind) { return kind == BuildingKind.Keep || kind == BuildingKind.HeroHall || kind == BuildingKind.PetLodge ? 4 : 3; }
        public static int UnlockCampLevel(TroopKind kind)
        {
            if (kind == TroopKind.Guardian || kind == TroopKind.SkyRider) return 2;
            if (kind == TroopKind.Sapper || kind == TroopKind.Alchemist || kind == TroopKind.Medic || kind == TroopKind.Summoner) return 3;
            return 1;
        }
        public static int TroopHealth(TroopKind kind, int level) { return Spec(kind).Health * (10 + Math.Max(0, level - 1)) / 10; }
        public static int TroopDamage(TroopKind kind, int level) { return Spec(kind).Damage * (10 + Math.Max(0, level - 1)) / 10; }
        public static int ResearchGold(int level) { return 180 * level; }
        public static int ResearchCrystal(int level) { return 120 * level; }
        public static readonly string[] SpellNames = { "疗愈之雨", "战吼", "霜封", "裂地" };
        public static int SpellUnlockKeepLevel(SpellKind kind)
        { return kind == SpellKind.Heal ? 1 : kind == SpellKind.Breach ? 3 : 2; }
        public static string SpellEffect(SpellKind kind, int level)
        {
            if (level <= 0) return "尚未研究";
            if (kind == SpellKind.Heal) return "恢复约" + (67 + (level - 1) * 10) + "%最大生命";
            if (kind == SpellKind.Fury) return "强化持续" + (6 + (level - 1)) + "秒";
            if (kind == SpellKind.Freeze) return "冻结持续" + (4 + (level - 1)) + "秒";
            return "破墙半径" + (3.6f + (level - 1) * 0.6f).ToString("0.0") + "格";
        }
        public static string BuildingData(Building b)
        {
            string stats = "生命 " + b.MaxHealth + " · 占地 " + b.Spec.Size + "×" + b.Spec.Size;
            if (b.Spec.Damage > 0) stats += "\n单次伤害 " + b.Spec.Damage * (b.Level + 1) / 2 + " · 射程 " + b.Spec.Range / 1000f + "格 · 间隔 " + (b.Spec.Cooldown + 1) / (float)TicksPerSecond + "秒";
            else if (b.Kind == BuildingKind.Mine) stats += "\n产出 " + 24 * b.Level + " 金币/分";
            else if (b.Kind == BuildingKind.Reservoir) stats += "\n产出 " + 18 * b.Level + " 晶露/分";
            else if (b.Kind == BuildingKind.Barracks) stats += "\n营位 " + (45 + (b.Level - 1) * 15) + " · 全营总量受建造上限限制";
            else if (b.Kind == BuildingKind.TrainingCamp) stats += "\n解锁 " + (b.Level == 1 ? "先锋、游侠" : b.Level == 2 ? "先锋、游侠、铁卫、翼骑" : "全部八种兵");
            else if (b.Kind == BuildingKind.Laboratory) stats += "\n研究上限 " + b.Level + " 级 · 兵种生命/伤害每级约 +10%";
            else if (b.Kind == BuildingKind.HeroHall) stats += "\n英雄等级上限 " + b.Level + " · 当前1个英雄出征槽";
            else if (b.Kind == BuildingKind.PetLodge) stats += "\n战宠等级上限 " + b.Level + " · 每名英雄可绑定1只战宠";
            return stats;
        }
        static Rules()
        {
            Troops[0].Description = "持剑近战步兵，攻击距离约1格。自动选择附近建筑，遇到挡路城墙会破墙。";
            Troops[0].Tactics = "在铁卫吸引火力、破城手打开缺口后成组投放，清理资源建筑。";
            Troops[0].Weakness = "不能隔墙攻击；单兵冲进防御塔火力区容易阵亡。";
            Troops[1].Description = "持弓远程射手，射程4.5格。可以隔着城墙攻击建筑，但自身生命较低。";
            Troops[1].Tactics = "放在铁卫和先锋后方；利用射程先打掉靠外的建筑。";
            Troops[1].Weakness = "生命只有120，别让游侠第一个吸引防御塔火力。";
            Troops[2].Description = "持盾重甲卫士，生命2800。优先攻击重弩炮、哨塔等防御建筑。";
            Troops[2].Tactics = "优先投放2—3名铁卫吸引火力，再跟进破城手和其他兵种。";
            Troops[2].Weakness = "移动慢、攻击间隔长。需要输出部队配合，不能只靠铁卫。";
            Troops[3].Description = "携带爆破器材的攻城手，优先攻击城墙；对城墙每次造成10倍伤害。";
            Troops[3].Tactics = "紧跟铁卫，从同一侧投下，打开缺口让近战部队进入基地。";
            Troops[3].Weakness = "生命较低；城墙拆完后的普通伤害有限，别作为主力输出。";
            Troops[4].Description = "不受城墙阻挡的空中突击兵，优先攻击防御建筑。";
            Troops[4].Tactics = "从防空薄弱侧切入，配合冻结优先拆除猎空弩。";
            Troops[4].Weakness = "会被哨塔、猎空弩、风暴塔和灼光塔锁定，不能无脑集中投放。";
            Troops[5].Description = "投掷炼金弹，对目标周围造成范围伤害。";
            Troops[5].Tactics = "让铁卫先吸引火力，再用炼金师清理密集建筑。";
            Troops[5].Weakness = "攻击慢、生命低；遭远程塔锁定时很快阵亡。";
            Troops[6].Description = "不攻击建筑，会寻找受伤友军并进行范围治疗。";
            Troops[6].Tactics = "跟随铁卫或主力团，延长核心推进时间。";
            Troops[6].Weakness = "自身输出为零；带得过多会导致时间不足。";
            Troops[7].Description = "周期召唤临时幽影战士，制造额外目标和持续压力。";
            Troops[7].Tactics = "在前排建立后投放，让召唤物替后排分担火力。";
            Troops[7].Weakness = "本体脆弱且启动慢，惧怕范围防御和快速突脸。";
        }
        public static int BuildLimit(BuildingKind kind, int keepLevel)
        {
            // Progression changes must first be recorded in docs/PROGRESSION-AND-ECONOMY-CONTRACT.md.
            int tier = Math.Max(1, Math.Min(4, keepLevel));
            switch (kind)
            {
                case BuildingKind.Keep: return 1;
                case BuildingKind.Mine: return 2 + tier;
                case BuildingKind.Reservoir: return 1 + tier;
                case BuildingKind.Barracks: return 1;
                case BuildingKind.Cannon: case BuildingKind.Watchtower: return 1 + tier;
                case BuildingKind.Wall: return 10 + tier * 30;
                case BuildingKind.TrainingCamp: case BuildingKind.Laboratory: return 1;
                case BuildingKind.HeroHall: return tier >= 4 ? 1 : 0;
                case BuildingKind.PetLodge: return tier >= 4 ? 1 : 0;
                case BuildingKind.Mortar: case BuildingKind.AirDefense: return tier >= 2 ? 1 : 0;
                case BuildingKind.ArcTower: case BuildingKind.BeamTower: return tier >= 3 ? 1 : 0;
                default: return 0;
            }
        }
        public static int BuildingUnlockKeepLevel(BuildingKind kind)
        { for (int level = 1; level <= 4; level++) if (BuildLimit(kind, level) > 0) return level; return 4; }
    }

    public sealed class Building
    {
        public int Id, X, Z, Level = 1, Health, Cooldown, FrozenTicks, LockedUnitId = -1, LockTicks;
        [XmlIgnore] public int BattleHealthScale = 1;
        public BuildingKind Kind;
        [XmlIgnore] public BuildingSpec Spec { get { return Rules.Spec(Kind); } }
        [XmlIgnore] public int MaxHealth { get { return Spec.Health * (Level + 1) / 2 * BattleHealthScale; } }
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
        public int Id, X, Z, Health, Cooldown, TargetId = -1, TargetRevision = -1, PathRevision = -1, RepathTick, FuryTicks, SummonTicks, SummonerId = -1;
        public bool IsSummon, IsHero, IsPet;
        public TroopKind Kind;
        public HeroKind HeroKind;
        public int HeroLevel;
        public PetKind PetKind;
        public int PetLevel, BondedHeroUnitId = -1;
        public List<Cell> Path = new List<Cell>();
        public int PathIndex;
        public TroopSpec Spec { get { return IsHero ? Rules.Spec(HeroKind).Combat : IsPet ? Rules.Spec(PetKind).Combat : Rules.Spec(Kind); } }
    }

    public sealed class CombatEffect
    {
        public int X, Z, EndX, EndZ, Ticks, Kind;
        public CombatEffect(int x, int z, int endX, int endZ, int ticks, int kind)
        { X = x; Z = z; EndX = endX; EndZ = endZ; Ticks = ticks; Kind = kind; }
    }

    public sealed class VillageData
    {
        public int Version = 2, Gold = 1600, Crystal = 700, NextId = 1, Wins;
        public long LastIncomeUtcTicks;
        public List<Building> Buildings = new List<Building>();
        public List<int> CampaignStars = new List<int>();
        public List<int> CampaignBest = new List<int>();
        public List<string> ClaimedAchievements = new List<string>();
        public bool ArmyInitialized;
        public List<int> ArmyCounts = new List<int>();
        public List<int> TrainingQueue = new List<int>();
        public long TrainingStartedUtcTicks;
        public List<int> TroopLevels = new List<int>();
        public int HealLevel = 1;
        public List<int> SpellLevels = new List<int>();
        public List<int> HeroLevels = new List<int>();
        public List<int> PetLevels = new List<int>();
        public List<int> HeroPetAssignments = new List<int>();
        public VillageData CopyForSave()
        {
            VillageData copy = (VillageData)MemberwiseClone();
            copy.ArmyCounts = new List<int>(ArmyCounts);
            copy.TroopLevels = new List<int>(TroopLevels);
            copy.SpellLevels = new List<int>(SpellLevels);
            copy.HeroLevels = new List<int>(HeroLevels);
            copy.PetLevels = new List<int>(PetLevels);
            copy.HeroPetAssignments = new List<int>(HeroPetAssignments);
            copy.TrainingQueue = new List<int>(TrainingQueue);
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
            v.Add(BuildingKind.TrainingCamp, 11, 24);
            v.Add(BuildingKind.Laboratory, 27, 15);
            v.Add(BuildingKind.Cannon, 15, 15);
            v.Add(BuildingKind.Watchtower, 23, 24);
            for (int x = 15; x <= 24; x++) v.Add(BuildingKind.Wall, x, 21);
            v.EnsureProgress(); v.EnsureTechnology(); v.EnsureHeroes(); v.EnsureArmy();
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
        [XmlIgnore] public int TrainingCampLevel { get { int level = 0; foreach (Building b in Buildings) if (b.Kind == BuildingKind.TrainingCamp) level = Math.Max(level, b.Level); return level; } }
        [XmlIgnore] public int LaboratoryLevel { get { int level = 0; foreach (Building b in Buildings) if (b.Kind == BuildingKind.Laboratory) level = Math.Max(level, b.Level); return level; } }
        [XmlIgnore] public int HeroHallLevel { get { int level = 0; foreach (Building b in Buildings) if (b.Kind == BuildingKind.HeroHall) level = Math.Max(level, b.Level); return level; } }
        [XmlIgnore] public int PetLodgeLevel { get { int level = 0; foreach (Building b in Buildings) if (b.Kind == BuildingKind.PetLodge) level = Math.Max(level, b.Level); return level; } }
        public bool IsHeroUnlocked(HeroKind kind) { EnsureHeroes(); return HeroHallLevel > 0 && HeroLevels[(int)kind] > 0; }
        public bool IsPetUnlocked(PetKind kind) { EnsureHeroes(); return PetLodgeLevel > 0 && PetLevels[(int)kind] > 0; }
        public bool IsTroopUnlocked(TroopKind kind) { return TrainingCampLevel >= Rules.UnlockCampLevel(kind); }
        public int SpellLevel(SpellKind kind) { EnsureTechnology(); return SpellLevels[(int)kind]; }
        public bool IsSpellUnlocked(SpellKind kind) { return SpellLevel(kind) > 0; }
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
                    ArmyCounts[i] = IsTroopUnlocked((TroopKind)i) ? Math.Min(FormationStarterCount(i), room) : 0;
                    used += ArmyCounts[i] * Rules.Troops[i].Housing;
                }
                ArmyInitialized = true;
            }
        }
        private static int FormationStarterCount(int index) { return Rules.FormationCounts[0][index]; }
        public void EnsureTechnology()
        {
            if (TroopLevels == null) TroopLevels = new List<int>();
            while (TroopLevels.Count < Rules.Troops.Length) TroopLevels.Add(1);
            if (SpellLevels == null) SpellLevels = new List<int>();
            if (SpellLevels.Count == 0) SpellLevels.Add(Math.Max(1, Math.Min(3, HealLevel)));
            while (SpellLevels.Count < Rules.SpellNames.Length) SpellLevels.Add(0);
            HealLevel = SpellLevels[(int)SpellKind.Heal];
        }
        public void EnsureHeroes()
        {
            if (HeroLevels == null) HeroLevels = new List<int>();
            while (HeroLevels.Count < Rules.Heroes.Length) HeroLevels.Add(0);
            if (PetLevels == null) PetLevels = new List<int>();
            while (PetLevels.Count < Enum.GetValues(typeof(PetKind)).Length) PetLevels.Add(0);
            if (HeroPetAssignments == null) HeroPetAssignments = new List<int>();
            while (HeroPetAssignments.Count < Rules.Heroes.Length) HeroPetAssignments.Add(-1);
        }
        public void MigrateLegacy()
        {
            if (Version != 1) return;
            // Keep an existing four-troop roster/train queue usable after migration.
            AddMigrationBuilding(BuildingKind.TrainingCamp, 3);
            AddMigrationBuilding(BuildingKind.Laboratory, 1);
            EnsureTechnology(); Version = 2;
        }
        private void AddMigrationBuilding(BuildingKind kind, int level)
        {
            if (Count(kind) > 0) return;
            for (int radius = 0; radius < Rules.MapSize; radius++)
                for (int z = 2; z < Rules.MapSize - 4; z++)
                    for (int x = 2; x < Rules.MapSize - 4; x++)
                        if (Math.Abs(x - 20) + Math.Abs(z - 20) == radius && CanPlace(kind, x, z, -1))
                        { Building b = Add(kind, x, z); b.Level = level; b.Health = b.MaxHealth; return; }
            throw new InvalidOperationException("旧存档没有空间安置新建筑，请先腾出位置。");
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
            "西侧栅线留有缺口，适合练习首次投兵。", "缺口由弩炮与风暴塔覆盖，分批投兵避免密集受伤。", "完整外墙与风暴塔考验破城手及后排配合。", "横向隔墙与风暴塔迫使部队选择突破方向。", "纵向隔墙迫使部队选择突破方向。", "十字内墙和更多资源点延长清扫路线。", "内外双环保护核心，先集中打开一侧。", "双营地与多座防御塔组成持久战。", "多重隔墙考验剩余兵力和治疗时机。", "三级王庭是当前战役终点，集中火力突破内环。"
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
            Add(v, index >= 9 ? BuildingKind.Mortar : BuildingKind.Cannon, 14, 20, level);
            Add(v, BuildingKind.Watchtower, 24, 15, level);
            if (index >= 1 && index <= 3) Add(v, BuildingKind.ArcTower, 13, 18, level);
            if (index >= 1) Add(v, BuildingKind.Cannon, 22, 24, level);
            if (index >= 2) Add(v, index >= 4 ? BuildingKind.Mortar : BuildingKind.Watchtower, 27, 21, level);
            if (index >= 3) Add(v, BuildingKind.Mine, 24, 25, level);
            if (index >= 4) Add(v, BuildingKind.Reservoir, 13, 24, level);
            if (index >= 5) Add(v, index >= 8 ? BuildingKind.ArcTower : BuildingKind.AirDefense, 20, 27, level);
            if (index >= 6) Add(v, index >= 8 ? BuildingKind.BeamTower : BuildingKind.ArcTower, 17, 14, level);
            if (index >= 7) Add(v, BuildingKind.Barracks, 25, 12, level);
            if (index >= 8) Add(v, BuildingKind.BeamTower, 27, 17, level);
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
