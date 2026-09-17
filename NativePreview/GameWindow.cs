using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using Hearthhold.Core;

namespace Hearthhold.Preview
{
    public sealed partial class GameWindow : Form
    {
        internal readonly GameSession Session;
        private readonly bool preview;
        private readonly string savePath;
        private readonly Timer loop = new Timer();
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly List<HitRegion> hits = new List<HitRegion>();
        private readonly HashSet<Keys> held = new HashSet<Keys>();
        private readonly Dictionary<string, Font> fonts = new Dictionary<string, Font>();
        private double lastTime, accumulator, saveTimer, animation;
        private float zoom = 0.88f, panX, panY;
        private Point mouse, lastMouse;
        private bool middleDrag, leftDown, heal, fury, freeze, breach, focusOrder, showHelp, showGrid, muted = true;
        private double nextDeploy, nextWall;
        private int selectedId = -1, movingId = -1;
        private BuildingKind? buildKind;
        private TroopKind troopKind;
        private Rectangle oldBounds;
        private bool fullscreen;
        private int cachedGold = -1, cachedCrystal = -1;
        private string persistentWarning = "";
        private sealed class HitRegion { public RectangleF Rect; public Action Action; }
        private static readonly Color Ink = Color.FromArgb(21, 35, 34);
        private static readonly Color Cream = Color.FromArgb(241, 234, 211);
        private static readonly Color Muted = Color.FromArgb(158, 177, 163);
        private static readonly Color Gold = Color.FromArgb(235, 186, 88);
        private static readonly Color Mint = Color.FromArgb(117, 212, 184);

        public GameWindow(bool renderOnly)
        {
            preview = renderOnly;
            Text = "篝火堡垒 · Hearthhold | Windows 可玩原型 0.9.0";
            ClientSize = new Size(1440, 900);
            MinimumSize = new Size(1100, 760);
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true; KeyPreview = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves", "village.xml");
            string message = "";
            Session = new GameSession(preview ? VillageData.Create() : SaveStore.Load(savePath, out message));
            if (message.Length > 0) { Session.Notice = message; persistentWarning = message; }
            loop.Interval = 16;
            loop.Tick += OnLoop;
            if (!preview) loop.Start();
            MouseDown += OnMouseDown;
            MouseUp += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Middle) middleDrag = false; if (e.Button == MouseButtons.Left) leftDown = false; };
            MouseMove += delegate(object sender, MouseEventArgs e) {
                mouse = e.Location;
                if (middleDrag) { panX += e.X - lastMouse.X; panY += e.Y - lastMouse.Y; }
                lastMouse = e.Location;
            };
            MouseWheel += delegate(object sender, MouseEventArgs e) { zoom = Math.Max(0.48f, Math.Min(1.7f, zoom + Math.Sign(e.Delta) * 0.08f)); };
            KeyDown += OnKeyDown;
            KeyUp += delegate(object sender, KeyEventArgs e) { held.Remove(e.KeyCode); };
            Deactivate += delegate { held.Clear(); leftDown = false; middleDrag = false; };
            FormClosing += delegate { if (Session.Battle != null && !Session.Battle.Settled) Session.AbandonBattle(); Persist(); };
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { loop.Dispose(); if (terrainCache != null) terrainCache.Dispose(); foreach (Font font in fonts.Values) font.Dispose(); foreach (ModelSprite sprite in modelSprites.Values) sprite.Dispose(); }
            base.Dispose(disposing);
        }
        private void OnLoop(object sender, EventArgs args)
        {
            double now = clock.Elapsed.TotalSeconds;
            double elapsed = Math.Min(0.2, now - lastTime); lastTime = now;
            animation += elapsed;
            if (Session.AdvanceTraining(DateTime.UtcNow)) Persist();
            if (!ModalActive)
            {
                accumulator += elapsed;
                while (accumulator >= 0.05)
                {
                    if (Session.Battle != null) Session.Battle.Step();
                    accumulator -= 0.05;
                }
                if (Session.Battle != null && Session.Battle.Finished && Session.Settle()) Persist();
                float speed = (float)elapsed * 420;
                if (held.Contains(Keys.W)) panY += speed;
                if (held.Contains(Keys.S)) panY -= speed;
                if (held.Contains(Keys.A)) panX += speed;
                if (held.Contains(Keys.D)) panX -= speed;
                if (leftDown && Session.Battle != null && !heal && !fury && !freeze && !breach && !focusOrder && animation > nextDeploy && !IsHud(mouse))
                { MapAction(mouse); nextDeploy = animation + 0.15; }
                if (leftDown && Session.Battle == null && buildKind == BuildingKind.Wall && animation > nextWall && !IsHud(mouse))
                { Cell c = Unproject(mouse); if (Session.Village.CanPlace(BuildingKind.Wall, c.X, c.Z, -1)) MapAction(mouse); nextWall = animation + 0.08; }
            }
            saveTimer += elapsed;
            if (saveTimer >= 15) { Persist(); saveTimer = 0; }
            Invalidate();
        }
        private void Persist()
        {
            if (preview || Session.Battle != null && !Session.Battle.Settled) return;
            try { SaveStore.Save(savePath, Session.SnapshotForSave()); if (persistentWarning.StartsWith("保存失败")) persistentWarning = ""; }
            catch (Exception ex) { persistentWarning = "保存失败：" + ex.Message; Session.Notice = persistentWarning; }
        }
        private bool IsHud(Point p)
        {
            if (ModalActive || Session.Battle != null && Session.Battle.Finished) return true;
            if (p.Y < 112 || p.Y > ClientSize.Height - 184) return true;
            if (p.X < 254 && p.Y < (Session.Battle == null ? 500 : 585)) return true;
            if (p.X > ClientSize.Width - 278 && (Session.Battle != null && p.Y < 507 || selectedId >= 0 && p.Y < 550)) return true;
            return false;
        }
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            Focus(); mouse = e.Location; lastMouse = e.Location;
            if (e.Button == MouseButtons.Middle) { middleDrag = true; return; }
            if (e.Button == MouseButtons.Right) { CancelAction(); return; }
            if (e.Button != MouseButtons.Left) return;
            for (int i = hits.Count - 1; i >= 0; i--) if (hits[i].Rect.Contains(e.Location)) { hits[i].Action(); Invalidate(); return; }
            if (IsHud(e.Location)) return;
            leftDown = true;
            MapAction(e.Location); nextDeploy = animation + 0.22;
        }
        private void MapAction(Point p)
        {
            Cell c = Unproject(p);
            if (Session.Battle != null)
            {
                bool success = heal ? Session.Battle.CastHeal(c.X * 1000 + 500, c.Z * 1000 + 500) : fury ? Session.Battle.CastFury(c.X * 1000 + 500, c.Z * 1000 + 500) : freeze ? Session.Battle.CastFreeze(c.X * 1000 + 500, c.Z * 1000 + 500) : breach ? Session.Battle.CastBreach(c.X * 1000 + 500, c.Z * 1000 + 500) : focusOrder ? Session.Battle.CastFocus(c.X * 1000 + 500, c.Z * 1000 + 500) : Session.Battle.DeployNearest(troopKind, c.X * 1000 + 500, c.Z * 1000 + 500);
                if (success) { Session.Notice = heal ? "疗愈之雨 · 范围内友军恢复生命。" : fury ? "战吼 · 范围内友军进入狂热状态。" : freeze ? "霜封 · 范围内防御停止攻击。" : breach ? "裂地 · 范围内城墙受到重创。" : focusOrder ? "集火令已下达，部队暂时转向目标。" : Rules.Spec(troopKind).Name + "已从最近战线入场。"; if (heal || fury || freeze || breach || focusOrder) { heal = fury = freeze = breach = focusOrder = false; leftDown = false; } }
                else Session.Notice = focusOrder ? "请点击一座存活的非城墙建筑，并确认还有集火次数。" : heal || fury || freeze || breach ? "该位置没有对应法术的有效目标，或次数已用尽。" : "该兵种已经没有余量。";
                return;
            }
            if (movingId >= 0)
            { if (Session.Move(movingId, c.X, c.Z)) { movingId = -1; Persist(); } return; }
            if (buildKind.HasValue)
            {
                if (Session.Build(buildKind.Value, c.X, c.Z)) { if (!muted) System.Media.SystemSounds.Asterisk.Play(); Persist(); }
                else if (Session.Village.AtLimit(buildKind.Value)) leftDown = false;
                return;
            }
            Building b = PickBuilding(p); selectedId = b == null ? -1 : b.Id;
        }
        private void CancelAction() { buildKind = null; movingId = -1; heal = fury = freeze = breach = focusOrder = false; leftDown = false; selectedId = -1; showHelp = false; showArmyGuide = false; showBattleBrief = false; showCampaign = false; showTraining = false; showResearch = false; demolishId = -1; hits.Clear(); }
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            held.Add(e.KeyCode);
            if (e.KeyCode == Keys.F1) showHelp = !showHelp;
            if (e.KeyCode == Keys.I && demolishId < 0 && !showBattleBrief) { showArmyGuide = !showArmyGuide; guideTroop = troopKind; }
            if (e.KeyCode == Keys.T && Session.Battle == null && demolishId < 0) showTraining = !showTraining;
            if (e.KeyCode == Keys.Escape) CancelAction();
            if (e.KeyCode == Keys.F11) ToggleFullscreen();
            if (e.KeyCode == Keys.Home) { zoom = 0.88f; panX = panY = 0; }
            if (e.KeyCode == Keys.G) showGrid = !showGrid;
            if (e.Control && e.KeyCode == Keys.S) { Persist(); Session.Notice = persistentWarning.Length == 0 ? "聚落已保存。" : persistentWarning; held.Remove(Keys.S); }
            if (ModalActive) return;
            if (Session.Battle != null)
            {
                if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D8) { troopKind = (TroopKind)((int)e.KeyCode - (int)Keys.D1); heal = fury = freeze = breach = focusOrder = false; }
                if (e.KeyCode == Keys.Q) { bool next = !heal; heal = fury = freeze = breach = focusOrder = false; heal = next; }
                if (e.KeyCode == Keys.Z) { bool next = !fury; heal = fury = freeze = breach = focusOrder = false; fury = next; }
                if (e.KeyCode == Keys.X) { bool next = !freeze; heal = fury = freeze = breach = focusOrder = false; freeze = next; }
                if (e.KeyCode == Keys.C) { bool next = !breach; heal = fury = freeze = breach = focusOrder = false; breach = next; }
                if (e.KeyCode == Keys.F) { bool next = !focusOrder; heal = fury = freeze = breach = focusOrder = false; focusOrder = next; }
            }
            else
            {
                if (e.Control && e.KeyCode == Keys.Z) { Session.Undo(false); Persist(); }
                if (e.Control && e.KeyCode == Keys.Y) { Session.Undo(true); Persist(); }
                if (e.KeyCode == Keys.U && selectedId >= 0) { Session.Upgrade(selectedId); Persist(); }
                if (e.KeyCode == Keys.M && selectedId >= 0) movingId = selectedId;
                if (e.KeyCode == Keys.C) { Session.Collect(DateTime.UtcNow); Persist(); }
                if (e.KeyCode == Keys.B) { buildKind = BuildingKind.Mine; selectedId = -1; }
                if (e.KeyCode == Keys.Delete && selectedId >= 0) RequestDemolition();
            }
        }
        private void ToggleFullscreen()
        {
            if (!fullscreen) { oldBounds = Bounds; FormBorderStyle = FormBorderStyle.None; Bounds = Screen.FromControl(this).Bounds; }
            else { FormBorderStyle = FormBorderStyle.Sizable; Bounds = oldBounds; }
            fullscreen = !fullscreen;
        }
        private void StartBattle()
        {
            CancelAction(); Session.BeginBattle(); accumulator = 0; panX = panY = 0; zoom = 0.82f;
            if (Session.Battle != null && !battleBriefSeen) showBattleBrief = true;
        }
        private void BackHome()
        {
            Session.ReturnHome(); CancelAction(); Persist(); panX = panY = 0; zoom = 0.88f;
        }
        public void PrepareBattlePreview()
        {
            StartBattle();
            showBattleBrief = false; battleBriefSeen = true;
            for (int i = 0; i < 12; i++) Session.Battle.Deploy(TroopKind.Vanguard, 10500, 16000 + i * 700);
            for (int i = 0; i < 7; i++) Session.Battle.Deploy(TroopKind.Ranger, 10000, 16000 + i * 1300);
            for (int i = 0; i < 3; i++) Session.Battle.Deploy(TroopKind.Guardian, 10000, 19000 + i * 1500);
            for (int i = 0; i < 4; i++) Session.Battle.Deploy(TroopKind.Sapper, 10500, 17000 + i * 2000);
            for (int i = 0; i < 185; i++) Session.Battle.Step();
            animation = 9.25;
            Session.Notice = "战斗进行中 · 使用不同兵种打开突破口，按 Q 选择范围治疗。";
        }
        public void RenderToFile(string path)
        {
            string directory = Path.GetDirectoryName(path); if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            using (Bitmap bitmap = new Bitmap(ClientSize.Width, ClientSize.Height))
            using (Graphics g = Graphics.FromImage(bitmap)) { Render(g); bitmap.Save(path, ImageFormat.Png); }
        }
        protected override void OnPaint(PaintEventArgs e) { Render(e.Graphics); }
        private void Render(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            hits.Clear();
            DrawWorld(g);
            DrawHud(g);
            if (Session.Battle != null && Session.Battle.Finished) DrawResult(g);
            if (showHelp) DrawHelp(g);
            if (showArmyGuide) DrawArmyGuide(g);
            if (showBattleBrief) DrawBattleBrief(g);
            if (showCampaign) DrawCampaign(g);
            if (showTraining) DrawTraining(g);
            if (showResearch) DrawResearch(g);
            if (demolishId >= 0) DrawDemolition(g);
        }
        private Font FontFor(float size, bool bold)
        {
            string key = size + ":" + bold;
            Font font;
            if (!fonts.TryGetValue(key, out font)) { font = new Font("Microsoft YaHei UI", size, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel); fonts.Add(key, font); }
            return font;
        }
        private void TextAt(Graphics g, string text, float x, float y, float size, Color color, bool bold)
        { using (Brush brush = new SolidBrush(color)) g.DrawString(text, FontFor(size, bold), brush, x, y); }
        private void TextBox(Graphics g, string text, RectangleF rect, float size, Color color)
        { using (Brush brush = new SolidBrush(color)) g.DrawString(text, FontFor(size, false), brush, rect); }
        private void Panel(Graphics g, RectangleF r, Color fill, Color border, float radius)
        {
            using (GraphicsPath path = Rounded(r, radius))
            { using (Brush brush = new SolidBrush(fill)) g.FillPath(brush, path); using (Pen pen = new Pen(border)) g.DrawPath(pen, path); }
        }
        private static GraphicsPath Rounded(RectangleF r, float radius)
        {
            GraphicsPath p = new GraphicsPath(); float d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p;
        }
        private void Button(Graphics g, string label, RectangleF r, Action action, bool primary, bool active)
        {
            Color fill = primary ? Gold : active ? Color.FromArgb(57, 84, 70) : Color.FromArgb(37, 54, 49);
            if (r.Contains(mouse)) fill = primary ? Color.FromArgb(250, 204, 112) : Color.FromArgb(58, 80, 66);
            Panel(g, r, fill, primary || active ? Gold : Color.FromArgb(75, 91, 77), 7);
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (Brush brush = new SolidBrush(primary ? Ink : Cream)) g.DrawString(label, FontFor(14, true), brush, r, sf);
            hits.Add(new HitRegion { Rect = r, Action = action });
        }
        private void DrawHud(Graphics g)
        {
            int w = ClientSize.Width, h = ClientSize.Height;
            using (Brush b = new SolidBrush(Color.FromArgb(242, 19, 34, 32))) { g.FillRectangle(b, 0, 0, w, 94); g.FillRectangle(b, 0, h - 184, w, 184); }
            using (Pen p = new Pen(Color.FromArgb(75, 92, 71))) { g.DrawLine(p, 0, 94, w, 94); g.DrawLine(p, 0, h - 184, w, h - 184); }
            DrawEmblem(g, 45, 43);
            TextAt(g, "篝火堡垒", 77, 16, 26, Cream, true);
            TextAt(g, "H E A R T H H O L D", 79, 52, 11, Gold, true);
            TextAt(g, "WINDOWS 原型 / 0.9.0", 285, 39, 11, Muted, false);
            Resource(g, w - 660, 20, "金币", Session.Village.Gold, Gold, false);
            Resource(g, w - 448, 20, "晶露", Session.Village.Crystal, Mint, true);
            Button(g, "操作 / F1", new RectangleF(w - 232, 25, 95, 42), delegate { showHelp = !showHelp; }, false, showHelp);
            Button(g, muted ? "音效 关" : "音效 开", new RectangleF(w - 125, 25, 100, 42), delegate { muted = !muted; }, false, !muted);

            if (Session.Battle == null) DrawHomePanel(g); else DrawBattlePanel(g);
            if (selectedId >= 0 && Session.Battle == null) DrawSelection(g);
            DrawTroopDetails(g);
            string status = persistentWarning.Length > 0 ? persistentWarning : Session.Notice;
            RectangleF notice = new RectangleF(274, 108, Math.Max(380, w - 548), 35);
            Panel(g, notice, Color.FromArgb(213, 22, 41, 34), Color.FromArgb(95, 117, 78), 8);
            TextAt(g, status, notice.X + 14, notice.Y + 9, 12, Cream, false);

            if (Session.Battle == null) DrawBuildBar(g); else DrawArmyBar(g);
            TextAt(g, "WASD / 中键 移动   ·   滚轮 缩放   ·   Home 复位   ·   G 网格   ·   F11 全屏", 25, h - 25, 11, Muted, false);
            TextAt(g, "本地试玩  /  " + (Session.Battle == null ? "自动保存" : "离线战斗"), w - 197, h - 25, 11, Muted, false);
        }
        private void Resource(Graphics g, int x, int y, string name, int amount, Color color, bool crystal)
        {
            Panel(g, new RectangleF(x, y, 194, 53), Color.FromArgb(35, 52, 46), Color.FromArgb(64, 83, 66), 9);
            if (crystal) Polygon(g, color, new PointF(x + 21, y + 9), new PointF(x + 31, y + 25), new PointF(x + 21, y + 42), new PointF(x + 11, y + 25));
            else { using (Brush b = new SolidBrush(color)) g.FillEllipse(b, x + 10, y + 15, 24, 24); using (Pen p = new Pen(Ink, 2)) g.DrawEllipse(p, x + 15, y + 20, 14, 14); }
            TextAt(g, name, x + 45, y + 5, 11, Muted, false);
            TextAt(g, amount.ToString("N0"), x + 44, y + 20, 21, Cream, true);
            TextAt(g, "/ " + Session.Village.Capacity.ToString("N0"), x + 123, y + 28, 10, Muted, false);
        }
        private void DrawHomePanel(Graphics g)
        {
            Panel(g, new RectangleF(24, 114, 222, 415), Color.FromArgb(238, 25, 43, 36), Color.FromArgb(86, 107, 75), 12);
            TextAt(g, "你的聚落", 42, 134, 20, Cream, true);
            TextAt(g, "松风谷  /  议事堡 " + Session.Village.KeepLevel + " 级", 43, 168, 12, Muted, false);
            using (Pen p = new Pen(Color.FromArgb(67, 88, 67))) g.DrawLine(p, 43, 198, 227, 198);
            TextAt(g, "从一簇篝火，到一座堡垒", 42, 212, 12, Gold, true);
            TextAt(g, (Session.Village.Buildings.Count >= 20 ? "✓" : "○") + "  建设你的第一座建筑", 43, 244, 12, Cream, false);
            TextAt(g, (Session.Village.KeepLevel > 1 ? "✓" : "○") + "  升级议事堡", 43, 273, 12, Cream, false);
            TextAt(g, (Session.Village.Wins > 0 ? "✓" : "○") + "  远征 " + Session.Village.Wins + " 胜 · 战役 " + Session.Village.TotalStars + "/30 星", 43, 302, 12, Cream, false);
            Session.Income(DateTime.UtcNow, out cachedGold, out cachedCrystal);
            Button(g, "收取产出  +" + cachedGold + " / +" + cachedCrystal, new RectangleF(40, 346, 190, 39), delegate { Session.Collect(DateTime.UtcNow); Persist(); }, false, false);
            Button(g, "兵种 I", new RectangleF(40, 395, 91, 34), delegate { showArmyGuide = true; }, false, false);
            Button(g, "战役 / 成就", new RectangleF(139, 395, 91, 34), delegate { showCampaign = true; }, false, false);
            Button(g, "编队 / 训练  T", new RectangleF(40, 438, 190, 34), delegate { showTraining = true; }, true, false);
            Button(g, "实验室 / 科技", new RectangleF(40, 480, 190, 34), delegate { showResearch = true; }, false, false);
        }
        private void DrawBattlePanel(Graphics g)
        {
            Battle b = Session.Battle;
            Panel(g, new RectangleF(24, 114, 222, 466), Color.FromArgb(238, 25, 43, 36), Color.FromArgb(86, 107, 75), 12);
            TextAt(g, "远征 · " + Missions.Names[b.Mission], 41, 135, 17, Cream, true);
            TextAt(g, b.Started ? "战斗进行中" : "侦察中 · 首次投兵开战", 42, 169, 12, Muted, false);
            TextAt(g, string.Format("{0:00}:{1:00}", b.SecondsLeft / 60, b.SecondsLeft % 60), 41, 200, 38, Cream, true);
            TextAt(g, "破坏率", 42, 259, 12, Muted, false);
            TextAt(g, b.Destruction + "%", 166, 252, 23, Gold, true);
            for (int i = 0; i < 3; i++) TextAt(g, i < b.Stars ? "★" : "☆", 48 + i * 58, 292, 33, Gold, true);
            TextAt(g, "在场 " + b.AliveCount + " 人  ·  四类战术法术", 43, 349, 12, Muted, false);
            Button(g, "Q 疗愈 ×" + b.SpellCharges, new RectangleF(41, 375, 88, 35), delegate { bool next = !heal; heal = fury = freeze = breach = focusOrder = false; heal = next; }, false, heal);
            Button(g, "Z 战吼 ×" + b.FuryCharges, new RectangleF(138, 375, 88, 35), delegate { bool next = !fury; heal = fury = freeze = breach = focusOrder = false; fury = next; }, false, fury);
            Button(g, "X 霜封 ×" + b.FreezeCharges, new RectangleF(41, 418, 88, 35), delegate { bool next = !freeze; heal = fury = freeze = breach = focusOrder = false; freeze = next; }, false, freeze);
            Button(g, "C 裂地 ×" + b.BreachCharges, new RectangleF(138, 418, 88, 35), delegate { bool next = !breach; heal = fury = freeze = breach = focusOrder = false; breach = next; }, false, breach);
            Button(g, "F  集火令  ×" + b.FocusCharges, new RectangleF(41, 465, 185, 38), delegate { bool next = !focusOrder; heal = fury = freeze = breach = focusOrder = false; focusOrder = next; }, false, focusOrder);
        }
        private void DrawSelection(Graphics g)
        {
            Building b = Session.Find(selectedId); if (b == null) return;
            float x = ClientSize.Width - 266;
            Panel(g, new RectangleF(x, 156, 242, 388), Color.FromArgb(242, 25, 43, 36), Color.FromArgb(86, 107, 75), 12);
            TextAt(g, b.Spec.Name, x + 18, 174, 21, Cream, true);
            TextAt(g, "等级 " + b.Level + "  /  生命 " + b.MaxHealth, x + 18, 211, 12, Gold, false);
            TextBox(g, b.Spec.Description, new RectangleF(x + 18, 237, 206, 57), 11, Muted);
            TextBox(g, Rules.BuildingData(b), new RectangleF(x + 18, 292, 206, 62), 11, Gold);
            TextAt(g, "升级：" + Session.UpgradeGold(b) + " 金 / " + Session.UpgradeCrystal(b) + " 晶", x + 18, 356, 12, Cream, false);
            Button(g, "升级建筑  U", new RectangleF(x + 16, 382, 210, 34), delegate { Session.Upgrade(selectedId); Persist(); }, true, false);
            Button(g, b.Kind == BuildingKind.Laboratory ? "查看科技研究" : "移动建筑  M", new RectangleF(x + 16, 422, 210, 33), delegate { if (b.Kind == BuildingKind.Laboratory) showResearch = true; else { movingId = selectedId; buildKind = null; Session.Notice = "选择新的位置，右键取消移动。"; } }, false, movingId >= 0);
            TextAt(g, "数量 " + Session.Village.Count(b.Kind) + " / " + Session.Village.Limit(b.Kind), x + 18, 465, 11, Muted, false);
            if (b.Kind == BuildingKind.Keep) TextAt(g, "聚落核心 · 不可拆除", x + 18, 503, 13, Gold, true);
            else Button(g, "拆除建筑  Del", new RectangleF(x + 16, 494, 210, 32), RequestDemolition, false, false);
        }
        private void DrawBuildBar(Graphics g)
        {
            int w = ClientSize.Width, h = ClientSize.Height;
            TextAt(g, "营地建设", 25, h - 167, 15, Cream, true);
            TextAt(g, "选择建筑，再点击地面放置  ·  石墙支持按住拖动", 122, h - 164, 11, Muted, false);
            float cardWidth = (w - 318) / (Rules.Buildings.Length - 1f);
            for (int i = 0; i < Rules.Buildings.Length - 1; i++)
            {
                BuildingKind kind = (BuildingKind)(i + 1);
                float x = 24 + i * cardWidth;
                RectangleF r = new RectangleF(x, h - 132, cardWidth - 10, 91);
                bool active = buildKind == kind;
                Panel(g, r, active ? Color.FromArgb(56, 76, 54) : Color.FromArgb(33, 51, 42), active ? Gold : Color.FromArgb(67, 86, 65), 9);
                PaintModelIcon(g, kind, new RectangleF(x + 3, h - 122, 42, 68));
                TextAt(g, Rules.Spec(kind).Name, x + 45, h - 117, 11, Cream, true);
                TextAt(g, Rules.Spec(kind).Cost + " 金币", x + 45, h - 91, 10, Gold, false);
                bool limited = Session.Village.AtLimit(kind);
                TextAt(g, Session.Village.Count(kind) + "/" + Session.Village.Limit(kind) + (limited ? " 已满" : " 已建"), x + 45, h - 67, 9, limited ? Color.Salmon : Muted, false);
                hits.Add(new HitRegion { Rect = r, Action = delegate {
                    if (Session.Village.AtLimit(kind)) { Session.Notice = Rules.Spec(kind).Name + "已达数量上限。" + ((kind == BuildingKind.Barracks || kind == BuildingKind.TrainingCamp || kind == BuildingKind.Laboratory) ? "该建筑只能建一座。" : "请升级议事堡或先拆除一座。"); buildKind = null; return; }
                    buildKind = kind; movingId = -1; selectedId = -1; Session.Notice = "放置" + Rules.Spec(kind).Name + " · 右键取消";
                } });
            }
            float right = w - 280;
            Button(g, "‹", new RectangleF(right, h - 132, 36, 30), delegate { Session.CycleMission(-1); }, false, false);
            TextAt(g, Missions.Names[Session.MissionIndex], right + 63, h - 127, 14, Cream, true);
            bool unlocked = Session.Village.IsMissionUnlocked(Session.MissionIndex);
            TextAt(g, unlocked ? "★ " + Session.Village.CampaignStars[Session.MissionIndex] + "/3 · 最佳 " + Session.Village.CampaignBest[Session.MissionIndex] + "%" : "尚未解锁", right + 62, h - 106, 10, unlocked ? Muted : Color.Salmon, false);
            Button(g, "›", new RectangleF(w - 60, h - 132, 36, 30), delegate { Session.CycleMission(1); }, false, false);
            bool armyReady = Session.Village.ArmyHousing > 0;
            string battleLabel = !unlocked ? "先通关上一关" : armyReady ? "出发远征  →  " + Session.Village.ArmyHousing + "营位" : "先编队训练士兵";
            Button(g, battleLabel, new RectangleF(right, h - 82, 256, 41), unlocked && armyReady ? (Action)StartBattle : delegate { Session.Notice = unlocked ? "远征队为空，按 T 打开编队训练。" : "该关卡尚未解锁。"; }, true, false);
        }
        private void DrawArmyBar(Graphics g)
        {
            int w = ClientSize.Width, h = ClientSize.Height;
            TextAt(g, "远征队", 25, h - 167, 15, Cream, true);
            TextAt(g, "数字键选兵  ·  当前编队 " + Session.Battle.InitialHousing + " 营位  ·  只有已训练士兵可投放", 111, h - 164, 11, Muted, false);
            float cardWidth = (w - 340) / (float)Rules.Troops.Length;
            for (int i = 0; i < Rules.Troops.Length; i++)
            {
                TroopKind kind = (TroopKind)i;
                float x = 24 + i * cardWidth;
                RectangleF r = new RectangleF(x, h - 132, cardWidth - 10, 91);
                bool selected = !heal && !fury && !freeze && !breach && !focusOrder && troopKind == kind;
                Panel(g, r, selected ? Color.FromArgb(56, 76, 54) : Color.FromArgb(33, 51, 42), selected ? Gold : Color.FromArgb(67, 86, 65), 9);
                TextAt(g, (i + 1) + " " + Rules.Spec(kind).Name, x + 10, h - 119, 11, Cream, true);
                TextAt(g, "×" + Session.Battle.Available[i], x + 10, h - 94, 18, Gold, true);
                TextAt(g, Rules.Spec(kind).Role, x + 10, h - 66, 9, Muted, false);
                hits.Add(new HitRegion { Rect = r, Action = delegate { troopKind = kind; heal = fury = freeze = breach = focusOrder = false; } });
            }
            Button(g, "结束进攻", new RectangleF(w - 295, h - 105, 270, 57), delegate { Session.Battle.Finish(); Session.Settle(); Persist(); }, false, false);
        }
        private void DrawResult(Graphics g)
        {
            hits.Clear(); leftDown = false;
            Battle b = Session.Battle;
            using (Brush brush = new SolidBrush(Color.FromArgb(175, 8, 21, 19))) g.FillRectangle(brush, ClientRectangle);
            float x = ClientSize.Width / 2f - 240, y = ClientSize.Height / 2f - 215;
            Panel(g, new RectangleF(x, y, 480, 420), Color.FromArgb(27, 45, 37), Gold, 18);
            TextAt(g, b.Stars > 0 ? "远征凯旋" : "远征结束", x + 153, y + 31, 31, Cream, true);
            TextAt(g, Missions.Names[b.Mission] + "  /  破坏率 " + b.Destruction + "%", x + 139, y + 87, 14, Muted, false);
            for (int i = 0; i < 3; i++) TextAt(g, i < b.Stars ? "★" : "☆", x + 101 + i * 94, y + 121, 65, Gold, true);
            TextAt(g, "+ " + b.GoldReward + " 金币", x + 80, y + 230, 23, Gold, true);
            TextAt(g, "+ " + b.CrystalReward + " 晶露", x + 270, y + 230, 23, Mint, true);
            TextAt(g, "奖励已结算 · 超出仓库上限的部分不会入库", x + 89, y + 279, 12, Muted, false);
            Button(g, "返回聚落", new RectangleF(x + 58, y + 327, 364, 55), BackHome, true, false);
        }
        private void DrawHelp(Graphics g)
        {
            hits.Clear(); leftDown = false;
            using (Brush brush = new SolidBrush(Color.FromArgb(200, 8, 21, 19))) g.FillRectangle(brush, ClientRectangle);
            float x = ClientSize.Width / 2f - 285, y = ClientSize.Height / 2f - 257;
            Panel(g, new RectangleF(x, y, 570, 514), Color.FromArgb(27, 45, 37), Gold, 16);
            TextAt(g, "指挥官手册", x + 32, y + 28, 29, Cream, true);
            TextAt(g, "本地战斗已暂停", x + 34, y + 75, 12, Gold, false);
            string[] lines = {
                "WASD / 中键拖动     移动镜头",
                "滚轮 / Home / F11    缩放 / 复位 / 全屏",
                "左键建筑 → M / U / Del   移动 / 升级 / 拆除",
                "底部建筑卡片          选择建筑，再点击地面放置",
                "按住左键              连续铺墙 / 连续投兵",
                "Ctrl+Z / Ctrl+Y       撤销 / 重做建筑移动",
                "1—8 / QZXC / F        选择兵种 / 四类法术 / 集火",
                "T / I                 编队训练 / 兵种图鉴与战术",
                "C / Ctrl+S / G        收取产出 / 保存 / 网格",
                "右键 / Esc            取消当前操作"
            };
            for (int i = 0; i < lines.Length; i++) TextAt(g, lines[i], x + 34, y + 116 + i * 32, 14, Cream, false);
            Button(g, "继续游戏", new RectangleF(x + 32, y + 441, 506, 45), delegate { showHelp = false; }, true, false);
        }
    }
}
