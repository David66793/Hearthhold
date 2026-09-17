using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Hearthhold.Core;
using Hearthhold.Preview;

internal static class UiSmoke
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int count;
    private static readonly string Output = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "screenshots"));
    private static GameSession Session(GameWindow game) { return (GameSession)typeof(GameWindow).GetField("Session", Hidden).GetValue(game); }
    private static void Check(bool condition, string name) { if (!condition) throw new Exception("FAIL " + name); count++; Console.WriteLine("PASS " + name); }
    private static void Render(GameWindow game, string name) { game.RenderToFile(Path.Combine(Output, name + ".png")); }
    private static void Click(GameWindow game, int x, int y)
    {
        typeof(GameWindow).GetMethod("OnMouseDown", Hidden, null, new[] { typeof(object), typeof(MouseEventArgs) }, null).Invoke(game, new object[] { game, new MouseEventArgs(MouseButtons.Left, 1, x, y, 0) });
        typeof(GameWindow).GetField("leftDown", Hidden).SetValue(game, false);
    }
    private static void Key(GameWindow game, Keys key)
    {
        typeof(GameWindow).GetMethod("OnKeyDown", Hidden, null, new[] { typeof(object), typeof(KeyEventArgs) }, null).Invoke(game, new object[] { game, new KeyEventArgs(key) });
    }
    private static void ClickWorld(GameWindow game, float x, float z, float height)
    {
        PointF p = (PointF)typeof(GameWindow).GetMethod("Project", Hidden).Invoke(game, new object[] { x, z, height });
        Click(game, (int)p.X, (int)p.Y);
    }
    [STAThread]
    private static int Main()
    {
        try
        {
            Application.EnableVisualStyles();
            using (GameWindow game = new GameWindow(true))
            {
                GameSession session = Session(game);
                Render(game, "41-native-home-v062");
                int gold = session.Village.Gold;
                Click(game, 130, 365);
                Check(session.Village.Gold > gold, "Resource collection via visible button");
                Render(game, "41-native-home-v062");
                int initialCount = session.Village.Buildings.Count;
                Click(game, 100, 805); // Gold mine card.
                ClickWorld(game, 8.25f, 23.25f, 0);
                Check(session.Village.Buildings.Count == initialCount + 1, "Building card plus map click creates building");
                Building placed = session.Village.Buildings[session.Village.Buildings.Count - 1];
                Check(placed.X == 8 && placed.Z == 23, "Isometric cursor maps to intended grid cell");
                Key(game, Keys.Escape);
                ClickWorld(game, 9.5f, 24.5f, 22);
                Check((int)typeof(GameWindow).GetField("selectedId", Hidden).GetValue(game) == placed.Id, "Clicking visible roof selects correct building");
                Render(game, "02-selected-building");
                Key(game, Keys.M); ClickWorld(game, 8.25f, 27.25f, 0);
                Check(placed.X == 8 && placed.Z == 27, "Move shortcut and map click relocate building");
                Key(game, Keys.Control | Keys.Z);
                Check(placed.Z == 23, "Undo works through keyboard input");
                Key(game, Keys.Control | Keys.Y);
                Check(placed.Z == 27, "Redo works through keyboard input");
                Key(game, Keys.Escape);
                Render(game, "03-edited-village");
                Click(game, 1280, 835);
                Check(session.Battle != null && !session.Battle.Started, "Expedition button enters scouting");
                Render(game, "12-first-expedition-guide");
                Check((bool)typeof(GameWindow).GetField("showBattleBrief", Hidden).GetValue(game), "First expedition opens an actionable troop tutorial");
                Click(game, 720, 690);
                Check(!(bool)typeof(GameWindow).GetField("showBattleBrief", Hidden).GetValue(game), "Tutorial can be acknowledged before deployment");
                Render(game, "04-scouting");
                Key(game, Keys.D1);
                ClickWorld(game, 20.5f, 20.5f, 0);
                Check(session.Battle.Units.Count == 1 && session.Battle.Units[0].Kind == TroopKind.Vanguard && session.Battle.CanDeploy(session.Battle.Units[0].X, session.Battle.Units[0].Z), "Central map click snaps selected troop to a legal deployment cell");
                for (int i = 0; i < 300; i++) session.Battle.Step();
                Render(game, "05-battle");
                Click(game, 1280, 819);
                Check(session.Battle.Finished && session.Battle.Settled, "End attack button settles battle");
                Render(game, "06-result");
                int resultGold = session.Village.Gold;
                Click(game, 720, 590);
                Check(session.Battle == null && session.Village.Gold == resultGold, "Return button does not duplicate reward");
                Render(game, "07-returned-home");
                Click(game, 184, 412); Render(game, "22-campaign-progress");
                Check((bool)typeof(GameWindow).GetField("showCampaign", Hidden).GetValue(game), "Campaign and achievements panel opens from home");
                Click(game, 950, 265);
                Check(session.Village.HasClaimed("builder"), "Ready achievement reward can be claimed from campaign panel");
                Click(game, 720, 726);
                Check(!(bool)typeof(GameWindow).GetField("showCampaign", Hidden).GetValue(game), "Campaign panel can be dismissed");
                Key(game, Keys.T); Render(game, "42-native-training-v062");
                Check((bool)typeof(GameWindow).GetField("showTraining", Hidden).GetValue(game), "T opens formation and training panel");
                int trainingGold = session.Village.Gold;
                Click(game, 415, 337);
                Check(session.Village.TrainingQueue.Count == 1 && session.Village.Gold == trainingGold - Rules.Spec(TroopKind.Vanguard).TrainCost, "Visible training button queues a troop and spends its cost");
                Click(game, 400, 633);
                Check(session.Village.TrainingQueue.Count == 0 && session.Village.Gold == trainingGold, "Visible cancel button removes queued training and refunds cost");
                Click(game, 950, 633);
                Check(!(bool)typeof(GameWindow).GetField("showTraining", Hidden).GetValue(game), "Formation panel can be dismissed");
                Render(game, "07-returned-home");
                Click(game, 125, 496); Render(game, "48-native-research-v070");
                Check((bool)typeof(GameWindow).GetField("showResearch", Hidden).GetValue(game), "Laboratory research opens from home");
                Click(game, 700, 717);
                Check(!(bool)typeof(GameWindow).GetField("showResearch", Hidden).GetValue(game), "Laboratory research panel can be dismissed");
                Key(game, Keys.F1); Render(game, "08-help");
                Click(game, 720, 657);
                Check(!(bool)typeof(GameWindow).GetField("showHelp", Hidden).GetValue(game), "Help modal can be dismissed");
                Key(game, Keys.I); Render(game, "13-troop-encyclopedia");
                Check((bool)typeof(GameWindow).GetField("showArmyGuide", Hidden).GetValue(game), "I opens troop encyclopedia from home");
                Key(game, Keys.Escape);
                ClickWorld(game, 9.5f, 28.5f, 22);
                Render(game, "14-demolition-selection");
                Key(game, Keys.Delete); Render(game, "15-demolition-confirmation");
                Check((int)typeof(GameWindow).GetField("demolishId", Hidden).GetValue(game) == placed.Id, "Delete key requests confirmation for selected building");
                int beforeDelete = session.Village.Buildings.Count;
                Click(game, 565, 600);
                Check(session.Village.Buildings.Count == beforeDelete, "Cancel demolition preserves building");
                Key(game, Keys.Delete); Render(game, "15-demolition-confirmation");
                Click(game, 835, 600);
                Check(session.Find(placed.Id) == null && session.Village.Buildings.Count == beforeDelete - 1, "Confirm demolition removes exactly the selected building");
                Render(game, "16-after-demolition");
                game.ClientSize = new Size(1100, 760);
                Render(game, "09-small-window");
                Check(File.Exists(Path.Combine(Output, "09-small-window.png")), "Minimum window dimensions render");
                Key(game, Keys.I); Render(game, "17-guide-small-window"); Key(game, Keys.Escape);
                game.ClientSize = new Size(1920, 1080);
                Render(game, "10-full-hd");
                Stopwatch sw = Stopwatch.StartNew();
                for (int i = 0; i < 8; i++) Render(game, "10-full-hd");
                Console.WriteLine("Render plus PNG export average: " + (sw.Elapsed.TotalMilliseconds / 8).ToString("F1") + "ms (not a frame-rate measurement)");
            }
            using (GameWindow battleView = new GameWindow(true)) { battleView.PrepareBattlePreview(); Render(battleView, "11-army-assault"); }
            Console.WriteLine(count + " UI interaction checks passed. Screenshots: " + Output);
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
