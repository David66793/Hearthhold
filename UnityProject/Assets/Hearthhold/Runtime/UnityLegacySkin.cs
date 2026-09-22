using UnityEngine;

namespace Hearthhold.UnityClient
{
    public sealed partial class GameBootstrap
    {
        private Texture2D legacyPanelTexture, legacyButtonTexture, legacyButtonHoverTexture, legacyButtonPressedTexture;

        private void ApplyLegacyForgeSkin()
        {
            if (legacyPanelTexture == null)
            {
                legacyPanelTexture = ForgeTexture("Legacy forge panel", new Color32(15, 36, 31, 250), new Color32(122, 88, 36, 255));
                legacyButtonTexture = ForgeTexture("Legacy forge button", new Color32(35, 70, 59, 255), new Color32(8, 23, 18, 255));
                legacyButtonHoverTexture = ForgeTexture("Legacy forge button hover", new Color32(55, 105, 88, 255), new Color32(205, 157, 68, 255));
                legacyButtonPressedTexture = ForgeTexture("Legacy forge button pressed", new Color32(125, 88, 38, 255), new Color32(235, 190, 91, 255));
            }

            GUI.skin.font = uiFont;
            GUI.skin.label.font = uiFont;
            GUI.skin.label.normal.textColor = new Color32(237, 226, 190, 255);
            GUI.skin.label.fontSize = 15;

            GUIStyle button = GUI.skin.button;
            button.font = uiFont; button.fontSize = 15; button.fontStyle = FontStyle.Normal;
            button.normal.background = legacyButtonTexture; button.hover.background = legacyButtonHoverTexture;
            button.active.background = legacyButtonPressedTexture; button.focused.background = legacyButtonHoverTexture;
            button.onNormal.background = legacyButtonHoverTexture; button.onHover.background = legacyButtonHoverTexture; button.onActive.background = legacyButtonPressedTexture;
            button.onFocused.background = legacyButtonHoverTexture;
            button.normal.textColor = new Color32(237, 226, 190, 255); button.hover.textColor = Color.white;
            button.active.textColor = Color.white; button.focused.textColor = Color.white;
            button.onFocused.textColor = Color.white;
            button.onNormal.textColor = Color.white; button.onHover.textColor = Color.white; button.onActive.textColor = Color.white;
            button.border = new RectOffset(1, 1, 1, 1); button.padding = new RectOffset(8, 8, 5, 5);
            button.wordWrap = false; button.clipping = TextClipping.Clip;

            GUIStyle box = GUI.skin.box;
            box.font = uiFont; box.fontSize = 15; box.fontStyle = FontStyle.Normal;
            box.normal.background = legacyPanelTexture; box.normal.textColor = new Color32(237, 226, 190, 255);
            box.border = new RectOffset(1, 1, 1, 1); box.padding = new RectOffset(12, 12, 10, 10);
        }

        private static Texture2D ForgeTexture(string name, Color center, Color edge)
        {
            Texture2D texture = new Texture2D(5, 5, TextureFormat.RGBA32, false);
            texture.name = name; texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[25];
            for (int y = 0; y < 5; y++) for (int x = 0; x < 5; x++) pixels[y * 5 + x] = x == 0 || y == 0 || x == 4 || y == 4 ? edge : center;
            texture.SetPixels(pixels); texture.Apply(false, true); return texture;
        }

        private void DisposeLegacyForgeSkin()
        {
            if (legacyPanelTexture != null) Destroy(legacyPanelTexture);
            if (legacyButtonTexture != null) Destroy(legacyButtonTexture);
            if (legacyButtonHoverTexture != null) Destroy(legacyButtonHoverTexture);
            if (legacyButtonPressedTexture != null) Destroy(legacyButtonPressedTexture);
            legacyPanelTexture = legacyButtonTexture = legacyButtonHoverTexture = legacyButtonPressedTexture = null;
        }
    }
}
