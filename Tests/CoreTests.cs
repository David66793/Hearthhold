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
            Construction(); Economy(); Persistence(); Combat(); Determinism(); MissionsCheck(); Progression(); Training(); LimitsAndDemolition(); ModelChecks();
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
        for (int i = 0; i < 7; i++) Check(Rules.BuildLimit((BuildingKind)i, 3) >= Rules.BuildLimit((BuildingKind)i, 1), "Count cap is monotonic for " + (BuildingKind)i);
    }
    private static void ModelChecks()
    {
        for (int kind = 0; kind < 7; kind++)
        {
            ModelMesh mesh = ModelFactory.Building((BuildingKind)kind, 1);
            Check(mesh.Faces.Count > 60, "Detailed shared model exists for " + (BuildingKind)kind);
            ValidateMesh(mesh);
            ValidateMesh(ModelFactory.Building((BuildingKind)kind, 3));
        }
        for (int kind = 0; kind < 4; kind++)
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
        Check(b.Deploy(TroopKind.Vanguard, 10500, 20500), "Deploy from outer boundary");
        Check(b.Available[0] == countBefore - 2 && b.Started, "Deployment consumes one soldier and starts timer");
        Check(!session.Build(BuildingKind.Cannon, 5, 5), "Village cannot mutate during battle");
        Unit u = b.Units[0]; u.Health = 1;
        Check(b.CastHeal(u.X, u.Z) && u.Health > 1, "Healing restores nearby living soldiers");
        Check(b.CastHeal(u.X, u.Z) && u.Health <= u.Spec.Health, "Healing cannot exceed max health");
        Check(!b.CastHeal(u.X, u.Z), "Spell charges are finite");
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
            Check(battle.Stars > 0, "Mission " + m + " can be won with the supplied army");
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
        Check(session.Village.ArmyHousing == 45 && session.Village.ArmyCapacity == 45 && session.Village.ArmyCounts[2] == 3, "Fresh village receives a balanced starter formation");
        int gold = session.Village.Gold;
        Check(!session.QueueTroop(TroopKind.Vanguard, start) && session.Village.Gold == gold, "Full formation rejects extra training without spending gold");
        Check(session.DismissTroop(TroopKind.Vanguard) && session.Village.ArmyHousing == 44, "Dismissing a trained troop frees its housing space");
        Check(session.QueueTroop(TroopKind.Vanguard, start) && session.Village.QueuedHousing == 1 && session.Village.Gold == gold - Rules.Spec(TroopKind.Vanguard).TrainCost, "Manual training spends cost and enters the queue");
        Check(!session.AdvanceTraining(start.AddSeconds(1)) && session.Village.ArmyCounts[0] == 11, "Training does not finish before its duration");
        Check(session.AdvanceTraining(start.AddSeconds(2)) && session.Village.ArmyCounts[0] == 12 && session.Village.TrainingQueue.Count == 0, "Training completes at its deterministic duration");
        session.DismissTroop(TroopKind.Ranger); gold = session.Village.Gold;
        session.QueueTroop(TroopKind.Ranger, start.AddSeconds(3));
        Check(session.CancelLastTraining(start.AddSeconds(3)) && session.Village.Gold == gold && session.Village.TrainingQueue.Count == 0, "Canceling queued training refunds its full cost");

        GameSession refund = NewSession(); refund.DismissTroop(TroopKind.Vanguard); refund.QueueTroop(TroopKind.Vanguard, start);
        refund.Village.Gold = refund.Village.Capacity;
        Check(!refund.CancelLastTraining(start) && refund.Village.TrainingQueue.Count == 1, "Full warehouse preserves queued training instead of truncating its refund");
        refund.Village.Gold -= Rules.Spec(TroopKind.Vanguard).TrainCost;
        Check(refund.CancelLastTraining(start) && refund.Village.Gold == refund.Village.Capacity, "Training remains cancelable for a full refund after freeing warehouse space");

        GameSession preset = NewSession();
        for (int i = 0; i < preset.Village.ArmyCounts.Count; i++) preset.Village.ArmyCounts[i] = 0;
        int presetCost = 0; for (int i = 0; i < 4; i++) presetCost += Rules.FormationCounts[1][i] * Rules.Troops[i].TrainCost;
        gold = preset.Village.Gold;
        Check(preset.QueueFormation(1, start) && preset.Village.QueuedHousing == 45 && preset.Village.Gold == gold - presetCost, "Heavy formation fills the queue with its exact cost and housing");
        Check(preset.AdvanceTraining(start.AddMinutes(5)) && preset.Village.ArmyHousing == 45 && preset.Village.TrainingQueue.Count == 0, "Offline elapsed time completes the queued formation");
        preset.BeginBattle();
        Check(preset.Battle != null && preset.Battle.Available[0] == 7 && preset.Battle.Available[2] == 4 && preset.Battle.InitialHousing == 45, "Battle receives the selected trained composition");
        preset.Battle.Deploy(TroopKind.Vanguard, 9500, 18000); preset.Battle.Finish(); preset.Settle();
        Check(preset.Village.ArmyCounts[0] == 6 && preset.Village.ArmyCounts[2] == 4, "Only deployed troops are consumed and unused reserves return home");
        preset.ReturnHome();
        Check(preset.QueueFormation(1, DateTime.UtcNow) && preset.Village.QueuedCount(TroopKind.Vanguard) == 1,
            "A formation preset refills only the soldier lost on the previous attack");
        Check(!preset.QueueFormation(1, DateTime.UtcNow) && preset.Village.QueuedCount(TroopKind.Vanguard) == 1,
            "Repeated preset clicks do not duplicate an already queued replacement");
        GameSession lastSappers = NewSession();
        for (int i = 0; i < lastSappers.Village.ArmyCounts.Count; i++) lastSappers.Village.ArmyCounts[i] = 0;
        lastSappers.Village.ArmyCounts[(int)TroopKind.Sapper] = 4;
        Check(lastSappers.QueueFormation(0, DateTime.UtcNow) && lastSappers.Village.ArmyHousing + lastSappers.Village.QueuedHousing == 45,
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

        Battle heavy = new Battle(Missions.Create(3), 3, Rules.FormationCounts[1]);
        Battle ranged = new Battle(Missions.Create(3), 3, Rules.FormationCounts[2]);
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
        abandoned.BeginBattle(); abandoned.Battle.Deploy(TroopKind.Vanguard, 500, 500); abandoned.Battle.Deploy(TroopKind.Guardian, 500, 500);
        bool restored = abandoned.AbandonBattle();
        for (int i = 0; i < originalArmy.Length; i++) restored &= abandoned.Village.ArmyCounts[i] == originalArmy[i];
        Check(restored && abandoned.Battle == null && abandoned.Village.Wins == 0, "Abandoning an uncommitted battle restores the full roster without rewards");

        GameSession interrupted = NewSession(); int[] departureArmy = interrupted.Village.ArmyCounts.ToArray();
        interrupted.BeginBattle(); interrupted.Battle.Deploy(TroopKind.Vanguard, 500, 500);
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
}
