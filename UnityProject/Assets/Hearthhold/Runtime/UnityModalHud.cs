using System;
using System.Collections;
using Hearthhold.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hearthhold.UnityClient
{
    // Player-facing village dialogs share one uGUI canvas. IMGUI remains for battle and layout tools.
    public sealed partial class GameBootstrap
    {
        private GameObject modalHudRoot;
        private string modalHudKey;
        private bool modalHudDirty;
        private bool smokeHudBaseVerified, smokeModalVerified;
        private Button modalPressedButton, modalPendingButton, modalLastClickedButton;
        private int modalLastClickedFrame;
        private string campaignFeedback;

        private void RefreshModalHud()
        {
            bool visible = modernFont != null && session != null && !layoutEditing && !help && ExtraModal && !showBuildCatalog;
            if (!visible)
            {
                if (modalHudRoot != null) modalHudRoot.SetActive(false);
                modalPressedButton = modalPendingButton = null;
                modalHudKey = null;
                return;
            }
            string key = showHeroes ? "heroes:" + rosterSelection + ":" + heroEquipmentTab + ":" + selectedEquipmentKind + ":" + selectedEquipmentSlot
                : showResearch ? "research:" + researchSelection : showTraining ? "training" : showCampaign ? "campaign"
                : showProgression ? "progression" : showBuildingDetails ? "building:" + selected
                : showArmyGuide ? "army:" + guideTroop + ":" + armyGuideFromTraining
                : demolishId >= 0 ? "demolish:" + demolishId : showBrief ? "brief" : "";
            if (key.Length == 0) return;
            if (key == "campaign" && modalHudKey == null) campaignFeedback = null;
            if (modalHudRoot != null && modalHudRoot.activeSelf && modalHudKey == key && !modalHudDirty) return;
            if (modalHudRoot != null) { modalHudRoot.SetActive(false); Destroy(modalHudRoot); }
            modalHudDirty = false; modalHudKey = key;
            modalHudRoot = new GameObject("Village modal canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = modalHudRoot.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 120;
            CanvasScaler scaler = modalHudRoot.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440, 900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = 0.55f;
            RectTransform veil = Panel("Dim world", modalHudRoot.transform, new Color(0.018f, 0.045f, 0.039f, 0.88f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(veil, 0, 0, 0, 0);
            bool compact = Screen.height < 800;
            float width = showHeroes ? 1110 : 1010, height = compact ? 670 : 720;
            RectTransform board = Panel("Forge dossier", veil, HudIron, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height));
            Outline(board.gameObject, HudBronzeDark, new Vector2(2, -2)); AddStrip(board, HudBronze, false, true);
            if (showCampaign) BuildCampaignModal(board, width, height);
            else if (showTraining) BuildTrainingModal(board, width, height);
            else if (showResearch) BuildResearchModal(board, width, height);
            else if (showHeroes) BuildHeroModal(board, width, height);
            else if (showProgression) BuildProgressionModal(board, width, height);
            else if (showBuildingDetails) BuildBuildingDetailModal(board, width, height);
            else if (showArmyGuide) BuildArmyGuideModal(board, width, height);
            else if (demolishId >= 0) BuildDemolishModal(board, width, height);
            else if (showBrief) BuildBriefModal(board, width, height);
            Debug.Log("HEARTHHOLD_UGUI_MODAL_READY: " + key);
        }

        private RectTransform ModalPanel(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            return Panel(name, parent, color, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height));
        }

        private TMP_Text ModalText(Transform parent, string value, float x, float y, float width, float height, int size = 18, Color? color = null)
        {
            TMP_Text result = Text("Text", parent, value, size, color ?? HudParchment, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Place(result.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height));
            result.overflowMode = TextOverflowModes.Ellipsis;
            return result;
        }

        private Button ModalButton(Transform parent, string value, float x, float y, float width, float height, Action click, bool enabled = true, bool primary = false)
        {
            Button button = ButtonWithText(value, parent, value, 18, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height), primary);
            button.interactable = enabled;
            if (!enabled) button.GetComponentInChildren<TMP_Text>().color = HudMuted;
            if (click != null) button.onClick.AddListener(delegate
            {
                modalLastClickedButton = button; modalLastClickedFrame = Time.frameCount;
                click(); modalHudDirty = true;
            });
            return button;
        }

        // Preserve native uGUI clicks, but handle a matching mouse press/release if the input module misses it.
        private void TrackModalPointer()
        {
            if (modalHudRoot == null || !modalHudRoot.activeInHierarchy || EventSystem.current == null)
            { modalPressedButton = null; return; }
            ProcessModalPointer(Input.GetMouseButtonDown(0), Input.GetMouseButtonUp(0), Input.mousePosition);
        }

        private void ProcessModalPointer(bool down, bool up, Vector2 position)
        {
            if (down) modalPressedButton = ModalButtonAt(position);
            if (up)
            {
                Button released = ModalButtonAt(position);
                if (released != null && released == modalPressedButton && released.IsInteractable()) modalPendingButton = released;
                modalPressedButton = null;
            }
        }

        private Button ModalButtonAt(Vector2 screenPosition)
        {
            var pointer = new PointerEventData(EventSystem.current) { position = screenPosition };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            if (hits.Count == 0) return null;
            Button button = hits[0].gameObject.GetComponentInParent<Button>();
            return button != null && button.transform.IsChildOf(modalHudRoot.transform) ? button : null;
        }

        private void DispatchModalPointerClick()
        {
            Button pending = modalPendingButton; modalPendingButton = null;
            if (pending == null || !pending.IsActive() || !pending.IsInteractable()) return;
            if (pending == modalLastClickedButton && modalLastClickedFrame == Time.frameCount) return;
            pending.onClick.Invoke();
        }

        private RawImage ModalImage(Transform parent, Texture texture, float x, float y, float width, float height)
        {
            GameObject obj = new GameObject("Live visual", typeof(RectTransform), typeof(RawImage));
            RectTransform rect = obj.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            Place(rect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height));
            RawImage image = obj.GetComponent<RawImage>(); image.texture = texture; image.raycastTarget = false;
            return image;
        }

        private void ModalPreview(Transform parent, Texture texture, float x, float y, float width, float height, int rosterIndex = -1)
        {
            RawImage image = ModalImage(parent, texture, x, y, width, height);
            image.raycastTarget = true;
            PreviewDragHandle drag = image.gameObject.AddComponent<PreviewDragHandle>();
            drag.Configure(rosterIndex < 0 ? (Action<Vector2>)delegate(Vector2 delta) { Previews.RotateDetail(delta); }
                : delegate(Vector2 delta) { Previews.RotateRoster(rosterIndex, delta); });
        }

        private void ModalHeader(RectTransform board, string title, string subtitle, float width, Action close)
        {
            ModalText(board, title, 26, 20, width - 200, 42, 29);
            if (!string.IsNullOrEmpty(subtitle)) ModalText(board, subtitle, 27, 62, width - 54, 31, 17, HudMuted);
            ModalButton(board, "关闭", width - 142, 20, 114, 42, close);
        }

        private void BuildCampaignModal(RectTransform board, float width, float height)
        {
            ModalHeader(board, "战役地图", "累计 " + session.Village.TotalStars + " / 30 星 · 每关至少一星解锁下一站", width, delegate { showCampaign = false; });
            ModalText(board, "远征关卡", 28, 105, 445, 32, 21, HudBronze);
            ModalText(board, "聚落成就", 519, 105, 460, 32, 21, HudBronze);
            bool compact = height < 700;
            float missionStep = compact ? 40 : 45, achievementStep = compact ? 80 : 91;
            for (int i = 0; i < Missions.Count; i++)
            {
                int mission = i; bool unlocked = session.Village.IsMissionUnlocked(i);
                string result = unlocked ? session.Village.CampaignStars[i] + "星 · 最佳 " + session.Village.CampaignBest[i] + "%" : "未解锁";
                Button row = ModalButton(board, (i + 1) + "  " + Missions.Names[i] + "    " + result,
                    28, 145 + i * missionStep, 445, compact ? 35 : 39, delegate
                    { session.MissionIndex = mission; campaignFeedback = null; }, unlocked);
                if (mission == session.MissionIndex) row.GetComponent<Image>().color = new Color32(58, 111, 83, 255);
            }
            for (int i = 0; i < Achievements.Specs.Length; i++)
            {
                AchievementSpec achievement = Achievements.Specs[i];
                int progress = Achievements.Progress(session.Village, achievement);
                bool claimed = session.Village.HasClaimed(achievement.Id), ready = progress >= achievement.Target;
                bool hasRoom = session.Village.Gold + achievement.GoldReward <= session.Village.Capacity
                    && session.Village.Crystal + achievement.CrystalReward <= session.Village.Capacity;
                string state = claimed ? "已领取" : !ready ? progress + " / " + achievement.Target : !hasRoom ? "仓库空间不足" : "可领取";
                RectTransform card = ModalPanel(board, achievement.Name, 519, 145 + i * achievementStep, 463, compact ? 72 : 82, HudIronSoft);
                ModalText(card, achievement.Name + "   " + state, 13, 8, 310, 29, 18);
                ModalText(card, achievement.Description + " · " + achievement.GoldReward + "金 / " + achievement.CrystalReward + "晶", 13, compact ? 39 : 43, 340, 28, 15, HudMuted);
                ModalButton(card, claimed ? "已领" : "领取", 363, compact ? 15 : 20, 87, 42, delegate
                {
                    bool success = session.ClaimAchievement(achievement.Id);
                    campaignFeedback = session.Notice;
                    if (success) Save();
                }, ready && !claimed && hasRoom, ready && !claimed && hasRoom);
            }
            RectTransform footer = ModalPanel(board, "Selected expedition", 28, height - 109, width - 56, 78, HudIronSoft);
            AddStrip(footer, HudBronze, true);
            bool selectedUnlocked = session.Village.IsMissionUnlocked(session.MissionIndex);
            string selectedName = Missions.Names[session.MissionIndex];
            ModalText(footer, campaignFeedback ?? (selectedUnlocked ? "已选择 " + selectedName + " · " + Missions.Descriptions[session.MissionIndex] : selectedName + "尚未解锁"),
                17, 12, width - 330, 55, 17);
            bool canDepart = selectedUnlocked && session.Village.Count(BuildingKind.Barracks) > 0
                && (session.Village.ArmyHousing > 0 || session.Village.IsHeroUnlocked(HeroKind.EmberWarden));
            ModalButton(footer, canDepart ? "出发远征" : session.Village.Count(BuildingKind.Barracks) == 0 ? "需要兵营" : "先编队训练", width - 297, 18, 252, 43,
                delegate { showCampaign = false; BeginBattle(); }, canDepart, canDepart);
        }

        private void BuildTrainingModal(RectTransform board, float width, float height)
        {
            ModalHeader(board, "远征编队", session.Village.ArmyHousing + " / " + session.Village.ArmyCapacity + " 营位 · 造兵即时完成；未投放士兵返回营地", width, delegate { showTraining = false; });
            float tileWidth = (width - 76) / 4f;
            for (int i = 0; i < Rules.Troops.Length; i++)
            {
                int index = i; TroopKind kind = (TroopKind)i; TroopSpec spec = Rules.Troops[i];
                RectTransform card = ModalPanel(board, spec.Name, 28 + (i % 4) * (tileWidth + 7), 109 + (i / 4) * 169, tileWidth, 157, HudIronSoft);
                ModalImage(card, ResearchIconAtlas.Get(i), 9, 10, 55, 55);
                ModalButton(card, "详情", 7, 73, 64, 36, delegate { guideTroop = kind; armyGuideFromTraining = true; showTraining = false; showArmyGuide = true; });
                ModalText(card, spec.Name, 78, 12, tileWidth - 86, 27, 18);
                ModalText(card, "数量 " + session.Village.ArmyCounts[i] + "\n" + (session.Village.IsTroopUnlocked(kind) ? "等级 " + session.Village.TroopLevels[i] : "未解锁"), 78, 45, tileWidth - 86, 52, 16, HudMuted);
                ModalButton(card, "遣散", 9, 113, 66, 35, delegate { if (session.DismissTroop(kind)) Save(); }, session.Village.ArmyCounts[i] > 0);
                ModalButton(card, "+1", 82, 113, 65, 35, delegate { if (session.QueueTroop(kind, DateTime.UtcNow)) Save(); }, session.Village.IsTroopUnlocked(kind));
                ModalButton(card, "+5", 154, 113, 65, 35, delegate { if (session.QueueTroops(kind, 5, DateTime.UtcNow)) Save(); }, session.Village.IsTroopUnlocked(kind));
            }
            ModalText(board, "编队预设 · 切换配比前先遣散多余士兵", 28, 467, width - 56, 31, 18, HudBronze);
            float presetWidth = (width - 56 - (Rules.FormationNames.Length - 1) * 8) / Rules.FormationNames.Length;
            for (int i = 0; i < Rules.FormationNames.Length; i++)
            {
                int formation = i;
                ModalButton(board, Rules.FormationNames[i], 28 + i * (presetWidth + 8), 506, presetWidth, 43,
                    delegate { if (session.QueueFormation(formation, DateTime.UtcNow)) Save(); });
            }
            ModalText(board, "兵营管营位 · 训练营管解锁 · 实验室管等级", 28, 571, width - 56, 32, 17, HudMuted);
        }

        private void BuildProgressionModal(RectTransform board, float width, float height)
        {
            ModalHeader(board, "议事堡发展路线", "当前 " + session.Village.KeepLevel + " 级 · 每一级都解锁新打法", width, delegate { showProgression = false; });
            string[] tiers = {
                "堡1  建村与基础远征\n先锋、游侠 · 疗愈之雨 · 重弩炮/哨塔 · 45营位",
                "堡2  地空克制与范围防守\n铁卫、翼骑 · 战吼、霜封 · 投石台/猎空弩 · 60营位",
                "堡3  完整兵种与城墙战术\n破城手、炼金师、医师、唤灵师 · 裂地 · 风暴塔/灼光塔 · 75营位",
                "堡4  英雄与战宠构筑\n英雄殿堂、战宠小屋 · 烬卫绑定燧爪 · 炉心号令"
            };
            for (int i = 0; i < tiers.Length; i++)
            {
                RectTransform card = ModalPanel(board, "堡" + (i + 1), 28, 114 + i * 110, width - 56, 98, i < session.Village.KeepLevel ? HudIronSoft : new Color32(23, 46, 41, 255));
                AddStrip(card, i < session.Village.KeepLevel ? HudBronze : HudMuted, true);
                ModalText(card, tiers[i], 21, 13, width - 250, 76, 18);
                ModalText(card, i < session.Village.KeepLevel ? "已到达" : "尚未到达", width - 185, 32, 120, 35, 18, i < session.Village.KeepLevel ? HudPatina : HudMuted);
            }
            ModalText(board, "规划：堡5 陷阱与回放 · 堡6 攻城器械 · 堡7 援军与战宠 · 堡8 第二英雄", 28, 574, width - 56, 60, 17, HudMuted);
        }

        private void BuildDemolishModal(RectTransform board, float width, float height)
        {
            Building building = session.Find(demolishId); if (building == null) { demolishId = -1; return; }
            int gold, crystal; session.DemolitionRefund(building, out gold, out crystal);
            ModalHeader(board, "确认拆除 " + building.Spec.Name + "？", "此操作不能撤销，已产生的收益会先收取", width, delegate { demolishId = -1; });
            RectTransform card = ModalPanel(board, "Demolition details", 28, 126, width - 56, 390, HudIronSoft);
            ModalText(card, "等级 " + building.Level + "  ·  位置 " + building.X + ", " + building.Z, 26, 28, width - 110, 38, 22, HudBronze);
            ModalText(card, "返还 " + gold + " 金币、" + crystal + " 晶露。\n\n返还建设与历次升级投入的 50%，受仓储上限限制。\n拆除后释放名额，无法通过撤销恢复。" + (building.Kind == BuildingKind.Barracks && session.Village.Count(building.Kind) == 1 ? "\n\n最后一座兵营拆除后，需重建才能出征。" : ""), 26, 89, width - 112, 250, 19);
            ModalButton(board, "保留建筑", 28, height - 90, 455, 50, delegate { demolishId = -1; });
            ModalButton(board, "确认拆除", 500, height - 90, 482, 50, delegate
            { int id = demolishId; demolishId = -1; if (session.Demolish(id)) { selected = -1; selectedWallIds.Clear(); RebuildBuildings(); Save(); } }, true, true);
        }

        private void BuildBriefModal(RectTransform board, float width, float height)
        {
            ModalHeader(board, "第一次远征", "先观察战线，再决定投放位置和时机", width, delegate { showBrief = false; briefSeen = true; });
            RectTransform card = ModalPanel(board, "Expedition guide", 28, 114, width - 56, 470, HudIronSoft);
            ModalText(card, "1  先锋沿缺口成组投放，游侠随后隔墙输出。\n\n2  铁卫先承受防御火力，破城手再打开城墙。\n\n3  点击战场地块会从最近的绿色战线入场；首次投兵后开始 180 秒计时。\n\n4  需要治疗时选择疗愈之雨，点击友军附近。\n\n摧毁议事堡、达到 50% 与 100% 破坏各得一星。", 24, 23, width - 104, 420, 20);
            ModalButton(board, "开始侦察", 28, height - 88, width - 56, 50, delegate { showBrief = false; briefSeen = true; }, true, true);
        }

        private void BuildBuildingDetailModal(RectTransform board, float width, float height)
        {
            Building building = session.Find(selected); if (building == null) { showBuildingDetails = false; return; }
            EnsureDetailPreview(true, (int)building.Kind, building.Level);
            ModalHeader(board, building.Spec.Name + " · " + building.Level + "级", "实时模型与当前等级数据", width, delegate { showBuildingDetails = false; });
            RectTransform stage = ModalPanel(board, "Building preview", 28, 114, 439, 490, HudIronSoft);
            ModalPreview(stage, detailTexture, 12, 12, 415, 405);
            ModalText(stage, "左键按住拖动 · 旋转模型视角", 18, 439, 397, 30, 17, HudMuted);
            RectTransform dossier = ModalPanel(board, "Building data", 485, 114, width - 513, 490, HudIronSoft);
            ModalText(dossier, "建筑数据", 20, 16, width - 555, 38, 23, HudBronze);
            ModalText(dossier, Rules.BuildingData(building) + "\n建设数量 " + session.Village.Count(building.Kind) + " / " + session.Village.Limit(building.Kind), 20, 70, width - 555, 142, 18);
            ModalText(dossier, "用途\n" + building.Spec.Description, 20, 235, width - 555, 138, 18);
            ModalText(dossier, building.Level >= Rules.MaxBuildingLevel(building.Kind) ? "已达最高等级" : "升级后外观饰件会变化", 20, 425, width - 555, 42, 17, HudMuted);
        }

        private void BuildArmyGuideModal(RectTransform board, float width, float height)
        {
            ModalHeader(board, "远征兵种图鉴", "点击兵种图标查看当前等级的模型、数值和打法", width,
                delegate { showArmyGuide = false; if (armyGuideFromTraining) showTraining = true; armyGuideFromTraining = false; });
            float tileWidth = (width - 64) / 4f;
            for (int i = 0; i < Rules.Troops.Length; i++)
            {
                TroopKind kind = (TroopKind)i;
                float x = 28 + (i % 4) * (tileWidth + 3), y = 108 + (i / 4) * 58;
                ModalButton(board, "", x, y, tileWidth - 4, 52, delegate { guideTroop = kind; });
                ModalImage(board, ResearchIconAtlas.Get(i), x + 5, y + 5, 41, 41);
                ModalText(board, Rules.Troops[i].Name, x + 56, y + 12, tileWidth - 62, 31, 18, kind == guideTroop ? HudBronze : HudParchment);
            }
            TroopSpec spec = Rules.Spec(guideTroop); int level = session.Village.TroopLevels[(int)guideTroop];
            EnsureDetailPreview(false, (int)guideTroop, level);
            RectTransform stage = ModalPanel(board, "Troop preview", 28, 242, 439, 360, HudIronSoft);
            ModalPreview(stage, detailTexture, 10, 8, 419, 299);
            ModalText(stage, "左键拖动旋转 · " + level + "级外观", 18, 322, 397, 29, 17, HudMuted);
            RectTransform dossier = ModalPanel(board, "Troop data", 485, 242, width - 513, 360, HudIronSoft);
            ModalText(dossier, spec.Name + " · " + spec.Role, 20, 15, width - 555, 38, 22, HudBronze);
            string status = session.Village.IsTroopUnlocked(guideTroop) ? "已解锁" : "训练营 " + Rules.UnlockCampLevel(guideTroop) + " 级解锁";
            string values = "科技 " + level + "级 · " + status + "\n生命 " + Rules.TroopHealth(guideTroop, level) + " · 伤害 " + Rules.TroopDamage(guideTroop, level)
                + " · 射程 " + (spec.Range / 1000f).ToString("0.##") + "格\n营位 " + spec.Housing + " · 攻击间隔 " + ((spec.Cooldown + 1f) / Rules.TicksPerSecond).ToString("0.#") + "秒";
            ModalText(dossier, values, 20, 66, width - 555, 91, 17);
            ModalText(dossier, spec.Description + "\n\n打法：" + spec.Tactics + "\n\n弱点：" + spec.Weakness, 20, 172, width - 555, 175, 17);
        }

        private void BuildResearchModal(RectTransform board, float width, float height)
        {
            ModalHeader(board, "实验室 · 科技图鉴", "实验室 " + session.Village.LaboratoryLevel + " 级 · 当前研究耗时：即时", width,
                delegate { if (researchSelection >= 0) researchSelection = -1; else showResearch = false; });
            if (researchSelection >= 0) { BuildResearchDetailModal(board, width, height); return; }
            ModalText(board, "兵种研究", 28, 106, width - 56, 35, 21, HudBronze);
            float tileWidth = (width - 79) / 4f;
            for (int i = 0; i < Rules.Troops.Length + Rules.SpellNames.Length; i++)
            {
                int index = i, column = i % 4, row = i < Rules.Troops.Length ? i / 4 : 2;
                if (i == Rules.Troops.Length) ModalText(board, "法术研究", 28, 414, width - 56, 35, 21, HudBronze);
                bool troopResearch = i < Rules.Troops.Length;
                int local = troopResearch ? i : i - Rules.Troops.Length;
                string name = troopResearch ? Rules.Troops[local].Name : Rules.SpellNames[local];
                int level = troopResearch ? session.Village.TroopLevels[local] : session.Village.SpellLevel((SpellKind)local);
                bool unlocked = troopResearch ? session.Village.IsTroopUnlocked((TroopKind)local)
                    : session.Village.KeepLevel >= Rules.SpellUnlockKeepLevel((SpellKind)local) && session.Village.LaboratoryLevel >= Rules.SpellUnlockKeepLevel((SpellKind)local);
                float x = 28 + column * (tileWidth + 8), y = troopResearch ? 147 + row * 128 : 455;
                RectTransform card = ModalPanel(board, name, x, y, tileWidth, 118, HudIronSoft);
                ModalButton(card, "", 0, 0, tileWidth, 118, delegate { researchSelection = index; Debug.Log("HEARTHHOLD_RESEARCH_DETAIL_READY: " + name); });
                ModalImage(card, ResearchIconAtlas.Get(i), 10, 10, 70, 70);
                ModalText(card, name, 89, 13, tileWidth - 97, 31, 18);
                ModalText(card, unlocked ? "等级 " + level : "未解锁", 89, 47, tileWidth - 97, 29, 17, unlocked ? HudBronze : HudMuted);
                ModalText(card, "研究耗时  即时", 12, 87, tileWidth - 24, 25, 15, HudMuted);
            }
            ModalText(board, "点击图标查看能力、升级变化和费用；升级立即生效。", 28, 599, width - 56, 29, 17, HudMuted);
        }

        private void BuildResearchDetailModal(RectTransform board, float width, float height)
        {
            bool troopResearch = researchSelection < Rules.Troops.Length;
            int index = troopResearch ? researchSelection : researchSelection - Rules.Troops.Length;
            if (index < 0 || index >= (troopResearch ? Rules.Troops.Length : Rules.SpellNames.Length)) { researchSelection = -1; return; }
            string name = troopResearch ? Rules.Troops[index].Name : Rules.SpellNames[index];
            int level = troopResearch ? session.Village.TroopLevels[index] : session.Village.SpellLevel((SpellKind)index);
            ModalText(board, name + " · " + (level == 0 ? "尚未解锁" : level + "级"), 28, 109, width - 56, 39, 25, HudBronze);
            RectTransform stage = ModalPanel(board, "Research visual", 28, 160, 350, 417, HudIronSoft);
            if (troopResearch)
            {
                EnsureDetailPreview(false, index, Mathf.Max(1, level));
                ModalPreview(stage, detailTexture, 12, 12, 326, 350);
                ModalText(stage, "左键拖动旋转 · 当前等级", 15, 377, 320, 29, 17, HudMuted);
            }
            else
            {
                ModalImage(stage, ResearchIconAtlas.Get(researchSelection), 60, 62, 230, 230);
                ModalText(stage, "原创法术标识", 15, 377, 320, 29, 17, HudMuted);
            }
            RectTransform data = ModalPanel(board, "Research data", 395, 160, width - 423, 417, HudIronSoft);
            bool canResearch;
            if (troopResearch)
            {
                TroopKind kind = (TroopKind)index; TroopSpec spec = Rules.Troops[index];
                string status = !session.Village.IsTroopUnlocked(kind) ? "训练营 " + Rules.UnlockCampLevel(kind) + " 级解锁"
                    : level >= session.Village.LaboratoryLevel ? "先升级实验室" : "可研究至 " + (level + 1) + " 级";
                ModalText(data, spec.Role + " · " + status, 20, 22, width - 465, 35, 19, HudBronze);
                ModalText(data, "生命 " + Rules.TroopHealth(kind, level) + " → " + Rules.TroopHealth(kind, level + 1)
                    + "\n伤害 " + Rules.TroopDamage(kind, level) + " → " + Rules.TroopDamage(kind, level + 1) + "\n营位 " + spec.Housing, 20, 79, width - 465, 105, 18);
                ModalText(data, spec.Description + "\n\n打法：" + spec.Tactics, 20, 203, width - 465, 177, 17);
                canResearch = session.Village.IsTroopUnlocked(kind) && level < session.Village.LaboratoryLevel;
            }
            else
            {
                SpellKind kind = (SpellKind)index; int unlock = Rules.SpellUnlockKeepLevel(kind);
                string status = session.Village.KeepLevel < unlock ? "议事堡 " + unlock + " 级解锁" : session.Village.LaboratoryLevel < unlock ? "实验室 " + unlock + " 级解锁"
                    : level >= 3 ? "已达最高等级" : level >= session.Village.LaboratoryLevel ? "先升级实验室" : "可研究至 " + (level + 1) + " 级";
                ModalText(data, status, 20, 22, width - 465, 35, 19, HudBronze);
                ModalText(data, "当前效果\n" + (level == 0 ? "尚未解锁" : Rules.SpellEffect(kind, level)), 20, 79, width - 465, 118, 18);
                ModalText(data, "下一级\n" + (level >= 3 ? "已达上限" : Rules.SpellEffect(kind, level + 1)), 20, 223, width - 465, 116, 18);
                canResearch = session.Village.KeepLevel >= unlock && session.Village.LaboratoryLevel >= unlock && level < 3 && (level == 0 || level < session.Village.LaboratoryLevel);
            }
            int costLevel = Mathf.Max(1, level), gold = Rules.ResearchGold(costLevel), crystal = Rules.ResearchCrystal(costLevel);
            bool affordable = session.Village.Gold >= gold && session.Village.Crystal >= crystal;
            ModalText(board, "研究耗时：即时 · 消耗 " + gold + " 金 / " + crystal + " 晶" + (canResearch && !affordable ? " · 资源不足" : ""), 28, 596, width - 260, 50, 18);
            ModalButton(board, troopResearch || level > 0 ? "研究升级" : "解锁法术", width - 238, 592, 210, 50, delegate
            { bool done = troopResearch ? session.ResearchTroop((TroopKind)index) : session.ResearchSpell((SpellKind)index); if (done) Save(); }, canResearch && affordable, true);
        }

        private void BuildHeroModal(RectTransform board, float width, float height)
        {
            session.Village.EnsureHeroes(); session.Village.EnsureEquipment(); EnsureRosterPreviews();
            ModalHeader(board, "英雄殿堂", "殿堂 " + session.Village.HeroHallLevel + "级 · 战宠小屋 " + session.Village.PetLodgeLevel + "级 · 点击名册查看数据", width, delegate { showHeroes = false; });
            ModalButton(board, heroEquipmentTab ? "英雄档案" : "装备构筑", width - 280, 107, 250, 42, delegate { heroEquipmentTab = !heroEquipmentTab; });
            int heroLevel = session.Village.HeroLevels[0], entry = Mathf.Clamp(rosterSelection, 0, Rules.Pets.Length);
            for (int i = 0; i < 1 + Rules.Pets.Length; i++)
            {
                int selection = i, level = i == 0 ? heroLevel : session.Village.PetLevels[i - 1];
                bool bonded = i > 0 && session.Village.HeroPetAssignments[0] == i - 1;
                string name = i == 0 ? Rules.Heroes[0].Name : Rules.Pets[i - 1].Name;
                string role = i == 0 ? Rules.Heroes[0].Role : Rules.Pets[i - 1].Role;
                RectTransform card = ModalPanel(board, name, 28, 171 + i * 143, 245, 132, i == entry ? new Color32(47, 88, 70, 255) : HudIronSoft);
                ModalButton(card, "", 0, 0, 245, 132, delegate { rosterSelection = selection; });
                ModalImage(card, rosterTextures[i], 8, 8, 93, 115);
                ModalText(card, name, 108, 11, 127, 30, 18, HudParchment);
                ModalText(card, role, 108, 46, 127, 42, 15, HudMuted);
                ModalText(card, level > 0 ? level + "级" + (bonded ? " · 已绑定" : "") : "未解锁", 108, 96, 127, 28, 15, HudBronze);
            }
            RectTransform stage = ModalPanel(board, "Live hero and pet", 290, 171, 350, 430, HudIronSoft);
            ModalPreview(stage, rosterTextures[entry], 10, 8, 330, 358, entry);
            ModalText(stage, entry == 0 ? "左键拖动旋转 · 英雄动作" : "左键拖动旋转 · 战宠协同", 15, 386, 323, 29, 16, HudMuted);
            RectTransform dossier = ModalPanel(board, "Hero dossier", 657, 171, 425, 430, HudIronSoft);
            if (entry == 0 && heroEquipmentTab) BuildEquipmentDossier(dossier, heroLevel);
            else if (entry == 0) BuildHeroDossier(dossier, heroLevel);
            else BuildPetDossier(dossier, (PetKind)(entry - 1), heroLevel);
        }

        private void BuildHeroDossier(RectTransform data, int level)
        {
            HeroKind kind = HeroKind.EmberWarden; HeroSpec hero = Rules.Spec(kind);
            int shown = Mathf.Max(1, level), next = Mathf.Min(4, shown + 1);
            ModalText(data, hero.Name + " · " + (level > 0 ? level + "级" : "未解锁"), 18, 12, 389, 36, 22, HudBronze);
            ModalText(data, hero.Role, 18, 53, 389, 29, 17, HudMuted);
            ModalText(data, "生命  " + Rules.HeroHealth(kind, shown) + (next > shown ? " → " + Rules.HeroHealth(kind, next) : "")
                + "\n伤害  " + Rules.HeroDamage(kind, shown) + (next > shown ? " → " + Rules.HeroDamage(kind, next) : "")
                + "\n射程  " + (hero.Combat.Range / 1000f).ToString("0.0") + " 格", 18, 98, 389, 90, 18);
            ModalText(data, hero.Description, 18, 203, 389, 62, 17);
            ModalText(data, "主动技能 · " + hero.AbilityName, 18, 273, 389, 32, 18, HudBronze);
            ModalText(data, hero.AbilityDescription, 18, 309, 389, 47, 16);
            int pet = session.Village.HeroPetAssignments[0];
            ModalText(data, pet >= 0 ? "当前战宠  " + Rules.Pets[pet].Name : "当前战宠  未绑定", 18, 359, 389, 27, 16, HudMuted);
            int cost = shown;
            ModalButton(data, "升级至 " + (shown + 1) + "级 · " + Rules.HeroUpgradeGold(cost) + "金 / " + Rules.HeroUpgradeCrystal(cost) + "晶", 18, 390, 389, 34,
                delegate { if (session.UpgradeHero(kind)) Save(); }, level > 0 && level < session.Village.HeroHallLevel && level < 4, true);
        }

        private void BuildPetDossier(RectTransform data, PetKind kind, int heroLevel)
        {
            int index = (int)kind, level = session.Village.PetLevels[index], shown = Mathf.Max(1, level), next = Mathf.Min(4, shown + 1);
            PetSpec pet = Rules.Spec(kind); bool assigned = session.Village.HeroPetAssignments[0] == index;
            ModalText(data, pet.Name + " · " + (level > 0 ? level + "级" : "未解锁"), 18, 12, 389, 36, 22, HudBronze);
            ModalText(data, pet.Role, 18, 53, 389, 29, 17, HudMuted);
            ModalText(data, "生命  " + Rules.PetHealth(kind, shown) + (next > shown ? " → " + Rules.PetHealth(kind, next) : "")
                + "\n伤害  " + Rules.PetDamage(kind, shown) + (next > shown ? " → " + Rules.PetDamage(kind, next) : "")
                + "\n跟随距离  " + (pet.FollowRange / 1000f).ToString("0.0") + " 格", 18, 98, 389, 96, 18);
            ModalText(data, pet.Description + "\n英雄阵亡后会独立索敌并继续攻击。", 18, 204, 389, 88, 17);
            ModalText(data, level > 0 ? (assigned ? "灵契：已绑定烬卫" : "灵契：尚未绑定") : "解锁：建造并升级战宠小屋", 18, 305, 389, 28, 16, HudMuted);
            ModalButton(data, assigned ? "解除灵契" : "绑定给烬卫", 18, 347, 389, 35,
                delegate { if (session.AssignPet(HeroKind.EmberWarden, assigned ? (PetKind?)null : kind)) Save(); }, level > 0 && heroLevel > 0);
            ModalButton(data, "升级至 " + (shown + 1) + "级 · " + Rules.PetUpgradeCrystal(shown) + "晶露", 18, 390, 389, 34,
                delegate { if (session.UpgradePet(kind)) Save(); }, level > 0 && level < session.Village.PetLodgeLevel && level < 4, true);
        }

        private void BuildEquipmentDossier(RectTransform data, int heroLevel)
        {
            VillageData village = session.Village;
            ModalText(data, "烬卫 · 装备工坊", 16, 10, 393, 34, 21, HudBronze);
            ModalText(data, "印记 " + village.CoreSigils + " · 粉尘 " + village.Stardust, 16, 45, 393, 28, 16, HudMuted);
            for (int i = 0; i < 2; i++)
            {
                int slot = i, equipped = village.HeroEquipmentSlots[i];
                string title = equipped < 0 ? "空槽" : Rules.EquipmentNames[equipped] + " Lv." + village.EquipmentLevels[equipped];
                ModalButton(data, "槽位 " + (i + 1) + "  " + title, 16, 78 + i * 43, 393, 37, delegate { selectedEquipmentSlot = slot; });
            }
            ModalText(data, "装备藏品", 16, 165, 393, 27, 17, HudBronze);
            for (int i = 0; i < Rules.EquipmentNames.Length; i++)
            {
                int gear = i, level = village.EquipmentLevels[i];
                ModalButton(data, Rules.EquipmentNames[i] + "  " + (level == 0 ? "未锻造" : "Lv." + level),
                    16 + (i % 2) * 202, 196 + (i / 2) * 42, 191, 37, delegate { selectedEquipmentKind = gear; });
            }
            int selected = Mathf.Clamp(selectedEquipmentKind, 0, Rules.EquipmentNames.Length - 1), current = village.EquipmentLevels[selected];
            ModalText(data, Rules.EquipmentNames[selected] + " · " + (current == 0 ? Rules.EquipmentEffect((EquipmentKind)selected, 1) : Rules.EquipmentEffect((EquipmentKind)selected, current)), 16, 289, 393, 46, 15);
            bool canEquip = heroLevel > 0 && current > 0 && village.HeroEquipmentSlots[1 - selectedEquipmentSlot] != selected && village.HeroEquipmentSlots[selectedEquipmentSlot] != selected;
            ModalButton(data, "装配", 16, 339, 123, 34, delegate { if (session.EquipHero(HeroKind.EmberWarden, selectedEquipmentSlot, (EquipmentKind)selected)) Save(); }, canEquip);
            ModalButton(data, "卸下", 150, 339, 123, 34, delegate { if (session.EquipHero(HeroKind.EmberWarden, selectedEquipmentSlot, null)) Save(); }, heroLevel > 0 && village.HeroEquipmentSlots[selectedEquipmentSlot] >= 0);
            int gold = Rules.EquipmentGoldCost(current), dust = Rules.EquipmentDustCost(current);
            bool canImprove = heroLevel > 0 && current < 3 && village.HeroHallLevel > current && village.Gold >= gold && village.Stardust >= dust;
            ModalButton(data, current == 0 ? "锻造" : "升级", 284, 339, 123, 34, delegate { if (session.ImproveEquipment((EquipmentKind)selected)) Save(); }, canImprove, true);
            string gate = current >= 3 ? "已达最高等级" : village.HeroHallLevel <= current ? "需英雄殿堂 " + (current + 1) + " 级"
                : village.Gold < gold || village.Stardust < dust ? "材料不足：" + gold + "金 / " + dust + "粉尘" : "消耗 " + gold + "金 / " + dust + "粉尘 · 即时";
            ModalText(data, gate, 16, 384, 393, 33, 15, HudMuted);
        }

        private IEnumerator VerifyModalHudInputSmoke()
        {
            showTraining = true; RefreshModalHud(); yield return new WaitForEndOfFrame();
            if (!SmokeModalClick("详情") || !showArmyGuide || showTraining) { ModalSmokeFailure("training detail button"); yield break; }
            RefreshModalHud(); yield return new WaitForEndOfFrame();
            var dragEvent = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, delta = new Vector2(64, -18) };
            PreviewDragHandle troopDrag = modalHudRoot.GetComponentInChildren<PreviewDragHandle>();
            float oldTroopYaw = Previews.DetailYaw;
            if (troopDrag == null) { ModalSmokeFailure("troop preview drag target"); yield break; }
            troopDrag.OnPointerDown(dragEvent); troopDrag.OnDrag(dragEvent); troopDrag.OnPointerUp(dragEvent);
            if (Mathf.Abs(Mathf.DeltaAngle(oldTroopYaw, Previews.DetailYaw)) < 10f) { ModalSmokeFailure("troop preview left-drag orbit"); yield break; }
            if (!SmokeModalClick("关闭") || !showTraining || showArmyGuide) { ModalSmokeFailure("army guide return button"); yield break; }
            CloseExtraModals(); showResearch = true; researchSelection = -1; RefreshModalHud(); yield return new WaitForEndOfFrame();
            if (!SmokeModalClick("", "先锋") || researchSelection != 0) { ModalSmokeFailure("research tile"); yield break; }
            RefreshModalHud(); yield return new WaitForEndOfFrame();
            if (!SmokeModalClick("关闭") || researchSelection != -1 || !showResearch) { ModalSmokeFailure("research back button"); yield break; }
            CloseExtraModals(); showHeroes = true; heroEquipmentTab = false; RefreshModalHud(); yield return new WaitForEndOfFrame();
            if (!SmokeModalClick("装备构筑") || !heroEquipmentTab) { ModalSmokeFailure("hero equipment tab"); yield break; }
            PreviewDragHandle rosterDrag = modalHudRoot.GetComponentInChildren<PreviewDragHandle>();
            float oldRosterYaw = Previews.RosterYaw(0);
            if (rosterDrag == null) { ModalSmokeFailure("hero preview drag target"); yield break; }
            rosterDrag.OnPointerDown(dragEvent); rosterDrag.OnDrag(dragEvent); rosterDrag.OnPointerUp(dragEvent);
            if (Mathf.Abs(Mathf.DeltaAngle(oldRosterYaw, Previews.RosterYaw(0))) < 10f) { ModalSmokeFailure("hero preview left-drag orbit"); yield break; }
            rosterSelection = 2; RefreshModalHud(); yield return new WaitForEndOfFrame();
            PreviewDragHandle petDrag = modalHudRoot.GetComponentInChildren<PreviewDragHandle>();
            float oldPetYaw = Previews.RosterYaw(2);
            if (petDrag == null) { ModalSmokeFailure("pet preview drag target"); yield break; }
            petDrag.OnPointerDown(dragEvent); petDrag.OnDrag(dragEvent); petDrag.OnPointerUp(dragEvent);
            if (Mathf.Abs(Mathf.DeltaAngle(oldPetYaw, Previews.RosterYaw(2))) < 10f) { ModalSmokeFailure("pet preview left-drag orbit"); yield break; }
            CloseExtraModals(); demolishId = selected; RefreshModalHud(); yield return new WaitForEndOfFrame();
            if (!SmokeModalClick("保留建筑") || demolishId >= 0) { ModalSmokeFailure("demolition cancel button"); yield break; }
            CloseExtraModals(); showBuildingDetails = true; RefreshModalHud(); yield return new WaitForEndOfFrame();
            PreviewDragHandle buildingDrag = modalHudRoot.GetComponentInChildren<PreviewDragHandle>();
            float oldDetailYaw = Previews.DetailYaw;
            if (buildingDrag == null) { ModalSmokeFailure("building preview drag target"); yield break; }
            buildingDrag.OnPointerDown(dragEvent); buildingDrag.OnDrag(dragEvent); buildingDrag.OnPointerUp(dragEvent);
            if (Mathf.Abs(Mathf.DeltaAngle(oldDetailYaw, Previews.DetailYaw)) < 10f) { ModalSmokeFailure("building preview left-drag orbit"); yield break; }
            CloseExtraModals(); RefreshModalHud(); yield return new WaitForEndOfFrame();
            RefreshModernHud();
            Debug.Log("HEARTHHOLD_MODAL_INPUT_READY: training, guide, research, hero equipment and demolition buttons clicked through uGUI raycasts.");
            smokeModalVerified = true;
        }

        private IEnumerator VerifyCampaignModalInputSmoke()
        {
            RefreshModalHud(); yield return new WaitForEndOfFrame();
            string missionLabel = "2  " + Missions.Names[1] + "    " + session.Village.CampaignStars[1] + "星 · 最佳 " + session.Village.CampaignBest[1] + "%";
            Button missionButton = null;
            foreach (Button button in modalHudRoot.GetComponentsInChildren<Button>())
                if (button.name == missionLabel) { missionButton = button; break; }
            if (missionButton == null) { ModalSmokeFailure("campaign mission button missing"); yield break; }
            RectTransform missionRect = (RectTransform)missionButton.transform;
            Vector2 missionPoint = RectTransformUtility.WorldToScreenPoint(null, missionRect.TransformPoint(missionRect.rect.center));
            if (ModalButtonAt(missionPoint) != missionButton) { ModalSmokeFailure("campaign mission button not under pointer"); yield break; }
            ProcessModalPointer(true, false, missionPoint);
            ProcessModalPointer(false, true, missionPoint);
            DispatchModalPointerClick();
            if (session.MissionIndex != 1 || !showCampaign) { ModalSmokeFailure("mission selection must stay visible"); yield break; }
            RefreshModalHud(); yield return new WaitForEndOfFrame();
            int oldGold = session.Village.Gold, oldCrystal = session.Village.Crystal;
            Button claimButton = null;
            foreach (Button button in modalHudRoot.GetComponentsInChildren<Button>())
                if (button.name == "领取" && button.transform.parent.name == "远征初捷") { claimButton = button; break; }
            if (claimButton == null) { ModalSmokeFailure("claim button missing"); yield break; }
            RectTransform claimRect = (RectTransform)claimButton.transform;
            Vector2 claimPoint = RectTransformUtility.WorldToScreenPoint(null, claimRect.TransformPoint(claimRect.rect.center));
            if (ModalButtonAt(claimPoint) != claimButton) { ModalSmokeFailure("claim button not under pointer"); yield break; }
            ProcessModalPointer(true, false, claimPoint);
            ProcessModalPointer(false, true, claimPoint);
            DispatchModalPointerClick();
            if (!session.Village.HasClaimed("first_victory")
                || session.Village.Gold != oldGold + 260 || session.Village.Crystal != oldCrystal + 80)
            { ModalSmokeFailure("achievement claim and reward"); yield break; }
            showCampaign = false; help = true; RefreshModernHud(); RefreshModalHud(); RefreshHelpHud();
            yield return new WaitForEndOfFrame();
            Button resume = null;
            if (helpHudRoot != null)
                foreach (Button button in helpHudRoot.GetComponentsInChildren<Button>())
                    if (button.name == "Resume game") { resume = button; break; }
            if (resume == null || !ModernHomeHudOwnsOnGUI || !SmokeClick(resume) || help)
            { ModalSmokeFailure("forge-style help and resume"); yield break; }
            showCampaign = true; campaignFeedback = null; RefreshModernHud(); RefreshModalHud(); RefreshHelpHud();
            Debug.Log("HEARTHHOLD_CAMPAIGN_INPUT_READY: mission selection remains visible, achievement rewards are claimed, and uGUI help resumes.");
        }

        private bool SmokeModalClick(string name, string parentName = null)
        {
            if (modalHudRoot == null || EventSystem.current == null) return false;
            Canvas.ForceUpdateCanvases();
            foreach (Button button in modalHudRoot.GetComponentsInChildren<Button>(true))
            {
                if (button.name != name || parentName != null && button.transform.parent.name != parentName) continue;
                RectTransform rect = (RectTransform)button.transform;
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
                var pointer = new PointerEventData(EventSystem.current) { position = screen };
                var hits = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, hits);
                if (hits.Count == 0 || hits[0].gameObject != button.gameObject)
                {
                    Debug.LogError("HEARTHHOLD_MODAL_RAYCAST_FAILED: " + name + " -> " + (hits.Count == 0 ? "none" : hits[0].gameObject.name) + " at " + screen);
                    return false;
                }
                return SmokeClick(button);
            }
            return false;
        }

        private void ModalSmokeFailure(string control)
        {
            Debug.LogError("HEARTHHOLD_MODAL_INPUT_FAILED: " + control);
            Application.Quit(7);
        }
    }
}
