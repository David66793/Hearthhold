using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthhold.UnityClient
{
    // The village help screen shares the forge-command-table uGUI language with its HUD and dossiers.
    public sealed partial class GameBootstrap
    {
        private GameObject helpHudRoot;

        private void RefreshHelpHud()
        {
            bool visible = modernFont != null && session != null && session.Battle == null && !layoutEditing && help;
            if (!visible)
            {
                if (helpHudRoot != null) helpHudRoot.SetActive(false);
                return;
            }
            if (helpHudRoot != null) { helpHudRoot.SetActive(true); return; }

            helpHudRoot = new GameObject("Commander handbook canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = helpHudRoot.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 140;
            CanvasScaler scaler = helpHudRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.55f;

            RectTransform veil = Panel("Handbook backdrop", helpHudRoot.transform,
                new Color(0.018f, 0.045f, 0.039f, 0.84f), Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(veil, 0, 0, 0, 0);
            RectTransform board = Panel("Forge command handbook", veil, HudIron,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(860, 640));
            Outline(board.gameObject, HudBronzeDark, new Vector2(2, -2));
            AddStrip(board, HudBronze, false, true);
            TMP_Text title = Text("Handbook title", board, "指挥官手册", 29, HudParchment, FontStyles.Normal, TextAlignmentOptions.Left);
            Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -21), new Vector2(570, 42));
            TMP_Text subtitle = Text("Handbook subtitle", board, "鼠标完成主要操作；快捷键只是加速器。", 18, HudMuted, FontStyles.Normal, TextAlignmentOptions.Left);
            Place(subtitle.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(29, -67), new Vector2(595, 31));
            Button close = ButtonWithText("Close handbook", board, "关闭", 18,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-27, -23), new Vector2(112, 42), false);
            close.onClick.AddListener(delegate { help = false; });

            HelpCard(board, "地图与阵型", "左键点击建筑，查看详情和操作。\n按住建筑拖动；划过相连城墙可选中整排。\n建造时左键放置，右键取消。\n编辑阵型可暂存建筑、整体移动并显示网格。", 28, 112);
            HelpCard(board, "聚落经营", "底部入口可建造、训练、查看图鉴与英雄。\n兵营决定营位；训练营解锁兵种。\n实验室升级兵种和法术。\n战役记录里选择关卡，并领取已完成成就。", 443, 112);
            HelpCard(board, "远征指挥", "在绿色战线附近点击地图投兵。\n底部选兵；左侧选择法术、英雄与集火令。\n先破墙，再保护远程单位。\n首次投兵后计时；未投兵力回营。", 28, 330);
            HelpCard(board, "可选快捷键", "B 建造、T 训练、I 图鉴；F1 开关手册。\nR 旋转镜头，滚轮缩放，中键拖动视野。\n1—8 选兵，H 英雄，V 技能。\nQ / Z / X / C 法术，F 集火；Esc 或右键取消。", 443, 330);

            Button resume = ButtonWithText("Resume game", board, "继续游戏", 18,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 27), new Vector2(-56, 44), true);
            resume.onClick.AddListener(delegate { help = false; });
            Debug.Log("HEARTHHOLD_HELP_UGUI_READY: handbook uses the forge HUD canvas language.");
        }

        private void HelpCard(Transform parent, string heading, string copy, float x, float y)
        {
            RectTransform card = Panel(heading, parent, HudIronSoft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(389, 201));
            AddStrip(card, HudPatina, true);
            TMP_Text title = Text(heading + " title", card, heading, 21, HudBronze, FontStyles.Normal, TextAlignmentOptions.Left);
            Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -13), new Vector2(350, 32));
            TMP_Text content = Text(heading + " instructions", card, copy, 18, HudParchment, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Place(content.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -53), new Vector2(355, 136));
        }

        private void DisposeHelpHud()
        {
            if (helpHudRoot != null) Destroy(helpHudRoot);
            helpHudRoot = null;
        }
    }
}
