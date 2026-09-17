using Hearthhold.Core;
using UnityEngine;

namespace Hearthhold.UnityClient
{
    public sealed partial class GameBootstrap
    {
        private bool showArmyGuide, showBrief, briefSeen, showCampaign, showTraining, showResearch, showBuildingDetails;
        private int demolishId = -1;
        private TroopKind guideTroop = TroopKind.Vanguard;
        private GameObject detailStage, detailModel;
        private Camera detailCamera;
        private RenderTexture detailTexture;
        private string detailKey;
        private float detailActionTimer;
        private bool ExtraModal { get { return showArmyGuide || showBrief || showCampaign || showTraining || showResearch || showBuildingDetails || demolishId >= 0; } }
        private void CloseExtraModals() { showArmyGuide = false; showBrief = false; showCampaign = false; showTraining = false; showResearch = false; showBuildingDetails = false; demolishId = -1; }
        private void SetDetailLayer(Transform node)
        {
            node.gameObject.layer = 30;
            foreach (Transform child in node) SetDetailLayer(child);
        }
        private void EnsureDetailPreview(bool building, int kind, int level)
        {
            string key = (building ? "building:" : "troop:") + kind + ":" + level;
            if (detailKey == key && detailModel != null) return;
            if (detailStage == null)
            {
                detailStage = new GameObject("Detail preview stage");
                detailStage.transform.position = new Vector3(1000, 0, 1000);
                detailTexture = new RenderTexture(512, 512, 16) { name = "Animated detail preview" };
                GameObject cameraObject = new GameObject("Detail preview camera");
                detailCamera = cameraObject.AddComponent<Camera>();
                detailCamera.targetTexture = detailTexture;
                detailCamera.cullingMask = 1 << 30;
                detailCamera.clearFlags = CameraClearFlags.SolidColor;
                detailCamera.backgroundColor = new Color(0.13f, 0.20f, 0.19f);
                detailCamera.orthographic = true;
            }
            if (detailModel != null) Destroy(detailModel);
            detailModel = building ? modelViews.BuildingPreview((BuildingKind)kind, level, detailStage.transform) : modelViews.TroopPreview((TroopKind)kind, level, detailStage.transform);
            SetDetailLayer(detailModel.transform);
            Renderer[] renderers = detailModel.GetComponentsInChildren<Renderer>();
            Bounds bounds = new Bounds(detailStage.transform.position, Vector3.one);
            bool started = false;
            foreach (Renderer renderer in renderers)
            {
                if (renderer.gameObject.name == "Ground contact shadow") continue;
                if (!started) { bounds = renderer.bounds; started = true; } else bounds.Encapsulate(renderer.bounds);
            }
            detailModel.transform.position -= bounds.center - detailStage.transform.position;
            if (!building)
                detailModel.transform.rotation = Quaternion.Euler(0, 140f, 0);
            float span = Mathf.Max(bounds.size.x, bounds.size.z);
            detailCamera.orthographicSize = building ? Mathf.Max(1.4f, bounds.size.y * 0.85f, span * 0.86f) : Mathf.Max(1.1f, bounds.size.y * 0.65f, span * 0.72f);
            detailCamera.transform.position = detailStage.transform.position + new Vector3(8, 6, -9);
            detailCamera.transform.LookAt(detailStage.transform.position);
            detailKey = key;
            detailActionTimer = 1.8f;
            if (smokeArmyDetail) smokeDetailStartedAt = Time.realtimeSinceStartup;
            Debug.Log("HEARTHHOLD_DETAIL_PREVIEW_READY: " + key);
        }
        private void UpdateDetailPreview()
        {
            if (detailCamera != null) detailCamera.enabled = showArmyGuide || showBuildingDetails;
            if (detailModel != null && (showArmyGuide || showBuildingDetails))
            {
                detailModel.transform.Rotate(0, 16f * Time.unscaledDeltaTime, 0, Space.World);
                ModelActionAnimator action = detailModel.GetComponent<ModelActionAnimator>();
                ImportedClipAnimator imported = detailModel.GetComponent<ImportedClipAnimator>();
                if (action != null && (detailModel.GetComponent<ArticulatedModelAnimator>() != null || imported != null))
                {
                    detailActionTimer += Time.unscaledDeltaTime;
                    if (detailActionTimer >= 2f && (imported == null || !imported.IsPlayingAction))
                    {
                        detailActionTimer = 0;
                        action.Attack(detailModel.transform.position + detailModel.transform.forward * 3f);
                    }
                }
            }
        }
        private void DisposeDetailPreview()
        {
            if (detailModel != null) Destroy(detailModel);
            if (detailStage != null) Destroy(detailStage);
            if (detailCamera != null) Destroy(detailCamera.gameObject);
            if (detailTexture != null) { detailTexture.Release(); Destroy(detailTexture); }
        }
        private void AskDemolish()
        {
            Building b = session.Find(selected);
            if (session.Battle != null || b == null) return;
            if (b.Kind == BuildingKind.Keep) { session.Notice = "议事堡不可拆除。"; return; }
            demolishId = b.Id; moving = -1; buildKind = null;
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
                GUI.Label(new Rect(x + 16, 235, 218, 212), "等级 " + session.Battle.HealLevel + " · 按Q后点击友军附近。\n恢复半径5格内存活友军约" + (67 + (session.Battle.HealLevel - 1) * 10) + "%最大生命。每场2次。\n\n等铁卫受伤再治疗；治疗不能复活阵亡部队。", label);
                return;
            }
            TroopSpec s = Rules.Spec(troop);
            GUI.Label(new Rect(x + 16, 181, 218, 35), s.Name, heading);
            GUI.Label(new Rect(x + 16, 223, 218, 28), s.Role, label);
            int troopLevel = session.Battle.TroopLevels[(int)troop];
            GUI.Label(new Rect(x + 16, 262, 218, 48), "等级 " + troopLevel + " · 生命 " + Rules.TroopHealth(troop, troopLevel) + " · 伤害 " + Rules.TroopDamage(troop, troopLevel) + "\n射程 " + (s.Range / 1000f).ToString("0.##") + "格", small);
            GUI.Label(new Rect(x + 16, 322, 218, 75), s.Tactics, small);
            GUI.Label(new Rect(x + 16, 405, 218, 59), s.Weakness, small);
        }
        private void DrawExtraModals()
        {
            if (!ExtraModal) return;
            GUI.enabled = true;
            GUI.color = new Color(0.025f, 0.055f, 0.05f, 0.94f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); GUI.color = Color.white;
            float w = Mathf.Min(Screen.width - 60, 830), modalHeight = showTraining ? 640 : showResearch ? 615 : 535;
            float x = (Screen.width - w) / 2, y = Mathf.Max(18, (Screen.height - modalHeight) / 2);
            Box(new Rect(x, y, w, modalHeight));
            if (demolishId >= 0)
            {
                Building b = session.Find(demolishId);
                if (b == null) { demolishId = -1; return; }
                int gold, crystal; session.DemolitionRefund(b, out gold, out crystal);
                GUI.Label(new Rect(x + 25, y + 25, w - 50, 40), "确认拆除 " + b.Spec.Name + "？", heading);
                GUI.Label(new Rect(x + 25, y + 100, w - 50, 235), "等级 " + b.Level + " · 位置 " + b.X + ", " + b.Z + "\n\n返还 " + gold + " 金币、" + crystal + " 晶露。\n返还建设与历次升级投入的50%，受仓储上限限制。\n已产生的收益会先收取。拆除后释放名额，不能撤销。" + (b.Kind == BuildingKind.Barracks && session.Village.Count(b.Kind) == 1 ? "\n\n拆除最后一座兵营后，需重建才能出征。" : ""), label);
                if (GUI.Button(new Rect(x + 25, y + 449, (w - 65) / 2, 48), "保留建筑")) demolishId = -1;
                if (GUI.Button(new Rect(x + 40 + (w - 65) / 2, y + 449, (w - 65) / 2, 48), "确认拆除"))
                { int id = demolishId; demolishId = -1; if (session.Demolish(id)) { selected = -1; RebuildBuildings(); Save(); } }
            }
            else if (showCampaign)
            {
                GUI.Label(new Rect(x + 25, y + 22, w - 50, 40), "战役地图 · " + session.Village.TotalStars + " / 30 星", heading);
                GUI.Label(new Rect(x + 25, y + 62, (w - 65) / 2, 26), "逐关获得至少1星即可解锁下一站", small);
                GUI.Label(new Rect(x + 40 + (w - 65) / 2, y + 62, (w - 65) / 2, 26), "成就奖励需要手动领取", small);
                float column = (w - 65) / 2;
                for (int i = 0; i < Missions.Count; i++)
                {
                    bool unlocked = session.Village.IsMissionUnlocked(i);
                    string result = unlocked ? session.Village.CampaignStars[i] + "星 · 最佳" + session.Village.CampaignBest[i] + "%" : "未解锁";
                    bool enabled = GUI.enabled; GUI.enabled = enabled && unlocked;
                    if (GUI.Button(new Rect(x + 25, y + 92 + i * 35, column, 30), (i + 1) + "  " + Missions.Names[i] + "    " + result))
                    { session.MissionIndex = i; showCampaign = false; session.Notice = Missions.Descriptions[i]; }
                    GUI.enabled = enabled;
                }
                for (int i = 0; i < Achievements.Specs.Length; i++)
                {
                    AchievementSpec achievement = Achievements.Specs[i];
                    int progress = Achievements.Progress(session.Village, achievement);
                    bool claimed = session.Village.HasClaimed(achievement.Id), ready = progress >= achievement.Target;
                    string state = claimed ? "已领取" : ready ? "点击领取" : progress + " / " + achievement.Target;
                    string reward = achievement.GoldReward + "金 / " + achievement.CrystalReward + "晶";
                    bool enabled = GUI.enabled; GUI.enabled = enabled && !claimed && ready;
                    if (GUI.Button(new Rect(x + 40 + column, y + 92 + i * 70, column, 62), achievement.Name + " · " + state + "\n" + achievement.Description + " · " + reward))
                    { if (session.ClaimAchievement(achievement.Id)) Save(); }
                    GUI.enabled = enabled;
                }
                if (GUI.Button(new Rect(x + 25, y + 462, w - 50, 44), "返回聚落")) showCampaign = false;
            }
            else if (showBrief)
            {
                GUI.Label(new Rect(x + 25, y + 25, w - 50, 40), "第一次远征，照这个顺序来", heading);
                GUI.Label(new Rect(x + 25, y + 88, w - 50, 345), "士兵落地后自动行动，你负责位置和时机。\n\n① 初期先选先锋，沿西侧缺口成组投放。\n② 游侠紧随其后，利用4.5格射程隔墙输出。\n③ 训练营升到2级后可解锁铁卫，先投铁卫吸引火力。\n④ 训练营升到3级后可解锁破城手，从同侧打开城墙。\n\n点击战场任意地块，单位会自动从最近的绿色战线入场；首次投兵后才开始180秒计时。\n按住左键可连续投兵。部队受伤时按Q选择治疗，点击友军附近。\n\n摧毁议事堡、50%破坏、100%破坏各得一星。", label);
                if (GUI.Button(new Rect(x + 25, y + 462, w - 50, 44), "知道了，开始侦察")) { showBrief = false; briefSeen = true; }
            }
            else if (showTraining)
            {
                int ready = session.Village.ArmyHousing, queued = session.Village.QueuedHousing;
                GUI.Label(new Rect(x + 25, y + 21, w - 50, 38), "远征编队 · " + ready + " 已就绪 / " + queued + " 训练中 / " + session.Village.ArmyCapacity + " 营位", heading);
                string queueState = session.Village.TrainingQueue.Count == 0 ? "训练队列为空" : "下一个 " + Rules.Troops[session.Village.TrainingQueue[0]].Name + " · 约 " + session.TrainingSecondsLeft(System.DateTime.UtcNow) + " 秒";
                GUI.Label(new Rect(x + 25, y + 60, w - 50, 28), queueState + "；未投放士兵会返回营地。", small);
                float cardWidth = (w - 65) / 4;
                for (int i = 0; i < Rules.Troops.Length; i++)
                {
                    TroopKind kind = (TroopKind)i; TroopSpec spec = Rules.Troops[i]; int column = i % 4, row = i / 4; float cx = x + 25 + column * (cardWidth + 5), cy = y + 97 + row * 128;
                    GUI.Box(new Rect(cx, cy, cardWidth, 120), "");
                    GUI.Label(new Rect(cx + 10, cy + 8, cardWidth - 20, 25), spec.Name + " · " + spec.Role, small);
                    int level = session.Village.TroopLevels[i];
                    GUI.Label(new Rect(cx + 10, cy + 34, cardWidth - 20, 47), (session.Village.IsTroopUnlocked(kind) ? "已解锁" : "训练营" + Rules.UnlockCampLevel(kind) + "级") + " · 科技" + level + "\n就绪" + session.Village.ArmyCounts[i] + " / 队列" + session.Village.QueuedCount(kind) + " · 占" + spec.Housing, small);
                    if (GUI.Button(new Rect(cx + 8, cy + 82, (cardWidth - 21) / 2, 30), "−")) { if (session.DismissTroop(kind)) Save(); }
                    if (GUI.Button(new Rect(cx + 13 + (cardWidth - 21) / 2, cy + 82, (cardWidth - 21) / 2, 30), "+")) { if (session.QueueTroop(kind, System.DateTime.UtcNow)) Save(); }
                }
                GUI.Label(new Rect(x + 25, y + 361, w - 50, 26), "预设只补缺；切换配比先遣散多余士兵。剩余 " + (session.Village.ArmyCapacity - ready - queued) + " 营位。", small);
                float presetWidth = (w - 55 - (Rules.FormationNames.Length - 1) * 8) / Rules.FormationNames.Length;
                for (int i = 0; i < Rules.FormationNames.Length; i++)
                    if (GUI.Button(new Rect(x + 25 + i * (presetWidth + 8), y + 393, presetWidth, 43), Rules.FormationNames[i])) { if (session.QueueFormation(i, System.DateTime.UtcNow)) Save(); }
                if (GUI.Button(new Rect(x + 25, y + 466, (w - 60) / 2, 43), "取消队尾训练并退款")) { if (session.CancelLastTraining(System.DateTime.UtcNow)) Save(); }
                if (GUI.Button(new Rect(x + 35 + (w - 60) / 2, y + 466, (w - 60) / 2, 43), "返回聚落")) showTraining = false;
                GUI.Label(new Rect(x + 25, y + 532, w - 50, 28), session.Notice, label);
                GUI.Label(new Rect(x + 25, y + 573, w - 50, 46), "训练按真实时间推进，离线时间也会结算。\n兵营管营位，训练营管解锁与队列，实验室管等级。", small);
            }
            else if (showResearch)
            {
                GUI.Label(new Rect(x + 25, y + 23, w - 50, 42), "实验室研究 · 实验室 " + session.Village.LaboratoryLevel + " 级", heading);
                GUI.Label(new Rect(x + 25, y + 66, w - 50, 58), "每级兵种生命和伤害约提升10%；疗愈之雨每级多恢复10%最大生命。\n研究等级不得超过实验室等级，兵种还需由训练营解锁。", small);
                for (int i = 0; i < Rules.Troops.Length; i++)
                {
                    TroopKind kind = (TroopKind)i; int level = session.Village.TroopLevels[i]; TroopSpec s = Rules.Troops[i];
                    int column = i % 2, row = i / 2; float columnWidth = (w - 60) / 2, rx = x + 25 + column * (columnWidth + 10), ry = y + 137 + row * 78;
                    GUI.Box(new Rect(rx, ry, columnWidth, 68), "");
                    GUI.Label(new Rect(rx + 12, ry + 7, columnWidth - 95, 55), s.Name + " " + level + "级 · " + Rules.TroopHealth(kind, level) + "生命 / " + Rules.TroopDamage(kind, level) + "伤害\n" + (session.Village.IsTroopUnlocked(kind) ? "已解锁" : "训练营" + Rules.UnlockCampLevel(kind) + "级"), small);
                    if (GUI.Button(new Rect(rx + columnWidth - 80, ry + 17, 68, 34), "研究")) { if (session.ResearchTroop(kind)) Save(); }
                }
                int healLevel = session.Village.HealLevel;
                GUI.Label(new Rect(x + 38, y + 460, w - 245, 60), "疗愈之雨 " + healLevel + "级 · 每战2次，半径5格\n下级 " + Rules.ResearchGold(healLevel) + "金 / " + Rules.ResearchCrystal(healLevel) + "晶", small);
                if (GUI.Button(new Rect(x + w - 196, y + 465, 158, 37), "研究法术")) { if (session.ResearchHeal()) Save(); }
                GUI.Label(new Rect(x + 25, y + 530, w - 50, 32), session.Notice, small);
                if (GUI.Button(new Rect(x + 25, y + 559, w - 50, 40), "返回聚落")) showResearch = false;
            }
            else if (showBuildingDetails)
            {
                Building b = session.Find(selected);
                if (b == null) { showBuildingDetails = false; return; }
                EnsureDetailPreview(true, (int)b.Kind, b.Level);
                float left = (w - 75) * 0.46f, rightX = x + 45 + left, rightW = w - 70 - left;
                GUI.Label(new Rect(x + 25, y + 22, w - 50, 40), b.Spec.Name + " · " + b.Level + "级", heading);
                GUI.Box(new Rect(x + 25, y + 75, left, 355), "");
                GUI.DrawTexture(new Rect(x + 30, y + 82, left - 10, 302), detailTexture, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(x + 36, y + 393, left - 22, 28), "实时 3D 动态展示 · 可见当前等级外观", small);
                GUI.Label(new Rect(rightX, y + 82, rightW, 34), "建筑数据", heading);
                GUI.Label(new Rect(rightX, y + 130, rightW, 125), Rules.BuildingData(b) + "\n建设数量 " + session.Village.Count(b.Kind) + " / " + session.Village.Limit(b.Kind), label);
                GUI.Label(new Rect(rightX, y + 263, rightW, 125), "简介\n" + b.Spec.Description, label);
                GUI.Label(new Rect(rightX, y + 396, rightW, 34), b.Level >= 3 ? "已达最高等级" : "下一等级会解锁更华丽的外观饰件", small);
                if (GUI.Button(new Rect(x + 25, y + 462, w - 50, 44), "返回聚落")) showBuildingDetails = false;
            }
            else if (showArmyGuide)
            {
                GUI.Label(new Rect(x + 25, y + 25, w - 50, 40), "远征兵种图鉴", heading);
                for (int i = 0; i < Rules.Troops.Length; i++)
                {
                    int column = i % 4, row = i / 4; float buttonWidth = (w - 50) / 4;
                    if (GUI.Button(new Rect(x + 25 + column * buttonWidth, y + 82 + row * 45, buttonWidth - 8, 40), Rules.Troops[i].Name)) guideTroop = (TroopKind)i;
                }
                TroopSpec s = Rules.Spec(guideTroop);
                int level = session.Village.TroopLevels[(int)guideTroop];
                EnsureDetailPreview(false, (int)guideTroop, level);
                float left = (w - 75) * 0.46f, rightX = x + 45 + left, rightW = w - 70 - left;
                GUI.Box(new Rect(x + 25, y + 183, left, 254), "");
                GUI.DrawTexture(new Rect(x + 30, y + 187, left - 10, 215), detailTexture, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(x + 35, y + 407, left - 20, 25), "实时 3D 动态展示 · " + level + "级外观", small);
                GUI.Label(new Rect(rightX, y + 181, rightW, 36), s.Name + " · " + s.Role, heading);
                string special = s.HealPower > 0 ? "每次治疗 " + s.HealPower + " · 范围治疗" : s.SummonCooldown > 0 ? "每 " + (s.SummonCooldown / (float)Rules.TicksPerSecond).ToString("0.#") + "秒召唤幽影 · 最多3个" : s.SplashRadius > 0 ? "溅射半径 " + (s.SplashRadius / 1000f).ToString("0.#") + "格" : s.PreferWalls ? "对城墙造成10倍伤害" : s.Flying ? "飞行单位 · 不受城墙阻挡" : "";
                GUI.Label(new Rect(rightX, y + 221, rightW, 87), "科技 " + level + "级 · " + (session.Village.IsTroopUnlocked(guideTroop) ? "已解锁" : "训练营 " + Rules.UnlockCampLevel(guideTroop) + "级解锁") + "\n生命 " + Rules.TroopHealth(guideTroop, level) + " · 伤害 " + Rules.TroopDamage(guideTroop, level) + " · 射程 " + (s.Range / 1000f).ToString("0.##") + "格\n营位 " + s.Housing + " · 攻击间隔 " + (s.Cooldown + 1) / (float)Rules.TicksPerSecond + "秒" + (special.Length == 0 ? "" : "\n" + special), small);
                GUI.Label(new Rect(rightX, y + 316, rightW, 123), s.Description + "\n\n打法：" + s.Tactics + "\n\n弱点：" + s.Weakness, small);
                if (GUI.Button(new Rect(x + 25, y + 462, w - 50, 44), "返回游戏")) showArmyGuide = false;
            }
        }
    }
}
