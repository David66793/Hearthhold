using System;
using System.Collections.Generic;

namespace Hearthhold.Core
{
    // Original, engine-independent model geometry. The desktop renderer and Unity
    // consume the same faces, dimensions and palette. No raster asset substitutes.
    public struct ModelPoint
    {
        public float X, Y, Z;
        public ModelPoint(float x, float y, float z) { X = x; Y = y; Z = z; }
        public static ModelPoint operator +(ModelPoint a, ModelPoint b) { return new ModelPoint(a.X + b.X, a.Y + b.Y, a.Z + b.Z); }
        public static ModelPoint operator -(ModelPoint a, ModelPoint b) { return new ModelPoint(a.X - b.X, a.Y - b.Y, a.Z - b.Z); }
        public static ModelPoint operator *(ModelPoint a, float k) { return new ModelPoint(a.X * k, a.Y * k, a.Z * k); }
        public static ModelPoint Cross(ModelPoint a, ModelPoint b) { return new ModelPoint(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X); }
        public ModelPoint Normalized() { float length = (float)Math.Sqrt(X * X + Y * Y + Z * Z); return length < 0.00001f ? new ModelPoint() : this * (1 / length); }
    }
    public sealed class ModelFace
    {
        public ModelPoint[] Points;
        public ModelPoint Normal;
        public int Color;
    }
    public sealed class ModelMesh
    {
        public readonly List<ModelFace> Faces = new List<ModelFace>();
        public void Face(int color, ModelPoint outward, params ModelPoint[] points)
        {
            ModelPoint normal = ModelPoint.Cross(points[1] - points[0], points[2] - points[0]).Normalized();
            if (normal.X * outward.X + normal.Y * outward.Y + normal.Z * outward.Z < 0) { Array.Reverse(points); normal = normal * -1; }
            Faces.Add(new ModelFace { Color = color, Points = points, Normal = normal });
        }
    }
    public static class ModelFactory
    {
        private const int Stone = 0xBFAF88, LightStone = 0xE8D8AA, DarkStone = 0x747A6C;
        private const int Timber = 0x65452F, Wood = 0x9B693D, Plaster = 0xE6D2A9;
        private const int Teal = 0x286B7A, Copper = 0xA85D3A, Red = 0xA9483D;
        private const int Metal = 0x344650, Brass = 0xD5A548, Glow = 0xFFD27A, Crystal = 0x58D7B5;
        private static ModelPoint P(float x, float y, float z) { return new ModelPoint(x, y, z); }
        public static int Tint(int rgb, int amount)
        { return Clamp((rgb >> 16 & 255) + amount) << 16 | Clamp((rgb >> 8 & 255) + amount) << 8 | Clamp((rgb & 255) + amount); }
        private static int Clamp(int n) { return Math.Max(0, Math.Min(255, n)); }
        public static ModelMesh Building(BuildingKind kind, int level)
        {
            ModelMesh m = new ModelMesh();
            float size = Rules.Spec(kind).Size;
            BevelBox(m, 0.02f, 0, 0.02f, size - 0.04f, 0.23f, size - 0.04f, 0.09f, DarkStone);
            BevelBox(m, 0.07f, 0.2f, 0.07f, size - 0.14f, 0.14f, size - 0.14f, 0.07f, Stone);
            switch (kind)
            {
                case BuildingKind.Keep: Keep(m, level); break;
                case BuildingKind.Mine: Mine(m, level); break;
                case BuildingKind.Reservoir: Pool(m, level); break;
                case BuildingKind.Barracks: Camp(m, level); break;
                case BuildingKind.Cannon: Cannon(m, level); break;
                case BuildingKind.Watchtower: Tower(m, level); break;
                case BuildingKind.Wall: Wall(m, level); break;
                case BuildingKind.TrainingCamp: TrainingCamp(m, level); break;
                case BuildingKind.Laboratory: Laboratory(m, level); break;
                case BuildingKind.Mortar: Mortar(m, level); break;
                case BuildingKind.AirDefense: AirDefense(m, level); break;
                case BuildingKind.ArcTower: ArcTower(m, level); break;
                case BuildingKind.BeamTower: BeamTower(m, level); break;
                case BuildingKind.HeroHall: HeroHall(m, level); break;
                case BuildingKind.PetLodge: PetLodge(m, level); break;
            }
            return m;
        }
        public static ModelMesh Troop(TroopKind kind)
        {
            ModelMesh m = new ModelMesh();
            float scale = kind == TroopKind.Guardian ? 1.42f : kind == TroopKind.SkyRider ? 1.25f : kind == TroopKind.Sapper ? 1.1f : 1.08f;
            int cloth = kind == TroopKind.Ranger ? 0x48785B : kind == TroopKind.Guardian ? 0x607B93 : kind == TroopKind.Sapper ? 0xA15A3E :
                kind == TroopKind.SkyRider ? 0x416C91 : kind == TroopKind.Alchemist ? 0x704D88 : kind == TroopKind.Medic ? 0xD9D2B2 : kind == TroopKind.Summoner ? 0x493A68 : 0xB17D3B;
            BevelBox(m, -0.25f, 0.04f, -0.1f, 0.21f, 0.23f, 0.38f, 0.035f, Timber);
            BevelBox(m, 0.04f, 0.04f, -0.1f, 0.21f, 0.23f, 0.38f, 0.035f, Timber);
            Box(m, -0.23f, 0.23f, -0.08f, 0.19f, 0.27f, 0.19f, 0x59625A);
            Box(m, 0.04f, 0.23f, -0.08f, 0.19f, 0.27f, 0.19f, 0x59625A);
            BevelBox(m, -0.32f, 0.47f, -0.16f, 0.64f, 0.57f, 0.41f, 0.09f, cloth);
            Box(m, -0.32f, 0.6f, -0.18f, 0.64f, 0.08f, 0.46f, Timber);
            BevelBox(m, -0.43f, 0.82f, -0.12f, 0.22f, 0.22f, 0.3f, 0.065f, kind == TroopKind.Guardian ? Metal : cloth);
            BevelBox(m, 0.21f, 0.82f, -0.12f, 0.22f, 0.22f, 0.3f, 0.065f, kind == TroopKind.Guardian ? Metal : cloth);
            Box(m, -0.34f, 0.53f, 0.23f, 0.68f, 0.1f, 0.08f, Brass);
            Box(m, -0.065f, 0.59f, 0.285f, 0.13f, 0.11f, 0.05f, Brass);
            BevelBox(m, -0.24f, 1.03f, -0.16f, 0.48f, 0.48f, 0.44f, 0.09f, 0xE7BC87);
            Box(m, -0.14f, 1.28f, 0.285f, 0.045f, 0.06f, 0.012f, 0x3F3630);
            Box(m, 0.1f, 1.28f, 0.285f, 0.045f, 0.06f, 0.012f, 0x3F3630);
            BevelBox(m, -0.055f, 1.14f, 0.27f, 0.11f, 0.09f, 0.08f, 0.025f, 0xC98A63);
            Beam(m, P(-0.33f, 0.93f, 0), P(-0.47f, 0.64f, 0.17f), 0.15f, cloth);
            Beam(m, P(0.33f, 0.93f, 0), P(0.48f, 0.75f, 0.23f), 0.15f, cloth);
            if (kind == TroopKind.Ranger)
            {
                Frustum(m, 0, 1.35f, 0.01f, 0.34f, 0.08f, 0.35f, 10, 0x456E50);
                BevelBox(m, -0.3f, 0.48f, -0.34f, 0.6f, 0.76f, 0.2f, 0.055f, 0x355F4B);
                Beam(m, P(0.5f, 0.44f, 0.34f), P(0.73f, 0.86f, 0.41f), 0.055f, Wood);
                Beam(m, P(0.73f, 0.86f, 0.41f), P(0.51f, 1.38f, 0.34f), 0.055f, Wood);
                Beam(m, P(0.51f, 1.38f, 0.34f), P(0.5f, 0.44f, 0.34f), 0.012f, Plaster);
                Box(m, -0.13f, 0.64f, -0.31f, 0.27f, 0.54f, 0.15f, Timber);
                for (int i = 0; i < 3; i++) Beam(m, P(-0.18f + i * 0.09f, 0.92f, -0.38f), P(-0.08f + i * 0.09f, 1.58f, -0.42f), 0.018f, LightStone);
            }
            else if (kind == TroopKind.Guardian)
            {
                BevelBox(m, -0.36f, 0.5f, -0.22f, 0.72f, 0.62f, 0.12f, 0.04f, Metal);
                BevelBox(m, -0.3f, 1.3f, -0.2f, 0.6f, 0.31f, 0.51f, 0.07f, Metal);
                Box(m, -0.31f, 1.24f, 0.31f, 0.62f, 0.09f, 0.05f, Brass);
                Box(m, -0.065f, 1.25f, 0.36f, 0.13f, 0.32f, 0.035f, LightStone);
                BevelBox(m, -0.65f, 0.3f, 0.27f, 0.47f, 0.85f, 0.12f, 0.07f, Brass);
                BevelBox(m, -0.61f, 0.36f, 0.41f, 0.39f, 0.72f, 0.03f, 0.06f, cloth);
                Box(m, -0.465f, 0.43f, 0.455f, 0.09f, 0.54f, 0.03f, LightStone);
                BevelBox(m, -0.53f, 0.67f, 0.445f, 0.18f, 0.18f, 0.07f, 0.04f, Brass);
                Beam(m, P(0.51f, 0.53f, 0.25f), P(0.6f, 1.65f, 0.18f), 0.06f, Metal);
                BevelBox(m, 0.38f, 1.45f, 0.08f, 0.43f, 0.29f, 0.23f, 0.035f, LightStone);
                Box(m, -0.31f, 0.38f, 0.31f, 0.62f, 0.11f, 0.06f, Brass);
            }
            else if (kind == TroopKind.Sapper)
            {
                BevelBox(m, -0.28f, 1.39f, -0.2f, 0.56f, 0.21f, 0.53f, 0.08f, Copper);
                Box(m, -0.22f, 1.3f, 0.3f, 0.19f, 0.14f, 0.06f, Brass);
                Box(m, 0.03f, 1.3f, 0.3f, 0.19f, 0.14f, 0.06f, Brass);
                Barrel(m, 0.49f, 0.44f, 0.42f, 0.24f, 0.5f);
                Beam(m, P(0.49f, 0.97f, 0.42f), P(0.59f, 1.13f, 0.42f), 0.025f, Glow);
                BevelBox(m, -0.23f, 0.55f, -0.4f, 0.46f, 0.52f, 0.21f, 0.05f, Wood);
                Beam(m, P(0.42f, 0.6f, 0.25f), P(0.68f, 1.34f, 0.25f), 0.07f, Timber);
                BevelBox(m, 0.47f, 1.24f, 0.06f, 0.48f, 0.22f, 0.38f, 0.07f, Metal);
                Box(m, -0.28f, 0.58f, -0.43f, 0.08f, 0.46f, 0.05f, Brass);
                Box(m, 0.2f, 0.58f, -0.43f, 0.08f, 0.46f, 0.05f, Brass);
            }
            else if (kind == TroopKind.SkyRider)
            {
                Beam(m, P(-0.22f, 0.82f, -0.2f), P(-0.86f, 1.18f, -0.12f), 0.08f, LightStone);
                Beam(m, P(0.22f, 0.82f, -0.2f), P(0.86f, 1.18f, -0.12f), 0.08f, LightStone);
                BevelBox(m, -1.03f, 1.08f, -0.34f, 0.78f, 0.1f, 0.56f, 0.04f, Teal);
                BevelBox(m, 0.25f, 1.08f, -0.34f, 0.78f, 0.1f, 0.56f, 0.04f, Teal);
                Frustum(m, 0, 1.47f, 0, 0.34f, 0.12f, 0.32f, 8, Brass);
            }
            else if (kind == TroopKind.Alchemist)
            {
                Frustum(m, 0, 1.39f, 0, 0.38f, 0.08f, 0.45f, 10, 0x704D88);
                Frustum(m, 0.52f, 0.72f, 0.28f, 0.23f, 0.18f, 0.42f, 10, Crystal);
                Frustum(m, 0.52f, 1.12f, 0.28f, 0.12f, 0.05f, 0.16f, 8, Glow);
                Beam(m, P(-0.46f, 0.55f, 0.18f), P(-0.61f, 1.5f, 0.22f), 0.055f, Wood);
            }
            else if (kind == TroopKind.Medic)
            {
                Frustum(m, 0, 1.4f, 0, 0.34f, 0.16f, 0.28f, 10, Plaster);
                Box(m, -0.06f, 0.75f, 0.3f, 0.12f, 0.5f, 0.04f, Red);
                Box(m, -0.25f, 0.94f, 0.3f, 0.5f, 0.12f, 0.04f, Red);
                Beam(m, P(0.47f, 0.46f, 0.22f), P(0.58f, 1.55f, 0.18f), 0.05f, LightStone);
                Frustum(m, 0.58f, 1.52f, 0.18f, 0.17f, 0.03f, 0.26f, 8, Crystal);
            }
            else if (kind == TroopKind.Summoner)
            {
                Frustum(m, 0, 1.36f, 0, 0.4f, 0.05f, 0.52f, 10, 0x493A68);
                Beam(m, P(0.48f, 0.48f, 0.24f), P(0.65f, 1.62f, 0.16f), 0.055f, Wood);
                Frustum(m, 0.65f, 1.6f, 0.16f, 0.18f, 0.04f, 0.32f, 8, 0x9E7AD1);
                Frustum(m, 0.65f, 1.91f, 0.16f, 0.08f, 0, 0.2f, 8, Glow);
            }
            else
            {
                Frustum(m, 0, 1.4f, 0, 0.32f, 0.24f, 0.23f, 10, Metal);
                Box(m, -0.31f, 1.35f, -0.21f, 0.62f, 0.09f, 0.53f, Brass);
                Box(m, -0.04f, 1.62f, -0.08f, 0.08f, 0.23f, 0.29f, Red);
                Beam(m, P(0.45f, 0.65f, 0.26f), P(0.68f, 1.58f, 0.35f), 0.06f, LightStone);
                Beam(m, P(0.35f, 0.84f, 0.29f), P(0.62f, 0.84f, 0.29f), 0.06f, Brass);
                BevelBox(m, -0.58f, 0.46f, 0.3f, 0.35f, 0.62f, 0.1f, 0.055f, Teal);
                BevelBox(m, -0.53f, 0.67f, 0.39f, 0.25f, 0.22f, 0.05f, 0.035f, Brass);
            }
            foreach (ModelFace face in m.Faces)
                for (int i = 0; i < face.Points.Length; i++) face.Points[i] = face.Points[i] * scale;
            return m;
        }
        public static ModelMesh Pet(PetKind kind)
        {
            ModelMesh m = new ModelMesh();
            int fur = kind == PetKind.CinderFox ? 0xC65A37 : 0x58745A, dark = kind == PetKind.CinderFox ? 0x6D3028 : 0x344B3B;
            BevelBox(m, -0.48f, 0.35f, -0.25f, 0.96f, 0.5f, 0.72f, 0.16f, fur);
            BevelBox(m, -0.36f, 0.65f, 0.28f, 0.72f, 0.58f, 0.62f, 0.17f, fur);
            Frustum(m, -0.22f, 1.15f, 0.36f, 0.18f, 0.03f, 0.42f, 4, dark);
            Frustum(m, 0.22f, 1.15f, 0.36f, 0.18f, 0.03f, 0.42f, 4, dark);
            Box(m, -0.24f, 0.91f, 0.86f, 0.12f, 0.1f, 0.08f, 0xFFF0B5);
            Box(m, 0.12f, 0.91f, 0.86f, 0.12f, 0.1f, 0.08f, 0xFFF0B5);
            for (int i = 0; i < 4; i++) BevelBox(m, -0.4f + i % 2 * 0.58f, 0.03f, -0.2f + i / 2 * 0.45f, 0.22f, 0.48f, 0.22f, 0.06f, dark);
            Beam(m, P(0, 0.72f, -0.22f), P(-0.72f, 1.05f, -0.58f), 0.18f, fur);
            Frustum(m, -0.77f, 1.07f, -0.62f, 0.25f, 0.05f, 0.42f, 8, kind == PetKind.CinderFox ? Glow : Crystal);
            BevelBox(m, -0.48f, 0.67f, 0.14f, 0.96f, 0.13f, 0.16f, 0.04f, Brass);
            return m;
        }
        private static void Mortar(ModelMesh m, int level)
        {
            Frustum(m, 1.5f, 0.34f, 1.5f, 1.05f, 0.83f, 0.5f, 12, Stone);
            Frustum(m, 1.5f, 0.82f, 1.5f, 0.72f, 0.62f, 0.28f, 12, Metal);
            BarrelAlong(m, P(1.5f, 0.95f, 1.5f), P(1.5f, 1.72f + level * 0.1f, 1.15f), 0.42f, Metal, 14);
            Frustum(m, 1.5f, 1.68f + level * 0.1f, 1.17f, 0.5f, 0.38f, 0.24f, 14, 0x263A43);
        }
        private static void AirDefense(ModelMesh m, int level)
        {
            Frustum(m, 1, 0.34f, 1, 0.8f, 0.58f, 0.55f, 10, Stone);
            for (int i = 0; i < 4; i++)
            {
                double a = i * Math.PI / 2; float x = 1 + (float)Math.Cos(a) * 0.28f, z = 1 + (float)Math.Sin(a) * 0.28f;
                Beam(m, P(x, 0.8f, z), P(x, 2.15f + level * 0.12f, z), 0.09f, Metal);
                Frustum(m, x, 2.1f + level * 0.12f, z, 0.16f, 0, 0.45f, 7, Red);
            }
        }
        private static void ArcTower(ModelMesh m, int level)
        {
            Tower(m, level);
            Frustum(m, 1, 2.9f + level * 0.12f, 1, 0.46f, 0.14f, 0.55f, 10, Crystal);
            for (int i = 0; i < 3; i++) Beam(m, P(1, 2.7f, 1), P(0.45f + i * 0.55f, 3.35f, 1), 0.035f, Glow);
        }
        private static void BeamTower(ModelMesh m, int level)
        {
            Tower(m, level);
            Frustum(m, 1, 2.72f + level * 0.12f, 1, 0.42f, 0.2f, 0.42f, 12, Brass);
            Frustum(m, 1, 3.12f + level * 0.12f, 1, 0.22f, 0.04f, 0.75f, 10, Glow);
            Frustum(m, 1, 3.84f + level * 0.12f, 1, 0.12f, 0, 0.3f, 10, 0xFFF1A8);
        }
        private static void Keep(ModelMesh m, int level)
        {
            float h = 1.8f + level * 0.13f;
            BevelBox(m, 0.54f, 0.32f, 0.45f, 2.9f, h, 2.83f, 0.08f, Stone);
            Masonry(m, 0.54f, 0.35f, 0.45f, 2.9f, h - 0.1f, 2.83f);
            BevelBox(m, 0.49f, h + 0.3f, 0.4f, 3, 0.2f, 2.95f, 0.08f, LightStone);
            GableRoof(m, 0.28f, h + 0.51f, 0.18f, 3.45f, 3.3f, 1.2f, Teal);
            Arch(m, 1.46f, 0.35f, 3.295f, 1.08f, 1.15f, true);
            Window(m, 0.82f, 1.02f, 3.32f, 0.37f, 0.62f);
            Window(m, 2.84f, 1.02f, 3.32f, 0.37f, 0.62f);
            for (int i = 0; i < 2; i++)
            {
                float cx = i == 0 ? 0.56f : 3.46f, cz = i == 0 ? 3.28f : 0.59f;
                Frustum(m, cx, 0.32f, cz, 0.52f, 0.48f, h + 0.43f, 12, Stone);
                for (int n = 0; n < 4; n++) Frustum(m, cx, 0.45f + n * 0.52f, cz, 0.525f, 0.525f, 0.075f, 12, LightStone);
                Frustum(m, cx, h + 0.77f, cz, 0.67f, 0.05f, 1.1f, 12, Teal);
                Frustum(m, cx, h + 0.73f, cz, 0.7f, 0.7f, 0.09f, 12, Brass);
                Window(m, cx - 0.14f, 1.5f, cz + 0.49f, 0.28f, 0.45f);
            }
            Steps(m, 1.45f, 3.32f, 1.1f);
            Flag(m, 2, h + 1.5f, 1.3f, 1, GoldColor(level));
            if (level >= 2) { Flag(m, 0.65f, h + 1.6f, 3.25f, 0.55f, Red); Flag(m, 3.4f, h + 1.6f, 0.6f, 0.55f, Red); }
            if (level == 3) BevelBox(m, 1.68f, 1.74f, 3.35f, 0.6f, 0.42f, 0.08f, 0.06f, Brass);
            BevelBox(m, 1.7f, 0.95f, 3.38f, 0.6f, 0.78f, 0.045f, 0.035f, Teal);
            Box(m, 1.95f, 1.08f, 3.43f, 0.1f, 0.49f, 0.025f, Brass);
            Lantern(m, 1.25f, 1.1f, 3.4f); Lantern(m, 2.75f, 1.1f, 3.4f);
        }
        private static int GoldColor(int level) { return level >= 2 ? 0xF0C166 : 0xD3A254; }
        private static void Mine(ModelMesh m, int level)
        {
            BevelBox(m, 0.3f, 0.33f, 0.22f, 2.15f, 1.35f, 1.95f, 0.11f, Plaster);
            TimberFrame(m, 0.3f, 0.38f, 0.22f, 2.15f, 1.32f, 1.95f);
            GableRoof(m, 0.14f, 1.72f, 0.05f, 2.5f, 2.31f, 0.85f, Copper);
            Arch(m, 0.85f, 0.34f, 2.2f, 0.88f, 0.82f, true);
            Window(m, 0.38f, 0.95f, 2.24f, 0.3f, 0.44f);
            Box(m, 2.05f, 1.9f, 0.5f, 0.38f, 1.05f, 0.43f, DarkStone);
            BevelBox(m, 1.99f, 2.91f, 0.44f, 0.5f, 0.15f, 0.55f, 0.04f, Stone);
            Beam(m, P(1.03f, 0.38f, 2.2f), P(1.03f, 0.36f, 2.91f), 0.04f, Metal);
            Beam(m, P(1.62f, 0.38f, 2.2f), P(1.62f, 0.36f, 2.91f), 0.04f, Metal);
            BevelBox(m, 1.03f, 0.46f, 2.49f, 0.64f, 0.42f, 0.4f, 0.04f, Timber);
            for (int i = 0; i < 5; i++) BevelBox(m, 1.07f + i % 3 * 0.16f, 0.84f + i % 2 * 0.07f, 2.52f + i / 3 * 0.13f, 0.2f, 0.13f, 0.17f, 0.035f, Brass);
            Barrel(m, 2.58f, 0.35f, 2.55f, 0.24f, 0.55f);
            Lantern(m, 1.89f, 1.09f, 2.27f);
            if (level >= 2) GableRoof(m, 2.23f, 1.0f, 0.55f, 0.73f, 1.42f, 0.45f, Copper);
            if (level >= 3) Flag(m, 0.7f, 2.4f, 0.8f, 0.6f, Brass);
        }
        private static void Pool(ModelMesh m, int level)
        {
            Frustum(m, 1.5f, 0.32f, 1.5f, 1.27f, 1.24f, 0.43f, 16, LightStone);
            Frustum(m, 1.5f, 0.73f, 1.5f, 1.3f, 1.3f, 0.13f, 16, Brass);
            Frustum(m, 1.5f, 0.87f, 1.5f, 1.14f, 1.14f, 0.015f, 24, 0x3AB996);
            Frustum(m, 1.5f, 0.89f, 1.5f, 0.87f, 0.87f, 0.015f, 20, 0x74D6B4);
            for (int i = 0; i < 5; i++)
            {
                double angle = i * Math.PI * 2 / 5;
                float x = 1.5f + (float)Math.Cos(angle) * 0.48f, z = 1.5f + (float)Math.Sin(angle) * 0.48f;
                float h = 0.7f + (i % 3) * 0.27f + level * 0.1f;
                Frustum(m, x, 0.91f, z, 0.13f, 0.23f, h * 0.65f, 5, Crystal);
                Frustum(m, x, 0.91f + h * 0.65f, z, 0.23f, 0, h * 0.42f, 5, Tint(Crystal, 16));
            }
            for (int i = 0; i < 4; i++)
            {
                double a = i * Math.PI / 2 + Math.PI / 4;
                float x = 1.5f + (float)Math.Cos(a) * 1.05f, z = 1.5f + (float)Math.Sin(a) * 1.05f;
                BevelBox(m, x - 0.12f, 0.83f, z - 0.12f, 0.24f, 0.32f, 0.24f, 0.035f, DarkStone);
                Frustum(m, x, 1.16f, z, 0.13f, 0.06f, 0.24f, 6, Crystal);
            }
            if (level >= 2) Frustum(m, 1.5f, 0.98f, 1.5f, 0.3f, 0.2f, 1.3f, 6, Crystal);
            if (level >= 3) Frustum(m, 1.5f, 2.28f, 1.5f, 0.2f, 0, 0.6f, 6, 0xB9FCE0);
        }
        private static void Camp(ModelMesh m, int level)
        {
            BevelBox(m, 0.25f, 0.33f, 0.25f, 2.43f, 1.18f, 2.25f, 0.06f, Plaster);
            TimberFrame(m, 0.25f, 0.34f, 0.25f, 2.43f, 1.18f, 2.25f);
            GableRoof(m, 0.08f, 1.55f, 0.08f, 2.76f, 2.6f, 1.02f, Red);
            Arch(m, 1, 0.34f, 2.54f, 0.88f, 0.82f, true);
            Window(m, 0.38f, 0.92f, 2.54f, 0.35f, 0.45f);
            BevelBox(m, 1.1f, 1.52f, 2.66f, 0.62f, 0.53f, 0.07f, 0.06f, Brass);
            Beam(m, P(1.18f, 1.58f, 2.76f), P(1.57f, 1.97f, 2.76f), 0.055f, LightStone);
            Beam(m, P(1.57f, 1.58f, 2.78f), P(1.18f, 1.97f, 2.78f), 0.055f, LightStone);
            Barrel(m, 0.51f, 0.34f, 2.75f, 0.23f, 0.5f);
            Beam(m, P(2.38f, 0.39f, 2.72f), P(2.38f, 1.68f, 2.72f), 0.08f, Wood);
            Beam(m, P(2.67f, 0.4f, 2.72f), P(2.67f, 1.48f, 2.72f), 0.06f, Metal);
            Flag(m, 2.75f, 1.55f, 0.45f, 0.7f, Red);
            BevelBox(m, 0.22f, 0.36f, 2.52f, 0.62f, 0.13f, 0.46f, 0.04f, Wood);
            for (int i = 0; i < 3; i++) Beam(m, P(0.3f + i * 0.2f, 0.5f, 2.77f), P(0.34f + i * 0.2f, 1.18f, 2.77f), 0.025f, i == 1 ? LightStone : Metal);
            if (level >= 2) BevelBox(m, 2.11f, 0.4f, 2.56f, 0.31f, 0.56f, 0.08f, 0.04f, Teal);
            if (level >= 3) Flag(m, 0.35f, 2.05f, 0.4f, 0.7f, Brass);
        }
        private static void TrainingCamp(ModelMesh m, int level)
        {
            BevelBox(m, 0.3f, 0.34f, 0.3f, 2.4f, 0.95f, 2.35f, 0.07f, Wood);
            GableRoof(m, 0.13f, 1.32f, 0.12f, 2.73f, 2.73f, 0.87f, Teal);
            Arch(m, 1.07f, 0.34f, 2.68f, 0.86f, 0.83f, true);
            for (int i = 0; i < 3; i++)
            {
                Beam(m, P(0.47f + i * 0.86f, 0.39f, 2.8f), P(0.47f + i * 0.86f, 1.45f, 2.8f), 0.045f, Metal);
                BevelBox(m, 0.4f + i * 0.86f, 0.43f, 2.65f, 0.15f, 0.25f, 0.18f, 0.025f, Brass);
            }
            Flag(m, 2.55f, 2.1f, 0.56f, 0.62f, Teal);
            if (level >= 2) Flag(m, 0.46f, 2.1f, 0.56f, 0.55f, Brass);
            if (level >= 3) Frustum(m, 1.5f, 2.07f, 1.5f, 0.25f, 0, 0.48f, 8, Crystal);
        }
        private static void Laboratory(ModelMesh m, int level)
        {
            BevelBox(m, 0.38f, 0.34f, 0.38f, 2.24f, 1.45f, 2.24f, 0.09f, LightStone);
            GableRoof(m, 0.18f, 1.83f, 0.18f, 2.64f, 2.64f, 0.91f, Teal);
            Arch(m, 1.08f, 0.34f, 2.66f, 0.83f, 1.05f, true);
            Window(m, 0.55f, 1.03f, 2.67f, 0.34f, 0.49f);
            Window(m, 2.14f, 1.03f, 2.67f, 0.34f, 0.49f);
            Frustum(m, 1.5f, 2.42f, 1.5f, 0.39f, 0.17f, 0.76f, 8, Crystal);
            Frustum(m, 1.5f, 3.16f, 1.5f, 0.18f, 0, 0.42f, 8, Glow);
            for (int i = 0; i < 3; i++) Frustum(m, 0.61f + i * 0.88f, 0.37f, 0.61f, 0.13f, 0.05f, 0.55f + level * 0.13f, 6, Crystal);
        }
        private static void HeroHall(ModelMesh m, int level)
        {
            BevelBox(m, 0.25f, 0.34f, 0.25f, 2.5f, 1.25f, 2.5f, 0.1f, DarkStone);
            BevelBox(m, 0.42f, 1.57f, 0.42f, 2.16f, 0.34f, 2.16f, 0.08f, Brass);
            Frustum(m, 1.5f, 1.9f, 1.5f, 1.38f, 0.25f, 1.05f, 4, Red);
            Arch(m, 1.02f, 0.34f, 2.76f, 0.96f, 1.15f, true);
            Beam(m, P(0.48f, 0.42f, 2.72f), P(0.48f, 2.52f, 2.72f), 0.07f, Metal);
            Flag(m, 0.54f, 2.4f, 0.62f, 0.72f, level >= 3 ? Crystal : Red);
            for (int i = 0; i < level; i++) Frustum(m, 0.7f + i * 0.5f, 1.92f, 0.38f, 0.1f, 0.02f, 0.38f, 6, Glow);
        }
        private static void PetLodge(ModelMesh m, int level)
        {
            BevelBox(m, 0.3f, 0.34f, 0.35f, 2.4f, 1.05f, 2.25f, 0.12f, Wood);
            GableRoof(m, 0.16f, 1.38f, 0.2f, 2.68f, 2.55f, 0.76f, Red);
            Arch(m, 1.03f, 0.34f, 2.63f, 0.94f, 0.86f, true);
            Frustum(m, 1.5f, 1.62f, 2.63f, 0.28f, 0.18f, 0.16f, 8, Brass);
            for (int i = 0; i < level; i++) Frustum(m, 0.55f + i * 0.58f, 0.42f, 2.7f, 0.12f, 0.05f, 0.25f, 6, Glow);
            Beam(m, P(0.42f, 0.38f, 0.42f), P(0.92f, 0.7f, 0.42f), 0.07f, Timber);
            Beam(m, P(0.92f, 0.7f, 0.42f), P(1.25f, 0.36f, 0.42f), 0.07f, Timber);
        }
        private static void Cannon(ModelMesh m, int level)
        {
            Frustum(m, 1, 0.34f, 1, 0.83f, 0.77f, 0.45f, 12, Stone);
            Frustum(m, 1, 0.78f, 1, 0.86f, 0.86f, 0.13f, 12, LightStone);
            BevelBox(m, 0.4f, 0.92f, 0.49f, 1.2f, 0.24f, 1.05f, 0.05f, Wood);
            BarrelAlong(m, P(0.56f, 1.18f, 1.27f), P(1.13f, 1.62f, 0.23f), 0.27f + level * 0.025f, Metal);
            BarrelAlong(m, P(1.09f, 1.6f, 0.31f), P(1.17f, 1.65f, 0.13f), 0.33f + level * 0.025f, Brass);
            BarrelAlong(m, P(1.17f, 1.65f, 0.125f), P(1.18f, 1.655f, 0.1f), 0.245f, 0x263A43);
            BarrelAlong(m, P(0.2f, 0.72f, 1.02f), P(0.43f, 0.72f, 1.02f), 0.34f, Timber, 14);
            BarrelAlong(m, P(1.57f, 0.72f, 1.02f), P(1.8f, 0.72f, 1.02f), 0.34f, Timber, 14);
            BarrelAlong(m, P(0.15f, 0.72f, 1.02f), P(1.85f, 0.72f, 1.02f), 0.08f, Metal, 10);
            for (int i = 0; i < 3; i++) Frustum(m, 0.47f + i * 0.34f, 0.34f, 1.72f, 0.13f, 0.09f, 0.22f, 8, Metal);
            if (level >= 2) { BevelBox(m, 0.28f, 0.84f, 0.33f, 0.22f, 0.55f, 1.1f, 0.05f, Metal); BevelBox(m, 1.45f, 0.84f, 0.33f, 0.22f, 0.55f, 1.1f, 0.05f, Metal); }
        }
        private static void Tower(ModelMesh m, int level)
        {
            BevelBox(m, 0.37f, 0.34f, 0.37f, 1.26f, 1.38f, 1.26f, 0.12f, Stone);
            Masonry(m, 0.37f, 0.35f, 0.37f, 1.26f, 1.35f, 1.26f);
            float deck = 2.25f + level * 0.17f;
            for (int i = 0; i < 4; i++) Box(m, 0.36f + i % 2 * 1.15f, 1.65f, 0.36f + i / 2 * 1.15f, 0.14f, deck - 1.65f + 0.35f, 0.14f, Timber);
            Beam(m, P(0.45f, 1.72f, 1.67f), P(1.58f, deck, 1.67f), 0.08f, Wood);
            Beam(m, P(1.58f, 1.72f, 1.69f), P(0.45f, deck, 1.69f), 0.08f, Wood);
            BevelBox(m, 0.1f, deck, 0.1f, 1.8f, 0.21f, 1.8f, 0.07f, Wood);
            for (int i = 0; i < 4; i++) Box(m, 0.16f + i % 2 * 1.55f, deck, 0.16f + i / 2 * 1.55f, 0.11f, 0.83f, 0.11f, Timber);
            Box(m, 0.13f, deck + 0.48f, 1.76f, 1.7f, 0.14f, 0.1f, Wood);
            Box(m, 1.76f, deck + 0.48f, 0.13f, 0.1f, 0.14f, 1.7f, Wood);
            GableRoof(m, -0.04f, deck + 0.91f, -0.04f, 2.08f, 2.08f, 0.79f, Teal);
            Window(m, 0.84f, 0.9f, 1.65f, 0.31f, 0.43f);
            for (int i = 0; i < 6; i++) Box(m, 0.55f, 0.38f + i * 0.29f, 1.79f, 0.37f, 0.065f, 0.09f, Wood);
            if (level >= 2) Flag(m, 1, deck + 1.3f, 0.7f, 0.65f, Brass);
        }
        private static void Wall(ModelMesh m, int level)
        {
            float h = 0.73f + level * 0.12f;
            BevelBox(m, 0.12f, 0.31f, 0.12f, 0.76f, h, 0.76f, 0.045f, Stone);
            Masonry(m, 0.12f, 0.35f, 0.12f, 0.76f, h - 0.07f, 0.76f);
            BevelBox(m, 0.05f, h + 0.3f, 0.05f, 0.9f, 0.17f, 0.9f, 0.04f, LightStone);
            for (int i = 0; i < 2; i++) BevelBox(m, 0.08f + i * 0.56f, h + 0.46f, 0.08f, 0.28f, 0.27f, 0.83f, 0.025f, LightStone);
            if (level >= 2) Box(m, 0.16f, h * 0.5f + 0.34f, 0.9f, 0.68f, 0.09f, 0.035f, Teal);
        }
        private static void TimberFrame(ModelMesh m, float x, float y, float z, float sx, float h, float sz)
        {
            for (int i = 0; i <= 3; i++) Box(m, x + (sx - 0.1f) * i / 3, y, z + sz, 0.1f, h, 0.07f, Timber);
            Box(m, x, y + 0.1f, z + sz, sx, 0.12f, 0.08f, Timber);
            Box(m, x, y + h - 0.13f, z + sz, sx, 0.13f, 0.08f, Timber);
            Box(m, x + sx, y + h - 0.13f, z, 0.08f, 0.13f, sz, Timber);
            for (int i = 0; i < 3; i++) Box(m, x + sx, y, z + (sz - 0.1f) * i / 2, 0.08f, h, 0.1f, Timber);
        }
        private static void Masonry(ModelMesh m, float x, float y, float z, float sx, float h, float sz)
        {
            int rows = Math.Max(2, (int)(h / 0.29f)); float rowH = h / rows;
            for (int row = 0; row < rows; row++)
            {
                int cols = Math.Max(2, (int)(sx / 0.44f));
                for (int col = 0; col < cols; col++)
                {
                    float start = col * sx / cols + (row % 2) * 0.12f;
                    float width = Math.Min(sx - start - 0.02f, sx / cols - 0.035f); if (width < 0.04f) continue;
                    Box(m, x + start + 0.015f, y + row * rowH + 0.012f, z + sz + 0.012f, width, rowH - 0.025f, 0.018f, Tint(Stone, (col * 7 + row * 11) % 23 - 9));
                }
                cols = Math.Max(2, (int)(sz / 0.44f));
                for (int col = 0; col < cols; col++)
                    Box(m, x + sx + 0.012f, y + row * rowH + 0.012f, z + col * sz / cols + 0.02f, 0.018f, rowH - 0.025f, sz / cols - 0.035f, Tint(Stone, (col * 13 + row * 3) % 18 - 6));
            }
        }
        private static void GableRoof(ModelMesh m, float x, float y, float z, float sx, float sz, float h, int color)
        {
            // Sloped shingles are real individual quads; ridge and fascia provide silhouette.
            m.Face(Timber, P(0, 0, 1), P(x, y, z + sz), P(x + sx, y, z + sz), P(x + sx / 2, y + h, z + sz));
            m.Face(Timber, P(0, 0, -1), P(x, y, z), P(x + sx, y, z), P(x + sx / 2, y + h, z));
            int rows = 6, cols = Math.Max(5, (int)(sz / 0.29f));
            for (int side = 0; side < 2; side++)
            for (int row = 0; row < rows; row++)
            for (int col = 0; col < cols; col++)
            {
                float t0 = row / (float)rows, t1 = (row + 1.04f) / rows;
                float x0 = side == 0 ? x + sx * 0.5f * t0 : x + sx - sx * 0.5f * t0;
                float x1 = side == 0 ? x + sx * 0.5f * t1 : x + sx - sx * 0.5f * t1;
                float zz = z + col * sz / cols, dz = sz / cols - 0.016f;
                int rgb = Tint(color, (col * 13 + row * 7 + side * 5) % 23 - 6);
                m.Face(rgb, P(side == 0 ? -1 : 1, 1, 0), P(x0, y + h * t0 + 0.035f, zz), P(x1, y + h * t1 + 0.022f, zz), P(x1, y + h * t1 + 0.022f, zz + dz), P(x0, y + h * t0 + 0.035f, zz + dz));
            }
            Beam(m, P(x, y, z + sz + 0.03f), P(x + sx / 2, y + h, z + sz + 0.03f), 0.1f, LightStone);
            Beam(m, P(x + sx / 2, y + h, z + sz + 0.03f), P(x + sx, y, z + sz + 0.03f), 0.1f, LightStone);
            Beam(m, P(x + sx / 2, y + h + 0.06f, z - 0.08f), P(x + sx / 2, y + h + 0.06f, z + sz + 0.1f), 0.12f, Tint(color, 24));
        }
        private static void Window(ModelMesh m, float x, float y, float z, float w, float h)
        {
            Box(m, x - 0.045f, y - 0.055f, z, w + 0.09f, h + 0.11f, 0.065f, Timber);
            Box(m, x, y, z + 0.07f, w, h, 0.02f, Glow);
            Box(m, x + w * 0.47f, y, z + 0.1f, 0.04f, h, 0.015f, Timber);
            Box(m, x, y + h * 0.5f, z + 0.1f, w, 0.04f, 0.02f, Timber);
            Box(m, x - 0.075f, y - 0.09f, z + 0.02f, w + 0.15f, 0.075f, 0.12f, LightStone);
        }
        private static void Arch(ModelMesh m, float x, float y, float z, float w, float h, bool door)
        {
            Box(m, x, y, z, w, h, 0.06f, door ? Timber : Metal);
            for (int i = 0; i < 6; i++) Box(m, x + i * w / 6 + 0.012f, y, z + 0.061f, w / 6 - 0.025f, h, 0.015f, Tint(Wood, -18 + i % 2 * 8));
            Box(m, x - 0.13f, y, z - 0.01f, 0.13f, h, 0.14f, LightStone);
            Box(m, x + w, y, z - 0.01f, 0.13f, h, 0.14f, LightStone);
            float r = w / 2;
            for (int i = 0; i < 9; i++)
            {
                double a = i * Math.PI / 9, b = (i + 1) * Math.PI / 9;
                ModelPoint center = P(x + r, y + h, z + 0.1f);
                m.Face(Timber, P(0, 0, 1), center, P(x + r + (float)Math.Cos(a) * r, y + h + (float)Math.Sin(a) * r, z + 0.1f), P(x + r + (float)Math.Cos(b) * r, y + h + (float)Math.Sin(b) * r, z + 0.1f));
                m.Face(Tint(LightStone, i % 3 * 5 - 5), P(0, 0, 1), P(x + r + (float)Math.Cos(a) * r, y + h + (float)Math.Sin(a) * r, z + 0.12f), P(x + r + (float)Math.Cos(a) * (r + 0.13f), y + h + (float)Math.Sin(a) * (r + 0.13f), z + 0.12f), P(x + r + (float)Math.Cos(b) * (r + 0.13f), y + h + (float)Math.Sin(b) * (r + 0.13f), z + 0.12f), P(x + r + (float)Math.Cos(b) * r, y + h + (float)Math.Sin(b) * r, z + 0.12f));
            }
            Box(m, x + 0.06f, y + h * 0.31f, z + 0.09f, w - 0.12f, 0.06f, 0.02f, Metal);
            Box(m, x + 0.06f, y + h * 0.75f, z + 0.09f, w - 0.12f, 0.06f, 0.02f, Metal);
        }
        private static void Steps(ModelMesh m, float x, float z, float w)
        { for (int i = 0; i < 3; i++) BevelBox(m, x, 0, z + i * 0.16f, w, 0.32f - i * 0.085f, 0.2f, 0.025f, Stone); }
        private static void Lantern(ModelMesh m, float x, float y, float z)
        {
            Box(m, x - 0.07f, y, z, 0.14f, 0.23f, 0.12f, Glow);
            Box(m, x - 0.1f, y + 0.23f, z - 0.01f, 0.2f, 0.05f, 0.16f, Metal);
            Box(m, x - 0.1f, y - 0.04f, z - 0.01f, 0.2f, 0.05f, 0.16f, Metal);
        }
        private static void Flag(ModelMesh m, float x, float y, float z, float s, int color)
        {
            Beam(m, P(x, y, z), P(x, y + 1.35f * s, z), 0.045f, Timber);
            m.Face(color, P(0, 0, 1), P(x, y + 1.24f * s, z), P(x + 0.75f * s, y + 1.12f * s, z + 0.09f), P(x + 0.57f * s, y + 0.86f * s, z + 0.15f), P(x, y + 0.81f * s, z));
            m.Face(color, P(0, 0, -1), P(x, y + 1.24f * s, z), P(x + 0.75f * s, y + 1.12f * s, z + 0.09f), P(x + 0.57f * s, y + 0.86f * s, z + 0.15f), P(x, y + 0.81f * s, z));
        }
        private static void Barrel(ModelMesh m, float x, float y, float z, float r, float h)
        {
            Frustum(m, x, y, z, r * 0.88f, r, h / 2, 10, Wood);
            Frustum(m, x, y + h / 2, z, r, r * 0.88f, h / 2, 10, Wood);
            Frustum(m, x, y + h * 0.15f, z, r * 0.96f, r * 0.99f, 0.04f, 10, Metal);
            Frustum(m, x, y + h * 0.78f, z, r * 0.99f, r * 0.96f, 0.04f, 10, Metal);
        }
        public static void Box(ModelMesh m, float x, float y, float z, float sx, float sy, float sz, int color)
        {
            ModelPoint a = P(x, y, z), b = P(x + sx, y, z), c = P(x + sx, y, z + sz), d = P(x, y, z + sz), up = P(0, sy, 0);
            m.Face(color, P(0, 1, 0), a + up, b + up, c + up, d + up);
            m.Face(color, P(0, -1, 0), a, b, c, d);
            m.Face(color, P(0, 0, -1), a, b, b + up, a + up);
            m.Face(color, P(1, 0, 0), b, c, c + up, b + up);
            m.Face(color, P(0, 0, 1), c, d, d + up, c + up);
            m.Face(color, P(-1, 0, 0), d, a, a + up, d + up);
        }
        private static void BevelBox(ModelMesh m, float x, float y, float z, float sx, float sy, float sz, float bevel, int color)
        {
            float b = Math.Min(bevel, Math.Min(sy / 3, Math.Min(sx / 3, sz / 3)));
            ModelPoint[] ring = { P(x + b, y, z), P(x + sx - b, y, z), P(x + sx, y, z + b), P(x + sx, y, z + sz - b), P(x + sx - b, y, z + sz), P(x + b, y, z + sz), P(x, y, z + sz - b), P(x, y, z + b) };
            ModelPoint[] top = new ModelPoint[8];
            for (int i = 0; i < 8; i++)
            {
                ModelPoint p = ring[i]; float cx = x + sx / 2, cz = z + sz / 2;
                top[i] = P(p.X + (cx - p.X) * b / sx, y + sy, p.Z + (cz - p.Z) * b / sz);
                ModelPoint next = ring[(i + 1) % 8], outward = P((p.X + next.X) / 2 - cx, 0, (p.Z + next.Z) / 2 - cz);
                m.Face(color, outward, p, next, P(next.X, y + sy - b, next.Z), P(p.X, y + sy - b, p.Z));
            }
            for (int i = 0; i < 8; i++)
            { ModelPoint a = ring[i], c = ring[(i + 1) % 8]; m.Face(Tint(color, 6), P(a.X - x - sx / 2, 1, a.Z - z - sz / 2), P(a.X, y + sy - b, a.Z), P(c.X, y + sy - b, c.Z), top[(i + 1) % 8], top[i]); }
            m.Face(color, P(0, 1, 0), top);
        }
        private static void Frustum(ModelMesh m, float x, float y, float z, float r0, float r1, float h, int sides, int color)
        {
            ModelPoint[] top = new ModelPoint[sides];
            for (int i = 0; i < sides; i++)
            {
                double a = i * Math.PI * 2 / sides, b = (i + 1) * Math.PI * 2 / sides;
                ModelPoint p = P(x + (float)Math.Cos(a) * r0, y, z + (float)Math.Sin(a) * r0), q = P(x + (float)Math.Cos(b) * r0, y, z + (float)Math.Sin(b) * r0);
                ModelPoint t = P(x + (float)Math.Cos(a) * r1, y + h, z + (float)Math.Sin(a) * r1), u = P(x + (float)Math.Cos(b) * r1, y + h, z + (float)Math.Sin(b) * r1);
                ModelPoint n = P((float)Math.Cos((a + b) / 2), 0, (float)Math.Sin((a + b) / 2));
                if (r1 == 0) m.Face(color, n, p, q, t); else m.Face(color, n, p, q, u, t);
                top[i] = t;
            }
            if (r1 > 0) m.Face(color, P(0, 1, 0), top);
        }
        private static void Beam(ModelMesh m, ModelPoint a, ModelPoint b, float width, int color)
        { BarrelAlong(m, a, b, width / 2, color, 4); }
        private static void BarrelAlong(ModelMesh m, ModelPoint a, ModelPoint b, float radius, int color, int sides = 12)
        {
            ModelPoint axis = (b - a).Normalized();
            ModelPoint u = ModelPoint.Cross(axis, Math.Abs(axis.Y) > 0.95f ? P(1, 0, 0) : P(0, 1, 0)).Normalized();
            ModelPoint v = ModelPoint.Cross(axis, u).Normalized();
            ModelPoint[] cap = new ModelPoint[sides];
            for (int i = 0; i < sides; i++)
            {
                double p = i * Math.PI * 2 / sides, q = (i + 1) * Math.PI * 2 / sides;
                ModelPoint r = u * ((float)Math.Cos(p) * radius) + v * ((float)Math.Sin(p) * radius), s = u * ((float)Math.Cos(q) * radius) + v * ((float)Math.Sin(q) * radius);
                m.Face(color, r + s, a + r, a + s, b + s, b + r); cap[i] = b + r;
            }
            m.Face(color, axis, cap);
        }
    }
}
