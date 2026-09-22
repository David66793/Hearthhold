using Hearthhold.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hearthhold.UnityClient
{
    public sealed partial class GameBootstrap
    {
        private static readonly Color HudIron = new Color32(15, 36, 31, 248);
        private static readonly Color HudIronSoft = new Color32(24, 52, 45, 246);
        private static readonly Color HudBronze = new Color32(205, 157, 68, 255);
        private static readonly Color HudBronzeDark = new Color32(111, 73, 30, 255);
        private static readonly Color HudPatina = new Color32(73, 139, 124, 255);
        private static readonly Color HudParchment = new Color32(237, 226, 190, 255);
        private static readonly Color HudMuted = new Color32(175, 192, 176, 255);
        private static readonly Color HudEmber = new Color32(220, 105, 48, 255);

        private GameObject modernHudRoot;
        private CanvasGroup modernHudCanvasGroup;
        private GameObject modernCatalogOverlay;
        private TMP_FontAsset modernFont;
        private TMP_Text modernResourceText, modernVillageText, modernIncomeText, modernMissionName, modernMissionState, modernNoticeText;
        private Button modernCollectButton, modernExpeditionButton;
        private TMP_Text modernCollectLabel, modernExpeditionLabel;
        private GameObject modernSelectionPanel;
        private TMP_Text modernSelectionTitle, modernSelectionDescription, modernSelectionData, modernSelectionCount;
        private Button modernUpgradeButton, modernManageButton, modernDemolishButton;
        private TMP_Text modernUpgradeLabel, modernManageLabel;
        private bool modernCatalogOpen;

        private bool ModernHomeHudOwnsOnGUI
        {
            get { return modernHudRoot != null && modernHudRoot.activeSelf; }
        }

        private void InitializeModernHud()
        {
            if (modernHudRoot != null || uiFont == null) return;
            modernFont = TMP_FontAsset.CreateFontAsset("Microsoft YaHei UI", "Regular");
            if (modernFont == null) modernFont = TMP_FontAsset.CreateFontAsset("Microsoft YaHei", "Regular");
            if (modernFont == null) modernFont = TMP_FontAsset.CreateFontAsset("Arial", "Regular");
            if (modernFont == null) { Debug.LogError("HEARTHHOLD_MODERN_HUD_FONT_FAILED: no supported DynamicOS font found."); return; }
            modernFont.name = "Hearthhold Dynamic Chinese";

            GameObject canvasObject = new GameObject("Modern HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            modernHudRoot = canvasObject;
            modernHudCanvasGroup = canvasObject.GetComponent<CanvasGroup>();
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.55f;

            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("HUD Event System", typeof(EventSystem), typeof(StandaloneInputModule));

            BuildModernTopBar(canvasObject.transform);
            BuildModernVillagePanel(canvasObject.transform);
            BuildModernBottomDock(canvasObject.transform);
            BuildModernMissionPanel(canvasObject.transform);
            BuildModernSelectionPanel(canvasObject.transform);
            BuildModernCatalog(canvasObject.transform);
            RefreshModernHud();
            Debug.Log("HEARTHHOLD_MODERN_HUD_READY: uGUI + TextMesh Pro forge command table initialized.");
        }

        private void BuildModernTopBar(Transform parent)
        {
            RectTransform bar = Panel("Top resource beam", parent, HudIron, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 82));
            AddStrip(bar, new Color32(128, 94, 39, 255), false);
            TMP_Text title = Text("Brand", bar, "烽火堡垒", 29, HudParchment, FontStyles.Bold, TextAlignmentOptions.Left);
            Place(title.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 2), new Vector2(260, 48));
            TMP_Text subtitle = Text("Brand subtitle", bar, "炉心议事厅", 15, HudBronze, FontStyles.Normal, TextAlignmentOptions.Left);
            Place(subtitle.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(194, -1), new Vector2(160, 28));

            RectTransform resources = Panel("Resources", bar, new Color32(31, 61, 51, 255), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-132, 0), new Vector2(360, 48));
            Outline(resources.gameObject, HudBronzeDark, new Vector2(1, -1));
            modernResourceText = Text("Resource values", resources, "", 18, HudParchment, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(modernResourceText.rectTransform, 12, 4, 12, 4);

            Button helpButton = ButtonWithText("Help", bar, "帮助", 15, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-22, 0), new Vector2(88, 42), false);
            helpButton.onClick.AddListener(delegate { help = true; modernCatalogOpen = false; });
            modernNoticeText = Text("World notice", bar.parent, "", 15, HudParchment, FontStyles.Normal, TextAlignmentOptions.Left);
            Place(modernNoticeText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(286, -94), new Vector2(-572, 38));
        }

        private void BuildModernVillagePanel(Transform parent)
        {
            RectTransform panel = Panel("Village plaque", parent, HudIron, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -104), new Vector2(248, 296));
            Outline(panel.gameObject, HudBronzeDark, new Vector2(2, -2));
            AddStrip(panel, HudBronze, true);
            TMP_Text title = Text("Village title", panel, "你的聚落", 27, HudParchment, FontStyles.Bold, TextAlignmentOptions.Left);
            Place(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(17, -17), new Vector2(-34, 38));
            modernVillageText = Text("Village progress", panel, "", 17, Color.white, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Place(modernVillageText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(17, -63), new Vector2(-34, 78));

            modernCollectButton = ButtonWithText("Collect", panel, "", 16, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(16, -148), new Vector2(-32, 43), true);
            modernCollectLabel = modernCollectButton.GetComponentInChildren<TMP_Text>();
            modernCollectButton.onClick.AddListener(delegate { session.Collect(System.DateTime.UtcNow); Save(); RefreshModernHud(); });

            Button campaign = ButtonWithText("Campaign", panel, "战役记录", 15, new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(0, 1), new Vector2(16, -202), new Vector2(-22, 38), false);
            campaign.onClick.AddListener(delegate { showCampaign = true; });
            Button research = ButtonWithText("Research", panel, "科技研究", 15, new Vector2(0.5f, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -202), new Vector2(-22, 38), false);
            research.onClick.AddListener(delegate { showResearch = true; });
            modernIncomeText = Text("Interaction hint", panel, "鼠标完成主要操作\n快捷键仅用于加速", 15, HudMuted, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Place(modernIncomeText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(17, -251), new Vector2(-34, 42));
        }

        private void BuildModernBottomDock(Transform parent)
        {
            RectTransform bar = Panel("Command dock", parent, HudIron, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 104));
            AddStrip(bar, HudBronzeDark, false, true);
            string[] labels = { "建造", "训练", "图鉴", "英雄与战宠", "编辑阵型" };
            for (int i = 0; i < labels.Length; i++)
            {
                int captured = i;
                Button button = ButtonWithText("Dock " + labels[i], bar, labels[i], i == 3 ? 14 : 16,
                    new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20 + i * 140, 0), new Vector2(128, 56), i == 0);
                button.onClick.AddListener(delegate
                {
                    if (captured == 0) OpenModernBuildCatalog();
                    else if (captured == 1) showTraining = true;
                    else if (captured == 2) showArmyGuide = true;
                    else if (captured == 3) showHeroes = true;
                    else EnterLayoutEditor();
                });
            }
            TMP_Text hint = Text("Dock hint", bar, "拖动地图 · 滚轮缩放 · 右键取消", 15, HudMuted, FontStyles.Normal, TextAlignmentOptions.Left);
            Place(hint.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(730, 0), new Vector2(270, 42));
        }

        private void BuildModernMissionPanel(Transform parent)
        {
            RectTransform panel = Panel("Expedition token", parent, HudIronSoft, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-18, 12), new Vector2(438, 82));
            Outline(panel.gameObject, HudBronzeDark, new Vector2(2, -2));
            Texture2D emblemTexture = Resources.Load<Texture2D>("UI/ExpeditionEmblemV1");
            if (emblemTexture != null)
            {
                GameObject emblemObject = new GameObject("Expedition emblem", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
                RectTransform emblem = emblemObject.GetComponent<RectTransform>(); emblem.SetParent(panel, false);
                Place(emblem, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(11, 0), new Vector2(68, 68));
                emblemObject.GetComponent<RawImage>().texture = emblemTexture;
                emblemObject.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            }
            Button previous = ButtonWithText("Previous mission", panel, "<", 21, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(87, 17), new Vector2(36, 32), false);
            previous.onClick.AddListener(delegate { session.CycleMission(-1); RefreshModernHud(); });
            Button next = ButtonWithText("Next mission", panel, ">", 21, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(87, -18), new Vector2(36, 32), false);
            next.onClick.AddListener(delegate { session.CycleMission(1); RefreshModernHud(); });
            modernMissionName = Text("Mission name", panel, "", 18, HudParchment, FontStyles.Bold, TextAlignmentOptions.Left);
            Place(modernMissionName.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(132, -10), new Vector2(158, 27));
            modernMissionState = Text("Mission state", panel, "", 15, HudMuted, FontStyles.Normal, TextAlignmentOptions.Left);
            Place(modernMissionState.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(132, -36), new Vector2(158, 23));
            modernExpeditionButton = ButtonWithText("Begin expedition", panel, "", 15, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(126, 54), true);
            modernExpeditionLabel = modernExpeditionButton.GetComponentInChildren<TMP_Text>();
            modernExpeditionButton.onClick.AddListener(BeginBattle);
        }

        private void BuildModernSelectionPanel(Transform parent)
        {
            RectTransform panel = Panel("Selected building inspector", parent, HudIron, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -146), new Vector2(280, 452));
            modernSelectionPanel = panel.gameObject;
            Outline(panel.gameObject, HudBronzeDark, new Vector2(2, -2)); AddStrip(panel, HudPatina, true);
            modernSelectionTitle = Text("Selected title", panel, "", 25, HudParchment, FontStyles.Bold, TextAlignmentOptions.Left);
            Place(modernSelectionTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(17, -16), new Vector2(-34, 38));
            modernSelectionDescription = Text("Selected description", panel, "", 15, HudMuted, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Place(modernSelectionDescription.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(17, -61), new Vector2(-34, 78));
            modernSelectionData = Text("Selected data", panel, "", 15, Color.white, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Place(modernSelectionData.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(17, -143), new Vector2(-34, 62));
            modernUpgradeButton = ButtonWithText("Upgrade selected", panel, "", 15, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(16, -218), new Vector2(-32, 38), true);
            modernUpgradeLabel = modernUpgradeButton.GetComponentInChildren<TMP_Text>();
            modernUpgradeButton.onClick.AddListener(UpgradeModernSelection);
            modernManageButton = ButtonWithText("Manage selected", panel, "", 15, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(16, -265), new Vector2(-32, 38), false);
            modernManageLabel = modernManageButton.GetComponentInChildren<TMP_Text>();
            modernManageButton.onClick.AddListener(ManageModernSelection);
            modernSelectionCount = Text("Selected count", panel, "", 15, HudMuted, FontStyles.Normal, TextAlignmentOptions.Left);
            Place(modernSelectionCount.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(17, -313), new Vector2(-34, 25));
            modernDemolishButton = ButtonWithText("Demolish selected", panel, "拆除建筑", 15, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(16, -348), new Vector2(-32, 36), false);
            modernDemolishButton.onClick.AddListener(AskDemolish);
            Button detail = ButtonWithText("Selected details", panel, "查看动态模型与详细数据", 15, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(16, -393), new Vector2(-32, 36), false);
            detail.onClick.AddListener(delegate { showBuildingDetails = true; });
            modernSelectionPanel.SetActive(false);
        }

        private void UpgradeModernSelection()
        {
            if (selected < 0) return;
            bool upgraded = selectedWallIds.Count > 1 ? session.UpgradeWallRow(selectedWallIds) : session.Upgrade(selected);
            if (upgraded) { RebuildBuildings(); Save(); RefreshModernHud(); }
        }

        private void ManageModernSelection()
        {
            Building building = session.Find(selected); if (building == null) return;
            if (building.Kind == BuildingKind.Laboratory) showResearch = true;
            else if (building.Kind == BuildingKind.HeroHall || building.Kind == BuildingKind.PetLodge) showHeroes = true;
            else if (building.Kind == BuildingKind.Wall) ToggleWallRowSelection(building);
            else { moving = building.Id; buildKind = null; }
        }

        private void BuildModernCatalog(Transform parent)
        {
            modernCatalogOverlay = new GameObject("Modern build catalog", typeof(RectTransform), typeof(Image));
            RectTransform overlay = modernCatalogOverlay.GetComponent<RectTransform>(); overlay.SetParent(parent, false); Stretch(overlay, 0, 0, 0, 0);
            modernCatalogOverlay.GetComponent<Image>().color = new Color(0.025f, 0.055f, 0.05f, 0.84f);
            RectTransform modal = Panel("Catalog forge board", overlay, HudIron, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040, 742));
            Outline(modal.gameObject, HudBronze, new Vector2(2, -2)); AddStrip(modal, HudBronze, false, true);
            TMP_Text title = Text("Catalog title", modal, "建造工坊", 30, HudParchment, FontStyles.Bold, TextAlignmentOptions.Left);
            Place(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(28, -20), new Vector2(-180, 42));
            TMP_Text intro = Text("Catalog intro", modal, "选择建筑后返回地图放置。卡片同时显示用途、价格、解锁与数量上限。", 15, HudMuted, FontStyles.Normal, TextAlignmentOptions.Left);
            Place(intro.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(28, -62), new Vector2(-210, 30));
            Button close = ButtonWithText("Close catalog", modal, "关闭", 16, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-26, -24), new Vector2(120, 42), false);
            close.onClick.AddListener(delegate { modernCatalogOpen = false; });

            float cardWidth = 479, cardHeight = 75;
            for (int i = 1; i < Rules.Buildings.Length; i++)
            {
                BuildingKind kind = (BuildingKind)i;
                BuildingSpec spec = Rules.Spec(kind);
                int item = i - 1, column = item % 2, row = item / 2;
                RectTransform card = Panel("Catalog " + spec.Name, modal, HudIronSoft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(28 + column * 505, -103 - row * 83), new Vector2(cardWidth, cardHeight));
                Outline(card.gameObject, new Color32(55, 100, 84, 255), new Vector2(1, -1));
                TMP_Text name = Text("Name", card, spec.Name, 18, HudParchment, FontStyles.Bold, TextAlignmentOptions.Left);
                Place(name.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(13, -8), new Vector2(180, 25));
                TMP_Text description = Text("Description", card, ModernCatalogDescription(kind), 15, HudMuted, FontStyles.Normal, TextAlignmentOptions.TopLeft);
                Place(description.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(13, -34), new Vector2(365, 38));
                int built = session.Village.Count(kind), limit = session.Village.Limit(kind);
                string state = limit == 0 ? "议事堡 " + Rules.BuildingUnlockKeepLevel(kind) + " 级解锁" : built >= limit ? "已达上限 " + built + "/" + limit : built + "/" + limit + " · " + spec.Cost + " 金";
                TMP_Text stateText = Text("State", card, state, 15, limit == 0 ? HudMuted : HudBronze, FontStyles.Normal, TextAlignmentOptions.Right);
                Place(stateText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-92, -9), new Vector2(170, 24));
                Button place = ButtonWithText("Place", card, "放置", 15, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(70, 38), true);
                place.interactable = limit > 0 && built < limit && session.Village.Gold >= spec.Cost;
                BuildingKind captured = kind;
                place.onClick.AddListener(delegate { SelectModernCatalogBuilding(captured); });
            }
            modernCatalogOverlay.SetActive(false);
        }

        private void OpenModernBuildCatalog()
        {
            modernCatalogOpen = true;
            if (modernCatalogOverlay != null)
            {
                Destroy(modernCatalogOverlay);
                BuildModernCatalog(modernHudRoot.transform);
            }
            modernCatalogOverlay.SetActive(true);
        }

        private void SelectModernCatalogBuilding(BuildingKind kind)
        {
            buildKind = kind; moving = selected = -1; movingWallRow = false; selectedWallIds.Clear(); modernCatalogOpen = false;
            session.Notice = "放置" + Rules.Spec(kind).Name + "：点击绿色空地，右键取消。";
        }

        private static string ModernCatalogDescription(BuildingKind kind)
        {
            switch (kind)
            {
                case BuildingKind.Mine: return "持续产出金币，离线收益可收取。";
                case BuildingKind.Reservoir: return "储存建筑升级所需的晶露。";
                case BuildingKind.Barracks: return "提供部队营位；升级可提高容量。";
                case BuildingKind.Cannon: return "地面单体重击，克制高血量前排。";
                case BuildingKind.Watchtower: return "远射地面与空中目标，火力稳定。";
                case BuildingKind.Wall: return "阻挡地面部队，迫使敌军绕行。";
                case BuildingKind.TrainingCamp: return "即时训练并解锁更高阶兵种。";
                case BuildingKind.Laboratory: return "研究兵种与四类战术法术。";
                case BuildingKind.Mortar: return "超远范围攻击，存在近距盲区。";
                case BuildingKind.AirDefense: return "专门攻击空中目标，单次伤害高。";
                case BuildingKind.ArcTower: return "地空范围电弧，惩罚密集军团。";
                case BuildingKind.BeamTower: return "持续锁定目标时伤害逐步提高。";
                case BuildingKind.HeroHall: return "解锁、升级英雄；英雄不占营位。";
                case BuildingKind.PetLodge: return "绑定英雄出征，阵亡后独立作战。";
                default: return Rules.Spec(kind).Description;
            }
        }

        private void RefreshModernHud()
        {
            if (modernHudRoot == null || session == null) return;
            bool visible = fatalError == null && session.Battle == null && !layoutEditing && !help;
            modernHudRoot.SetActive(visible);
            if (!visible) return;
            modernHudCanvasGroup.interactable = !ExtraModal;
            modernHudCanvasGroup.blocksRaycasts = !ExtraModal;
            if (modernCatalogOverlay != null) modernCatalogOverlay.SetActive(modernCatalogOpen);

            modernResourceText.text = "◆ 金币  " + session.Village.Gold + "      ◇ 晶露  " + session.Village.Crystal;
            modernNoticeText.text = session.Notice;
            modernVillageText.text = "议事堡  " + session.Village.KeepLevel + " 级\n远征胜利  " + session.Village.Wins + " 次\n战役星章  " + session.Village.TotalStars + " / 30";
            int gold, crystal; session.Income(System.DateTime.UtcNow, out gold, out crystal);
            modernCollectLabel.text = "收取  " + gold + " 金  /  " + crystal + " 晶";
            modernCollectButton.interactable = gold > 0 || crystal > 0;

            bool unlocked = session.Village.IsMissionUnlocked(session.MissionIndex);
            modernMissionName.text = Missions.Names[session.MissionIndex];
            modernMissionState.text = unlocked ? "★ " + session.Village.CampaignStars[session.MissionIndex] + "/3   最佳 " + session.Village.CampaignBest[session.MissionIndex] + "%" : "完成前一关后解锁";
            bool armyReady = session.Village.ArmyHousing > 0 || session.Village.IsHeroUnlocked(HeroKind.EmberWarden);
            modernExpeditionButton.interactable = unlocked && armyReady;
            modernExpeditionLabel.text = !unlocked ? "尚未解锁" : armyReady ? "出发远征\n" + session.Village.ArmyHousing + " 营位" : "需要训练部队";

            Building selectedBuilding = session.Find(selected);
            modernSelectionPanel.SetActive(selectedBuilding != null);
            if (selectedBuilding != null)
            {
                int wallCount = selectedBuilding.Kind == BuildingKind.Wall ? Mathf.Max(1, selectedWallIds.Count) : 1;
                modernSelectionTitle.text = wallCount > 1 ? "城墙排 × " + wallCount : selectedBuilding.Spec.Name + "  " + selectedBuilding.Level + "级";
                modernSelectionDescription.text = wallCount > 1 ? "已选中同一直线上的连续墙段。批量操作不会跨越缺口或拐角。" : selectedBuilding.Spec.Description;
                modernSelectionData.text = wallCount > 1 ? WallRowSummary() : Rules.BuildingData(selectedBuilding);
                int upgradeGold = 0, upgradeCrystal = 0;
                if (wallCount > 1) foreach (int id in selectedWallIds) { Building wall = session.Find(id); if (wall != null) { upgradeGold += session.UpgradeGold(wall); upgradeCrystal += session.UpgradeCrystal(wall); } }
                else { upgradeGold = session.UpgradeGold(selectedBuilding); upgradeCrystal = session.UpgradeCrystal(selectedBuilding); }
                modernUpgradeLabel.text = (wallCount > 1 ? "整排升级  " : "升级  ") + upgradeGold + " 金 / " + upgradeCrystal + " 晶";
                modernUpgradeButton.interactable = session.Village.Gold >= upgradeGold && session.Village.Crystal >= upgradeCrystal;
                string manage = selectedBuilding.Kind == BuildingKind.Laboratory ? "查看科技研究" : selectedBuilding.Kind == BuildingKind.HeroHall || selectedBuilding.Kind == BuildingKind.PetLodge ? "管理英雄与战宠" : "移动建筑";
                if (selectedBuilding.Kind == BuildingKind.Wall) manage = wallCount > 1 ? "收起为单段城墙" : "选择所在整排城墙";
                modernManageLabel.text = manage;
                modernSelectionCount.text = "已建  " + session.Village.Count(selectedBuilding.Kind) + " / " + session.Village.Limit(selectedBuilding.Kind);
                modernDemolishButton.gameObject.SetActive(selectedBuilding.Kind != BuildingKind.Keep && wallCount == 1);
            }
        }

        private void DisposeModernHud()
        {
            if (modernHudRoot != null) Destroy(modernHudRoot);
            if (modernFont != null) Destroy(modernFont);
            modernHudRoot = null; modernFont = null;
        }

        private RectTransform Panel(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = obj.GetComponent<RectTransform>(); rect.SetParent(parent, false); Place(rect, anchorMin, anchorMax, pivot, position, size);
            obj.GetComponent<Image>().color = color;
            return rect;
        }

        private TMP_Text Text(string name, Transform parent, string value, float size, Color color, FontStyles style, TextAlignmentOptions alignment)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rect = obj.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            TMP_Text text = obj.GetComponent<TMP_Text>(); text.font = modernFont; text.text = value; text.fontSize = size; text.color = color;
            text.fontStyle = style == FontStyles.Bold ? FontStyles.Normal : style;
            text.alignment = alignment; text.richText = false; text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private Button ButtonWithText(string name, Transform parent, string value, float fontSize, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, bool primary)
        {
            RectTransform rect = Panel(name, parent, primary ? HudBronze : new Color32(36, 70, 59, 255), anchorMin, anchorMax, pivot, position, size);
            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = primary ? new Color32(255, 225, 164, 255) : new Color32(151, 209, 184, 255);
            colors.pressedColor = primary ? new Color32(177, 123, 45, 255) : new Color32(44, 105, 89, 255);
            colors.selectedColor = colors.highlightedColor; colors.disabledColor = new Color(0.35f, 0.39f, 0.36f, 0.72f); colors.fadeDuration = 0.08f;
            button.colors = colors;
            Outline(rect.gameObject, primary ? HudBronzeDark : new Color32(8, 22, 18, 255), new Vector2(1, -1));
            TMP_Text text = Text("Label", rect, value, fontSize, primary ? new Color32(39, 29, 16, 255) : HudParchment, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 7, 4, 7, 4);
            return button;
        }

        private static void Outline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = target.AddComponent<Outline>(); outline.effectColor = color; outline.effectDistance = distance; outline.useGraphicAlpha = true;
        }

        private static void AddStrip(RectTransform panel, Color color, bool vertical, bool top = false)
        {
            GameObject stripObject = new GameObject("Forge accent", typeof(RectTransform), typeof(Image));
            RectTransform strip = stripObject.GetComponent<RectTransform>(); strip.SetParent(panel, false);
            if (vertical) Place(strip, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(5, 0));
            else Place(strip, new Vector2(0, top ? 1 : 0), new Vector2(1, top ? 1 : 0), new Vector2(0.5f, top ? 1 : 0), Vector2.zero, new Vector2(0, 4));
            stripObject.GetComponent<Image>().color = color;
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
