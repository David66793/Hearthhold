using UnityEngine;

namespace Hearthhold.UnityClient
{
    // Small original UI glyphs. World actors remain full 3D models; these are catalog symbols only.
    internal static class ResearchIconAtlas
    {
        private static readonly Texture2D[] Icons = new Texture2D[27];

        public static Texture2D Get(int index)
        {
            if (index < 0 || index >= Icons.Length) return Texture2D.whiteTexture;
            if (Icons[index] != null) return Icons[index];
            const int size = 96;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Hearthhold research icon " + index, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            Color[] pixels = new Color[size * size];
            Color baseColor = index >= 12 ? new Color(0.24f, 0.26f, 0.20f) : index >= 8 ? new Color(0.12f, 0.27f, 0.31f) : new Color(0.20f, 0.31f, 0.27f);
            Color ink = index == 10 ? new Color(0.68f, 0.91f, 1f) : index == 9 ? new Color(1f, 0.65f, 0.42f) : new Color(0.96f, 0.83f, 0.54f);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f, v = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                Color pixel = Color.Lerp(baseColor * 0.58f, baseColor, Mathf.Clamp01(1.2f - r * 0.65f));
                if (Mathf.Abs(r - 0.84f) < 0.025f) pixel = new Color(0.57f, 0.48f, 0.30f);
                if (Mark(index, u, v)) pixel = ink;
                pixels[y * size + x] = pixel;
            }
            texture.SetPixels(pixels); texture.Apply(false, true); Icons[index] = texture;
            return texture;
        }

        private static bool Mark(int kind, float x, float y)
        {
            switch (kind)
            {
                case 0: // Vanguard: sword
                    return Segment(x, y, 0, -0.58f, 0, 0.55f, 0.075f) || Segment(x, y, -0.31f, -0.32f, 0.31f, -0.32f, 0.065f) || Circle(x, y, 0, -0.61f, 0.12f);
                case 1: // Ranger: bow and arrow
                    return Mathf.Abs(Mathf.Sqrt((x + 0.22f) * (x + 0.22f) + y * y) - 0.46f) < 0.065f && x < 0.19f || Segment(x, y, -0.24f, 0, 0.58f, 0, 0.07f) || Segment(x, y, 0.58f, 0, 0.36f, 0.14f, 0.055f) || Segment(x, y, 0.58f, 0, 0.36f, -0.14f, 0.055f);
                case 2: // Guardian: shield
                    return Mathf.Abs(x) < 0.37f && y > -0.36f && y < 0.45f && (Mathf.Abs(x) > 0.29f || y > 0.34f) || Mathf.Abs(x) + Mathf.Abs(y + 0.20f) * 0.58f < 0.42f && y < 0.04f;
                case 3: // Sapper: hammer
                    return Segment(x, y, -0.35f, -0.54f, 0.32f, 0.38f, 0.065f) || Mathf.Abs(y - 0.42f) < 0.13f && x > 0.02f && x < 0.58f;
                case 4: // Sky rider: paired wings
                    return Segment(x, y, -0.06f, -0.40f, -0.57f, 0.35f, 0.08f) || Segment(x, y, 0.06f, -0.40f, 0.57f, 0.35f, 0.08f) || Segment(x, y, -0.55f, 0.35f, -0.20f, 0.20f, 0.08f) || Segment(x, y, 0.55f, 0.35f, 0.20f, 0.20f, 0.08f);
                case 5: // Alchemist: flask
                    return Mathf.Abs(x) < 0.10f && y > 0.06f && y < 0.56f || Circle(x, y, 0, -0.24f, 0.37f) && y < 0.08f || Segment(x, y, -0.22f, 0.55f, 0.22f, 0.55f, 0.05f);
                case 6: // Medic: cross
                    return Mathf.Abs(x) < 0.13f && Mathf.Abs(y) < 0.53f || Mathf.Abs(y) < 0.13f && Mathf.Abs(x) < 0.53f;
                case 7: // Summoner: rune
                    return Mathf.Abs(Mathf.Abs(x) + Mathf.Abs(y) - 0.56f) < 0.07f || Circle(x, y, 0, 0, 0.13f);
                case 8: // Heal: drops and cross
                    return Mathf.Abs(x) < 0.10f && Mathf.Abs(y) < 0.40f || Mathf.Abs(y) < 0.10f && Mathf.Abs(x) < 0.40f || Circle(x, y, -0.46f, -0.46f, 0.07f) || Circle(x, y, 0.46f, -0.46f, 0.07f);
                case 9: // Fury: radial burst
                    return Circle(x, y, 0, 0, 0.21f) || Segment(x, y, -0.60f, 0, 0.60f, 0, 0.055f) || Segment(x, y, 0, -0.60f, 0, 0.60f, 0.055f) || Segment(x, y, -0.42f, -0.42f, 0.42f, 0.42f, 0.05f) || Segment(x, y, -0.42f, 0.42f, 0.42f, -0.42f, 0.05f);
                case 10: // Freeze: snowflake
                    return Segment(x, y, -0.60f, 0, 0.60f, 0, 0.055f) || Segment(x, y, -0.30f, -0.52f, 0.30f, 0.52f, 0.055f) || Segment(x, y, -0.30f, 0.52f, 0.30f, -0.52f, 0.055f);
                case 11: // Quake: split stone
                    return Segment(x, y, -0.20f, 0.58f, 0.12f, 0.12f, 0.075f) || Segment(x, y, 0.12f, 0.12f, -0.12f, -0.14f, 0.075f) || Segment(x, y, -0.12f, -0.14f, 0.21f, -0.58f, 0.075f) || Segment(x, y, -0.56f, -0.40f, -0.12f, -0.14f, 0.045f) || Segment(x, y, 0.12f, 0.12f, 0.57f, 0.39f, 0.045f);
                case 12: // Keep
                    return Mathf.Abs(x) < 0.42f && y > -0.52f && y < 0.24f || Mathf.Abs(x) < 0.52f && y > 0.24f && y < 0.40f || Mathf.Abs(x) < 0.13f && y > -0.52f && y < -0.18f && !Circle(x, y, 0, -0.18f, 0.13f);
                case 13: // Mine
                    return Segment(x, y, -0.45f, -0.52f, 0.35f, 0.45f, 0.07f) || Segment(x, y, -0.42f, 0.37f, 0.38f, 0.37f, 0.08f);
                case 14: // Reservoir
                    return Circle(x, y, 0, -0.21f, 0.35f) || Mathf.Abs(x) + Mathf.Abs(y - 0.23f) < 0.38f;
                case 15: // Barracks
                    return Mathf.Abs(x) < 0.46f && y > -0.53f && y < 0.13f || Segment(x, y, -0.56f, 0.13f, 0, 0.52f, 0.08f) || Segment(x, y, 0, 0.52f, 0.56f, 0.13f, 0.08f);
                case 16: // Cannon
                    return Mathf.Abs(y + 0.15f) < 0.17f && x > -0.55f && x < 0.45f || Circle(x, y, -0.22f, -0.45f, 0.16f) || Circle(x, y, 0.27f, -0.45f, 0.16f);
                case 17: // Watchtower
                    return Mathf.Abs(x) < 0.24f && y > -0.54f && y < 0.42f || Mathf.Abs(y - 0.43f) < 0.09f && Mathf.Abs(x) < 0.47f;
                case 18: // Wall
                    return Mathf.Abs(x) < 0.60f && Mathf.Abs(y) < 0.38f && (Mathf.Abs(y) > 0.04f || Mathf.Abs(x) > 0.06f);
                case 19: // Training camp
                    return Segment(x, y, -0.36f, -0.56f, -0.36f, 0.55f, 0.06f) || x > -0.35f && x < 0.45f && y > 0.10f && y < 0.49f;
                case 20: // Laboratory
                    return Mathf.Abs(x) < 0.10f && y > 0.06f && y < 0.56f || Circle(x, y, 0, -0.24f, 0.37f) && y < 0.08f;
                case 21: // Mortar
                    return Mathf.Abs(x) < 0.45f && y > -0.45f && y < -0.13f || Segment(x, y, -0.45f, 0.20f, 0.45f, 0.20f, 0.08f);
                case 22: // Air defense
                    return Segment(x, y, 0, -0.53f, 0, 0.55f, 0.07f) || Segment(x, y, -0.38f, 0.13f, 0, 0.55f, 0.07f) || Segment(x, y, 0.38f, 0.13f, 0, 0.55f, 0.07f);
                case 23: // Arc tower
                    return Segment(x, y, -0.30f, 0.52f, 0.09f, 0.10f, 0.09f) || Segment(x, y, 0.09f, 0.10f, -0.10f, -0.09f, 0.09f) || Segment(x, y, -0.10f, -0.09f, 0.31f, -0.55f, 0.09f);
                case 24: // Beam tower
                    return Circle(x, y, 0, 0.28f, 0.23f) || Segment(x, y, 0, 0.10f, 0, -0.55f, 0.11f) || Segment(x, y, -0.42f, -0.55f, 0.42f, -0.55f, 0.08f);
                case 25: // Hero hall
                    return Segment(x, y, -0.52f, 0.18f, -0.30f, -0.40f, 0.07f) || Segment(x, y, -0.30f, -0.40f, 0.30f, -0.40f, 0.07f) || Segment(x, y, 0.30f, -0.40f, 0.52f, 0.18f, 0.07f) || Circle(x, y, 0, 0.46f, 0.13f);
                default: // Pet lodge: paw
                    return Circle(x, y, 0, -0.21f, 0.27f) || Circle(x, y, -0.42f, 0.32f, 0.11f) || Circle(x, y, -0.14f, 0.49f, 0.11f) || Circle(x, y, 0.16f, 0.49f, 0.11f) || Circle(x, y, 0.43f, 0.32f, 0.11f);
            }
        }
        private static bool Circle(float x, float y, float cx, float cy, float radius)
        { float dx = x - cx, dy = y - cy; return dx * dx + dy * dy <= radius * radius; }
        private static bool Segment(float x, float y, float ax, float ay, float bx, float by, float thickness)
        {
            float dx = bx - ax, dy = by - ay, t = Mathf.Clamp01(((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy));
            return Circle(x, y, ax + t * dx, ay + t * dy, thickness);
        }
    }
}
