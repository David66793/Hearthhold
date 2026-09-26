using Hearthhold.Core;
using UnityEngine;

namespace Hearthhold.UnityClient
{
    public sealed partial class GameBootstrap
    {
        private bool showArmyGuide, showBrief, briefSeen, showCampaign, showTraining, showResearch, showProgression, showHeroes, showBuildCatalog, showBuildingDetails;
        private int demolishId = -1;
        private TroopKind guideTroop = TroopKind.Vanguard;
        private bool armyGuideFromTraining;
        private int researchSelection = -1;
        private ModelPreviewService previewService;
        private ModelPreviewService Previews { get { return previewService ?? (previewService = new ModelPreviewService(modelViews)); } }
        private RenderTexture detailTexture { get { return Previews.DetailTexture; } }
        private RenderTexture[] rosterTextures { get { return Previews.RosterTextures; } }
        private int rosterSelection;
        private bool heroEquipmentTab;
        private int selectedEquipmentKind, selectedEquipmentSlot;
        private bool ExtraModal { get { return showArmyGuide || showBrief || showCampaign || showTraining || showResearch || showProgression || showHeroes || showBuildCatalog || showBuildingDetails || demolishId >= 0; } }
        private void CloseExtraModals() { showArmyGuide = false; armyGuideFromTraining = false; showBrief = false; showCampaign = false; showTraining = false; showResearch = false; researchSelection = -1; showProgression = false; showHeroes = false; showBuildCatalog = false; showBuildingDetails = false; demolishId = -1; }
        private void EnsureRosterPreviews() { Previews.EnsureRoster(session.Village); }
        private void EnsureDetailPreview(bool building, int kind, int level)
        {
            if (Previews.EnsureDetail(building, kind, level) && smokeArmyDetail)
                smokeDetailStartedAt = Time.realtimeSinceStartup;
        }
        private void UpdateDetailPreview()
        {
            if (previewService == null) return;
            bool researchPreview = showResearch && researchSelection >= 0 && researchSelection < Rules.Troops.Length;
            previewService.Update(session.Village, showHeroes, showArmyGuide || showBuildingDetails || researchPreview);
        }
        private void DisposeDetailPreview()
        {
            if (previewService != null) previewService.Dispose();
            previewService = null;
        }
        private void AskDemolish()
        {
            Building b = session.Find(selected);
            if (session.Battle != null || b == null) return;
            if (b.Kind == BuildingKind.Keep) { session.Notice = "议事堡不可拆除。"; return; }
            demolishId = b.Id; moving = -1; movingWallRow = false; buildKind = null;
        }
        private void ToggleWallRowSelection(Building wall)
        {
            selectedWallIds.Clear();
            if (wall == null || wall.Kind != BuildingKind.Wall) return;
            if (session.WallRow(wall.Id).Count <= 1) { selectedWallIds.Add(wall.Id); session.Notice = "这段城墙没有同轴相连的墙段。"; return; }
            foreach (Building segment in session.WallRow(wall.Id)) selectedWallIds.Add(segment.Id);
            session.Notice = "已选择连续城墙 ×" + selectedWallIds.Count + "；按 U 或右侧按钮可整排升级。";
        }
        private string WallRowSummary()
        {
            int min = int.MaxValue, max = 0;
            foreach (int id in selectedWallIds) { Building wall = session.Find(id); if (wall != null) { min = Mathf.Min(min, wall.Level); max = Mathf.Max(max, wall.Level); } }
            return "墙段 " + selectedWallIds.Count + " · 等级 " + (min == max ? min.ToString() : min + "—" + max) + "\n范围只包含无缺口的直线墙段";
        }
        private static string CatalogDescription(BuildingKind kind)
        {
            if (kind == BuildingKind.TrainingCamp) return "即时训练并解锁兵种；升级后开放重甲、空军与专业兵种。";
            if (kind == BuildingKind.Laboratory) return "研究兵种与四类法术；科技等级不超过实验室等级。";
            if (kind == BuildingKind.HeroHall) return "解锁英雄、装备工坊与双槽构筑；英雄独立出征。";
            if (kind == BuildingKind.PetLodge) return "解锁并绑定战宠；英雄阵亡后，战宠继续独立作战。";
            return Rules.Spec(kind).Description;
        }
        private void DrawTroopCard()
        {
            if (session.Battle == null || session.Battle.Finished) return;
            float x = Screen.width - 267;
            Box(new Rect(x, 164, 245, 307));
            if (focusOrder)
            {
                GUI.Label(new Rect(x + 16, 182, 218, 36), "集火令", heading);
                GUI.Label(new Rect(x + 16, 235, 218, 212), "按 E 选择后点击敌方非城墙建筑。\n在场部队集中攻击目标7秒；每场2次。\n\n集火令不造成伤害，仍要先打开进攻路线。", label);
                return;
            }
            if (heal)
            {
                GUI.Label(new Rect(x + 16, 182, 218, 36), "疗愈之雨", heading);
                GUI.Label(new Rect(x + 16, 235, 218, 212), "等级 " + session.Battle.HealLevel + " · 按Q后点击友军附近。\n半径5格 · " + Rules.SpellEffect(SpellKind.Heal, session.Battle.HealLevel) + "。每场2次。\n\n区域内后来进入的友军也会获得治疗；不能复活阵亡部队。", label);
                return;
            }
            if (fury || freeze || breach)
            {
                SpellKind kind = fury ? SpellKind.Fury : freeze ? SpellKind.Freeze : SpellKind.Breach;
                int level = session.Battle.SpellLevels[(int)kind];
                GUI.Label(new Rect(x + 16, 182, 218, 36), Rules.SpellNames[(int)kind], heading);
                string usage = fury ? "点击友军主力团，使范围内单位加速攻击。适合突破高压火力区。"
                    : freeze ? "点击敌方防御群，使范围内防御暂停攻击。适合保护破墙或收尾。"
                    : "点击城墙密集处，立即摧毁范围内墙段。等级越高，覆盖半径越大。";
                GUI.Label(new Rect(x + 16, 235, 218, 212), "等级 " + level + " · " + Rules.SpellEffect(kind, level) + "\n每场1次。\n\n" + usage, label);
                return;
            }
            TroopSpec s = Rules.Spec(troop);
            GUI.Label(new Rect(x + 16, 181, 218, 35), s.Name, heading);
            GUI.Label(new Rect(x + 16, 223, 218, 28), s.Role, label);
            int troopLevel = session.Battle.TroopLevels[(int)troop];
            GUI.Label(new Rect(x + 16, 262, 218, 48), "等级 " + troopLevel + " · 生命 " + Rules.TroopHealth(troop, troopLevel) + " · 伤害 " + Rules.TroopDamage(troop, troopLevel) + "\n射程 " + (s.Range / 1000f).ToString("0.##") + "格", small);
            GUI.Label(new Rect(x + 16, 310, 218, 24), session.Battle.TargetSummary(troop), small);
            GUI.Label(new Rect(x + 16, 339, 218, 68), s.Tactics, small);
            GUI.Label(new Rect(x + 16, 411, 218, 55), s.Weakness, small);
        }
    }
}
