using System;
using System.Drawing;
using Hearthhold.Core;

namespace Hearthhold.Preview
{
    public sealed partial class GameWindow
    {
        private bool showArmyGuide, showBattleBrief, battleBriefSeen, showCampaign, showTraining;
        private int demolishId = -1;
        private TroopKind guideTroop = TroopKind.Guardian;
        private bool ModalActive { get { return showHelp || showArmyGuide || showBattleBrief || showCampaign || showTraining || demolishId >= 0; } }
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
                TextAt(g, "这是最后一座远征营，拆除后需重建才能出征。", x + 30, y + 285, 12, Color.Salmon, true);
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
            if (heal)
            {
                TextAt(g, "疗愈之雨", x + 18, 183, 22, Mint, true);
                TextBox(g, "按 Q 选择后，点击友军集中的位置。恢复半径5格内每名存活友军最大生命的约2/3，每场可用2次。", new RectangleF(x + 18, 231, 211, 100), 13, Cream);
                TextBox(g, "等铁卫受到伤害再治疗；治疗不能复活已经阵亡的士兵。", new RectangleF(x + 18, 355, 211, 75), 13, Muted);
            }
            else
            {
                TroopSpec spec = Rules.Spec(troopKind);
                PaintTroopPortrait(g, troopKind, new RectangleF(x + 170, 179, 54, 87));
                TextAt(g, spec.Name, x + 18, 183, 23, Cream, true);
                TextAt(g, spec.Role, x + 18, 220, 12, Gold, true);
                TextAt(g, "生命 " + spec.Health + "  单次伤害 " + spec.Damage, x + 18, 268, 12, Cream, false);
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
            for (int i = 0; i < 4; i++)
            {
                TroopKind kind = (TroopKind)i;
                RectangleF card = new RectangleF(x + 26, y + 113 + i * 83, 208, 73);
                Panel(g, card, guideTroop == kind ? Color.FromArgb(65, 80, 55) : Color.FromArgb(34, 52, 44), guideTroop == kind ? Gold : Color.FromArgb(61, 83, 64), 8);
                PaintTroopPortrait(g, kind, new RectangleF(card.X + 7, card.Y + 3, 52, 65));
                TextAt(g, Rules.Spec(kind).Name, card.X + 71, card.Y + 12, 17, Cream, true);
                TextAt(g, Rules.Spec(kind).Role, card.X + 72, card.Y + 42, 11, Muted, false);
                hits.Add(new HitRegion { Rect = card, Action = delegate { guideTroop = kind; } });
            }
            float dx = x + 264, available = width - 295;
            TroopSpec spec = Rules.Spec(guideTroop);
            PaintTroopPortrait(g, guideTroop, new RectangleF(x + width - 161, y + 108, 118, 158));
            TextAt(g, spec.Name, dx, y + 111, 30, Cream, true);
            TextAt(g, spec.Role, dx, y + 158, 14, Gold, true);
            TextAt(g, "生命 " + spec.Health + "    单次伤害 " + spec.Damage, dx, y + 204, 14, Cream, false);
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
                float cx = x + 25 + i * (cardWidth + 5);
                Panel(g, new RectangleF(cx, y + 94, cardWidth, 284), Color.FromArgb(34, 52, 44), Color.FromArgb(70, 91, 68), 9);
                PaintTroopPortrait(g, kind, new RectangleF(cx + cardWidth / 2 - 40, y + 105, 80, 104));
                TextAt(g, spec.Name, cx + 15, y + 216, 18, Cream, true);
                TextAt(g, "就绪 " + Session.Village.ArmyCounts[i] + " · 队列 " + Session.Village.QueuedCount(kind), cx + 15, y + 249, 12, Gold, true);
                TextAt(g, "占 " + spec.Housing + " 营位 · " + spec.TrainCost + " 金 · " + spec.TrainSeconds + " 秒", cx + 15, y + 276, 11, Muted, false);
                TextBox(g, spec.Role, new RectangleF(cx + 15, y + 306, cardWidth - 30, 30), 11, Cream);
                Button(g, "− 遣散", new RectangleF(cx + 13, y + 337, (cardWidth - 31) / 2, 30), delegate { if (Session.DismissTroop(capturedKind)) Persist(); }, false, false);
                Button(g, "+ 训练", new RectangleF(cx + 18 + (cardWidth - 31) / 2, y + 337, (cardWidth - 31) / 2, 30), delegate { if (Session.QueueTroop(capturedKind, DateTime.UtcNow)) Persist(); }, true, false);
            }
            TextAt(g, "点击预设可补齐战后缺员；当前剩余 " + (Session.Village.ArmyCapacity - ready - queued) + " 营位。", x + 28, y + 402, 12, Muted, false);
            float presetWidth = (width - 70) / 3;
            for (int i = 0; i < Rules.FormationNames.Length; i++)
            {
                int preset = i;
                Button(g, Rules.FormationNames[i], new RectangleF(x + 25 + i * (presetWidth + 10), y + 430, presetWidth, 40), delegate { if (Session.QueueFormation(preset, DateTime.UtcNow)) Persist(); }, false, false);
            }
            Button(g, "取消队尾训练并退款", new RectangleF(x + 25, y + 489, (width - 60) / 2, 38), delegate { if (Session.CancelLastTraining(DateTime.UtcNow)) Persist(); }, false, false);
            Button(g, "返回聚落", new RectangleF(x + 35 + (width - 60) / 2, y + 489, (width - 60) / 2, 38), delegate { showTraining = false; }, true, false);
            TextAt(g, Session.Notice, x + 29, y + 547, 12, Gold, false);
            TextAt(g, "训练按真实时间推进，离线时间也会结算。营位不足可调整编队或扩建远征营。", x + 29, y + 579, 11, Muted, false);
        }
        private void DrawBattleBrief(Graphics g)
        {
            ModalBackdrop(g);
            float width = Math.Min(ClientSize.Width - 80, 950), x = (ClientSize.Width - width) / 2, y = (ClientSize.Height - 570) / 2f;
            Panel(g, new RectangleF(x, y, width, 570), Color.FromArgb(26, 43, 36), Gold, 16);
            TextAt(g, "第一次远征，照这个顺序来", x + 28, y + 26, 27, Cream, true);
            TextAt(g, "士兵落地后会自动寻路和攻击，你负责选择位置与时机。", x + 30, y + 75, 14, Muted, false);
            TroopKind[] sequence = { TroopKind.Guardian, TroopKind.Sapper, TroopKind.Vanguard, TroopKind.Ranger };
            string[] verbs = { "① 铁卫先上", "② 破城手开墙", "③ 先锋推进", "④ 游侠跟进" };
            string[] tips = { "按3选铁卫，先投2—3名\n让防御塔瞄准重甲单位。", "按4选破城手，紧跟铁卫\n从同一侧打开城墙缺口。", "按1选先锋，沿缺口投放\n清理基地内的建筑。", "按2选游侠，放在后方\n利用射程进行远程输出。" };
            float cw = (width - 68) / 4;
            for (int i = 0; i < 4; i++)
            {
                float cx = x + 25 + i * (cw + 6);
                Panel(g, new RectangleF(cx, y + 123, cw, 256), Color.FromArgb(36, 54, 44), Color.FromArgb(76, 96, 71), 9);
                PaintTroopPortrait(g, sequence[i], new RectangleF(cx + cw / 2 - 43, y + 138, 86, 107));
                TextAt(g, verbs[i], cx + 15, y + 259, 15, Gold, true);
                TextBox(g, tips[i], new RectangleF(cx + 15, y + 300, cw - 30, 70), 12, Cream);
            }
            TextBox(g, "点击战场任意地块，单位会自动吸附到最近的绿色战线；按住可连续投放。首次投兵后才开始180秒计时。铁卫受伤时按Q，在友军附近释放治疗。", new RectangleF(x + 30, y + 404, width - 60, 58), 14, Cream);
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
