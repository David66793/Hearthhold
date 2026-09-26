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
        private RectTransform modernCatalogBoard;
        private GameObject modernCatalogDetailOverlay;
        private TMP_FontAsset modernFont;
        private TMP_Text modernResourceText, modernVillageText, modernMissionName, modernMissionState;
        private Button modernCollectButton, modernExpeditionButton, modernPreviousMissionButton, modernNextMissionButton;
        private TMP_Text modernCollectLabel, modernExpeditionLabel;
        private GameObject modernSelectionPanel;
        private TMP_Text modernSelectionTitle, modernSelectionDescription, modernSelectionData, modernSelectionCount;
        private Button modernUpgradeButton, modernManageButton, modernDemolishButton, modernDetailButton;
        private TMP_Text modernUpgradeLabel, modernManageLabel;
        private bool modernCatalogOpen;
        private Button modernPressedInspectorButton, modernPendingInspectorButton, modernLastClickedInspectorButton;
        private int modernLastInspectorClickFrame = -1;

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
            TMP_Text title = Text("Brand", bar, "篝火堡垒", 29, HudParchment, FontStyles.Bold, TextAlignmentOptions.Left);
            Place(title.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 2), new Vector2(260, 48));
            TMP_Text subtitle = Text("Brand subtitle", bar, "炉心议事厅", 15, HudBronze, FontStyles.Normal, TextAlignmentOptions.Left);
            Place(subtitle.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(194, -1), new Vector2(160, 28));

            RectTransform resources = Panel("Resources", bar, new Color32(31, 61, 51, 255), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-132, 0), new Vector2(570, 48));
            Outline(resources.gameObject, HudBronzeDark, new Vector2(1, -1));
            modernResourceText = Text("Resource values", resources, "", 18, HudParchment, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(modernResourceText.rectTransform, 12, 4, 12, 4);

            Button helpButton = ButtonWithText("Help", bar, "帮助", 15, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-22, 0), new Vector2(88, 42), false);
            helpButton.onClick.AddListener(delegate { help = true; modernCatalogOpen = false; });
        }

        private void BuildModernVillagePanel(Transform parent)
        {
            RectTransform panel = Panel("Village plaque", parent, HudIron, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -104), new Vector2(248, 250));
            Outline(panel.gameObject, HudBronzeDark, new Vector2(2, -2));
            AddStrip(panel, HudBronze, true);
            TMP_Text title = Text("Village title", panel, "你的聚落", 27, HudParchment, FontStyles.Bold, TextAlignmentOptions.Left);
            Place(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(17, -17), new Vector2(-34, 38));
            modernVillageText = Text("Village progress", panel, "", 18, Color.white, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Place(modernVillageText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(17, -63), new Vector2(-34, 78));

            modernCollectButton = ButtonWithText("Collect", panel, "", 16, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(16, -148), new Vector2(-32, 43), true);
            modernCollectLabel = modernCollectButton.GetComponentInChildren<TMP_Text>();
            modernCollectButton.onClick.AddListener(delegate { session.Collect(System.DateTime.UtcNow); Save(); RefreshModernHud(); });

            Button campaign = ButtonWithText("Campaign", panel, "战役记录", 15, new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(0, 1), new Vector2(16, -202), new Vector2(-22, 38), false);
            campaign.onClick.AddListener(delegate { showCampaign = true; });
            Button research = ButtonWithText("Research", panel, "科技研究", 15, new Vector2(0.5f, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -202), new Vector2(-22, 38), false);
            research.onClick.AddListener(delegate { showResearch = true; });
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
            Button previous = ButtonWithText("Previous mission", panel, "", 21, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(87, 17), new Vector2(36, 32), false);
            modernPreviousMissionButton = previous;
            AddMissionArrow(previous.transform, true);
            previous.onClick.AddListener(delegate { session.CycleMission(-1); RefreshModernHud(); });
            Button next = ButtonWithText("Next mission", panel, "", 21, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(87, -18), new Vector2(36, 32), false);
            modernNextMissionButton = next;
            AddMissionArrow(next.transform, false);
            next.onClick.AddListener(delegate { session.CycleMission(1); RefreshModernHud(); });
            modernMissionName = Text("Mission name", panel, "", 18, HudParchment, FontStyles.Bold, TextAlignmentOptions.Left);
            Place(modernMissionName.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(132, -10), new Vector2(158, 27));
            modernMissionState = Text("Mission state", panel, "", 18, HudMuted, FontStyles.Normal, TextAlignmentOptions.Left);
            Place(modernMissionState.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(132, -36), new Vector2(158, 23));
            modernExpeditionButton = ButtonWithText("Begin expedition", panel, "", 15, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(126, 54), true);
            modernExpeditionLabel = modernExpeditionButton.GetComponentInChildren<TMP_Text>();
            modernExpeditionButton.onClick.AddListener(BeginBattle);
        }

        private void BuildModernSelectionPanel(Transform parent)
        {
            RectTransform panel = Panel("Selected building inspector", parent, HudIron, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -146), new Vector2(350, 510));
            modernSelectionPanel = panel.gameObject;
            Outline(panel.gameObject, HudBronzeDark, new Vector2(2, -2)); AddStrip(panel, HudPatina, true);
            modernSelectionTitle = Text("Selected title", panel, "", 25, HudParchment, FontStyles.Bold, TextAlignmentOptions.Left);
            Place(modernSelectionTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(17, -16), new Vector2(-34, 38));
            modernSelectionDescription = Text("Selected description", panel, "", 18, HudMuted, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Place(modernSelectionDescription.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(17, -65), new Vector2(-34, 100));
            modernSelectionData = Text("Selected data", panel, "", 18, Color.white, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Place(modernSelectionData.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(17, -173), new Vector2(-34, 80));
            modernUpgradeButton = ButtonWithText("Upgrade selected", panel, "", 18, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(16, -270), new Vector2(-32, 42), true);
            modernUpgradeLabel = modernUpgradeButton.GetComponentInChildren<TMP_Text>();
            modernUpgradeButton.onClick.AddListener(delegate { MarkModernInspectorClick(modernUpgradeButton); UpgradeModernSelection(); });
            modernManageButton = ButtonWithText("Manage selected", panel, "", 18, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(16, -322), new Vector2(-32, 42), false);
            modernManageLabel = modernManageButton.GetComponentInChildren<TMP_Text>();
            modernManageButton.onClick.AddListener(delegate { MarkModernInspectorClick(modernManageButton); ManageModernSelection(); });
            modernSelectionCount = Text("Selected count", panel, "", 18, HudMuted, FontStyles.Normal, TextAlignmentOptions.Left);
            Place(modernSelectionCount.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(17, -375), new Vector2(-34, 27));
            modernDemolishButton = ButtonWithText("Demolish selected", panel, "拆除建筑", 18, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(16, -415), new Vector2(-32, 40), false);
            modernDemolishButton.onClick.AddListener(delegate { MarkModernInspectorClick(modernDemolishButton); AskDemolish(); });
            Button detail = ButtonWithText("Selected details", panel, "查看模型与详细数据", 18, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(16, -462), new Vector2(-32, 40), false);
            modernDetailButton = detail;
            detail.onClick.AddListener(delegate { MarkModernInspectorClick(modernDetailButton); showBuildingDetails = true; });
            modernSelectionPanel.SetActive(false);
        }

        private void UpgradeModernSelection()
        {
            if (selected < 0) return;
            bool upgraded = selectedWallIds.Count > 1 ? session.UpgradeWallRow(selectedWallIds) : session.Upgrade(selected);
            if (upgraded) { RebuildBuildings(); Save(); }
            RefreshModernHud();
        }

        private void ManageModernSelection()
        {
            Building building = session.Find(selected); if (building == null) return;
            if (building.Kind == BuildingKind.Laboratory) showResearch = true;
            else if (building.Kind == BuildingKind.HeroHall || building.Kind == BuildingKind.PetLodge) showHeroes = true;
            else if (building.Kind == BuildingKind.Wall) ToggleWallRowSelection(building);
            else
            {
                moving = building.Id; buildKind = null;
                session.Notice = "移动" + building.Spec.Name + "：点击地图上的目标位置，右键取消。";
                RefreshModernHud();
            }
        }

        private void BuildModernCatalog(Transform parent)
        {
            modernCatalogOverlay = new GameObject("Modern build catalog", typeof(RectTransform), typeof(Image));
            RectTransform overlay = modernCatalogOverlay.GetComponent<RectTransform>(); overlay.SetParent(parent, false); Stretch(overlay, 0, 0, 0, 0);
            modernCatalogOverlay.GetComponent<Image>().color = new Color(0.025f, 0.055f, 0.05f, 0.84f);
            bool compact = Screen.height < 800;
            RectTransform modal = Panel("Catalog forge board", overlay, HudIron, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040, compact ? 680 : 742));
            modernCatalogBoard = modal;
            Outline(modal.gameObject, HudBronze, new Vector2(2, -2)); AddStrip(modal, HudBronze, false, true);
            TMP_Text title = Text("Catalog title", modal, "建造工坊", 30, HudParchment, FontStyles.Bold, TextAlignmentOptions.Left);
            Place(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(28, -20), new Vector2(-180, 42));
            TMP_Text intro = Text("Catalog intro", modal, "点击建筑图标查看用途与条件，点击放置后返回地图。", 18, HudMuted, FontStyles.Normal, TextAlignmentOptions.Left);
            Place(intro.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(28, -62), new Vector2(-210, 30));
            Button close = ButtonWithText("Close catalog", modal, "关闭", 16, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-26, -24), new Vector2(120, 42), false);
            close.onClick.AddListener(delegate { modernCatalogOpen = false; });

            float cardWidth = 479, cardHeight = compact ? 67 : 75, cardStep = compact ? 75 : 83;
            for (int i = 1; i < Rules.Buildings.Length; i++)
            {
                BuildingKind kind = (BuildingKind)i;
                BuildingSpec spec = Rules.Spec(kind);
                int item = i - 1, column = item % 2, row = item / 2;
                RectTransform card = Panel("Catalog " + spec.Name, modal, HudIronSoft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(28 + column * 505, -103 - row * cardStep), new Vector2(cardWidth, cardHeight));
                Outline(card.gameObject, new Color32(55, 100, 84, 255), new Vector2(1, -1));
                BuildingKind captured = kind;
                Button icon = ButtonWithText("Inspect " + spec.Name, card, "", 15, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(9, 0), new Vector2(59, 59), false);
                GameObject emblem = new GameObject("Building icon", typeof(RectTransform), typeof(RawImage));
                RectTransform emblemRect = emblem.GetComponent<RectTransform>(); emblemRect.SetParent(icon.transform, false); Stretch(emblemRect, 4, 4, 4, 4);
                emblem.GetComponent<RawImage>().texture = ResearchIconAtlas.Get(12 + i);
                emblem.GetComponent<RawImage>().raycastTarget = false;
                icon.onClick.AddListener(delegate { OpenModernCatalogDetail(captured); });
                TMP_Text name = Text("Name", card, spec.Name, 18, HudParchment, FontStyles.Bold, TextAlignmentOptions.Left);
                Place(name.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(77, -8), new Vector2(190, 25));
                int built = session.Village.Count(kind), limit = session.Village.Limit(kind);
                bool needsSigil = kind == BuildingKind.HeroHall && !session.Village.HeroHallPermit;
                string state = limit == 0 ? "议事堡 " + Rules.BuildingUnlockKeepLevel(kind) + " 级解锁" : built >= limit ? "已达上限 " + built + "/" + limit : needsSigil ? "需 1 炉心印记" : built + "/" + limit + " · " + spec.Cost + " 金";
                TMP_Text stateText = Text("State", card, state, 18, limit == 0 ? HudMuted : HudBronze, FontStyles.Normal, TextAlignmentOptions.Left);
                Place(stateText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(77, -36), new Vector2(310, 27));
                Button place = ButtonWithText("Place", card, "放置", 15, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(70, 38), true);
                place.interactable = limit > 0 && built < limit && session.Village.Gold >= spec.Cost && (!needsSigil || session.Village.CoreSigils > 0);
                if (!place.interactable) place.GetComponentInChildren<TMP_Text>().color = HudParchment;
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

        private void OpenModernCatalogDetail(BuildingKind kind)
        {
            if (modernCatalogBoard == null) return;
            if (modernCatalogDetailOverlay != null) Destroy(modernCatalogDetailOverlay);
            modernCatalogDetailOverlay = new GameObject("Building detail overlay", typeof(RectTransform), typeof(Image));
            RectTransform backdrop = modernCatalogDetailOverlay.GetComponent<RectTransform>(); backdrop.SetParent(modernCatalogBoard, false); Stretch(backdrop, 0, 0, 0, 0);
            modernCatalogDetailOverlay.GetComponent<Image>().color = new Color(0.015f, 0.045f, 0.04f, 0.90f);
            RectTransform panel = Panel("Building detail card", backdrop, HudIronSoft, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 490));
            Outline(panel.gameObject, HudBronze, new Vector2(2, -2));
            BuildingSpec spec = Rules.Spec(kind);
            TMP_Text title = Text("Building title", panel, spec.Name, 28, HudParchment, FontStyles.Bold, TextAlignmentOptions.Left);
            Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -22), new Vector2(650, 45));
            GameObject emblem = new GameObject("Building emblem", typeof(RectTransform), typeof(RawImage));
            RectTransform emblemRect = emblem.GetComponent<RectTransform>(); emblemRect.SetParent(panel, false);
            Place(emblemRect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -92), new Vector2(176, 176));
            emblem.GetComponent<RawImage>().texture = ResearchIconAtlas.Get(12 + (int)kind);
            emblem.GetComponent<RawImage>().raycastTarget = false;
            string combatData = spec.Damage > 0 ? " · 攻击 " + spec.Damage + " · 射程 " + (spec.Range / 1000f).ToString("0.#") + " 格" : "";
            TMP_Text description = Text("Building description", panel, spec.Description + "\n\n占地 " + spec.Size + " × " + spec.Size + " · 生命 " + spec.Health + combatData, 18, HudParchment, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Place(description.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(236, -98), new Vector2(490, 160));
            int built = session.Village.Count(kind), limit = session.Village.Limit(kind);
            bool needsSigil = kind == BuildingKind.HeroHall && !session.Village.HeroHallPermit;
            string state = limit == 0 ? "议事堡 " + Rules.BuildingUnlockKeepLevel(kind) + " 级解锁" : built >= limit ? "当前数量已达上限" : needsSigil ? "需 1 炉心印记" : "可以建造";
            TMP_Text requirements = Text("Building requirements", panel, "数量  " + built + " / " + limit + "\n建造消耗  " + spec.Cost + " 金币\n状态  " + state, 18, HudMuted, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Place(requirements.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -295), new Vector2(685, 110));
            Button back = ButtonWithText("Back to buildings", panel, "返回目录", 17, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(32, 27), new Vector2(310, 45), false);
            back.onClick.AddListener(delegate { Destroy(modernCatalogDetailOverlay); modernCatalogDetailOverlay = null; });
            Button place = ButtonWithText("Place building from detail", panel, "放置建筑", 17, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-32, 27), new Vector2(310, 45), true);
            place.interactable = limit > 0 && built < limit && session.Village.Gold >= spec.Cost && (!needsSigil || session.Village.CoreSigils > 0);
            if (!place.interactable) place.GetComponentInChildren<TMP_Text>().color = HudParchment;
            place.onClick.AddListener(delegate { SelectModernCatalogBuilding(kind); });
            Debug.Log("HEARTHHOLD_BUILDING_CATALOG_DETAIL_READY: " + spec.Name);
        }

        private void SmokeOpenModernCatalogDetail(BuildingKind kind)
        {
            if (modernCatalogBoard == null) { Debug.LogError("HEARTHHOLD_BUILDING_CATALOG_DETAIL_FAILED: catalog board missing"); return; }
            string expected = "Inspect " + Rules.Spec(kind).Name;
            foreach (Button button in modernCatalogBoard.GetComponentsInChildren<Button>())
                if (button.name == expected) { button.onClick.Invoke(); return; }
            Debug.LogError("HEARTHHOLD_BUILDING_CATALOG_DETAIL_FAILED: icon click binding missing for " + expected);
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
                case BuildingKind.HeroHall: return "解锁英雄与装备工坊；英雄不占营位。";
                case BuildingKind.PetLodge: return "绑定英雄出征，阵亡后独立作战。";
                default: return Rules.Spec(kind).Description;
            }
        }

        private void RefreshModernHud()
        {
            if (modernHudRoot == null || session == null) return;
            bool visible = fatalError == null && session.Battle == null && !layoutEditing;
            modernHudRoot.SetActive(visible);
            if (!visible) return;
            modernHudCanvasGroup.interactable = !ExtraModal && !help;
            modernHudCanvasGroup.blocksRaycasts = !ExtraModal && !help;
            if (modernCatalogOverlay != null) modernCatalogOverlay.SetActive(modernCatalogOpen);

            modernResourceText.text = "◆ 金币 " + session.Village.Gold + "   ◇ 晶露 " + session.Village.Crystal + "   印记 " + session.Village.CoreSigils + "   粉尘 " + session.Village.Stardust;
            modernVillageText.text = "议事堡  " + session.Village.KeepLevel + " 级\n远征胜利  " + session.Village.Wins + " 次\n战役星章  " + session.Village.TotalStars + " / 30";
            int gold, crystal; session.Income(System.DateTime.UtcNow, out gold, out crystal);
            modernCollectLabel.text = "收取  " + gold + " 金  /  " + crystal + " 晶";
            modernCollectButton.interactable = gold > 0 || crystal > 0;
            modernCollectLabel.color = modernCollectButton.interactable ? new Color32(39, 29, 16, 255) : HudParchment;

            bool unlocked = session.Village.IsMissionUnlocked(session.MissionIndex);
            modernMissionName.text = Missions.Names[session.MissionIndex];
            modernMissionState.text = unlocked ? "★ " + session.Village.CampaignStars[session.MissionIndex] + "/3   最佳 " + session.Village.CampaignBest[session.MissionIndex] + "%" : "完成前一关后解锁";
            bool armyReady = session.Village.ArmyHousing > 0 || session.Village.IsHeroUnlocked(HeroKind.EmberWarden);
            modernExpeditionButton.interactable = unlocked && armyReady;
            modernExpeditionLabel.text = !unlocked ? "尚未解锁" : armyReady ? "出发远征\n" + session.Village.ArmyHousing + " 营位" : "需要训练部队";
            modernExpeditionLabel.color = modernExpeditionButton.interactable ? new Color32(39, 29, 16, 255) : HudParchment;

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
                bool atVersionCap = selectedBuilding.Level >= Rules.MaxBuildingLevel(selectedBuilding.Kind);
                bool needsKeep = selectedBuilding.Kind != BuildingKind.Keep && selectedBuilding.Level >= session.Village.KeepLevel;
                if (wallCount > 1)
                    foreach (int id in selectedWallIds)
                    {
                        Building wall = session.Find(id);
                        if (wall == null) continue;
                        atVersionCap |= wall.Level >= Rules.MaxBuildingLevel(wall.Kind);
                        needsKeep |= wall.Level >= session.Village.KeepLevel;
                    }
                bool hasResources = session.Village.Gold >= upgradeGold && session.Village.Crystal >= upgradeCrystal;
                modernUpgradeLabel.text = atVersionCap ? "已达当前最高等级" : needsKeep ? "先升级议事堡" : !hasResources ? "资源不足：" + upgradeGold + " 金 / " + upgradeCrystal + " 晶" : (wallCount > 1 ? "整排升级  " : "升级  ") + upgradeGold + " 金 / " + upgradeCrystal + " 晶";
                modernUpgradeButton.interactable = !atVersionCap && !needsKeep && hasResources;
                modernUpgradeLabel.color = modernUpgradeButton.interactable ? new Color32(39, 29, 16, 255) : HudParchment;
                string manage = selectedBuilding.Kind == BuildingKind.Laboratory ? "查看科技研究" : selectedBuilding.Kind == BuildingKind.HeroHall || selectedBuilding.Kind == BuildingKind.PetLodge ? "管理英雄与战宠" : "移动建筑";
                if (selectedBuilding.Kind == BuildingKind.Wall) manage = wallCount > 1 ? "收起为单段城墙" : "选择所在整排城墙";
                modernManageLabel.text = manage;
                modernSelectionCount.text = "已建  " + session.Village.Count(selectedBuilding.Kind) + " / " + session.Village.Limit(selectedBuilding.Kind);
                modernDemolishButton.gameObject.SetActive(selectedBuilding.Kind != BuildingKind.Keep && wallCount == 1);
            }
        }

        private void DisposeModernHud()
        {
            if (modalHudRoot != null) Destroy(modalHudRoot);
            if (modernHudRoot != null) Destroy(modernHudRoot);
            if (modernFont != null) Destroy(modernFont);
            modalHudRoot = null; modernHudRoot = null; modernFont = null;
        }

        private void MarkModernInspectorClick(Button button)
        {
            modernLastClickedInspectorButton = button;
            modernLastInspectorClickFrame = Time.frameCount;
        }

        private void TrackModernInspectorMouse()
        {
            if (modernHudRoot == null || !modernHudRoot.activeInHierarchy || modernSelectionPanel == null
                || !modernSelectionPanel.activeInHierarchy || ExtraModal || modernCatalogOpen)
            {
                modernPressedInspectorButton = null;
                return;
            }
            ProcessModernInspectorPointer(Input.GetMouseButtonDown(0), Input.GetMouseButtonUp(0), Input.mousePosition);
        }

        private void ProcessModernInspectorPointer(bool down, bool up, Vector2 screenPosition)
        {
            if (down) modernPressedInspectorButton = ModernInspectorButtonAt(screenPosition);
            if (up)
            {
                Button released = ModernInspectorButtonAt(screenPosition);
                if (released != null && released == modernPressedInspectorButton && released.IsInteractable())
                    modernPendingInspectorButton = released;
                modernPressedInspectorButton = null;
            }
        }

        private Button ModernInspectorButtonAt(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return null;
            var pointer = new PointerEventData(EventSystem.current) { position = screenPosition };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            if (hits.Count == 0) return null;
            Button button = hits[0].gameObject.GetComponentInParent<Button>();
            return button == modernUpgradeButton || button == modernManageButton
                || button == modernDemolishButton || button == modernDetailButton ? button : null;
        }

        private bool ModernHudContainsPointer(Vector2 screenPosition)
        {
            if (modernHudRoot == null || !modernHudRoot.activeInHierarchy || EventSystem.current == null) return false;
            var pointer = new PointerEventData(EventSystem.current) { position = screenPosition };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            foreach (RaycastResult hit in hits)
                if (hit.gameObject != null && hit.gameObject.transform.IsChildOf(modernHudRoot.transform)) return true;
            return false;
        }

        private void DispatchModernInspectorClick()
        {
            Button pending = modernPendingInspectorButton;
            modernPendingInspectorButton = null;
            if (pending == null || !pending.IsActive() || !pending.IsInteractable()) return;
            // The EventSystem processes input in Update. Only deliver the click here if it did not.
            if (modernLastClickedInspectorButton != pending || modernLastInspectorClickFrame != Time.frameCount)
                pending.onClick.Invoke();
        }

        private bool VerifyModernHudInputSmoke()
        {
            Debug.Log("HEARTHHOLD_HUD_MODULE: " + (EventSystem.current != null && EventSystem.current.currentInputModule != null ? EventSystem.current.currentInputModule.GetType().Name : "none")
                + " active=" + (EventSystem.current != null && EventSystem.current.currentInputModule != null && EventSystem.current.currentInputModule.IsActive()));
            if (modernSelectionPanel == null || !modernSelectionPanel.activeInHierarchy || EventSystem.current == null)
            {
                Debug.LogError("HEARTHHOLD_HUD_INPUT_FAILED: inspector or EventSystem is unavailable.");
                return false;
            }
            Button[] buttons = { modernUpgradeButton, modernManageButton, modernDemolishButton, modernDetailButton, modernPreviousMissionButton, modernNextMissionButton };
            foreach (Button button in buttons)
            {
                if (!button.gameObject.activeInHierarchy) continue;
                RectTransform rect = (RectTransform)button.transform;
                var pointer = new PointerEventData(EventSystem.current)
                {
                    position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center))
                };
                var hits = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, hits);
                string target = hits.Count == 0 ? "none" : hits[0].gameObject.name;
                Debug.Log("HEARTHHOLD_HUD_INPUT_RAYCAST: " + button.name + " -> " + target + " at " + pointer.position);
                if (hits.Count == 0 || hits[0].gameObject != button.gameObject)
                {
                    Debug.LogError("HEARTHHOLD_HUD_INPUT_FAILED: " + button.name + " is blocked by " + target);
                    return false;
                }
                if (!OverHudAt(pointer.position))
                {
                    Debug.LogError("HEARTHHOLD_HUD_INPUT_FAILED: " + button.name + " click would pass through to the map.");
                    return false;
                }
            }
            Building building = session.Find(selected);
            if (building == null) return false;
            int beforeMission = session.MissionIndex;
            if (!SmokeClick(modernNextMissionButton) || session.MissionIndex == beforeMission
                || !SmokeClick(modernPreviousMissionButton) || session.MissionIndex != beforeMission)
            {
                Debug.LogError("HEARTHHOLD_HUD_INPUT_FAILED: mission arrow buttons did not cycle the mission.");
                return false;
            }
            int beforeLevel = building.Level;
            SmokePressInspectorButton(modernUpgradeButton);
            DispatchModernInspectorClick();
            if (building.Level <= beforeLevel)
            {
                Debug.LogError("HEARTHHOLD_HUD_INPUT_FAILED: upgrade button did not upgrade the selected building.");
                return false;
            }
            SmokePressInspectorButton(modernManageButton);
            DispatchModernInspectorClick();
            if (moving != selected)
            {
                Debug.LogError("HEARTHHOLD_HUD_INPUT_FAILED: manage button did not start moving the selected building.");
                return false;
            }
            moving = -1;
            SmokePressInspectorButton(modernDemolishButton);
            DispatchModernInspectorClick();
            if (demolishId != selected)
            {
                Debug.LogError("HEARTHHOLD_HUD_INPUT_FAILED: demolish button did not open confirmation.");
                return false;
            }
            demolishId = -1;
            SmokePressInspectorButton(modernDetailButton);
            DispatchModernInspectorClick();
            if (!showBuildingDetails)
            {
                Debug.LogError("HEARTHHOLD_HUD_INPUT_FAILED: details button did not open details.");
                return false;
            }
            showBuildingDetails = false;
            int managedClicks = 0;
            UnityEngine.Events.UnityAction countClick = delegate { managedClicks++; };
            modernManageButton.onClick.AddListener(countClick);
            bool nativeClick = SmokeClick(modernManageButton);
            SmokePressInspectorButton(modernManageButton);
            DispatchModernInspectorClick();
            modernManageButton.onClick.RemoveListener(countClick);
            if (!nativeClick || managedClicks != 1)
            {
                Debug.LogError("HEARTHHOLD_HUD_INPUT_FAILED: native click and fallback dispatched twice.");
                return false;
            }
            moving = -1;
            BuildingKind previewKind = BuildingKind.Mine;
            int previewSize = Rules.Spec(previewKind).Size;
            buildKind = previewKind;
            ShowPlacementPresentation(previewKind, 1, 4, 4, previewSize, true);
            CancelWorldAction();
            if (buildKind.HasValue || placement.activeSelf || placementPreview == null || placementPreview.activeSelf)
            {
                Debug.LogError("HEARTHHOLD_HUD_INPUT_FAILED: canceled construction left a visible preview.");
                return false;
            }
            moving = selected;
            ShowPlacementPresentation(previewKind, 1, 4, 4, previewSize, true);
            CancelWorldAction();
            if (moving >= 0 || placement.activeSelf || placementPreview.activeSelf)
            {
                Debug.LogError("HEARTHHOLD_HUD_INPUT_FAILED: canceled move left a visible preview.");
                return false;
            }
            RefreshModernHud();
            Debug.Log("HEARTHHOLD_HUD_INPUT_READY: inspector clicks stay above the map, actions dispatch once, and canceled previews hide immediately.");
            return true;
        }

        private static bool SmokeClick(Button button)
        {
            if (button == null || !button.IsInteractable()) return false;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, eligibleForClick = true };
            return ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }

        private void SmokePressInspectorButton(Button button)
        {
            RectTransform rect = (RectTransform)button.transform;
            Vector2 position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            ProcessModernInspectorPointer(true, false, position);
            ProcessModernInspectorPointer(false, true, position);
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
            button.targetGraphic = rect.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = primary ? new Color32(255, 225, 164, 255) : new Color32(151, 209, 184, 255);
            colors.pressedColor = primary ? new Color32(177, 123, 45, 255) : new Color32(44, 105, 89, 255);
            colors.selectedColor = colors.highlightedColor; colors.disabledColor = new Color(0.35f, 0.39f, 0.36f, 0.72f); colors.fadeDuration = 0.08f;
            button.colors = colors;
            Outline(rect.gameObject, primary ? HudBronzeDark : new Color32(8, 22, 18, 255), new Vector2(1, -1));
            TMP_Text text = Text("Label", rect, value, Mathf.Max(18, fontSize), primary ? new Color32(39, 29, 16, 255) : HudParchment, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 7, 4, 7, 4);
            return button;
        }

        private static void AddMissionArrow(Transform parent, bool pointsUp)
        {
            GameObject icon = new GameObject(pointsUp ? "Up arrow" : "Down arrow", typeof(RectTransform), typeof(MissionArrowGraphic));
            RectTransform rect = icon.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            Place(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14, 11));
            MissionArrowGraphic arrow = icon.GetComponent<MissionArrowGraphic>();
            arrow.color = HudParchment; arrow.pointsUp = pointsUp; arrow.raycastTarget = false;
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

    public sealed class MissionArrowGraphic : Graphic
    {
        public bool pointsUp;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            float tipY = pointsUp ? r.yMax : r.yMin;
            float baseY = pointsUp ? r.yMin : r.yMax;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = new Vector3(r.center.x, tipY); vh.AddVert(vertex);
            vertex.position = new Vector3(r.xMin, baseY); vh.AddVert(vertex);
            vertex.position = new Vector3(r.xMax, baseY); vh.AddVert(vertex);
            vh.AddTriangle(0, 1, 2);
        }
    }
}
