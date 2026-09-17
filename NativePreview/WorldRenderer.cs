using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using Hearthhold.Core;

namespace Hearthhold.Preview
{
    public sealed partial class GameWindow
    {
        private Bitmap terrainCache;
        private string terrainKey = "";
        private sealed class Drawable { public float Depth; public Action Draw; }
        private PointF Project(float x, float z, float height)
        {
            return new PointF(ClientSize.Width * 0.5f + panX + (x - z) * 22 * zoom,
                (ClientSize.Height - 90) * 0.5f + panY + (x + z - 40) * 11 * zoom - height * zoom);
        }
        private Cell Unproject(Point p)
        {
            float a = (p.X - ClientSize.Width * 0.5f - panX) / (22 * zoom);
            float b = (p.Y - (ClientSize.Height - 90) * 0.5f - panY) / (11 * zoom) + 40;
            return new Cell((int)Math.Floor((a + b) / 2), (int)Math.Floor((b - a) / 2));
        }
        private Building PickBuilding(Point p)
        {
            List<Building> ordered = new List<Building>(Session.Village.Buildings);
            ordered.Sort(delegate(Building a, Building b) { return (b.X + b.Z + b.Spec.Size).CompareTo(a.X + a.Z + a.Spec.Size); });
            foreach (Building b in ordered)
            {
                ModelSprite sprite = BuildingSprite(b.Kind, b.Level);
                PointF origin = Project(b.X, b.Z, 0);
                RectangleF bounds = new RectangleF(origin.X + sprite.Bounds.X * zoom, origin.Y + sprite.Bounds.Y * zoom, sprite.Bounds.Width * zoom, sprite.Bounds.Height * zoom);
                if (!bounds.Contains(p)) continue;
                int px = Math.Max(0, Math.Min(sprite.Image.Width - 1, (int)((p.X - bounds.X) / bounds.Width * sprite.Image.Width)));
                int py = Math.Max(0, Math.Min(sprite.Image.Height - 1, (int)((p.Y - bounds.Y) / bounds.Height * sprite.Image.Height)));
                if (sprite.Image.GetPixel(px, py).A > 48) return b;
            }
            Cell c = Unproject(p); return Session.Village.At(c.X, c.Z);
        }
        private static Color Shade(Color c, int amount)
        { return Color.FromArgb(c.A, Math.Max(0, Math.Min(255, c.R + amount)), Math.Max(0, Math.Min(255, c.G + amount)), Math.Max(0, Math.Min(255, c.B + amount))); }
        private static void Polygon(Graphics g, Color color, params PointF[] points)
        { using (Brush b = new SolidBrush(color)) g.FillPolygon(b, points); }
        private void GroundPoly(Graphics g, Color color, float x, float z, float sx, float sz, float height)
        { Polygon(g, color, Project(x, z, height), Project(x + sx, z, height), Project(x + sx, z + sz, height), Project(x, z + sz, height)); }
        private void Block(Graphics g, float x, float z, float sx, float sz, float baseH, float height, Color top, Color left, Color right)
        {
            PointF a = Project(x, z, baseH + height), b = Project(x + sx, z, baseH + height);
            PointF c = Project(x + sx, z + sz, baseH + height), d = Project(x, z + sz, baseH + height);
            PointF bb = Project(x + sx, z, baseH), cc = Project(x + sx, z + sz, baseH), dd = Project(x, z + sz, baseH);
            Polygon(g, left, d, c, cc, dd); Polygon(g, right, b, c, cc, bb); Polygon(g, top, a, b, c, d);
        }
        private void DrawWorld(Graphics g)
        {
            string key = ClientSize.Width + ":" + ClientSize.Height + ":" + zoom + ":" + panX + ":" + panY + ":" + showGrid;
            if (terrainCache == null || key != terrainKey)
            {
                if (terrainCache != null) terrainCache.Dispose();
                terrainCache = new Bitmap(Math.Max(1, ClientSize.Width), Math.Max(1, ClientSize.Height));
                using (Graphics ground = Graphics.FromImage(terrainCache)) { ground.SmoothingMode = SmoothingMode.AntiAlias; DrawTerrain(ground); }
                terrainKey = key;
            }
            g.DrawImageUnscaled(terrainCache, 0, 0);
            Battle battle = Session.Battle;
            if (battle != null) DrawDeployment(g);
            List<Drawable> items = new List<Drawable>();
            List<Building> buildings = battle == null ? Session.Village.Buildings : battle.Buildings;
            foreach (Building b0 in buildings)
            {
                Building b = b0;
                items.Add(new Drawable { Depth = b.X + b.Z + b.Spec.Size, Draw = delegate { DrawBuilding(g, b, battle != null); } });
            }
            for (int i = 0; i < 86; i++)
            {
                int seed = i;
                float t = (i % 22) * 1.91f - 0.6f, x, z;
                switch (i / 22)
                {
                    case 0: x = t; z = -0.7f - (i % 3) * 1.15f; break;
                    case 1: x = -0.7f - (i % 3) * 1.15f; z = t; break;
                    case 2: x = t; z = 40.8f + i % 2; break;
                    default: x = 40.8f + i % 2; z = t; break;
                }
                float tx = x, tz = z;
                items.Add(new Drawable { Depth = x + z, Draw = delegate { DrawTree(g, tx, tz, seed); } });
            }
            if (battle != null)
                foreach (Unit u0 in battle.Units)
                {
                    Unit u = u0;
                    items.Add(new Drawable { Depth = (u.X + u.Z) / 1000f, Draw = delegate { DrawUnit(g, u); } });
                }
            items.Sort(delegate(Drawable a, Drawable b) { return a.Depth.CompareTo(b.Depth); });
            foreach (Drawable item in items) item.Draw();
            if (battle == null)
            {
                PointF fire = Project(20, 24, 0);
                DrawFire(g, fire.X, fire.Y, zoom);
                if (!IsHud(mouse)) DrawPlacement(g);
            }
            else
            {
                foreach (CombatEffect effect in battle.Effects) DrawEffect(g, effect);
                if (!battle.Finished && !IsHud(mouse))
                {
                    Cell cell = Unproject(mouse);
                    if (heal) DrawRange(g, cell.X + 0.5f, cell.Z + 0.5f, 5, Mint, 2);
                    else if (!focusOrder)
                    {
                        int x, z;
                        if (battle.NearestDeployment(cell.X * 1000 + 500, cell.Z * 1000 + 500, out x, out z))
                            GroundPoly(g, Color.FromArgb(135, Mint), x / 1000, z / 1000, 1, 1, 2);
                    }
                }
            }
        }
        private void DrawTerrain(Graphics g)
        {
            using (LinearGradientBrush bg = new LinearGradientBrush(new Rectangle(0, 0, ClientSize.Width, ClientSize.Height), Color.FromArgb(56, 90, 68), Color.FromArgb(33, 64, 58), 90)) g.FillRectangle(bg, ClientRectangle);
            // River and distant banks lie outside the editable island.
            GroundPoly(g, Color.FromArgb(46, 108, 112), -12, -12, 66, 66, -70);
            for (int i = 0; i < 46; i++)
            {
                float x = (i * 13 % 65) - 12, z = (i * 7 % 65) - 12;
                PointF p = Project(x, z, -66);
                using (Pen pen = new Pen(Color.FromArgb(70, 148, 185, 171), 1.3f)) g.DrawLine(pen, p.X - 12 * zoom, p.Y, p.X + 17 * zoom, p.Y);
            }
            Block(g, -1, -1, 42, 42, -51, 36, Color.FromArgb(95, 105, 77), Color.FromArgb(70, 76, 60), Color.FromArgb(50, 65, 54));
            Block(g, -0.5f, -0.5f, 41, 41, -15, 15, Color.FromArgb(105, 137, 72), Color.FromArgb(99, 111, 68), Color.FromArgb(79, 98, 64));
            for (int z = 0; z < 40; z++)
            for (int x = 0; x < 40; x++)
            {
                int noise = ((x * 73856093 ^ z * 19349663) & 15) - 8;
                float distance = Math.Abs(x - 20) + Math.Abs(z - 20);
                Color grass = distance < 21 ? Color.FromArgb(132 + noise, 162 + noise, 89 + noise / 2) : Color.FromArgb(113 + noise, 146 + noise, 79 + noise / 2);
                bool road = (z == 23 || z == 24) && x >= 12 && x <= 29 || (x == 20 || x == 21) && z >= 11 && z <= 27;
                if (road) grass = Color.FromArgb(167 + noise / 2, 161 + noise / 2, 112 + noise / 2);
                GroundPoly(g, grass, x, z, 1.015f, 1.015f, 0);
                if (showGrid)
                { using (Pen pen = new Pen(Color.FromArgb(40, 38, 77, 39))) g.DrawPolygon(pen, new[] { Project(x, z, 0), Project(x + 1, z, 0), Project(x + 1, z + 1, 0), Project(x, z + 1, 0) }); }
                if ((x * 11 + z * 17) % 29 == 0 && !road)
                {
                    PointF p = Project(x + 0.6f, z + 0.3f, 1);
                    using (Pen pen = new Pen(Color.FromArgb(115, 77, 119, 58), zoom))
                    { g.DrawLine(pen, p.X - 2, p.Y, p.X - 3, p.Y - 3); g.DrawLine(pen, p.X, p.Y, p.X + 1, p.Y - 4); }
                }
            }
            for (int i = 0; i < 26; i++)
            {
                float x = i % 2 == 0 ? -0.3f : 40.3f, z = (i * 7 % 40);
                Block(g, x, z, 0.6f, 0.8f, 1, 6 + i % 8, Color.FromArgb(155, 157, 127), Color.FromArgb(123, 129, 110), Color.FromArgb(96, 113, 99));
            }
        }
        private void DrawDeployment(Graphics g)
        {
            PointF[] boundary = { Project(10.5f, 10.5f, 1), Project(30.5f, 10.5f, 1), Project(30.5f, 31.5f, 1), Project(10.5f, 31.5f, 1) };
            Polygon(g, Color.FromArgb(18, 45, 105, 78), boundary);
            using (Pen pen = new Pen(Color.FromArgb(225, Mint), 3)) { pen.DashStyle = DashStyle.Dash; g.DrawPolygon(pen, boundary); }
            PointF marker = Project(10, 25, 2);
            TextAt(g, "← 绿色战线 · 全图点击自动吸附", marker.X - 182, marker.Y + 28, 12, Cream, true);
        }
        private void DrawTree(Graphics g, float x, float z, int seed)
        {
            PointF p = Project(x, z, 0);
            if (p.X < -90 || p.X > ClientSize.Width + 90 || p.Y < -40 || p.Y > ClientSize.Height + 100) return;
            float s = zoom * (0.85f + seed % 4 * 0.12f);
            using (Brush shadow = new SolidBrush(Color.FromArgb(48, 20, 49, 35))) g.FillEllipse(shadow, p.X - 12 * s, p.Y - 7 * s, 40 * s, 14 * s);
            using (Brush trunk = new SolidBrush(Color.FromArgb(94, 79, 49))) g.FillRectangle(trunk, p.X - 3 * s, p.Y - 22 * s, 6 * s, 24 * s);
            for (int layer = 0; layer < 3; layer++)
            {
                float width = (25 - layer * 5) * s, y = p.Y - (14 + layer * 14) * s, top = y - 31 * s;
                Polygon(g, Color.FromArgb(43 + layer * 8, 84 + layer * 9 + seed % 10, 56 + layer * 5), new PointF(p.X, top), new PointF(p.X - width, y), new PointF(p.X, y + 7 * s), new PointF(p.X + width, y));
                Polygon(g, Color.FromArgb(74 + layer * 8, 116 + layer * 8 + seed % 10, 65 + layer * 5), new PointF(p.X, top), new PointF(p.X - width, y), new PointF(p.X, y + 7 * s));
            }
        }
        private void DrawBuilding(Graphics g, Building b, bool battle)
        {
            float size = b.Spec.Size;
            PointF center = Project(b.X + size / 2, b.Z + size / 2, 0);
            if (center.X < -180 || center.X > ClientSize.Width + 180 || center.Y < 30 || center.Y > ClientSize.Height + 190) return;
            if (b.Health <= 0)
            {
                GroundPoly(g, Color.FromArgb(125, 88, 78, 55), b.X, b.Z, size, size, 1);
                for (int i = 0; i < 5; i++) Block(g, b.X + (i % 3) * size * 0.3f, b.Z + (i / 3) * size * 0.4f, size * 0.27f, size * 0.28f, 0, 4 + i * 2, Color.FromArgb(153, 141, 115), Color.FromArgb(113, 108, 91), Color.FromArgb(94, 100, 83));
                return;
            }
            using (Brush shadow = new SolidBrush(Color.FromArgb(45, 37, 46, 30)))
                g.FillEllipse(shadow, center.X - size * 17 * zoom, center.Y - size * 5 * zoom, size * 39 * zoom, size * 17 * zoom);
            if (b.Id == selectedId && !battle)
            {
                GroundPoly(g, Color.FromArgb(115, Gold), b.X - 0.15f, b.Z - 0.15f, size + 0.3f, size + 0.3f, 1);
                if (b.Spec.Range > 0) DrawRange(g, b.X + size / 2, b.Z + size / 2, b.Spec.Range / 1000f, Gold, 1);
            }
            if (battle && Session.Battle.FocusTicks > 0 && b.Id == Session.Battle.FocusTargetId)
                GroundPoly(g, Color.FromArgb(135, Gold), b.X - 0.2f, b.Z - 0.2f, size + 0.4f, size + 0.4f, 2);
            PaintModelBuilding(g, b);
            if (b.Kind == BuildingKind.Wall)
            {
                if (battle && b.Health < b.MaxHealth) HealthBar(g, center.X, center.Y - 42 * zoom, 23 * zoom, b.Health, b.MaxHealth, Gold);
                return;
            }
            float labelY = center.Y + size * 10 * zoom + 7;
            if (!battle && (b.Id == selectedId || zoom >= 1.05f))
            {
                string title = b.Spec.Name + "  Lv." + b.Level;
                SizeF measure = g.MeasureString(title, FontFor(11, true));
                Panel(g, new RectangleF(center.X - measure.Width / 2 - 7, labelY, measure.Width + 14, 23), Color.FromArgb(220, 33, 57, 42), Color.FromArgb(115, 92, 114, 73), 6);
                TextAt(g, title, center.X - measure.Width / 2, labelY + 4, 11, Cream, true);
            }
            else if (battle && b.Health < b.MaxHealth) HealthBar(g, center.X, center.Y - 84 * zoom, 53 * zoom, b.Health, b.MaxHealth, Gold);
        }
        private void Roof(Graphics g, float x, float z, float sx, float sz, float baseH, float height, Color color)
        {
            PointF a = Project(x, z, baseH), b = Project(x + sx, z, baseH), c = Project(x + sx, z + sz, baseH), d = Project(x, z + sz, baseH);
            PointF ridge1 = Project(x + sx * 0.5f, z, baseH + height), ridge2 = Project(x + sx * 0.5f, z + sz, baseH + height);
            Polygon(g, Shade(color, 23), a, ridge1, ridge2, d);
            Polygon(g, Shade(color, -13), ridge1, b, c, ridge2);
            Polygon(g, Shade(color, -31), d, ridge2, c);
            using (Pen ridge = new Pen(Shade(color, 45), 2 * zoom)) g.DrawLine(ridge, ridge1, ridge2);
            for (int i = 1; i < 4; i++)
            {
                float t = i / 4f;
                PointF p = Project(x + sx * 0.5f * t, z + sz, baseH + height * t), q = Project(x + sx * 0.5f * t, z, baseH + height * t);
                using (Pen pen = new Pen(Color.FromArgb(70, 22, 44, 46), zoom)) g.DrawLine(pen, p, q);
            }
        }
        private void WindowOnFront(Graphics g, float x, float z, float height, float windowHeight, bool door)
        {
            PointF a = Project(x, z, height), b = Project(x + (door ? 0.6f : 0.35f), z, height);
            PointF c = new PointF(b.X, b.Y - windowHeight * zoom), d = new PointF(a.X, a.Y - windowHeight * zoom);
            Polygon(g, door ? Color.FromArgb(70, 66, 46) : Gold, a, b, c, d);
            using (Pen pen = new Pen(Color.FromArgb(218, 190, 128), 2 * zoom)) g.DrawLines(pen, new[] { a, d, c, b });
        }
        private void Flag(Graphics g, float x, float z, float height, Color color)
        {
            PointF p = Project(x, z, height);
            using (Pen pole = new Pen(Color.FromArgb(83, 71, 45), 2 * zoom)) g.DrawLine(pole, p.X, p.Y + 17 * zoom, p.X, p.Y - 23 * zoom);
            float flutter = (float)Math.Sin(animation * 2 + x) * 3 * zoom;
            Polygon(g, color, new PointF(p.X, p.Y - 22 * zoom), new PointF(p.X + 24 * zoom, p.Y - 18 * zoom + flutter), new PointF(p.X + 18 * zoom, p.Y - 10 * zoom + flutter), new PointF(p.X, p.Y - 8 * zoom));
        }
        private void DrawPlacement(Graphics g)
        {
            Cell cell = Unproject(mouse);
            Building moving = movingId >= 0 ? Session.Find(movingId) : null;
            BuildingKind? kind = moving == null ? buildKind : moving.Kind;
            if (!kind.HasValue) return;
            int size = Rules.Spec(kind.Value).Size;
            bool can = Session.Village.CanPlace(kind.Value, cell.X, cell.Z, movingId) && (moving != null || Session.Village.Gold >= Rules.Spec(kind.Value).Cost && !Session.Village.AtLimit(kind.Value));
            Color color = can ? Mint : Color.FromArgb(239, 115, 99);
            GroundPoly(g, Color.FromArgb(105, color), cell.X, cell.Z, size, size, 3);
            using (Pen pen = new Pen(color, 2)) g.DrawPolygon(pen, new[] { Project(cell.X, cell.Z, 4), Project(cell.X + size, cell.Z, 4), Project(cell.X + size, cell.Z + size, 4), Project(cell.X, cell.Z + size, 4) });
            PointF p = Project(cell.X + size / 2f, cell.Z + size / 2f, 15);
            TextAt(g, can ? "点击放置" : "无法放置", p.X - 29, p.Y - 20, 12, Cream, true);
        }
        private void DrawRange(Graphics g, float x, float z, float radius, Color color, float thickness)
        {
            PointF p = Project(x, z, 1);
            float rx = radius * 31.1127f * zoom, ry = radius * 15.5563f * zoom;
            using (Brush brush = new SolidBrush(Color.FromArgb(24, color))) g.FillEllipse(brush, p.X - rx, p.Y - ry, rx * 2, ry * 2);
            using (Pen pen = new Pen(Color.FromArgb(175, color), thickness)) g.DrawEllipse(pen, p.X - rx, p.Y - ry, rx * 2, ry * 2);
        }
        private void HealthBar(Graphics g, float x, float y, float width, int value, int max, Color color)
        {
            using (Brush bg = new SolidBrush(Color.FromArgb(180, 28, 40, 33))) g.FillRectangle(bg, x - width / 2 - 1, y - 1, width + 2, 6);
            using (Brush fill = new SolidBrush(color)) g.FillRectangle(fill, x - width / 2, y, width * Math.Max(0, value) / Math.Max(1, max), 4);
        }
        private void DrawUnit(Graphics g, Unit u)
        {
            PointF p = Project(u.X / 1000f, u.Z / 1000f, u.Spec.Flying ? 48 : 0);
            if (u.Health <= 0)
            { using (Pen cross = new Pen(Color.FromArgb(115, 208, 197, 164), 2)) { g.DrawLine(cross, p.X - 3, p.Y - 2, p.X + 3, p.Y + 2); g.DrawLine(cross, p.X + 3, p.Y - 2, p.X - 3, p.Y + 2); } return; }
            float bob = (float)Math.Sin(animation * 10 + u.Id) * 1.1f * zoom;
            DrawUnitIcon(g, u.Kind, p.X, p.Y + bob, zoom);
            if (u.Health < u.Spec.Health) HealthBar(g, p.X, p.Y - (u.Kind == TroopKind.Guardian ? 34 : 26) * zoom, 24 * zoom, u.Health, u.Spec.Health, Mint);
        }
        private void DrawUnitIcon(Graphics g, TroopKind kind, float x, float y, float scale)
        {
            using (Brush shadow = new SolidBrush(Color.FromArgb(65, 23, 43, 32))) g.FillEllipse(shadow, x - 9 * scale, y - 3 * scale, 20 * scale, 8 * scale);
            PaintSprite(g, TroopSprite(kind), x, y, scale * 0.65f);
        }
        private void DrawEffect(Graphics g, CombatEffect effect)
        {
            PointF a = Project(effect.X / 1000f, effect.Z / 1000f, 14), b = Project(effect.EndX / 1000f, effect.EndZ / 1000f, 18);
            if (effect.Kind == 0 || effect.Kind == 5)
            {
                Color arrow = effect.Kind == 5 ? Color.FromArgb(255, 183, 72) : Cream;
                using (Pen glow = new Pen(Color.FromArgb(75, arrow), effect.Kind == 5 ? 5 : 3)) g.DrawLine(glow, a, b);
                using (Pen pen = new Pen(arrow, effect.Kind == 5 ? 2.2f : 1.5f)) g.DrawLine(pen, a, b);
                float dx = b.X - a.X, dy = b.Y - a.Y, length = Math.Max(1, (float)Math.Sqrt(dx * dx + dy * dy)); dx /= length; dy /= length;
                PointF left = new PointF(b.X - dx * 8 - dy * 4, b.Y - dy * 8 + dx * 4), right = new PointF(b.X - dx * 8 + dy * 4, b.Y - dy * 8 - dx * 4);
                Polygon(g, arrow, b, left, right);
            }
            else if (effect.Kind == 1)
            {
                using (Pen smoke = new Pen(Color.FromArgb(115, 74, 70, 63), 7)) g.DrawLine(smoke, a, b);
                using (Pen core = new Pen(Color.FromArgb(255, 191, 76), 3)) g.DrawLine(core, a, b);
                using (Brush hot = new SolidBrush(Color.FromArgb(255, 219, 129))) g.FillEllipse(hot, b.X - 6, b.Y - 4, 12, 8);
                for (int i = 0; i < 5; i++) using (Pen spark = new Pen(Color.FromArgb(220 - i * 26, Gold), 2)) g.DrawLine(spark, b.X, b.Y, b.X + (i - 2) * 7, b.Y - 7 - i % 2 * 5);
            }
            else if (effect.Kind == 3)
            {
                DrawRange(g, effect.X / 1000f, effect.Z / 1000f, 5, Mint, 3);
                DrawRange(g, effect.X / 1000f, effect.Z / 1000f, 3.6f, Color.FromArgb(190, 217, 255, 229), 2);
                for (int i = 0; i < 9; i++)
                {
                    double angle = i * Math.PI * 2 / 9 + animation * 0.4;
                    float px = b.X + (float)Math.Cos(angle) * (18 + i % 3 * 11) * zoom, py = b.Y + (float)Math.Sin(angle) * (8 + i % 3 * 5) * zoom - (24 - effect.Ticks) * 0.7f;
                    using (Brush mote = new SolidBrush(Color.FromArgb(Math.Min(230, effect.Ticks * 9), Mint))) g.FillEllipse(mote, px - 2, py - 2, 4, 4);
                }
            }
            else if (effect.Kind == 6)
            {
                DrawRange(g, effect.X / 1000f, effect.Z / 1000f, 2.2f, Gold, 3);
            }
            else if (effect.Kind >= 7 && effect.Kind <= 11)
            {
                Color color = effect.Kind == 7 ? Color.FromArgb(246, 150, 68) : effect.Kind == 8 ? Color.FromArgb(112, 211, 248) : effect.Kind == 9 ? Color.FromArgb(205, 146, 80) : effect.Kind == 10 ? Color.FromArgb(164, 112, 220) : Mint;
                float radius = effect.Kind == 7 ? 4.5f : effect.Kind == 8 ? 4f : effect.Kind == 9 ? 3.6f : 1.8f;
                DrawRange(g, effect.X / 1000f, effect.Z / 1000f, radius, color, 3);
                if (effect.Kind == 11) using (Pen beam = new Pen(color, 2.5f)) g.DrawLine(beam, a, b);
            }
            else
            {
                float radius = effect.Kind == 4 ? (25 - effect.Ticks) * 1.5f * zoom : 7 * zoom;
                using (Brush dust = new SolidBrush(Color.FromArgb(Math.Min(115, effect.Ticks * 5), 151, 119, 82))) g.FillEllipse(dust, b.X - radius, b.Y - radius / 2, radius * 2, radius);
                using (Pen pen = new Pen(Color.FromArgb(Math.Min(255, effect.Ticks * 10), Gold), 2.4f)) g.DrawEllipse(pen, b.X - radius, b.Y - radius / 2, radius * 2, radius);
                for (int i = 0; i < (effect.Kind == 4 ? 8 : 4); i++)
                {
                    float angle = (float)(i * Math.PI * 2 / (effect.Kind == 4 ? 8 : 4));
                    using (Brush spark = new SolidBrush(i % 2 == 0 ? Gold : Color.FromArgb(214, 118, 66))) g.FillRectangle(spark, b.X + (float)Math.Cos(angle) * radius - 2, b.Y + (float)Math.Sin(angle) * radius * 0.5f - 2, 4, 4);
                }
            }
        }
        private void DrawFire(Graphics g, float x, float y, float scale)
        {
            using (Pen logs = new Pen(Color.FromArgb(93, 66, 41), 5 * scale)) { g.DrawLine(logs, x - 12 * scale, y - 1 * scale, x + 12 * scale, y + 3 * scale); g.DrawLine(logs, x + 10 * scale, y - 3 * scale, x - 8 * scale, y + 4 * scale); }
            float sway = (float)Math.Sin(animation * 7) * 3 * scale;
            Polygon(g, Color.FromArgb(239, 133, 63), new PointF(x - 9 * scale, y), new PointF(x - 4 * scale, y - 15 * scale), new PointF(x + sway, y - 27 * scale), new PointF(x + 8 * scale, y - 5 * scale), new PointF(x + 6 * scale, y + 1 * scale));
            Polygon(g, Gold, new PointF(x - 4 * scale, y), new PointF(x + sway, y - 17 * scale), new PointF(x + 4 * scale, y));
        }
        private void DrawEmblem(Graphics g, float x, float y)
        {
            Polygon(g, Gold, new PointF(x, y - 25), new PointF(x + 22, y - 13), new PointF(x + 18, y + 16), new PointF(x, y + 27), new PointF(x - 18, y + 16), new PointF(x - 22, y - 13));
            Polygon(g, Ink, new PointF(x, y - 19), new PointF(x + 16, y - 10), new PointF(x + 12, y + 12), new PointF(x, y + 20), new PointF(x - 12, y + 12), new PointF(x - 16, y - 10));
            DrawFire(g, x, y + 9, 0.68f);
        }
        private void DrawMiniBuilding(Graphics g, BuildingKind kind, float x, float y)
        {
            Color color = kind == BuildingKind.Reservoir || kind == BuildingKind.Laboratory ? Mint : kind == BuildingKind.TrainingCamp ? Color.FromArgb(77, 149, 145) : kind == BuildingKind.Barracks ? Color.FromArgb(194, 113, 78) : kind == BuildingKind.Wall ? Color.FromArgb(181, 176, 149) : Gold;
            Polygon(g, Shade(color, -20), new PointF(x - 17, y - 8), new PointF(x, y + 1), new PointF(x, y + 20), new PointF(x - 17, y + 11));
            Polygon(g, Shade(color, -45), new PointF(x, y + 1), new PointF(x + 17, y - 8), new PointF(x + 17, y + 11), new PointF(x, y + 20));
            Polygon(g, color, new PointF(x - 20, y - 8), new PointF(x, y - 26), new PointF(x + 20, y - 8), new PointF(x, y + 1));
            if (kind == BuildingKind.Cannon) using (Pen p = new Pen(Color.FromArgb(67, 83, 82), 10)) g.DrawLine(p, x, y - 8, x - 18, y - 20);
            if (kind == BuildingKind.Watchtower) using (Pen p = new Pen(Cream, 3)) g.DrawLine(p, x + 1, y - 24, x + 1, y - 39);
        }
    }
}
