using System;
using System.Drawing;
using Hearthhold.Core;

namespace Hearthhold.Preview
{
    public sealed partial class GameWindow
    {
        private bool showArmyGuide, showBattleBrief, battleBriefSeen, showCampaign, showTraining, showResearch;
        private int demolishId = -1;
        private TroopKind guideTroop = TroopKind.Vanguard;
        private bool ModalActive { get { return showHelp || showArmyGuide || showBattleBrief || showCampaign || showTraining || showResearch || demolishId >= 0; } }
        private void RequestDemolition()
        {
            Building b = Session.Find(selectedId);
            if (Session.Battle != null || b == null) return;
            if (b.Kind == BuildingKind.Keep) { Session.Notice = "议事堡是聚落核心，不能拆除。"; return; }
            demolishId = b.Id; leftDown = false; movingId = -1; buildKind = null;
        }
        private void ModalBackdrop(Graphics g)
        {
            hits.Clear(); leftDown = false;
            using (Brush brush = new SolidBrush(Color.FromArgb(203, 8, 21, 19))) g.FillRectangle(brush, ClientRectangle);
        }
        private void DrawDemolition(Graphics g)
        {
            Building b = Session.Find(demolishId);
            if (b == null) { demolishId = -1; return; }
            ModalBackdrop(g);
            float x = ClientSize.Width / 2f - 270, y = ClientSize.Height / 2f - 200;
            Panel(g, new RectangleF(x, y, 540, 400), Color.FromArgb(28, 44, 36), Color.FromArgb(204, 126, 96), 16);
            TextAt(g, "拆除这座建筑？", x + 28, y + 25, 27, Cream, true);
            PaintModelIcon(g, b.Kind, new RectangleF(x + 29, y + 88, 111, 112));
            TextAt(g, b.Spec.Name + "  ·  等级 " + b.Level, x + 160, y + 91, 20, Cream, true);
            TextAt(g, "位置 " + b.X + ", " + b.Z + "  /  拆除后释放建造名额", x + 161, y + 129, 12, Muted, false);
            int gold, crystal; Session.DemolitionRefund(b, out gold, out crystal);
            TextAt(g, "返还 " + gold + " 金币  /  " + crystal + " 晶露", x + 160, y + 165, 16, Gold, true);
            TextBox(g, "返还建造和历次升级投入的50%，受仓储上限限制。已产生的资源会先收取。拆除不能撤销。", new RectangleF(x + 30, y + 221, 480, 60), 13, Muted);
            if (b.Kind == BuildingKind.Barracks && Session.Village.Count(BuildingKind.Barracks) == 1)
                TextAt(g, "这是最后一座兵营，拆除后需重建才能出征。", x + 30, y + 285, 12, Color.Salmon, true);
            Button(g, "保留建筑", new RectangleF(x + 30, y + 329, 228, 44), delegate { demolishId = -1; }, false, false);
            Button(g, "确认拆除", new RectangleF(x + 280, y + 329, 230, 44), delegate {
                int id = demolishId; demolishId = -1;
                if (Session.Demolish(id)) { selectedId = movingId = -1; buildKind = null; Persist(); }
            }, true, false);
        }
        private void DrawTroopDetails(Graphics g)
        {
            if (Session.Battle == null || Session.Battle.Finished) return;
            float x = ClientSize.Width - 271;
            Panel(g, new RectangleF(x, 163, 247, 338), Color.FromArgb(240, 25, 43, 36), Color.FromArgb(86, 107, 75), 12);
            if (focusOrder)
            {
                TextAt(g, "集火令", x + 18, 183, 22, Gold, true);
                TextBox(g, "按 E 选择后点击敌方建筑，让在场部队集中攻击该目标7秒；每场2次。不能点城墙，也不会直接造成伤害。", new RectangleF(x + 18, 231, 211, 115), 13, Cream);
                TextBox(g, "先从薄弱一侧打开道路，再集火炮塔或议事堡。集火不代替破墙与治疗。", new RectangleF(x + 18, 365, 211, 90), 13, Muted);
            }
            else if (heal)
            {
                TextAt(g, "疗愈之雨", x + 18, 183, 22, Mint, true);
                TextBox(g, "等级 " + Session.Battle.HealLevel + "。按 Q 选择后点击友军集中的位置；半径5格内恢复约" + (67 + (Session.Battle.HealLevel - 1) * 10) + "%最大生命，每场2次。", new RectangleF(x + 18, 231, 211, 100), 13, Cream);
                TextBox(g, "等铁卫受到伤害再治疗；治疗不能复活已经阵亡的士兵。", new RectangleF(x + 18, 355, 211, 75), 13, Muted);
            }
            else
            {
                TroopSpec spec = Rules.Spec(troopKind);
                PaintTroopPortrait(g, troopKind, new RectangleF(x + 170, 179, 54, 87));
                TextAt(g, spec.Name, x + 18, 183, 23, Cream, true);
                TextAt(g, spec.Role, x + 18, 220, 12, Gold, true);
                int level = Session.Battle.TroopLevels[(int)troopKind];
                TextAt(g, "等级 " + level + "  生命 " + Rules.TroopHealth(troopKind, level) + "  伤害 " + Rules.TroopDamage(troopKind, level), x + 18, 268, 12, Cream, false);
                TextAt(g, "射程 " + (spec.Range / 1000f).ToString("0.##") + "格  攻击间隔 " + (spec.Cooldown / 20f).ToString("0.##") + "秒", x + 18, 293, 11, Muted, false);
                TextBox(g, spec.Tactics, new RectangleF(x + 18, 330, 211, 74), 13, Cream);
                TextBox(g, spec.Weakness, new RectangleF(x + 18, 415, 211, 64), 12, Color.FromArgb(216, 170, 135));
            }
        }
        private void DrawArmyGuide(Graphics g)
        {
            ModalBackdrop(g);
            float width = Math.Min(ClientSize.Width - 80, 910), height = 570;
            float x = (ClientSize.Width - width) / 2, y = (ClientSize.Height - height) / 2;
            Panel(g, new RectangleF(x, y, width, height), Color.FromArgb(26, 43, 36), Gold, 16);
            TextAt(g, "远征兵种图鉴", x + 30, y + 24, 27, Cream, true);
            TextAt(g, "了解职责，再决定投兵顺序。", x + 31, y + 66, 13, Muted, false);
            for (int i = 0; i < Rules.Troops.Length; i++)
            {
                TroopKind kind = (TroopKind)i;
                int column = i % 2, row = i / 2;
                RectangleF card = new RectangleF(x + 26 + column * 105, y + 113 + row * 83, 100, 73);
                Panel(g, card, guideTroop == kind ? Color.FromArgb(65, 80, 55) : Color.FromArgb(34, 52, 44), guideTroop == kind ? Gold : Color.FromArgb(61, 83, 64), 8);
                TextAt(g, (i + 1) + " " + Rules.Spec(kind).Name, card.X + 9, card.Y + 12, 13, Cream, true);
                TextAt(g, Rules.Spec(kind).Role, card.X + 9, card.Y + 41, 9, Muted, false);
                hits.Add(new HitRegion { Rect = card, Action = delegate { guideTroop = kind; } });
            }
            float dx = x + 264, available = width - 295;
            TroopSpec spec = Rules.Spec(guideTroop);
            PaintTroopPortrait(g, guideTroop, new RectangleF(x + width - 161, y + 108, 118, 158));
            TextAt(g, spec.Name, dx, y + 111, 30, Cream, true);
            TextAt(g, spec.Role, dx, y + 158, 14, Gold, true);
            int guideLevel = Session.Village.TroopLevels[(int)guideTroop];
            TextAt(g, "科技 " + guideLevel + "级 · " + (Session.Village.IsTroopUnlocked(guideTroop) ? "已解锁" : "训练营 " + Rules.UnlockCampLevel(guideTroop) + "级解锁"), dx, y + 190, 12, Gold, false);
            TextAt(g, "生命 " + Rules.TroopHealth(guideTroop, guideLevel) + "    单次伤害 " + Rules.TroopDamage(guideTroop, guideLevel), dx, y + 215, 14, Cream, false);
            TextAt(g, "射程 " + (spec.Range / 1000f).ToString("0.##") + " 格    占 " + spec.Housing + " 营位    训练 " + spec.TrainSeconds + " 秒", dx, y + 234, 13, Muted, false);
            TextBox(g, spec.Description, new RectangleF(dx, y + 283, available, 60), 14, Cream);
            TextAt(g, "怎么用", dx, y + 349, 14, Gold, true);
            TextBox(g, spec.Tactics, new RectangleF(dx, y + 376, available, 52), 14, Cream);
            TextBox(g, "注意：" + spec.Weakness, new RectangleF(dx, y + 436, available, 52), 13, Color.FromArgb(218, 177, 143));
            Button(g, "返回游戏", new RectangleF(x + 26, y + 503, width - 52, 42), delegate { showArmyGuide = false; }, true, false);
        }
        private void DrawTraining(Graphics g)
        {
            ModalBackdrop(g);
            float width = Math.Min(ClientSize.Width - 60, 1000), height = Math.Min(ClientSize.Height - 40, 650);
            float x = (ClientSize.Width - width) / 2, y = (ClientSize.Height - height) / 2;
            Panel(g, new RectangleF(x, y, width, height), Color.FromArgb(26, 43, 36), Gold, 16);
            int ready = Session.Village.ArmyHousing, queued = Session.Village.QueuedHousing;
            TextAt(g, "远征编队 · " + ready + " 已就绪 / " + queued + " 训练中 / " + Session.Village.ArmyCapacity + " 营位", x + 27, y + 22, 24, Cream, true);
            string queueState = Session.Village.TrainingQueue.Count == 0 ? "队列为空" : "下一个 " + Rules.Troops[Session.Village.TrainingQueue[0]].Name + " · 约 " + Session.TrainingSecondsLeft(DateTime.UtcNow) + " 秒";
            TextAt(g, queueState + "；只有实际投下的士兵会消耗，未投兵力会返回营地。", x + 29, y + 61, 12, Muted, false);
            float cardWidth = (width - 66) / 4;
            for (int i = 0; i < Rules.Troops.Length; i++)
            {
                TroopKind kind = (TroopKind)i, capturedKind = kind; TroopSpec spec = Rules.Spec(kind);
                int column = i % 4, row = i / 4; float cx = x + 25 + column * (cardWidth + 5), cy = y + 94 + row * 151;
                Panel(g, new RectangleF(cx, cy, cardWidth, 143), Color.FromArgb(34, 52, 44), Color.FromArgb(70, 91, 68), 9);
                TextAt(g, spec.Name + " · " + spec.Role, cx + 12, cy + 10, 13, Cream, true);
                int level = Session.Village.TroopLevels[i];
                TextAt(g, "就绪 " + Session.Village.ArmyCounts[i] + " · 队列 " + Session.Village.QueuedCount(kind) + " · 科技" + level, cx + 12, cy + 36, 10, Gold, true);
                TextAt(g, "生命 " + Rules.TroopHealth(kind, level) + " · 伤害 " + Rules.TroopDamage(kind, level) + " · 占" + spec.Housing, cx + 12, cy + 57, 9, Cream, false);
                TextAt(g, Session.Village.IsTroopUnlocked(kind) ? spec.TrainCost + "金 / " + spec.TrainSeconds + "秒" : "训练营" + Rules.UnlockCampLevel(kind) + "级解锁", cx + 12, cy + 78, 9, Muted, false);
                Button(g, "−", new RectangleF(cx + 10, cy + 103, (cardWidth - 25) / 2, 29), delegate { if (Session.DismissTroop(capturedKind)) Persist(); }, false, false);
                Button(g, "+", new RectangleF(cx + 15 + (cardWidth - 25) / 2, cy + 103, (cardWidth - 25) / 2, 29), delegate { if (Session.QueueTroop(capturedKind, DateTime.UtcNow)) Persist(); }, true, false);
            }
            TextAt(g, "预设只补缺；切换配比先遣散多余士兵。剩余 " + (Session.Village.ArmyCapacity - ready - queued) + " 营位。", x + 28, y + 400, 11, Muted, false);
            float presetWidth = (width - 55 - (Rules.FormationNames.Length - 1) * 8) / Rules.FormationNames.Length;
            for (int i = 0; i < Rules.FormationNames.Length; i++)
            {
                int preset = i;
                Button(g, Rules.FormationNames[i], new RectangleF(x + 25 + i * (presetWidth + 8), y + 430, presetWidth, 40), delegate { if (Session.QueueFormation(preset, DateTime.UtcNow)) Persist(); }, false, false);
            }
            Button(g, "取消队尾训练并退款", new RectangleF(x + 25, y + 489, (width - 60) / 2, 38), delegate { if (Session.CancelLastTraining(DateTime.UtcNow)) Persist(); }, false, false);
            Button(g, "返回聚落", new RectangleF(x + 35 + (width - 60) / 2, y + 489, (width - 60) / 2, 38), delegate { showTraining = false; }, true, false);
            TextAt(g, Session.Notice, x + 29, y + 547, 12, Gold, false);
            TextAt(g, "训练按真实时间推进。兵营管营位，训练营管解锁和队列，实验室管等级。", x + 29, y + 579, 11, Muted, false);
        }
        private void DrawResearch(Graphics g)
        {
            ModalBackdrop(g);
            float width = Math.Min(ClientSize.Width - 80, 770), height = 600;
            float x = (ClientSize.Width - width) / 2, y = (ClientSize.Height - height) / 2;
            Panel(g, new RectangleF(x, y, width, height), Color.FromArgb(26, 43, 36), Gold, 16);
            TextAt(g, "实验室研究 · " + Session.Village.LaboratoryLevel + "级", x + 26, y + 23, 25, Cream, true);
            TextBox(g, "兵种每级生命与伤害约+10%，疗愈之雨每级多恢复10%。科技等级不得超过实验室等级；兵种还需由训练营解锁。", new RectangleF(x + 28, y + 64, width - 55, 54), 12, Muted);
            for (int i = 0; i < Rules.Troops.Length; i++)
            {
                TroopKind kind = (TroopKind)i; int level = Session.Village.TroopLevels[i];
                int column = i % 2, row = i / 2; float columnWidth = (width - 58) / 2, rx = x + 24 + column * (columnWidth + 10), ry = y + 130 + row * 76;
                Panel(g, new RectangleF(rx, ry, columnWidth, 68), Color.FromArgb(35, 53, 44), Color.FromArgb(70, 91, 68), 8);
                TextAt(g, Rules.Troops[i].Name + " " + level + "级 · " + Rules.TroopHealth(kind, level) + "生命 / " + Rules.TroopDamage(kind, level) + "伤害", rx + 12, ry + 8, 11, Cream, true);
                TextAt(g, Session.Village.IsTroopUnlocked(kind) ? "已解锁" : "训练营" + Rules.UnlockCampLevel(kind) + "级解锁", rx + 12, ry + 38, 9, Muted, false);
                Button(g, "研究", new RectangleF(rx + columnWidth - 78, ry + 31, 66, 27), delegate { if (Session.ResearchTroop(kind)) Persist(); }, true, false);
            }
            int healLevel = Session.Village.HealLevel;
            TextAt(g, "疗愈之雨 " + healLevel + "级 · 下级 " + Rules.ResearchGold(healLevel) + "金 / " + Rules.ResearchCrystal(healLevel) + "晶", x + 37, y + 451, 13, Cream, true);
            Button(g, "研究法术", new RectangleF(x + width - 150, y + 444, 110, 34), delegate { if (Session.ResearchHeal()) Persist(); }, true, false);
            TextAt(g, Session.Notice, x + 28, y + 505, 11, Gold, false);
            Button(g, "返回聚落", new RectangleF(x + 25, y + 546, width - 50, 38), delegate { showResearch = false; }, true, false);
        }
        private void DrawBattleBrief(Graphics g)
        {
            ModalBackdrop(g);
            float width = Math.Min(ClientSize.Width - 80, 950), x = (ClientSize.Width - width) / 2, y = (ClientSize.Height - 570) / 2f;
            Panel(g, new RectangleF(x, y, width, 570), Color.FromArgb(26, 43, 36), Gold, 16);
            TextAt(g, "第一次远征，照这个顺序来", x + 28, y + 26, 27, Cream, true);
            TextAt(g, "士兵落地后会自动寻路和攻击，你负责选择位置与时机。", x + 30, y + 75, 14, Muted, false);
            TroopKind[] sequence = { TroopKind.Vanguard, TroopKind.Ranger, TroopKind.Guardian, TroopKind.Sapper };
            string[] verbs = { "① 先锋先上", "② 游侠后排", "③ 升营得铁卫", "④ 再得破城手" };
            string[] tips = { "按1选先锋，沿西侧缺口成组投放，吸引火力。", "按2选游侠，放在先锋后面隔墙远程输出。", "训练营2级解锁铁卫，先投重甲吸引防御塔。", "训练营3级解锁破城手，用来快速拆墙。" };
            float cw = (width - 68) / 4;
            for (int i = 0; i < 4; i++)
            {
                float cx = x + 25 + i * (cw + 6);
                Panel(g, new RectangleF(cx, y + 123, cw, 256), Color.FromArgb(36, 54, 44), Color.FromArgb(76, 96, 71), 9);
                PaintTroopPortrait(g, sequence[i], new RectangleF(cx + cw / 2 - 43, y + 138, 86, 107));
                TextAt(g, verbs[i], cx + 15, y + 259, 15, Gold, true);
                TextBox(g, tips[i], new RectangleF(cx + 15, y + 300, cw - 30, 70), 12, Cream);
            }
            TextBox(g, "点击战场任意地块，单位会自动吸附到最近的绿色战线；按住可连续投放。首次投兵后才开始180秒计时。部队受伤时按Q，在友军附近释放治疗。", new RectangleF(x + 30, y + 404, width - 60, 58), 14, Cream);
            TextAt(g, "胜利星级：摧毁议事堡 / 破坏率50% / 破坏率100%，各得一星。", x + 30, y + 466, 12, Muted, false);
            Button(g, "知道了，开始侦察", new RectangleF(x + 28, y + 510, width - 56, 40), delegate { showBattleBrief = false; battleBriefSeen = true; }, true, false);
        }
        private void DrawCampaign(Graphics g)
        {
            ModalBackdrop(g);
            float width = Math.Min(ClientSize.Width - 70, 950), height = Math.Min(ClientSize.Height - 50, 620);
            float x = (ClientSize.Width - width) / 2, y = (ClientSize.Height - height) / 2;
            Panel(g, new RectangleF(x, y, width, height), Color.FromArgb(26, 43, 36), Gold, 16);
            TextAt(g, "战役地图 · " + Session.Village.TotalStars + " / 30 星", x + 27, y + 22, 27, Cream, true);
            float column = (width - 72) / 2;
            TextAt(g, "逐关获得至少1星，解锁下一站", x + 28, y + 63, 12, Muted, false);
            TextAt(g, "成就奖励", x + 45 + column, y + 63, 12, Muted, false);
            for (int i = 0; i < Missions.Count; i++)
            {
                int mission = i;
                float rowY = y + 91 + mission * 39; bool unlocked = Session.Village.IsMissionUnlocked(mission);
                string record = unlocked ? Session.Village.CampaignStars[mission] + "星 · " + Session.Village.CampaignBest[mission] + "%" : "未解锁";
                RectangleF row = new RectangleF(x + 27, rowY, column, 33);
                if (unlocked) Button(g, (mission + 1) + "  " + Missions.Names[mission] + "    " + record, row, delegate { Session.MissionIndex = mission; showCampaign = false; Session.Notice = Missions.Descriptions[mission]; }, false, false);
                else { Panel(g, row, Color.FromArgb(31, 45, 39), Color.FromArgb(54, 67, 59), 6); TextAt(g, (mission + 1) + "  " + Missions.Names[mission] + "    " + record, row.X + 12, row.Y + 8, 11, Muted, false); }
            }
            for (int i = 0; i < Achievements.Specs.Length; i++)
            {
                AchievementSpec achievement = Achievements.Specs[i], capturedAchievement = achievement;
                int progress = Achievements.Progress(Session.Village, achievement);
                bool claimed = Session.Village.HasClaimed(achievement.Id), ready = progress >= achievement.Target;
                string state = claimed ? "已领取" : ready ? "可领取" : progress + "/" + achievement.Target;
                RectangleF row = new RectangleF(x + 45 + column, y + 91 + i * 78, column, 68);
                if (ready && !claimed) Button(g, achievement.Name + " · " + state + "  +" + achievement.GoldReward + "金/" + achievement.CrystalReward + "晶", row, delegate { if (Session.ClaimAchievement(capturedAchievement.Id)) Persist(); }, true, false);
                else
                {
                    Panel(g, row, Color.FromArgb(33, 51, 42), claimed ? Mint : Color.FromArgb(66, 84, 68), 7);
                    TextAt(g, achievement.Name + " · " + state, row.X + 12, row.Y + 10, 13, claimed ? Mint : Cream, true);
                    TextAt(g, achievement.Description + "  ·  " + achievement.GoldReward + "金/" + achievement.CrystalReward + "晶", row.X + 12, row.Y + 38, 10, Muted, false);
                }
            }
            Button(g, "返回聚落", new RectangleF(x + 27, y + height - 52, width - 54, 38), delegate { showCampaign = false; }, true, false);
        }
    }
}
