using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Hearthhold.Core;

internal static class CoreTests
{
    private static int count;
    private static void Check(bool condition, string name)
    { if (!condition) throw new Exception("FAIL: " + name); count++; Console.WriteLine("PASS " + name); }
    private static GameSession NewSession() { return new GameSession(VillageData.Create()); }
    private static int Main()
    {
        Stopwatch watch = Stopwatch.StartNew();
        try
        {
            Construction(); Economy(); Persistence(); Combat(); Determinism(); MissionsCheck(); Progression(); Training(); TechnologyAndUnlocks(); Balance(); LimitsAndDemolition(); ModelChecks();
            Console.WriteLine("\n" + count + " checks passed in " + watch.Elapsed.TotalSeconds.ToString("F2") + "s."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static void Construction()
    {
        GameSession s = NewSession(); SaveStore.Validate(s.Village);
        Check(!s.Build(BuildingKind.Cannon, 18, 16), "Reject overlapping construction");
        Check(!s.Build(BuildingKind.Mine, -1, 10), "Reject negative coordinates");
        Check(!s.Build(BuildingKind.Mine, 37, 37), "Reject edge overflow");
        Check(!s.Build(BuildingKind.Keep, 5, 5), "Reject a second keep");
        int buildings = s.Village.Buildings.Count;
        Check(s.Build(BuildingKind.Cannon, 6, 6), "Build on valid empty cells");
        Check(s.Village.Buildings.Count == buildings + 1, "Construction adds exactly one entity");
        Building cannon = s.Village.Buildings[s.Village.Buildings.Count - 1];
        Check(s.Move(cannon.Id, 8, 8), "Move a building");
        Check(!s.Move(cannon.Id, 18, 17), "Reject move into keep footprint");
        Check(s.Undo(false) && cannon.X == 6 && cannon.Z == 6, "Undo restores original position");
        Check(s.Undo(true) && cannon.X == 8 && cannon.Z == 8, "Redo restores moved position");
        int gold = s.Village.Gold;
        Check(!s.Upgrade(cannon.Id) && s.Village.Gold == gold, "Upgrade gate preserves resources");
        Check(s.Upgrade(1) && s.Village.KeepLevel == 2, "Upgrade keep unlocks tier two");
        Check(s.Upgrade(cannon.Id) && cannon.Level == 2, "Upgrade non-keep after unlocking");
        SaveStore.Validate(s.Village);
        s.Village.Gold = 0;
        Check(!s.Build(BuildingKind.Mine, 6, 20), "Reject unaffordable construction");
        Check(s.Village.Gold == 0, "Failed construction does not mutate balance");
    }
    private static void Economy()
    {
        GameSession s = NewSession();
        DateTime now = new DateTime(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc);
        s.Village.LastIncomeUtcTicks = now.AddMinutes(-10).Ticks;
        int g, c; s.Income(now, out g, out c);
        Check(g == 480 && c == 180, "Income follows building rates");
        s.Collect(now); int balance = s.Village.Gold;
        s.Collect(now);
        Check(s.Village.Gold == balance, "Collecting twice cannot duplicate rewards");
        s.Village.LastIncomeUtcTicks = now.AddHours(2).Ticks;
        s.Income(now, out g, out c);
        Check(g == 0 && c == 0, "Backward local clock cannot produce negative income");
        s.Village.Gold = s.Village.Capacity - 2;
        s.Village.LastIncomeUtcTicks = now.AddDays(-2).Ticks;
        s.Collect(now);
        Check(s.Village.Gold == s.Village.Capacity, "Collection obeys warehouse cap");
        GameSession a = NewSession(), b = NewSession();
        a.Village.LastIncomeUtcTicks = now.AddHours(-8).Ticks;
        b.Village.LastIncomeUtcTicks = now.AddDays(-10).Ticks;
        int ga, ca, gb, cb;
        a.Income(now, out ga, out ca); b.Income(now, out gb, out cb);
        Check(ga == gb && ca == cb, "Offline earnings cap at eight hours");
    }
    private static void LimitsAndDemolition()
    {
        GameSession s = NewSession();
        Check(s.Village.Count(BuildingKind.Mine) == 2 && s.Village.Limit(BuildingKind.Mine) == 3, "Tier-one mine cap is visible and leaves one slot");
        Check(s.Build(BuildingKind.Mine, 5, 5), "Build the last available mine");
        int gold = s.Village.Gold, crystal = s.Village.Crystal, count = s.Village.Buildings.Count, nextId = s.Village.NextId;
        Check(!s.Build(BuildingKind.Mine, 9, 5), "Reject construction beyond count cap");
        Check(s.Village.Gold == gold && s.Village.Crystal == crystal && s.Village.NextId == nextId && s.Village.Buildings.Count == count, "Cap rejection does not spend or allocate entity IDs");
        Check(s.Upgrade(1) && s.Village.Limit(BuildingKind.Mine) == 4, "Keep upgrade increases building allowance");
        Check(s.Build(BuildingKind.Mine, 9, 5), "New slot can be used after keep upgrade");
        Building mine = s.Village.Buildings[s.Village.Buildings.Count - 1];
        Check(s.Move(mine.Id, 9, 9), "Moving at cap remains allowed");
        s.Village.LastIncomeUtcTicks = DateTime.UtcNow.Ticks;
        int refundGold, refundCrystal; s.DemolitionRefund(mine, out refundGold, out refundCrystal);
        gold = s.Village.Gold;
        Check(refundGold == 90 && refundCrystal == 0, "Base building refunds half its investment");
        Check(s.Demolish(mine.Id) && s.Find(mine.Id) == null && s.Village.At(9, 9) == null, "Demolition removes entity and releases occupied cells");
        Check(s.Village.Gold == gold + refundGold, "Demolition credits expected refund once");
        gold = s.Village.Gold;
        Check(!s.Demolish(mine.Id) && s.Village.Gold == gold, "Repeated demolition cannot duplicate refund");
        Check(!s.Undo(false) && !s.Undo(true), "Undo history cannot resurrect demolished structures");
        Check(!s.Demolish(1) && s.Village.Count(BuildingKind.Keep) == 1, "Keep cannot be demolished");
        Check(s.Build(BuildingKind.Mine, 9, 9), "Demolition frees its building allowance");
        Building upgraded = s.Village.Buildings[s.Village.Buildings.Count - 1];
        s.Village.Gold = 3000; s.Village.Crystal = 2000;
        Check(s.Upgrade(upgraded.Id), "Upgrade a demolition test building");
        s.DemolitionRefund(upgraded, out refundGold, out refundCrystal);
        Check(refundGold == 180 && refundCrystal == 27, "Refund includes half of historical upgrade costs, rounded down");
        s.Village.Gold = s.Village.Capacity - 1; s.Village.Crystal = s.Village.Capacity - 1;
        s.Village.LastIncomeUtcTicks = DateTime.UtcNow.Ticks;
        Check(s.Demolish(upgraded.Id) && s.Village.Gold == s.Village.Capacity && s.Village.Crystal == s.Village.Capacity, "Refund respects both warehouse caps");
        s.BeginBattle();
        Building barracks = null; foreach (Building b in s.Village.Buildings) if (b.Kind == BuildingKind.Barracks) barracks = b;
        Check(!s.Demolish(barracks.Id), "No village demolition during combat");
        s.ReturnHome(); Check(s.Demolish(barracks.Id), "A barracks can be demolished at home");
        s.BeginBattle(); Check(s.Battle == null, "No expedition without a barracks");
        Check(s.Build(BuildingKind.Barracks, 30, 30), "Player can rebuild the last barracks");
        s.BeginBattle(); Check(s.Battle != null, "Rebuilt barracks restores expeditions");
        GameSession legacy = NewSession();
        for (int i = 0; i < 4; i++) legacy.Village.Add(BuildingKind.Mine, 3 + i * 4, 5);
        SaveStore.Validate(legacy.Village);
        Check(legacy.Village.Count(BuildingKind.Mine) == 6 && !legacy.Build(BuildingKind.Mine, 27, 5), "Legacy over-cap buildings survive while new construction is blocked");
        Check(legacy.Move(legacy.Village.Buildings[legacy.Village.Buildings.Count - 1].Id, 30, 5), "Legacy over-cap buildings can still be moved");
        for (int i = 0; i < Rules.Buildings.Length; i++) Check(Rules.BuildLimit((BuildingKind)i, 3) >= Rules.BuildLimit((BuildingKind)i, 1), "Count cap is monotonic for " + (BuildingKind)i);
    }
    private static void ModelChecks()
    {
        for (int kind = 0; kind < Rules.Buildings.Length; kind++)
        {
            ModelMesh mesh = ModelFactory.Building((BuildingKind)kind, 1);
            Check(mesh.Faces.Count > 60, "Detailed shared model exists for " + (BuildingKind)kind);
            ValidateMesh(mesh);
            ValidateMesh(ModelFactory.Building((BuildingKind)kind, 3));
        }
        for (int kind = 0; kind < Rules.Troops.Length; kind++)
        {
            ValidateMesh(ModelFactory.Troop((TroopKind)kind));
            TroopSpec spec = Rules.Spec((TroopKind)kind);
            Check(!string.IsNullOrEmpty(spec.Description) && !string.IsNullOrEmpty(spec.Tactics) && !string.IsNullOrEmpty(spec.Weakness), "Role, tactics and weakness exist for " + (TroopKind)kind);
        }
    }
    private static void ValidateMesh(ModelMesh mesh)
    {
        foreach (ModelFace face in mesh.Faces)
        {
            if (face.Points.Length < 3 || float.IsNaN(face.Normal.X) || float.IsNaN(face.Normal.Y) || float.IsNaN(face.Normal.Z)) throw new Exception("Invalid model face");
            foreach (ModelPoint p in face.Points) if (float.IsNaN(p.X) || float.IsNaN(p.Y) || float.IsNaN(p.Z) || float.IsInfinity(p.X) || float.IsInfinity(p.Y) || float.IsInfinity(p.Z)) throw new Exception("Nonfinite model vertex");
        }
    }
    private static void Persistence()
    {
        string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "village.xml");
        VillageData v = VillageData.Create(); SaveStore.Save(path, v);
        string message;
        VillageData read = SaveStore.Load(path, out message);
        Check(read.Buildings.Count == v.Buildings.Count && read.Gold == v.Gold, "Save roundtrip preserves settlement");
        string oldBalancePath = Path.Combine(directory, "old-arc-tower.xml");
        VillageData oldBalance = VillageData.Create();
        oldBalance.Add(BuildingKind.ArcTower, 30, 30);
        SaveStore.Save(oldBalancePath, oldBalance);
        System.Xml.XmlDocument oldBalanceXml = new System.Xml.XmlDocument();
        oldBalanceXml.Load(oldBalancePath);
        System.Xml.XmlNode oldArcHealth = oldBalanceXml.SelectSingleNode("/VillageData/Buildings/Building[Kind='ArcTower']/Health");
        if (oldArcHealth == null) throw new Exception("Old balance test fixture has no arc tower health");
        oldArcHealth.InnerText = "760"; oldBalanceXml.Save(oldBalancePath);
        VillageData migratedBalance = SaveStore.Load(oldBalancePath, out message);
        Building migratedArc = migratedBalance.Buildings.Find(b => b.Kind == BuildingKind.ArcTower);
        Check(migratedArc != null && migratedArc.Health == migratedArc.MaxHealth && migratedBalance.Buildings.Count == oldBalance.Buildings.Count,
            "Old full-health arc tower loads without losing village buildings");
        Check(message.Contains("风暴塔") && File.ReadAllText(oldBalancePath).Contains("<Health>760</Health>"),
            "Balance migration informs the player and leaves the original save unchanged until saving");
        oldArcHealth.InnerText = "759"; oldBalanceXml.Save(oldBalancePath);
        bool malformedHealthRejected = false;
        try { SaveStore.Load(oldBalancePath, out message); } catch (InvalidDataException) { malformedHealthRejected = true; }
        Check(malformedHealthRejected, "Unknown arc tower health remains invalid instead of being silently repaired");
        string legacyPath = Path.Combine(directory, "legacy-v02.xml");
        string legacyXml = File.ReadAllText(path);
        foreach (string element in new[] { "CampaignStars", "CampaignBest", "ClaimedAchievements", "ArmyInitialized", "ArmyCounts", "TrainingQueue", "TrainingStartedUtcTicks" }) legacyXml = WithoutElement(legacyXml, element);
        File.WriteAllText(legacyPath, legacyXml);
        VillageData legacyRead = SaveStore.Load(legacyPath, out message);
        Check(legacyRead.CampaignStars.Count == Missions.Count && legacyRead.UnlockedMissionCount == 1, "Legacy save gains default campaign progress on load");
        v.Gold += 10; SaveStore.Save(path, v);
        Check(File.Exists(path + ".bak"), "Atomic replacement creates backup");
        File.WriteAllText(path, "corrupt fixture");
        read = SaveStore.Load(path, out message);
        Check(read.Gold == v.Gold - 10 && message.Length > 0, "Recover from corrupted main save using backup");
        File.WriteAllText(path + ".bak", "corrupt fixture");
        bool rejected = false;
        try { SaveStore.Load(path, out message); } catch (InvalidDataException) { rejected = true; }
        Check(rejected && File.ReadAllText(path) == "corrupt fixture", "Unrecoverable save stays intact and fails visibly");
        v.Version = 99; rejected = false;
        try { SaveStore.Validate(v); } catch (InvalidDataException) { rejected = true; }
        Check(rejected, "Reject unknown future save version");
        v.Version = 1; v.Buildings[1].X = 18; v.Buildings[1].Z = 16; rejected = false;
        try { SaveStore.Validate(v); } catch (InvalidDataException) { rejected = true; }
        Check(rejected, "Reject overlapping persisted buildings");
    }
    private static string WithoutElement(string xml, string name)
    {
        string selfClosing = "<" + name + " />";
        if (xml.IndexOf(selfClosing, StringComparison.Ordinal) >= 0) return xml.Replace(selfClosing, "");
        selfClosing = "<" + name + "/>";
        if (xml.IndexOf(selfClosing, StringComparison.Ordinal) >= 0) return xml.Replace(selfClosing, "");
        string opening = "<" + name + ">", closing = "</" + name + ">";
        int start = xml.IndexOf(opening, StringComparison.Ordinal), end = xml.IndexOf(closing, StringComparison.Ordinal);
        if (start < 0 || end < start) return xml;
        return xml.Remove(start, end + closing.Length - start);
    }
    private static void Combat()
    {
        GameSession session = NewSession(); session.BeginBattle(); Battle b = session.Battle;
        int countBefore = b.Available[0];
        Check(!b.Deploy(TroopKind.Vanguard, 20000, 20000) && b.Available[0] == countBefore, "No deployment inside protected area");
        Check(b.DeployNearest(TroopKind.Vanguard, 20000, 20000) && b.Available[0] == countBefore - 1 && b.CanDeploy(b.Units[0].X, b.Units[0].Z), "Central battlefield click snaps to the nearest legal deployment cell");
        Check(!b.Deploy(TroopKind.Vanguard, -100, 1000), "Reject out-of-map deployment");
        Battle scout = new Battle(0); scout.Step(); Check(scout.TickNumber == 0, "Scouting does not consume battle time");
        Check(!scout.CastHeal(1000, 1000) && scout.SpellCharges == 2, "Cannot waste a spell before battle");
        Check(!scout.CastFocus(18000, 18000) && scout.FocusCharges == 2, "Cannot issue focus during scouting");
        Check(b.Deploy(TroopKind.Vanguard, 10500, 20500), "Deploy from outer boundary");
        Check(b.Available[0] == countBefore - 2 && b.Started, "Deployment consumes one soldier and starts timer");
        Check(!session.Build(BuildingKind.Cannon, 5, 5), "Village cannot mutate during battle");
        Unit u = b.Units[0]; u.Health = 1;
        Check(b.CastHeal(u.X, u.Z) && u.Health > 1, "Healing restores nearby living soldiers");
        Check(b.CastHeal(u.X, u.Z) && u.Health <= u.Spec.Health, "Healing cannot exceed max health");
        Check(!b.CastHeal(u.X, u.Z), "Spell charges are finite");
        Building focusTarget = null;
        foreach (Building candidate in b.Buildings) if (candidate.Health > 0 && candidate.Kind == BuildingKind.Keep) { focusTarget = candidate; break; }
        Check(focusTarget != null && !b.CastFocus(-1, 0) && b.FocusCharges == 2, "Invalid focus cannot consume a command");
        Check(!b.CastFocus(500, 500) && b.FocusCharges == 2, "Focus requires a live non-wall target");
        Check(b.CastFocus(focusTarget.CenterX, focusTarget.CenterZ) && b.FocusTargetId == focusTarget.Id && b.FocusCharges == 1, "Focus selects a live building and consumes one charge");
        b.Step();
        Check(b.Units[0].TargetId == focusTarget.Id, "Active troops obey a focus order");
        Check(b.CastFocus(focusTarget.CenterX, focusTarget.CenterZ) && !b.CastFocus(focusTarget.CenterX, focusTarget.CenterZ), "Focus charges are finite");
        for (int tick = 0; tick < 140 && !b.Finished; tick++) b.Step();
        Check(b.FocusTargetId == -1, "Focus order expires after seven seconds");
        b.Finish(); int gold = session.Village.Gold;
        Check(session.Settle(), "Finished battle settles once");
        int reward = session.Village.Gold - gold;
        Check(!session.Settle() && session.Village.Gold == gold + reward, "Settlement is idempotent");
        session.ReturnHome(); Check(session.Battle == null, "Returning home clears battle");

        List<Building> map = new List<Building>();
        Building keep = new Building { Id = 1, X = 17, Z = 18, Kind = BuildingKind.Keep }; keep.Health = keep.MaxHealth; map.Add(keep);
        for (int z = 0; z < 40; z++) { Building wall = new Building { Id = 10 + z, X = 12, Z = z, Kind = BuildingKind.Wall }; wall.Health = wall.MaxHealth; map.Add(wall); }
        Battle breach = new Battle(map, 0); breach.Deploy(TroopKind.Vanguard, 10500, 20500);
        bool wallDamaged = false;
        for (int i = 0; i < 1500 && !breach.Finished; i++)
        {
            breach.Step();
            foreach (Building bb in breach.Buildings) if (bb.Kind == BuildingKind.Wall && bb.Health < bb.MaxHealth) wallDamaged = true;
        }
        Check(wallDamaged, "Ground troop attacks wall blocking its route");
        Check(breach.Units[0].X > 13000, "Ground troop crosses wall only after breach");

        Building target = new Building { Id = 1, X = 13, Z = 18, Kind = BuildingKind.Keep }; target.Health = target.MaxHealth;
        Building barrier = new Building { Id = 2, X = 12, Z = 20, Kind = BuildingKind.Wall }; barrier.Health = barrier.MaxHealth;
        Battle ranged = new Battle(new List<Building> { target, barrier }, 0);
        ranged.Deploy(TroopKind.Ranger, 10500, 20500);
        for (int i = 0; i < 20; i++) ranged.Step();
        Check(ranged.Buildings[0].Health < target.MaxHealth && ranged.Buildings[1].Health == barrier.MaxHealth, "Ranger shoots buildings across wall");
        Building cannon = new Building { Id = 3, X = 14, Z = 19, Kind = BuildingKind.Cannon }; cannon.Health = cannon.MaxHealth;
        Battle clustered = new Battle(new List<Building> { cannon }, 0, new[] { 2, 0, 0, 0 });
        Check(clustered.Deploy(TroopKind.Vanguard, 10500, 19500) && clustered.Deploy(TroopKind.Vanguard, 10500, 20500), "Two troops can form a compact group");
        clustered.Step();
        Check(clustered.Units[0].Health == clustered.MaxHealth(TroopKind.Vanguard) || clustered.Units[1].Health == clustered.MaxHealth(TroopKind.Vanguard), "Cannon remains a ground single-target defense");
        Building arc = new Building { Id = 5, X = 14, Z = 19, Kind = BuildingKind.ArcTower }; arc.Health = arc.MaxHealth;
        Battle splashDefense = new Battle(new List<Building> { arc }, 0, new[] { 2, 0, 0, 0 });
        splashDefense.Deploy(TroopKind.Vanguard, 10500, 19500); splashDefense.Deploy(TroopKind.Vanguard, 10500, 20500); splashDefense.Step();
        Check(splashDefense.Units[0].Health < splashDefense.MaxHealth(TroopKind.Vanguard) && splashDefense.Units[1].Health < splashDefense.MaxHealth(TroopKind.Vanguard), "Arc tower splashes a compact group");
        Building tower = new Building { Id = 4, X = 14, Z = 19, Kind = BuildingKind.Watchtower }; tower.Health = tower.MaxHealth;
        Battle spacedDefense = new Battle(new List<Building> { tower }, 0, new[] { 2, 0, 0, 0 });
        spacedDefense.Deploy(TroopKind.Vanguard, 10500, 19500); spacedDefense.Deploy(TroopKind.Vanguard, 10500, 20500); spacedDefense.Step();
        Check(spacedDefense.Units[0].Health == spacedDefense.MaxHealth(TroopKind.Vanguard) || spacedDefense.Units[1].Health == spacedDefense.MaxHealth(TroopKind.Vanguard), "Watchtower remains a single-target defense");

        Building walledKeep = new Building { Id = 20, X = 17, Z = 18, Kind = BuildingKind.Keep }; walledKeep.Health = walledKeep.MaxHealth;
        Building spellWall = new Building { Id = 21, X = 12, Z = 20, Kind = BuildingKind.Wall }; spellWall.Health = spellWall.MaxHealth;
        Battle tactics = new Battle(new List<Building> { walledKeep, spellWall }, 0, new[] { 1, 0, 0, 0, 1, 0, 0, 0 });
        tactics.Deploy(TroopKind.Vanguard, 10500, 20500); tactics.Deploy(TroopKind.SkyRider, 10500, 19500);
        Check(tactics.CastFury(10500, 20500) && tactics.Units[0].FuryTicks > 0, "Fury affects deployed troops and consumes its charge");
        Building tacticalWall = tactics.Buildings[1];
        Check(tactics.CastBreach(tacticalWall.CenterX, tacticalWall.CenterZ) && tacticalWall.Health < tacticalWall.MaxHealth, "Breach damages walls and consumes its charge");
        int skyStart = tactics.Units[1].X; for (int i = 0; i < 20; i++) tactics.Step();
        Check(tactics.Units[1].X > skyStart, "Flying troop advances directly without wall pathfinding");

        Building frozenTower = new Building { Id = 30, X = 14, Z = 19, Kind = BuildingKind.Watchtower }; frozenTower.Health = frozenTower.MaxHealth;
        Battle frozen = new Battle(new List<Building> { frozenTower }, 0, new[] { 1, 0, 0, 0 });
        frozen.Deploy(TroopKind.Vanguard, 10500, 20500); int beforeFreeze = frozen.Units[0].Health;
        Check(frozen.CastFreeze(frozenTower.CenterX, frozenTower.CenterZ), "Freeze accepts a live defense target");
        for (int i = 0; i < 30; i++) frozen.Step();
        Check(frozen.Units[0].Health == beforeFreeze, "Frozen defense cannot attack during the effect");
        Battle timeout = new Battle(0); timeout.Deploy(TroopKind.Vanguard, 500, 500);
        for (int i = 0; i < 3600; i++) timeout.Step();
        Check(timeout.Finished, "A started battle always terminates by deadline");
    }
    private static void Determinism()
    {
        Battle a = new Battle(1), b = new Battle(1);
        for (int tick = 0; tick < 1700; tick++)
        {
            if (tick < 480 && tick % 20 == 0)
            {
                TroopKind kind = (TroopKind)(tick / 20 % 4);
                int z = 14000 + tick / 20 % 12 * 1000;
                a.Deploy(kind, 10500, z); b.Deploy(kind, 10500, z);
            }
            if (tick == 300) { a.CastHeal(15500, 19500); b.CastHeal(15500, 19500); }
            a.Step(); b.Step();
        }
        Check(a.StateFingerprint() == b.StateFingerprint(), "Identical command sequence reproduces combat state");
        Check(string.Join("|", a.Commands.ToArray()) == string.Join("|", b.Commands.ToArray()), "Replay command capture is stable");
    }
    private static void MissionsCheck()
    {
        Check(Missions.Count == 10 && Missions.Descriptions.Length == Missions.Count, "Campaign exposes ten named original missions");
        for (int m = 0; m < Missions.Count; m++)
        {
            List<Building> map = Missions.Create(m);
            HashSet<int> occupied = new HashSet<int>(); bool overlaps = false;
            foreach (Building building in map)
                for (int x = building.X; x < building.X + building.Spec.Size; x++)
                    for (int z = building.Z; z < building.Z + building.Spec.Size; z++)
                        if (!occupied.Add(x + z * 40)) overlaps = true;
            Check(!overlaps, "Mission " + m + " has valid non-overlapping layout");
            Battle battle = new Battle(m);
            for (int kind = 3; kind >= 0; kind--)
                for (int i = 0; i < Rules.Troops[kind].Count; i++) battle.Deploy((TroopKind)kind, 10500, 15000 + i % 12 * 1000);
            for (int i = 0; i < 3600 && !battle.Finished; i++)
            { if (i == 250 || i == 550) battle.CastHeal(17000, 19000); battle.Step(); }
            Check(battle.Finished, "Mission " + m + " reaches result screen");
            Console.WriteLine("  Mission " + m + ": " + battle.Destruction + "% / " + battle.Stars + " stars / " + battle.AliveCount + " alive");
            if (m < 4) Check(battle.Stars > 0, "Opening mission " + m + " can be won with the legacy level-one army");
        }
    }
    private static void Progression()
    {
        GameSession session = NewSession();
        Check(session.Village.CampaignStars.Count == Missions.Count && session.Village.UnlockedMissionCount == 1, "Fresh village starts with one unlocked campaign mission");
        session.MissionIndex = 1; session.BeginBattle();
        Check(session.Battle == null, "Locked campaign mission cannot start");
        session.MissionIndex = 0; session.BeginBattle();
        foreach (Building building in session.Battle.Buildings) if (building.Kind != BuildingKind.Wall) building.Health = 0;
        session.Battle.Finish();
        Check(session.Settle(), "Campaign battle settles into persistent progress");
        Check(session.Village.CampaignStars[0] == 3 && session.Village.CampaignBest[0] == 100 && session.Village.UnlockedMissionCount == 2, "Victory record unlocks the next mission");
        int gold = session.Village.Gold, crystal = session.Village.Crystal;
        Check(session.ClaimAchievement("first_victory"), "Completed achievement reward can be claimed");
        Check(session.Village.Gold == gold + 260 && session.Village.Crystal == crystal + 80, "Achievement grants its exact reward");
        Check(!session.ClaimAchievement("first_victory"), "Achievement reward cannot be claimed twice");
        Check(!session.ClaimAchievement("ten_stars"), "Incomplete achievement cannot be claimed");
        GameSession capped = NewSession(); capped.Village.Wins = 1; capped.Village.Gold = capped.Village.Capacity;
        int cappedCrystal = capped.Village.Crystal;
        Check(!capped.ClaimAchievement("first_victory") && !capped.Village.HasClaimed("first_victory") && capped.Village.Gold == capped.Village.Capacity && capped.Village.Crystal == cappedCrystal, "Full warehouse preserves unclaimed achievement reward");
        capped.Village.Gold -= 260;
        Check(capped.ClaimAchievement("first_victory") && capped.Village.HasClaimed("first_victory") && capped.Village.Gold == capped.Village.Capacity, "Achievement remains claimable after freeing warehouse space");
        session.ReturnHome();
        session.MissionIndex = 0; session.BeginBattle(); session.Battle.Finish(); session.Settle();
        Check(session.Village.CampaignStars[0] == 3 && session.Village.CampaignBest[0] == 100, "Lower replay result cannot erase a campaign record");
    }
    private static void Training()
    {
        DateTime start = DateTime.UtcNow;
        GameSession session = NewSession();
        Check(session.Village.ArmyHousing == 45 && session.Village.ArmyCapacity == 45 && session.Village.ArmyCounts[0] == 22 && session.Village.ArmyCounts[1] == 23 && session.Village.ArmyCounts[2] == 0, "Fresh village receives a full basic formation without locked troops");
        int gold = session.Village.Gold;
        Check(!session.QueueTroop(TroopKind.Guardian, start) && session.Village.Gold == gold, "Locked troop cannot enter training queue");
        Check(!session.QueueTroop(TroopKind.Vanguard, start) && session.Village.Gold == gold, "Full formation rejects extra training without spending gold");
        Check(session.DismissTroop(TroopKind.Vanguard) && session.Village.ArmyHousing == 44, "Dismissing a trained troop frees its housing space");
        Check(session.QueueTroop(TroopKind.Vanguard, start) && session.Village.QueuedHousing == 1 && session.Village.Gold == gold - Rules.Spec(TroopKind.Vanguard).TrainCost, "Manual training spends cost and enters the queue");
        Check(!session.AdvanceTraining(start.AddSeconds(1)) && session.Village.ArmyCounts[0] == 21, "Training does not finish before its duration");
        Check(session.AdvanceTraining(start.AddSeconds(2)) && session.Village.ArmyCounts[0] == 22 && session.Village.TrainingQueue.Count == 0, "Training completes at its deterministic duration");
        session.DismissTroop(TroopKind.Ranger); gold = session.Village.Gold;
        session.QueueTroop(TroopKind.Ranger, start.AddSeconds(3));
        Check(session.CancelLastTraining(start.AddSeconds(3)) && session.Village.Gold == gold && session.Village.TrainingQueue.Count == 0, "Canceling queued training refunds its full cost");

        GameSession refund = NewSession(); refund.DismissTroop(TroopKind.Vanguard); refund.QueueTroop(TroopKind.Vanguard, start);
        refund.Village.Gold = refund.Village.Capacity;
        Check(!refund.CancelLastTraining(start) && refund.Village.TrainingQueue.Count == 1, "Full warehouse preserves queued training instead of truncating its refund");
        refund.Village.Gold -= Rules.Spec(TroopKind.Vanguard).TrainCost;
        Check(refund.CancelLastTraining(start) && refund.Village.Gold == refund.Village.Capacity, "Training remains cancelable for a full refund after freeing warehouse space");

        GameSession preset = NewSession();
        foreach (Building b in preset.Village.Buildings) if (b.Kind == BuildingKind.TrainingCamp) { b.Level = 3; b.Health = b.MaxHealth; }
        for (int i = 0; i < preset.Village.ArmyCounts.Count; i++) preset.Village.ArmyCounts[i] = 0;
        int presetCost = 0; for (int i = 0; i < Rules.Troops.Length; i++) presetCost += Rules.FormationCounts[2][i] * Rules.Troops[i].TrainCost;
        gold = preset.Village.Gold;
        Check(preset.QueueFormation(2, start) && preset.Village.QueuedHousing == 45 && preset.Village.Gold == gold - presetCost, "Heavy formation fills the queue with its exact cost and housing");
        Check(preset.AdvanceTraining(start.AddMinutes(5)) && preset.Village.ArmyHousing == 45 && preset.Village.TrainingQueue.Count == 0, "Offline elapsed time completes the queued formation");
        preset.BeginBattle();
        Check(preset.Battle != null && preset.Battle.Available[0] == 7 && preset.Battle.Available[2] == 4 && preset.Battle.InitialHousing == 45, "Battle receives the selected trained composition");
        preset.Battle.Deploy(TroopKind.Vanguard, 9500, 18000); preset.Battle.Finish(); preset.Settle();
        Check(preset.Village.ArmyCounts[0] == 6 && preset.Village.ArmyCounts[2] == 4, "Only deployed troops are consumed and unused reserves return home");
        preset.ReturnHome();
        Check(preset.QueueFormation(2, DateTime.UtcNow) && preset.Village.QueuedCount(TroopKind.Vanguard) == 1,
            "A formation preset refills only the soldier lost on the previous attack");
        Check(!preset.QueueFormation(2, DateTime.UtcNow) && preset.Village.QueuedCount(TroopKind.Vanguard) == 1,
            "Repeated preset clicks do not duplicate an already queued replacement");
        GameSession lastSappers = NewSession();
        foreach (Building b in lastSappers.Village.Buildings) if (b.Kind == BuildingKind.TrainingCamp) { b.Level = 3; b.Health = b.MaxHealth; }
        for (int i = 0; i < lastSappers.Village.ArmyCounts.Count; i++) lastSappers.Village.ArmyCounts[i] = 0;
        lastSappers.Village.ArmyCounts[(int)TroopKind.Sapper] = 4;
        Check(lastSappers.QueueFormation(1, DateTime.UtcNow) && lastSappers.Village.ArmyHousing + lastSappers.Village.QueuedHousing == 45,
            "Balanced preset refills a post-battle roster containing only four surviving sappers");
        preset.Village.TrainingQueue.Clear();
        for (int i = 0; i < preset.Village.ArmyCounts.Count; i++) preset.Village.ArmyCounts[i] = 0;
        preset.BeginBattle(); Check(preset.Battle == null, "Empty formation cannot start an expedition");
        GameSession exhausted = NewSession(); exhausted.Village.Gold = 0; exhausted.BeginBattle();
        for (int kind = 0; kind < exhausted.Battle.Available.Length; kind++)
        {
            int count = exhausted.Battle.Available[kind];
            for (int i = 0; i < count; i++) exhausted.Battle.Deploy((TroopKind)kind, 500, 500);
        }
        exhausted.Battle.Finish(); exhausted.ReturnHome();
        Check(exhausted.Village.ArmyHousing == 0 && exhausted.Village.Gold >= 80,
            "A fully spent army earns enough expedition pay to train a small replacement squad");
        Check(exhausted.QueueTroop(TroopKind.Vanguard, DateTime.UtcNow), "Training remains available after the full army was deployed");
        GameSession capacity = NewSession(); foreach (Building b in capacity.Village.Buildings) if (b.Kind == BuildingKind.Barracks) b.Level = 2;
        Check(capacity.Village.ArmyCapacity == 60, "Upgrading the expedition camp increases army capacity");

        Battle heavy = new Battle(Missions.Create(3), 3, Rules.FormationCounts[2]);
        Battle ranged = new Battle(Missions.Create(3), 3, Rules.FormationCounts[3]);
        bool deployed = true;
        for (int kind = 0; kind < 4; kind++)
        {
            int heavyCount = heavy.Available[kind], rangedCount = ranged.Available[kind];
            for (int i = 0; i < heavyCount; i++) deployed &= heavy.Deploy((TroopKind)kind, 500, 500);
            for (int i = 0; i < rangedCount; i++) deployed &= ranged.Deploy((TroopKind)kind, 500, 500);
        }
        Check(deployed, "Formation simulation deploys both selected rosters");
        for (int i = 0; i < 900 && (!heavy.Finished || !ranged.Finished); i++) { heavy.Step(); ranged.Step(); }
        Check(heavy.InitialHousing == ranged.InitialHousing && heavy.Units.Count != ranged.Units.Count, "Equal housing can represent materially different troop mixes");
        Check(heavy.StateFingerprint() != ranged.StateFingerprint(), "Different formations produce different battle states under identical deployment orders");

        GameSession abandoned = NewSession(); int[] originalArmy = abandoned.Village.ArmyCounts.ToArray();
        abandoned.BeginBattle(); abandoned.Battle.Deploy(TroopKind.Vanguard, 500, 500); abandoned.Battle.Deploy(TroopKind.Ranger, 500, 500);
        abandoned.Battle.Units.Add(new Unit { Kind = TroopKind.Vanguard, IsSummon = true, Health = 80 });
        bool restored = abandoned.AbandonBattle();
        for (int i = 0; i < originalArmy.Length; i++) restored &= abandoned.Village.ArmyCounts[i] == originalArmy[i];
        Check(restored && abandoned.Battle == null && abandoned.Village.Wins == 0, "Abandoning an uncommitted battle restores the full roster without rewards");

        GameSession interrupted = NewSession(); int[] departureArmy = interrupted.Village.ArmyCounts.ToArray();
        interrupted.BeginBattle(); interrupted.Battle.Deploy(TroopKind.Vanguard, 500, 500);
        interrupted.Battle.Units.Add(new Unit { Kind = TroopKind.Vanguard, IsSummon = true, Health = 80 });
        VillageData safeSnapshot = interrupted.SnapshotForSave();
        bool snapshotMatches = interrupted.Village.ArmyHousing == 0 && safeSnapshot.ArmyHousing == 45;
        for (int i = 0; i < departureArmy.Length; i++) snapshotMatches &= safeSnapshot.ArmyCounts[i] == departureArmy[i];
        string interruptedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", Guid.NewGuid().ToString("N"), "village.xml");
        SaveStore.Save(interruptedPath, safeSnapshot);
        string recoveryMessage; VillageData restartedVillage = SaveStore.Load(interruptedPath, out recoveryMessage);
        Check(snapshotMatches && restartedVillage.ArmyHousing == 45,
            "Autosave during combat restores the departure army after an interrupted game");

        VillageData invalid = VillageData.Create(); invalid.TrainingQueue.Add(99); bool rejected = false;
        try { SaveStore.Validate(invalid); } catch (InvalidDataException) { rejected = true; }
        Check(rejected, "Invalid training queue data is rejected on load");
    }
    private static void TechnologyAndUnlocks()
    {
        GameSession session = NewSession();
        Check(session.Village.Count(BuildingKind.Barracks) == 1 && session.Village.Count(BuildingKind.TrainingCamp) == 1 && session.Village.Count(BuildingKind.Laboratory) == 1, "New village has separate barracks, training camp and laboratory");
        Check(session.Village.IsTroopUnlocked(TroopKind.Vanguard) && !session.Village.IsTroopUnlocked(TroopKind.Guardian) && !session.Village.IsTroopUnlocked(TroopKind.Sapper), "Camp level one unlocks only basic troops");
        Check(!session.ResearchTroop(TroopKind.Vanguard) && session.Village.TroopLevels[0] == 1, "Lab level one blocks level-two research");
        Building camp = null, lab = null;
        foreach (Building b in session.Village.Buildings) { if (b.Kind == BuildingKind.TrainingCamp) camp = b; if (b.Kind == BuildingKind.Laboratory) lab = b; }
        Check(session.Upgrade(1) && session.Upgrade(camp.Id) && session.Village.IsTroopUnlocked(TroopKind.Guardian), "Keep and camp upgrades unlock the guardian");
        Check(!session.Village.IsTroopUnlocked(TroopKind.Sapper), "Sapper remains locked until camp level three");
        Check(session.Upgrade(lab.Id) && session.ResearchTroop(TroopKind.Vanguard) && session.ResearchHeal(), "Upgraded laboratory researches a troop and the spell");
        Check(session.Village.TroopLevels[0] == 2 && session.Village.HealLevel == 2 && Rules.TroopHealth(TroopKind.Vanguard, 2) > Rules.TroopHealth(TroopKind.Vanguard, 1), "Research raises persisted combat statistics");
        Check(!session.ResearchTroop(TroopKind.Vanguard) && !session.ResearchTroop(TroopKind.Sapper), "Lab cap and camp unlock gates both apply");
        string researchPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", Guid.NewGuid().ToString("N"), "research.xml");
        SaveStore.Save(researchPath, session.Village);
        string researchMessage; VillageData loadedResearch = SaveStore.Load(researchPath, out researchMessage);
        Check(loadedResearch.TroopLevels[0] == 2 && loadedResearch.HealLevel == 2 && loadedResearch.TrainingCampLevel == 2, "Research and camp unlock survive a save roundtrip");
        session.BeginBattle();
        Check(session.Battle != null && session.Battle.TroopLevels[0] == 2 && session.Battle.HealLevel == 2, "Battle snapshots troop and spell levels");
        Check(session.Battle.Deploy(TroopKind.Vanguard, 500, 500) && session.Battle.Units[0].Health == Rules.TroopHealth(TroopKind.Vanguard, 2), "Researched health applies on deployment");
        session.Battle.Units[0].Health = 1;
        Check(session.Battle.CastHeal(500, 500) && session.Battle.Units[0].Health > 1 + Rules.TroopHealth(TroopKind.Vanguard, 2) * 2 / 3, "Researched heal restores more than base spell");

        VillageData legacy = VillageData.Create();
        legacy.Version = 1;
        legacy.Buildings.RemoveAll(delegate(Building b) { return b.Kind == BuildingKind.TrainingCamp || b.Kind == BuildingKind.Laboratory; });
        legacy.ArmyCounts[0] = 12; legacy.ArmyCounts[1] = 10; legacy.ArmyCounts[2] = 3; legacy.ArmyCounts[3] = 4;
        legacy.TrainingQueue.Add((int)TroopKind.Sapper);
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", Guid.NewGuid().ToString("N"), "legacy-v1.xml");
        SaveStore.Save(path, legacy);
        string message; VillageData migrated = SaveStore.Load(path, out message);
        Check(migrated.Version == 2 && migrated.TrainingCampLevel == 3 && migrated.LaboratoryLevel == 1, "Version-one save gains compatible camp and laboratory");
        Check(migrated.ArmyCounts[2] == 3 && migrated.ArmyCounts[3] == 4 && migrated.QueuedCount(TroopKind.Sapper) == 1 && migrated.IsTroopUnlocked(TroopKind.Sapper), "Legacy roster and queue survive migration");
        SaveStore.Validate(migrated);
        string preArmyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", Guid.NewGuid().ToString("N"), "pre-army-v1.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(preArmyPath));
        string preArmyXml = File.ReadAllText(path);
        foreach (string element in new[] { "ArmyInitialized", "ArmyCounts", "TrainingQueue", "TrainingStartedUtcTicks" }) preArmyXml = WithoutElement(preArmyXml, element);
        File.WriteAllText(preArmyPath, preArmyXml);
        VillageData preArmy = SaveStore.Load(preArmyPath, out message);
        Check(preArmy.Version == 2 && preArmy.ArmyHousing == 45 && preArmy.TrainingCampLevel == 3, "Pre-army legacy save gains a playable starter roster after migration");

        GameSession paused = NewSession(); DateTime trainingStart = DateTime.UtcNow;
        paused.DismissTroop(TroopKind.Vanguard); paused.QueueTroop(TroopKind.Vanguard, trainingStart);
        int campId = -1; foreach (Building b in paused.Village.Buildings) if (b.Kind == BuildingKind.TrainingCamp) campId = b.Id;
        Check(paused.Demolish(campId) && !paused.AdvanceTraining(trainingStart.AddMinutes(1)) && paused.Village.TrainingQueue.Count == 1, "Removing training camp pauses, rather than erases, the paid queue");
        Check(paused.Build(BuildingKind.TrainingCamp, 5, 5) && paused.AdvanceTraining(trainingStart.AddMinutes(1)) && paused.Village.TrainingQueue.Count == 0, "Rebuilding training camp resumes the preserved queue");
    }
    private static void Balance()
    {
        int[] basic = Rules.FormationCounts[0];
        int[] advanced = Rules.FormationCounts[4];
        for (int mission = 0; mission < Missions.Count; mission++)
        {
            Battle battle = new Battle(Missions.Create(mission), mission, basic);
            for (int kind = 0; kind < basic.Length; kind++)
                for (int i = 0; i < basic[kind]; i++) battle.Deploy((TroopKind)kind, 10500, 15000 + i % 12 * 1000);
            for (int i = 0; i < 3600 && !battle.Finished; i++) { if (i == 250 || i == 550) battle.CastHeal(17000, 19000); battle.Step(); }
            Console.WriteLine("  Basic formation mission " + mission + ": " + battle.Destruction + "% / " + battle.Stars + " stars / " + battle.AliveCount + " alive");
            if (mission == 0) Check(battle.Stars >= 1, "Starter troops can earn the first campaign star");
            if (mission == 4) Check(battle.Stars <= 2, "Basic spam cannot automatically three-star the mid-campaign fortress");
            if (mission == 6 || mission == 7) Check(battle.Stars <= 1, "Unresearched basic troops struggle against later defenses: " + mission);
            if (mission >= 8) Check(battle.Stars == 0, "Late missions resist an unresearched basic formation: " + mission);
            if (mission >= 7)
            {
                int expandedStars = 0;
                if (mission >= 8)
                {
                    int[] largeBasic = { 37, 38, 0, 0 };
                    Battle expanded = new Battle(Missions.Create(mission), mission, largeBasic);
                    for (int kind = 0; kind < largeBasic.Length; kind++)
                        for (int i = 0; i < largeBasic[kind]; i++) expanded.Deploy((TroopKind)kind, 10500, 15000 + i % 12 * 1000);
                    for (int i = 0; i < 3600 && !expanded.Finished; i++) { if (i == 250 || i == 550) expanded.CastHeal(17000, 19000); expanded.Step(); }
                    expandedStars = expanded.Stars;
                    Console.WriteLine("  Expanded basic mission " + mission + ": " + expanded.Destruction + "% / " + expanded.Stars + " stars / " + expanded.AliveCount + " alive");
                }
                Battle veteran = new Battle(Missions.Create(mission), mission, advanced, new[] { 3, 3, 3, 3, 3, 3, 3, 3 }, 3);
                for (int kind = 0; kind < advanced.Length; kind++)
                    for (int i = 0; i < advanced[kind]; i++) veteran.Deploy((TroopKind)kind, 10500, 15000 + i % 12 * 1000);
                for (int i = 0; i < 3600 && !veteran.Finished; i++)
                {
                    if (i == 1) veteran.CastBreach(11500, 20000);
                    if (i == 2) veteran.CastFury(10500, 20000);
                    if (i == 120) veteran.CastFreeze(18000, 15000);
                    if (i == 300 || i == 500) veteran.CastFocus(20000, 20000);
                    if (i == 250 || i == 550) veteran.CastHeal(17000, 19000);
                    veteran.Step();
                }
                Console.WriteLine("  Veteran formation mission " + mission + ": " + veteran.Destruction + "% / " + veteran.Stars + " stars / " + veteran.AliveCount + " alive");
                if (mission >= 8) Check(veteran.Stars >= 1 && veteran.Destruction > battle.Destruction, "Researched 75-housing veterans can progress past basic troops: " + mission);
                if (mission >= 8) Check(veteran.Stars >= 2, "Specialist veteran formation can earn two stars on the final chapter: " + mission);
                if (mission == 9) Check(veteran.Stars > expandedStars, "At equal 75 housing, researched specialist troops beat basic spam in the final mission");
            }
        }
    }
}
