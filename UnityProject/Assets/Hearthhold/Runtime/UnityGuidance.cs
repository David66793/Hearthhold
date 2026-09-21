using Hearthhold.Core;
using UnityEngine;

namespace Hearthhold.UnityClient
{
    public sealed partial class GameBootstrap
    {
        private bool showArmyGuide, showBrief, briefSeen, showCampaign, showTraining, showResearch, showProgression, showHeroes, showBuildCatalog, showBuildingDetails;
        private int demolishId = -1;
        private TroopKind guideTroop = TroopKind.Vanguard;
        private GameObject detailStage, detailModel;
        private Camera detailCamera;
        private RenderTexture detailTexture;
        private string detailKey;
        private float detailActionTimer;
        private int rosterSelection;
        private GameObject[] rosterStages, rosterModels;
        private Camera[] rosterCameras;
        private RenderTexture[] rosterTextures;
        private int[] rosterLevels;
        private float rosterActionTimer;
        private bool rosterReadyLogged;
        private bool ExtraModal { get { return showArmyGuide || showBrief || showCampaign || showTraining || showResearch || showProgression || showHeroes || showBuildCatalog || showBuildingDetails || demolishId >= 0; } }
        private void CloseExtraModals() { showArmyGuide = false; showBrief = false; showCampaign = false; showTraining = false; showResearch = false; showProgression = false; showHeroes = false; showBuildCatalog = false; showBuildingDetails = false; demolishId = -1; }
        private void SetDetailLayer(Transform node)
        {
            SetPreviewLayer(node, 30);
        }
        private void SetPreviewLayer(Transform node, int layer)
        {
            node.gameObject.layer = layer;
            foreach (Transform child in node) SetPreviewLayer(child, layer);
        }
        private void EnsureRosterPreviews()
        {
            int count = 1 + Rules.Pets.Length;
            if (rosterStages == null)
            {
                rosterStages = new GameObject[count]; rosterModels = new GameObject[count]; rosterCameras = new Camera[count];
                rosterTextures = new RenderTexture[count]; rosterLevels = new int[count];
                for (int i = 0; i < count; i++) rosterLevels[i] = -1;
            }
            for (int i = 0; i < count; i++)
            {
                int level = i == 0 ? session.Village.HeroLevels[0] : session.Village.PetLevels[i - 1];
                int visualLevel = Mathf.Max(1, level);
                if (rosterStages[i] == null)
                {
                    int layer = 27 + i;
                    rosterStages[i] = new GameObject("Roster preview stage " + i);
                    rosterStages[i].transform.position = new Vector3(1100 + i * 40, 0, 1100);
                    rosterTextures[i] = new RenderTexture(512, 512, 16) { name = "Roster animated preview " + i };
                    GameObject cameraObject = new GameObject("Roster preview camera " + i);
                    rosterCameras[i] = cameraObject.AddComponent<Camera>(); rosterCameras[i].targetTexture = rosterTextures[i];
                    rosterCameras[i].cullingMask = 1 << layer; rosterCameras[i].clearFlags = CameraClearFlags.SolidColor;
                    rosterCameras[i].backgroundColor = i == 0 ? new Color(0.18f, 0.12f, 0.08f) : new Color(0.08f, 0.19f, 0.17f);
                    rosterCameras[i].orthographic = true;
                }
                if (rosterModels[i] != null && rosterLevels[i] == visualLevel) continue;
                if (rosterModels[i] != null) Destroy(rosterModels[i]);
                rosterModels[i] = i == 0 ? modelViews.HeroPreview(HeroKind.EmberWarden, visualLevel, rosterStages[i].transform)
                    : modelViews.PetPreview((PetKind)(i - 1), visualLevel, rosterStages[i].transform);
                SetPreviewLayer(rosterModels[i].transform, 27 + i);
                Renderer[] renderers = rosterModels[i].GetComponentsInChildren<Renderer>(); bool started = false;
                Bounds bounds = new Bounds(rosterStages[i].transform.position, Vector3.one);
                foreach (Renderer renderer in renderers)
                {
                    if (renderer.gameObject.name == "Ground contact shadow") continue;
                    if (!started) { bounds = renderer.bounds; started = true; } else bounds.Encapsulate(renderer.bounds);
                }
                rosterModels[i].transform.position -= bounds.center - rosterStages[i].transform.position;
                rosterModels[i].transform.rotation = Quaternion.Euler(0, 145, 0);
                float span = Mathf.Max(bounds.size.x, bounds.size.z);
                rosterCameras[i].orthographicSize = Mathf.Max(1.05f, bounds.size.y * 0.68f, span * 0.72f);
                rosterCameras[i].transform.position = rosterStages[i].transform.position + new Vector3(5, 3.6f, -6);
                rosterCameras[i].transform.LookAt(rosterStages[i].transform.position);
                rosterLevels[i] = visualLevel;
            }
            if (!rosterReadyLogged)
            {
                rosterReadyLogged = true;
                Debug.Log("HEARTHHOLD_ROSTER_PREVIEW_READY: hero=" + Rules.Heroes.Length + " pets=" + Rules.Pets.Length);
            }
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
            if (showHeroes)
            {
                EnsureRosterPreviews(); rosterActionTimer += Time.unscaledDeltaTime;
                for (int i = 0; i < rosterModels.Length; i++)
                {
                    if (rosterCameras[i] != null) rosterCameras[i].enabled = true;
                    if (rosterModels[i] == null) continue;
                    rosterModels[i].transform.Rotate(0, (i == 0 ? 18f : 24f) * Time.unscaledDeltaTime, 0, Space.World);
                    float pulse = 1 + Mathf.Sin(Time.unscaledTime * 2.2f + i) * 0.012f;
                    rosterModels[i].transform.localScale = Vector3.one * pulse;
                }
                if (rosterActionTimer >= 2.4f)
                {
                    rosterActionTimer = 0;
                    ModelActionAnimator action = rosterModels[0] == null ? null : rosterModels[0].GetComponent<ModelActionAnimator>();
                    if (action != null) action.Attack(rosterModels[0].transform.position + rosterModels[0].transform.forward * 3f);
                }
            }
            else if (rosterCameras != null) foreach (Camera camera in rosterCameras) if (camera != null) camera.enabled = false;
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
            if (rosterModels != null) foreach (GameObject model in rosterModels) if (model != null) Destroy(model);
            if (rosterStages != null) foreach (GameObject stage in rosterStages) if (stage != null) Destroy(stage);
            if (rosterCameras != null) foreach (Camera camera in rosterCameras) if (camera != null) Destroy(camera.gameObject);
            if (rosterTextures != null) foreach (RenderTexture texture in rosterTextures) if (texture != null) { texture.Release(); Destroy(texture); }
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
            if (kind == BuildingKind.HeroHall) return "解锁与升级英雄；英雄独立出征，不占兵营营位。";
            if (kind == BuildingKind.PetLodge) return "解锁并绑定战宠；英雄阵亡后，战宠继续独立作战。";
            return Rules.Spec(kind).Description;
        }
        private void DrawRosterCard(Rect rect, int entry, string name, string role, string state)
        {
            GUI.color = rosterSelection == entry ? new Color(0.16f, 0.36f, 0.29f, 1) : new Color(0.055f, 0.12f, 0.10f, 1);
            GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = Color.white;
            if (rosterTextures != null && entry < rosterTextures.Length && rosterTextures[entry] != null)
                GUI.DrawTexture(new Rect(rect.x + 7, rect.y + 7, 91, rect.height - 14), rosterTextures[entry], ScaleMode.ScaleToFit);
            GUI.Label(new Rect(rect.x + 106, rect.y + 12, rect.width - 114, 27), name, new GUIStyle(small) { fontSize = 18, fontStyle = FontStyle.Bold });
            GUI.Label(new Rect(rect.x + 106, rect.y + 42, rect.width - 114, 40), role, small);
            GUI.Label(new Rect(rect.x + 106, rect.y + rect.height - 45, rect.width - 114, 38), state, small);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) rosterSelection = entry;
        }
        private void DrawHeroManagement(float x, float y, float w)
        {
            session.Village.EnsureHeroes(); EnsureRosterPreviews();
            GUI.Label(new Rect(x + 25, y + 18, w - 50, 38), "英雄殿堂", heading);
            GUI.Label(new Rect(x + 25, y + 55, w - 50, 27), "英雄殿堂 " + session.Village.HeroHallLevel + "级　战宠小屋 " + session.Village.PetLodgeLevel + "级　点击左侧动态名册查看详情", small);
            float listX = x + 25, listY = y + 94, listW = 250;
            int heroLevel = session.Village.HeroLevels[0];
            DrawRosterCard(new Rect(listX, listY, listW, 132), 0, Rules.Heroes[0].Name, Rules.Heroes[0].Role,
                heroLevel > 0 ? heroLevel + "级 · 可出征" : "未解锁 · 需要英雄殿堂");
            for (int i = 0; i < Rules.Pets.Length; i++)
            {
                int level = session.Village.PetLevels[i]; bool assigned = session.Village.HeroPetAssignments[0] == i;
                DrawRosterCard(new Rect(listX, listY + 143 + i * 143, listW, 132), i + 1, Rules.Pets[i].Name, Rules.Pets[i].Role,
                    level > 0 ? level + "级" + (assigned ? " · 已缔结" : " · 待命") : "未解锁 · 战宠小屋");
            }
            int entry = Mathf.Clamp(rosterSelection, 0, Rules.Pets.Length);
            float stageX = x + 295, stageW = 345;
            GUI.color = entry == 0 ? new Color(0.18f, 0.12f, 0.08f, 1) : new Color(0.08f, 0.19f, 0.17f, 1);
            GUI.DrawTexture(new Rect(stageX, listY, stageW, 430), Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.DrawTexture(new Rect(stageX + 10, listY + 8, stageW - 20, 365), rosterTextures[entry], ScaleMode.ScaleToFit);
            GUI.Label(new Rect(stageX + 18, listY + 382, stageW - 36, 34), entry == 0 ? "实时英雄动作预览" : "实时战宠动态预览 · 与英雄协同出征", small);
            float dataX = x + 660, dataW = w - 685;
            GUI.color = new Color(0.055f, 0.12f, 0.10f, 1); GUI.DrawTexture(new Rect(dataX, listY, dataW, 520), Texture2D.whiteTexture); GUI.color = Color.white;
            if (entry == 0)
            {
                HeroKind kind = HeroKind.EmberWarden; HeroSpec hero = Rules.Spec(kind); int shown = Mathf.Max(1, heroLevel);
                GUI.Label(new Rect(dataX + 22, listY + 17, dataW - 44, 34), hero.Name + "　" + (heroLevel > 0 ? heroLevel + "级" : "未解锁"), heading);
                GUI.Label(new Rect(dataX + 22, listY + 60, dataW - 44, 30), hero.Role, label);
                int next = Mathf.Min(4, shown + 1);
                GUI.Label(new Rect(dataX + 22, listY + 104, dataW - 44, 92),
                    "生命　" + Rules.HeroHealth(kind, shown) + (next > shown ? "  →  " + Rules.HeroHealth(kind, next) : "") +
                    "\n伤害　" + Rules.HeroDamage(kind, shown) + (next > shown ? "  →  " + Rules.HeroDamage(kind, next) : "") +
                    "\n射程　" + (hero.Combat.Range / 1000f).ToString("0.0") + "格　攻击间隔 " + ((hero.Combat.Cooldown + 1) / (float)Rules.TicksPerSecond).ToString("0.0") + "秒", label);
                GUI.Label(new Rect(dataX + 22, listY + 207, dataW - 44, 68), hero.Description, small);
                GUI.Label(new Rect(dataX + 22, listY + 289, dataW - 44, 30), "主动技能　" + hero.AbilityName, new GUIStyle(small) { fontSize = 18, fontStyle = FontStyle.Bold });
                GUI.Label(new Rect(dataX + 22, listY + 326, dataW - 44, 64), hero.AbilityDescription, small);
                int pet = session.Village.HeroPetAssignments[0];
                GUI.Label(new Rect(dataX + 22, listY + 402, dataW - 44, 28), pet >= 0 ? "当前战宠　" + Rules.Pets[pet].Name : "当前战宠　未绑定", small);
                bool enabled = GUI.enabled; GUI.enabled = enabled && heroLevel > 0 && heroLevel < session.Village.HeroHallLevel && heroLevel < 4;
                int cost = Mathf.Max(1, heroLevel);
                if (GUI.Button(new Rect(dataX + 20, listY + 455, dataW - 40, 45), "升级至 " + (shown + 1) + "级　" + Rules.HeroUpgradeGold(cost) + "金 / " + Rules.HeroUpgradeCrystal(cost) + "晶"))
                { if (session.UpgradeHero(kind)) Save(); }
                GUI.enabled = enabled;
            }
            else
            {
                PetKind kind = (PetKind)(entry - 1); PetSpec pet = Rules.Spec(kind); int level = session.Village.PetLevels[entry - 1]; int shown = Mathf.Max(1, level);
                bool assigned = session.Village.HeroPetAssignments[0] == entry - 1;
                GUI.Label(new Rect(dataX + 22, listY + 17, dataW - 44, 34), pet.Name + "　" + (level > 0 ? level + "级" : "未解锁"), heading);
                GUI.Label(new Rect(dataX + 22, listY + 60, dataW - 44, 30), pet.Role, label);
                int next = Mathf.Min(4, shown + 1);
                GUI.Label(new Rect(dataX + 22, listY + 104, dataW - 44, 112),
                    "生命　" + Rules.PetHealth(kind, shown) + (next > shown ? "  →  " + Rules.PetHealth(kind, next) : "") +
                    "\n伤害　" + Rules.PetDamage(kind, shown) + (next > shown ? "  →  " + Rules.PetDamage(kind, next) : "") +
                    "\n跟随距离　" + (pet.FollowRange / 1000f).ToString("0.0") + "格\n英雄阵亡后　独立索敌并继续攻击", label);
                GUI.Label(new Rect(dataX + 22, listY + 234, dataW - 44, 86), pet.Description, small);
                GUI.Label(new Rect(dataX + 22, listY + 334, dataW - 44, 44), level > 0 ? (assigned ? "灵契状态　已绑定烬卫" : "灵契状态　尚未绑定") : "解锁条件　建造战宠小屋并达到对应阶段", small);
                bool enabled = GUI.enabled; GUI.enabled = enabled && level > 0 && heroLevel > 0;
                if (GUI.Button(new Rect(dataX + 20, listY + 392, dataW - 40, 42), assigned ? "解除与烬卫的灵契" : "绑定给烬卫"))
                { if (session.AssignPet(HeroKind.EmberWarden, assigned ? (PetKind?)null : kind)) Save(); }
                GUI.enabled = enabled && level < session.Village.PetLodgeLevel && level < 4;
                if (GUI.Button(new Rect(dataX + 20, listY + 447, dataW - 40, 45), "升级至 " + (shown + 1) + "级　" + Rules.PetUpgradeCrystal(shown) + "晶露"))
                { if (session.UpgradePet(kind)) Save(); }
                GUI.enabled = enabled;
            }
            GUI.Label(new Rect(x + 25, y + 635, w - 250, 28), session.Notice, small);
            if (GUI.Button(new Rect(x + w - 195, y + 628, 170, 42), "返回聚落　Esc")) showHeroes = false;
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
        private void DrawExtraModals()
        {
            if (!ExtraModal) return;
            GUI.enabled = true;
            GUI.color = new Color(0.025f, 0.055f, 0.05f, 0.94f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); GUI.color = Color.white;
            float w = Mathf.Min(Screen.width - 60, showHeroes ? 1120 : showBuildCatalog ? 1040 : 830), modalHeight = showTraining ? 640 : showResearch ? 750 : showProgression ? 650 : showHeroes ? 700 : showBuildCatalog ? 760 : 535;
            float x = (Screen.width - w) / 2, y = Mathf.Max(18, (Screen.height - modalHeight) / 2);
            Box(new Rect(x, y, w, modalHeight));
            if (showBuildCatalog)
            {
                GUI.Label(new Rect(x + 25, y + 20, w - 50, 40), "建造目录 · 选择后返回地图放置", heading);
                GUI.Label(new Rect(x + 25, y + 60, w - 50, 28), "建筑规则、价格和数量上限均来自同一套数据；锁定项目会直接显示所需议事堡等级。", label);
                float columnWidth = (w - 65) / 2f;
                for (int i = 1; i < Rules.Buildings.Length; i++)
                {
                    BuildingKind kind = (BuildingKind)i; BuildingSpec spec = Rules.Spec(kind);
                    int item = i - 1, column = item % 2, row = item / 2;
                    float rx = x + 25 + column * (columnWidth + 15), ry = y + 103 + row * 80;
                    GUI.Box(new Rect(rx, ry, columnWidth, 75), "");
                    GUIStyle catalogTitle = new GUIStyle(small) { fontSize = 18, fontStyle = FontStyle.Bold, clipping = TextClipping.Clip };
                    GUI.Label(new Rect(rx + 12, ry + 7, columnWidth - 210, 26), spec.Name, catalogTitle);
                    GUI.Label(new Rect(rx + 12, ry + 34, columnWidth - 24, 39), CatalogDescription(kind), small);
                    int built = session.Village.Count(kind), limit = session.Village.Limit(kind);
                    string state = limit == 0 ? "堡" + Rules.BuildingUnlockKeepLevel(kind) + "解锁" : built >= limit ? "已达上限 " + built + "/" + limit : built + "/" + limit + " · " + spec.Cost + "金";
                    GUI.Label(new Rect(rx + columnWidth - 194, ry + 9, 106, 24), state, small);
                    bool enabled = GUI.enabled; GUI.enabled = enabled && limit > 0 && built < limit && session.Village.Gold >= spec.Cost;
                    if (GUI.Button(new Rect(rx + columnWidth - 78, ry + 17, 66, 34), "放置"))
                    { buildKind = kind; moving = selected = -1; movingWallRow = false; selectedWallIds.Clear(); showBuildCatalog = false; session.Notice = "放置" + spec.Name + " · 点击绿色空地，右键取消。"; }
                    GUI.enabled = enabled;
                }
                GUI.Label(new Rect(x + 25, y + 665, w - 230, 28), "金币 " + session.Village.Gold + " · 建筑升级请点击地图上的现有建筑。", label);
                if (GUI.Button(new Rect(x + w - 185, y + 658, 160, 38), "关闭  Esc")) showBuildCatalog = false;
            }
            else if (demolishId >= 0)
            {
                Building b = session.Find(demolishId);
                if (b == null) { demolishId = -1; return; }
                int gold, crystal; session.DemolitionRefund(b, out gold, out crystal);
                GUI.Label(new Rect(x + 25, y + 25, w - 50, 40), "确认拆除 " + b.Spec.Name + "？", heading);
                GUI.Label(new Rect(x + 25, y + 100, w - 50, 235), "等级 " + b.Level + " · 位置 " + b.X + ", " + b.Z + "\n\n返还 " + gold + " 金币、" + crystal + " 晶露。\n返还建设与历次升级投入的50%，受仓储上限限制。\n已产生的收益会先收取。拆除后释放名额，不能撤销。" + (b.Kind == BuildingKind.Barracks && session.Village.Count(b.Kind) == 1 ? "\n\n拆除最后一座兵营后，需重建才能出征。" : ""), label);
                if (GUI.Button(new Rect(x + 25, y + 449, (w - 65) / 2, 48), "保留建筑")) demolishId = -1;
                if (GUI.Button(new Rect(x + 40 + (w - 65) / 2, y + 449, (w - 65) / 2, 48), "确认拆除"))
                { int id = demolishId; demolishId = -1; if (session.Demolish(id)) { selected = -1; selectedWallIds.Clear(); RebuildBuildings(); Save(); } }
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
                int ready = session.Village.ArmyHousing;
                GUI.Label(new Rect(x + 25, y + 21, w - 50, 38), "远征编队 · " + ready + " / " + session.Village.ArmyCapacity + " 营位", heading);
                GUI.Label(new Rect(x + 25, y + 60, w - 50, 30), "造兵立即完成；未投放士兵会返回营地。", label);
                float cardWidth = (w - 65) / 4;
                for (int i = 0; i < Rules.Troops.Length; i++)
                {
                    TroopKind kind = (TroopKind)i; TroopSpec spec = Rules.Troops[i]; int column = i % 4, row = i / 4; float cx = x + 25 + column * (cardWidth + 5), cy = y + 97 + row * 128;
                    GUI.Box(new Rect(cx, cy, cardWidth, 120), "");
                    GUI.Label(new Rect(cx + 10, cy + 8, cardWidth - 20, 25), spec.Name + " · " + spec.Role, small);
                    int level = session.Village.TroopLevels[i];
                    GUI.Label(new Rect(cx + 10, cy + 34, cardWidth - 20, 47), (session.Village.IsTroopUnlocked(kind) ? "已解锁" : "训练营" + Rules.UnlockCampLevel(kind) + "级") + " · 科技" + level + "\n现有" + session.Village.ArmyCounts[i] + " · 占" + spec.Housing + " · 立即就绪", small);
                    float actionWidth = (cardWidth - 28) / 3;
                    if (GUI.Button(new Rect(cx + 8, cy + 82, actionWidth, 30), "−")) { if (session.DismissTroop(kind)) Save(); }
                    if (GUI.Button(new Rect(cx + 14 + actionWidth, cy + 82, actionWidth, 30), "+1")) { if (session.QueueTroop(kind, System.DateTime.UtcNow)) Save(); }
                    if (GUI.Button(new Rect(cx + 20 + actionWidth * 2, cy + 82, actionWidth, 30), "+5")) { if (session.QueueTroops(kind, 5, System.DateTime.UtcNow)) Save(); }
                }
                GUI.Label(new Rect(x + 25, y + 361, w - 50, 28), "预设立即补齐缺员；切换配比先遣散多余士兵。剩余 " + (session.Village.ArmyCapacity - ready) + " 营位。", label);
                float presetWidth = (w - 55 - (Rules.FormationNames.Length - 1) * 8) / Rules.FormationNames.Length;
                for (int i = 0; i < Rules.FormationNames.Length; i++)
                    if (GUI.Button(new Rect(x + 25 + i * (presetWidth + 8), y + 393, presetWidth, 43), Rules.FormationNames[i])) { if (session.QueueFormation(i, System.DateTime.UtcNow)) Save(); }
                if (GUI.Button(new Rect(x + 25, y + 466, w - 50, 43), "返回聚落")) showTraining = false;
                GUI.Label(new Rect(x + 25, y + 532, w - 50, 28), session.Notice, label);
                GUI.Label(new Rect(x + 25, y + 573, w - 50, 46), "+1 / +5 都立即完成，批量会先检查全部费用与营位。\n兵营管营位，训练营管解锁，实验室管等级。", label);
            }
            else if (showResearch)
            {
                GUI.Label(new Rect(x + 25, y + 23, w - 50, 42), "实验室研究 · 实验室 " + session.Village.LaboratoryLevel + " 级", heading);
                GUI.Label(new Rect(x + 25, y + 66, w - 50, 62), "兵种每级生命和伤害约提升10%；四类法术都有独立等级与效果成长。\n研究等级不得超过实验室等级；法术还受议事堡阶段限制。", label);
                for (int i = 0; i < Rules.Troops.Length; i++)
                {
                    TroopKind kind = (TroopKind)i; int level = session.Village.TroopLevels[i]; TroopSpec s = Rules.Troops[i];
                    int column = i % 2, row = i / 2; float columnWidth = (w - 60) / 2, rx = x + 25 + column * (columnWidth + 10), ry = y + 137 + row * 78;
                    GUI.Box(new Rect(rx, ry, columnWidth, 68), "");
                    GUI.Label(new Rect(rx + 12, ry + 7, columnWidth - 95, 55), s.Name + " " + level + "级 · " + Rules.TroopHealth(kind, level) + "生命 / " + Rules.TroopDamage(kind, level) + "伤害\n" + (session.Village.IsTroopUnlocked(kind) ? "已解锁" : "训练营" + Rules.UnlockCampLevel(kind) + "级"), small);
                    if (GUI.Button(new Rect(rx + columnWidth - 80, ry + 17, 68, 34), "研究")) { if (session.ResearchTroop(kind)) Save(); }
                }
                float spellWidth = (w - 60) / 2;
                for (int i = 0; i < Rules.SpellNames.Length; i++)
                {
                    SpellKind kind = (SpellKind)i, captured = kind; int level = session.Village.SpellLevel(kind), column = i % 2, row = i / 2;
                    float sx = x + 25 + column * (spellWidth + 10), sy = y + 457 + row * 72;
                    GUI.Box(new Rect(sx, sy, spellWidth, 64), "");
                    string state = level > 0 ? level + "级 · " + Rules.SpellEffect(kind, level) : "锁定 · 议事堡/实验室" + Rules.SpellUnlockKeepLevel(kind) + "级";
                    int costLevel = Mathf.Max(1, level);
                    GUI.Label(new Rect(sx + 12, sy + 7, spellWidth - 102, 53), Rules.SpellNames[i] + " · " + state + "\n研究 " + Rules.ResearchGold(costLevel) + "金 / " + Rules.ResearchCrystal(costLevel) + "晶", small);
                    if (GUI.Button(new Rect(sx + spellWidth - 86, sy + 15, 74, 34), level == 0 ? "解锁" : "研究")) { if (session.ResearchSpell(captured)) Save(); }
                }
                GUI.Label(new Rect(x + 25, y + 610, w - 50, 32), session.Notice, small);
                if (GUI.Button(new Rect(x + 25, y + 674, w - 50, 43), "返回聚落")) showResearch = false;
            }
            else if (showHeroes)
            {
                DrawHeroManagement(x, y, w);
            }
            else if (showProgression)
            {
                GUI.Label(new Rect(x + 25, y + 23, w - 50, 42), "议事堡发展路线 · 当前 " + session.Village.KeepLevel + " 级", heading);
                GUI.Label(new Rect(x + 25, y + 68, w - 50, 34), "每一级都解锁新打法；兵营管容量，训练营管兵种，实验室管科技。", label);
                string[] tiers = {
                    "堡1  建村与基础远征\n先锋、游侠 · 疗愈之雨 · 重弩炮/哨塔 · 45营位",
                    "堡2  地空克制与范围防守\n铁卫、翼骑 · 战吼、霜封 · 投石台/猎空弩 · 60营位",
                    "堡3  完整兵种与城墙战术\n破城手、炼金师、医师、唤灵师 · 裂地 · 风暴塔/灼光塔 · 75营位",
                    "堡4  英雄与战宠构筑\n英雄殿堂、战宠小屋 · 烬卫绑定燧爪 · 炉心号令"
                };
                for (int i = 0; i < tiers.Length; i++)
                {
                    float ty = y + 112 + i * 90; GUI.Box(new Rect(x + 25, ty, w - 50, 78), "");
                    GUI.Label(new Rect(x + 42, ty + 8, w - 84, 66), tiers[i] + (session.Village.KeepLevel > i ? "\n状态：已到达" : "\n状态：尚未到达"), small);
                }
                GUI.Label(new Rect(x + 25, y + 482, w - 50, 75), "后续路线（规划）\n堡5 陷阱/防守阵型/回放 · 堡6 攻城器械 · 堡7 援军与战宠 · 堡8 第二英雄", label);
                GUI.Label(new Rect(x + 25, y + 559, w - 50, 34), "完整数值基准：docs/PROGRESSION-AND-ECONOMY-CONTRACT.md", small);
                if (GUI.Button(new Rect(x + 25, y + 596, w - 50, 42), "返回聚落")) showProgression = false;
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
                GUI.Label(new Rect(rightX, y + 396, rightW, 34), b.Level >= Rules.MaxBuildingLevel(b.Kind) ? "已达最高等级" : "下一等级会解锁更华丽的外观饰件", small);
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
