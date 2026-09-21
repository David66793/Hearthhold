using System.Collections.Generic;
using Hearthhold.Core;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

namespace Hearthhold.UnityClient
{
    // Unity prefers the bright KayKit CC0 models and retains shared procedural geometry as a safe fallback.
    public sealed class ModelViews
    {
        private readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        private readonly Dictionary<Texture2D, Material> spriteMaterials = new Dictionary<Texture2D, Material>();
        private readonly Dictionary<Texture2D, Material> externalTextureMaterials = new Dictionary<Texture2D, Material>();
        private readonly HashSet<string> loadedExternalResources = new HashSet<string>();
        private readonly Dictionary<int, Material> levelMaterials = new Dictionary<int, Material>();
        private Material material, contactShadowMaterial;
        private Mesh contactShadowMesh;
        public GameObject Building(Building building, Transform parent)
        {
            string key = "building:" + building.Kind + ":" + building.Level;
            // The third-party straight-wall FBX has a paper-thin side silhouette. Use the solid 3D masonry segment
            // on both map axes so opposite isometric directions share the same height and thickness.
            GameObject root = building.Kind == BuildingKind.Wall ? null : CreateExternal(key, BuildingArt(building.Kind), building.Spec.Size * 0.98f,
                new Vector3(building.Spec.Size * 0.5f, 0, building.Spec.Size * 0.5f), parent);
            if (root == null) root = Create(key, ModelFactory.Building(building.Kind, building.Level), parent);
            else ApplyExternalTexture(root, "ThirdParty/KayKitMedieval/KayKitMedieval_Texture");
            AddBuildingLevelArt(root, building.Kind, building.Level);
            if (building.Kind == BuildingKind.Wall) AlignWallToCell(root);
            if (building.Kind == BuildingKind.ArcTower) AddStormMechanism(root);
            root.name = building.Spec.Name + " #" + building.Id;
            root.transform.position = new Vector3(building.X, 0, building.Z);
            Bounds bounds = LocalBounds(root.transform);
            BoxCollider collider = root.AddComponent<BoxCollider>(); collider.center = bounds.center; collider.size = bounds.size;
            root.AddComponent<BuildingHandle>().Id = building.Id;
            AddContactShadow(root, building.Spec.Size * 0.64f, building.Spec.Size * 0.48f,
                new Vector3(building.Spec.Size * 0.5f, 0.018f, building.Spec.Size * 0.5f));
            root.AddComponent<ModelActionAnimator>().Visual = root.transform.Find("3D model");
            if (building.Kind == BuildingKind.ArcTower) root.AddComponent<ArticulatedModelAnimator>().ConfigureStormTower();
            return root;
        }
        public GameObject Troop(Unit unit, Transform parent, int level = 1)
        {
            GameObject root = unit.IsPet ? Create("pet:" + unit.PetKind, ModelFactory.Pet(unit.PetKind), parent)
                : CreateExternal("troop:" + unit.Kind, TroopArt(unit.Kind), 1.38f, Vector3.zero, parent);
            bool authoredTroop = !unit.IsPet && root != null && UsesAuthoredRig(unit.Kind);
            if (root == null) root = Create("troop:" + unit.Kind, ModelFactory.Troop(unit.Kind), parent);
            else if (authoredTroop) ApplyRpgTextures(root, unit.Kind);
            else if (!unit.IsPet) ApplyExternalTexture(root, TroopTexture(unit.Kind));
            Bounds actorBounds = LocalBounds(root.transform);
            if (!unit.IsPet && !authoredTroop) AddTroopRoleArt(root, unit.Kind);
            if (!unit.IsPet && authoredTroop) AddAuthoredTroopLevelArt(root, unit.Kind, level, actorBounds);
            else if (!unit.IsPet) AddTroopLevelArt(root, unit.Kind, level, actorBounds);
            if (unit.IsHero)
            {
                root.transform.localScale *= 1.32f;
                Ornament(root, "Hero mantle", PrimitiveType.Cube, new Vector3(0, actorBounds.min.y + actorBounds.size.y * 0.58f, -0.18f), new Vector3(0.86f, 0.68f, 0.12f), 0x8F352F);
                Ornament(root, "Hero crest", PrimitiveType.Sphere, new Vector3(0, actorBounds.max.y + 0.13f, 0), new Vector3(0.28f, 0.18f, 0.28f), 0xFFD06A);
                Ornament(root, "Hero core", PrimitiveType.Sphere, new Vector3(0, actorBounds.min.y + actorBounds.size.y * 0.62f, actorBounds.max.z + 0.05f), Vector3.one * 0.17f, 0xFF8B3D);
            }
            if (unit.IsPet && level >= 2) Ornament(root, "Pet level crest", PrimitiveType.Sphere, new Vector3(0, actorBounds.max.y + 0.08f, 0), Vector3.one * (0.08f + level * 0.02f), 0xFFD06A);
            root.name = unit.Spec.Name;
            AddContactShadow(root, 0.7f, 0.48f, new Vector3(0, 0.018f, 0));
            root.AddComponent<ModelActionAnimator>().Visual = root.transform.Find("3D model");
            if (authoredTroop) root.AddComponent<ImportedClipAnimator>().Configure(TroopArt(unit.Kind), unit.Kind);
            else if (unit.Kind == TroopKind.Guardian || unit.Kind == TroopKind.SkyRider || unit.Kind == TroopKind.Alchemist
                || unit.Kind == TroopKind.Medic || unit.Kind == TroopKind.Summoner)
                root.AddComponent<ArticulatedModelAnimator>().ConfigureTroop(unit.Kind);
            return root;
        }
        public GameObject BuildingPreview(BuildingKind kind, int level, Transform parent)
        {
            GameObject root = kind == BuildingKind.Wall ? null : CreateExternal("building:" + kind + ":" + level, BuildingArt(kind), Rules.Spec(kind).Size * 0.98f,
                new Vector3(Rules.Spec(kind).Size * 0.5f, 0, Rules.Spec(kind).Size * 0.5f), parent);
            if (root == null) root = Create("building:" + kind + ":" + level, ModelFactory.Building(kind, level), parent);
            else ApplyExternalTexture(root, "ThirdParty/KayKitMedieval/KayKitMedieval_Texture");
            AddBuildingLevelArt(root, kind, level);
            if (kind == BuildingKind.Wall) AlignWallToCell(root);
            if (kind == BuildingKind.ArcTower) AddStormMechanism(root);
            if (kind == BuildingKind.ArcTower)
            {
                root.AddComponent<ModelActionAnimator>().Visual = root.transform.Find("3D model");
                root.AddComponent<ArticulatedModelAnimator>().ConfigureStormTower();
            }
            root.name = Rules.Spec(kind).Name + " preview";
            return root;
        }
        public GameObject TroopPreview(TroopKind kind, int level, Transform parent)
        {
            Unit unit = new Unit { Kind = kind };
            return Troop(unit, parent, level);
        }
        public GameObject HeroPreview(HeroKind kind, int level, Transform parent)
        {
            Unit unit = new Unit { IsHero = true, HeroKind = kind, HeroLevel = level, Kind = TroopKind.Guardian };
            return Troop(unit, parent, level);
        }
        public GameObject PetPreview(PetKind kind, int level, Transform parent)
        {
            Unit unit = new Unit { IsPet = true, PetKind = kind, PetLevel = level, Kind = TroopKind.Sapper };
            return Troop(unit, parent, level);
        }
        // Align the long side of a straight wall with its neighbours, independent of the imported FBX axis.
        public void OrientWall(GameObject root, bool alongX)
        {
            Transform visual = root != null ? root.transform.Find("3D model") : null;
            if (visual == null) return;
            Bounds before = BoundsRelativeTo(root.transform, visual);
            bool modelAlongX = before.size.x >= before.size.z;
            visual.localRotation = Quaternion.Euler(0, modelAlongX == alongX ? 0 : 90, 0);
            // Recenter the actual 3D model on the logical 1x1 cell after rotation. The old
            // calculation included the stationary contact-shadow renderer, which diluted the
            // correction and left opposite wall axes with different visual offsets.
            Bounds after = BoundsRelativeTo(root.transform, visual);
            visual.localPosition += new Vector3(0.5f - after.center.x, 0, 0.5f - after.center.z);
            BoxCollider collider = root.GetComponent<BoxCollider>();
            if (collider != null) { Bounds fitted = BoundsRelativeTo(root.transform, visual); collider.center = fitted.center; collider.size = fitted.size; }
        }
        private static void AlignWallToCell(GameObject root)
        {
            Transform visual = root != null ? root.transform.Find("3D model") : null;
            if (visual == null) return;
            Bounds bounds = LocalBounds(root.transform);
            visual.localPosition += new Vector3(0.5f - bounds.center.x, -bounds.min.y, 0.5f - bounds.center.z);
        }
        private Material LevelMaterial(int color)
        {
            Material result;
            if (levelMaterials.TryGetValue(color, out result)) return result;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            result = new Material(shader) { name = "Upgrade ornament " + color.ToString("X6"), color = new Color32((byte)(color >> 16), (byte)(color >> 8), (byte)color, 255) };
            levelMaterials.Add(color, result);
            return result;
        }
        private void Ornament(GameObject root, string name, PrimitiveType shape, Vector3 position, Vector3 scale, int color)
        { Ornament(root, name, shape, position, scale, color, Vector3.zero); }
        private void Ornament(GameObject root, string name, PrimitiveType shape, Vector3 position, Vector3 scale, int color, Vector3 euler)
        {
            GameObject piece = GameObject.CreatePrimitive(shape);
            piece.name = name;
            piece.transform.SetParent(root.transform, false);
            piece.transform.localPosition = position;
            piece.transform.localRotation = Quaternion.Euler(euler);
            piece.transform.localScale = scale;
            Collider collider = piece.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            piece.GetComponent<Renderer>().sharedMaterial = LevelMaterial(color);
            Transform visual = root.transform.Find("3D model");
            if (visual != null) piece.transform.SetParent(visual, true);
        }
        private static void GroupOrnaments(GameObject root, string groupName, params string[] names)
        {
            Transform visual = root.transform.Find("3D model");
            if (visual == null || names.Length == 0) return;
            Transform first = visual.Find(names[0]);
            if (first == null) return;
            GameObject group = new GameObject(groupName);
            group.transform.SetParent(visual, false);
            group.transform.position = first.position;
            foreach (string name in names)
            {
                Transform ornament = visual.Find(name);
                if (ornament != null) ornament.SetParent(group.transform, true);
            }
        }
        private void AddSkyWing(GameObject root, int side, float y)
        {
            string key = side < 0 ? "sky-wing-left" : "sky-wing-right";
            Mesh wingMesh;
            if (!meshes.TryGetValue(key, out wingMesh))
            {
                // Two-sided, scalloped silhouette: each outer point reads as a feather rather than a flat box.
                Vector3[] outline = {
                    new Vector3(0, 0, 0), new Vector3(side * 0.34f, 0.10f, -0.04f),
                    new Vector3(side * 1.15f, 0.19f, -0.20f), new Vector3(side * 1.03f, -0.09f, -0.45f),
                    new Vector3(side * 0.78f, -0.21f, -0.67f), new Vector3(side * 0.52f, -0.15f, -0.54f),
                    new Vector3(side * 0.22f, -0.11f, -0.34f)
                };
                Vector3[] vertices = new Vector3[outline.Length * 2];
                for (int i = 0; i < outline.Length; i++) { vertices[i] = outline[i]; vertices[i + outline.Length] = outline[i]; }
                int[] triangles = new int[(outline.Length - 2) * 6];
                for (int i = 0; i < outline.Length - 2; i++)
                {
                    int offset = i * 6;
                    triangles[offset] = 0; triangles[offset + 1] = i + 1; triangles[offset + 2] = i + 2;
                    triangles[offset + 3] = outline.Length; triangles[offset + 4] = outline.Length + i + 2; triangles[offset + 5] = outline.Length + i + 1;
                }
                wingMesh = new Mesh { name = key, vertices = vertices, triangles = triangles };
                wingMesh.RecalculateNormals(); wingMesh.RecalculateBounds();
                meshes.Add(key, wingMesh);
            }
            GameObject wing = new GameObject(side < 0 ? "Sky rider left wing joint" : "Sky rider right wing joint");
            wing.transform.SetParent(root.transform, false);
            wing.transform.localPosition = new Vector3(side * 0.25f, y, -0.25f);
            wing.AddComponent<MeshFilter>().sharedMesh = wingMesh;
            MeshRenderer renderer = wing.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = LevelMaterial(0x5EACC2);
            renderer.shadowCastingMode = ShadowCastingMode.On;
            Transform visual = root.transform.Find("3D model");
            if (visual != null) wing.transform.SetParent(visual, true);
        }
        private void AddGuardianShield(GameObject root, float y)
        {
            Mesh shieldMesh;
            if (!meshes.TryGetValue("guardian-shield", out shieldMesh))
            {
                Vector2[] edge = {
                    new Vector2(0, 0.56f), new Vector2(0.27f, 0.38f), new Vector2(0.24f, -0.30f),
                    new Vector2(0, -0.58f), new Vector2(-0.24f, -0.30f), new Vector2(-0.27f, 0.38f)
                };
                Vector3[] vertices = new Vector3[14];
                vertices[0] = new Vector3(0, 0, 0.15f);
                vertices[7] = new Vector3(0, 0, -0.12f);
                for (int i = 0; i < edge.Length; i++)
                {
                    vertices[1 + i] = new Vector3(edge[i].x, edge[i].y, 0.04f);
                    vertices[8 + i] = new Vector3(edge[i].x, edge[i].y, -0.09f);
                }
                List<int> triangles = new List<int>();
                for (int i = 0; i < edge.Length; i++)
                {
                    int next = (i + 1) % edge.Length;
                    triangles.AddRange(new[] { 0, 1 + next, 1 + i, 7, 8 + i, 8 + next,
                        1 + i, 1 + next, 8 + next, 1 + i, 8 + next, 8 + i });
                }
                shieldMesh = new Mesh { name = "guardian-shield", vertices = vertices, triangles = triangles.ToArray() };
                shieldMesh.RecalculateNormals(); shieldMesh.RecalculateBounds();
                meshes.Add("guardian-shield", shieldMesh);
            }
            GameObject shield = new GameObject("Guardian shield joint");
            shield.transform.SetParent(root.transform, false);
            shield.transform.localPosition = new Vector3(-0.55f, y, 0.33f);
            shield.AddComponent<MeshFilter>().sharedMesh = shieldMesh;
            MeshRenderer renderer = shield.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = LevelMaterial(0x526D7E);
            renderer.shadowCastingMode = ShadowCastingMode.On;
            Transform visual = root.transform.Find("3D model");
            if (visual != null) shield.transform.SetParent(visual, true);
            Ornament(root, "Guardian shield boss", PrimitiveType.Sphere,
                new Vector3(-0.55f, y, 0.49f), Vector3.one * 0.19f, 0xD6AD65);
            Transform boss = visual != null ? visual.Find("Guardian shield boss") : null;
            if (boss != null) boss.SetParent(shield.transform, true);
        }
        private void AddStormMechanism(GameObject root)
        {
            Bounds b = LocalBounds(root.transform);
            GameObject rotor = new GameObject("Storm rotor");
            rotor.transform.SetParent(root.transform, false);
            rotor.transform.localPosition = new Vector3(b.center.x, b.max.y + 0.16f, b.center.z);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f;
                GameObject prong = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                prong.name = "Copper induction arm " + i;
                prong.transform.SetParent(rotor.transform, false);
                prong.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.36f, 0.12f, Mathf.Sin(angle) * 0.36f);
                prong.transform.localRotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 55);
                prong.transform.localScale = new Vector3(0.085f, 0.38f, 0.085f);
                Object.Destroy(prong.GetComponent<Collider>());
                prong.GetComponent<Renderer>().sharedMaterial = LevelMaterial(0xC79D62);
                GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tip.name = "Arc terminal " + i;
                tip.transform.SetParent(rotor.transform, false);
                tip.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.72f, 0.36f, Mathf.Sin(angle) * 0.72f);
                tip.transform.localScale = Vector3.one * 0.21f;
                Object.Destroy(tip.GetComponent<Collider>());
                tip.GetComponent<Renderer>().sharedMaterial = LevelMaterial(0x77DDE9);
            }
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Storm charge core";
            core.transform.SetParent(rotor.transform, false);
            core.transform.localPosition = new Vector3(0, 0.35f, 0);
            core.transform.localScale = Vector3.one * 0.55f;
            Object.Destroy(core.GetComponent<Collider>());
            core.GetComponent<Renderer>().sharedMaterial = LevelMaterial(0xA4F0F3);
        }
        private void AddTroopRoleArt(GameObject root, TroopKind kind)
        {
            Bounds b = LocalBounds(root.transform);
            float floor = b.min.y, h = b.size.y;
            if (kind == TroopKind.Vanguard)
            {
                Ornament(root, "Vanguard shield boss", PrimitiveType.Sphere, new Vector3(-0.46f, floor + h * 0.47f, 0.27f), new Vector3(0.35f, 0.35f, 0.17f), 0xC7934A);
                Ornament(root, "Vanguard helm crest", PrimitiveType.Cube, new Vector3(0, floor + h * 0.99f, -0.05f), new Vector3(0.12f, 0.3f, 0.42f), 0xB96349);
                return;
            }
            if (kind == TroopKind.Ranger)
            {
                Ornament(root, "Ranger quiver", PrimitiveType.Cylinder, new Vector3(-0.36f, floor + h * 0.58f, -0.32f), new Vector3(0.16f, 0.30f, 0.16f), 0x76543B, new Vector3(0, 0, -17));
                for (int i = -1; i <= 1; i++) Ornament(root, "Ranger visible arrow", PrimitiveType.Cylinder, new Vector3(-0.36f + i * 0.08f, floor + h * 0.88f, -0.34f), new Vector3(0.015f, 0.21f, 0.015f), 0xDEC89B);
                return;
            }
            if (kind == TroopKind.Guardian)
            {
                AddGuardianShield(root, floor + h * 0.46f);
                return;
            }
            if (kind == TroopKind.Sapper)
            {
                Ornament(root, "Sapper powder keg", PrimitiveType.Cylinder, new Vector3(-0.43f, floor + h * 0.42f, -0.33f), new Vector3(0.31f, 0.36f, 0.31f), 0x9F6441);
                Ornament(root, "Sapper keg metal band", PrimitiveType.Cylinder, new Vector3(-0.43f, floor + h * 0.42f + 0.24f, -0.33f), new Vector3(0.34f, 0.045f, 0.34f), 0x465258);
                Ornament(root, "Sapper fuse", PrimitiveType.Cylinder, new Vector3(-0.43f, floor + h * 0.42f + 0.43f, -0.33f), new Vector3(0.025f, 0.13f, 0.025f), 0xF1B965);
                return;
            }
            if (kind == TroopKind.SkyRider)
            {
                AddSkyWing(root, -1, floor + h * 0.66f);
                AddSkyWing(root, 1, floor + h * 0.66f);
                Ornament(root, "Sky rider flight crystal", PrimitiveType.Sphere, new Vector3(0, floor + h * 0.63f, -0.42f), Vector3.one * 0.33f, 0x7FDCE9);
                GroupOrnaments(root, "Sky rider crystal joint", "Sky rider flight crystal");
                return;
            }
            if (kind == TroopKind.Alchemist)
            {
                Tint(root, new Color(0.83f, 0.68f, 0.93f));
                Ornament(root, "Alchemist leather potion rack", PrimitiveType.Cube, new Vector3(0, floor + h * 0.53f, -0.42f), new Vector3(0.77f, 0.78f, 0.2f), 0x704633);
                Ornament(root, "Alchemist rack brass rim", PrimitiveType.Cube, new Vector3(0, floor + h * 0.72f, -0.43f), new Vector3(0.84f, 0.10f, 0.24f), 0xB98D50);
                for (int i = -1; i <= 1; i++)
                {
                    float x = i * 0.24f;
                    Ornament(root, "Alchemist glass vial", PrimitiveType.Cylinder, new Vector3(x, floor + h * 0.76f, -0.48f), new Vector3(0.105f, 0.21f, 0.105f), i < 0 ? 0x52CDB4 : i > 0 ? 0xF4A353 : 0xA477E8);
                    Ornament(root, "Alchemist vial cork", PrimitiveType.Cylinder, new Vector3(x, floor + h * 0.76f + 0.23f, -0.48f), new Vector3(0.064f, 0.055f, 0.064f), 0x765D3D);
                }
                Ornament(root, "Alchemist throwing bomb", PrimitiveType.Sphere, new Vector3(0.55f, floor + h * 0.40f, 0.24f), Vector3.one * 0.43f, 0xDA7D42);
                Ornament(root, "Alchemist bomb fuse", PrimitiveType.Cylinder, new Vector3(0.55f, floor + h * 0.40f + 0.25f, 0.24f), new Vector3(0.045f, 0.12f, 0.045f), 0xEBD39A);
                GroupOrnaments(root, "Alchemist bomb joint", "Alchemist throwing bomb", "Alchemist bomb fuse");
            }
            else if (kind == TroopKind.Medic)
            {
                Tint(root, new Color(0.93f, 1f, 0.89f));
                Ornament(root, "Medic split mantle", PrimitiveType.Cylinder, new Vector3(0, floor + h * 0.24f, 0), new Vector3(0.48f, 0.32f, 0.45f), 0xF3F0DB);
                Ornament(root, "Medic fitted white tunic", PrimitiveType.Cylinder, new Vector3(0, floor + h * 0.51f, 0), new Vector3(0.37f, 0.34f, 0.31f), 0xF3F0DB);
                Ornament(root, "Medic white tabard", PrimitiveType.Cube, new Vector3(0, floor + h * 0.43f, 0.31f), new Vector3(0.39f, 0.70f, 0.07f), 0xF4F2DE);
                Ornament(root, "Medic red cross upright", PrimitiveType.Cube, new Vector3(0, floor + h * 0.46f, 0.37f), new Vector3(0.12f, 0.43f, 0.055f), 0xC3474A);
                Ornament(root, "Medic red cross bar", PrimitiveType.Cube, new Vector3(0, floor + h * 0.46f, 0.40f), new Vector3(0.38f, 0.11f, 0.055f), 0xC3474A);
                Ornament(root, "Medic rear cross upright", PrimitiveType.Cube, new Vector3(0, floor + h * 0.46f, -0.30f), new Vector3(0.12f, 0.43f, 0.055f), 0xC3474A);
                Ornament(root, "Medic rear cross bar", PrimitiveType.Cube, new Vector3(0, floor + h * 0.46f, -0.33f), new Vector3(0.38f, 0.11f, 0.055f), 0xC3474A);
                for (int side = -1; side <= 1; side += 2)
                {
                    Ornament(root, "Medic side cross upright", PrimitiveType.Cube, new Vector3(side * 0.38f, floor + h * 0.50f, 0), new Vector3(0.065f, 0.35f, 0.11f), 0xC3474A);
                    Ornament(root, "Medic side cross bar", PrimitiveType.Cube, new Vector3(side * 0.41f, floor + h * 0.50f, 0), new Vector3(0.065f, 0.10f, 0.33f), 0xC3474A);
                }
                Ornament(root, "Medic healing satchel", PrimitiveType.Cube, new Vector3(-0.52f, floor + h * 0.36f, 0.13f), new Vector3(0.33f, 0.42f, 0.35f), 0xEFE1BA);
                Ornament(root, "Medic brass belt", PrimitiveType.Cylinder, new Vector3(0, floor + h * 0.36f, 0), new Vector3(0.38f, 0.045f, 0.34f), 0xC7A66D);
                Ornament(root, "Medic satchel clasp", PrimitiveType.Sphere, new Vector3(-0.56f, floor + h * 0.43f, 0.34f), Vector3.one * 0.11f, 0x68D8C8);
                Ornament(root, "Medic staff shaft", PrimitiveType.Cylinder, new Vector3(0.62f, floor + h * 0.56f, 0.05f), new Vector3(0.055f, h * 0.46f, 0.055f), 0xCFB984);
                Ornament(root, "Medic staff crystal", PrimitiveType.Sphere, new Vector3(0.62f, floor + h * 1.02f, 0.05f), Vector3.one * 0.36f, 0x6CE6D3);
                Ornament(root, "Medic staff collar", PrimitiveType.Cylinder, new Vector3(0.62f, floor + h * 0.88f, 0.05f), new Vector3(0.12f, 0.055f, 0.12f), 0xD9B86F);
                GroupOrnaments(root, "Medic staff joint", "Medic staff shaft", "Medic staff crystal", "Medic staff collar");
            }
            else
            {
                Tint(root, new Color(0.61f, 0.55f, 0.85f));
                Ornament(root, "Summoner shadow mantle", PrimitiveType.Cylinder, new Vector3(0, floor + h * 0.25f, 0), new Vector3(0.45f, 0.34f, 0.42f), 0x45365D);
                Ornament(root, "Summoner fitted vest", PrimitiveType.Cylinder, new Vector3(0, floor + h * 0.52f, 0), new Vector3(0.35f, 0.36f, 0.31f), 0x45365D);
                Ornament(root, "Summoner hood silhouette", PrimitiveType.Sphere, new Vector3(0, floor + h * 0.84f, -0.08f), new Vector3(0.49f, 0.38f, 0.49f), 0x46345F);
                Ornament(root, "Summoner left horn", PrimitiveType.Cylinder, new Vector3(-0.29f, floor + h * 1.04f, -0.03f), new Vector3(0.085f, 0.29f, 0.085f), 0xC8B5D9, new Vector3(0, 0, -22));
                Ornament(root, "Summoner right horn", PrimitiveType.Cylinder, new Vector3(0.29f, floor + h * 1.04f, -0.03f), new Vector3(0.085f, 0.29f, 0.085f), 0xC8B5D9, new Vector3(0, 0, 22));
                Ornament(root, "Summoner ritual staff", PrimitiveType.Cylinder, new Vector3(0.65f, floor + h * 0.58f, -0.06f), new Vector3(0.07f, h * 0.45f, 0.07f), 0x4D3D5A);
                Ornament(root, "Summoner ritual orb", PrimitiveType.Sphere, new Vector3(0.65f, floor + h * 1.04f, -0.06f), Vector3.one * 0.49f, 0xAB78ED);
                Ornament(root, "Summoner ritual ring", PrimitiveType.Cylinder, new Vector3(0.65f, floor + h * 0.91f, -0.06f), new Vector3(0.23f, 0.045f, 0.23f), 0xC7A3DE);
                Ornament(root, "Summoner belt seal", PrimitiveType.Sphere, new Vector3(0, floor + h * 0.35f, 0.40f), Vector3.one * 0.20f, 0xB587DC);
                Ornament(root, "Summoner chest rune", PrimitiveType.Cube, new Vector3(0, floor + h * 0.52f, 0.38f), new Vector3(0.27f, 0.27f, 0.06f), 0xB890ED, new Vector3(0, 0, 45));
                GroupOrnaments(root, "Summoner staff joint", "Summoner ritual staff", "Summoner ritual orb", "Summoner ritual ring");
            }
        }
        private void AddBuildingLevelArt(GameObject root, BuildingKind kind, int level)
        {
            if (level < 2) return;
            Bounds b = LocalBounds(root.transform);
            float cx = b.center.x, cz = b.center.z, top = b.max.y;
            float edge = Mathf.Max(0.25f, Mathf.Min(b.size.x, b.size.z) * 0.32f);
            int bronze = kind == BuildingKind.Wall ? 0xC99A52 : 0xD6A543;
            if (kind == BuildingKind.Wall)
            {
                // Crenellations stay on the wall's local long axis and rotate with its segment.
                bool longX = b.size.x >= b.size.z;
                for (int i = -1; i <= 1; i++)
                    Ornament(root, "Tier 2 wall merlon", PrimitiveType.Cube,
                        new Vector3(cx + (longX ? i * b.size.x * 0.29f : 0), top + 0.12f, cz + (!longX ? i * b.size.z * 0.29f : 0)),
                        new Vector3(longX ? 0.22f : b.size.x * 0.68f, 0.24f, longX ? b.size.z * 0.68f : 0.22f), bronze);
                if (level >= 3)
                    for (int i = -1; i <= 1; i += 2)
                        Ornament(root, "Tier 3 wall beacon", PrimitiveType.Sphere,
                            new Vector3(cx + (longX ? i * b.size.x * 0.29f : 0), top + 0.32f, cz + (!longX ? i * b.size.z * 0.29f : 0)),
                            Vector3.one * 0.2f, 0x62D7E1);
                return;
            }
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    Ornament(root, "Tier 2 grounded corner post", PrimitiveType.Cylinder,
                        new Vector3(cx + sx * edge, 0.34f, cz + sz * edge), new Vector3(0.2f, 0.34f, 0.2f), bronze);
            if (level < 3) return;
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    Ornament(root, "Tier 3 corner gem", PrimitiveType.Sphere,
                        new Vector3(cx + sx * edge, 0.76f, cz + sz * edge), Vector3.one * 0.25f, 0x62D7E1);
        }
        private void AddTroopLevelArt(GameObject root, TroopKind kind, int level, Bounds b)
        {
            if (level < 2) return;
            float y = b.min.y + b.size.y * 0.66f;
            float side = Mathf.Clamp(b.size.x * 0.32f, 0.22f, 0.48f);
            int color = kind == TroopKind.Medic || kind == TroopKind.Alchemist ? 0x64CFC6 : 0xD6A543;
            Ornament(root, "Tier 2 left pauldron", PrimitiveType.Sphere, new Vector3(-side, y, 0), new Vector3(0.29f, 0.18f, 0.34f), color);
            Ornament(root, "Tier 2 right pauldron", PrimitiveType.Sphere, new Vector3(side, y, 0), new Vector3(0.29f, 0.18f, 0.34f), color);
            if (level < 3) return;
            if (kind == TroopKind.Alchemist || kind == TroopKind.Medic || kind == TroopKind.Summoner)
            {
                Ornament(root, "Tier 3 arcane coat clasp", PrimitiveType.Sphere,
                    new Vector3(0, b.min.y + b.size.y * 0.51f, 0.51f), Vector3.one * 0.24f, 0xF4D574);
                return;
            }
            Ornament(root, "Tier 3 armor badge", PrimitiveType.Sphere,
                new Vector3(0, b.min.y + b.size.y * 0.55f, 0.43f), Vector3.one * 0.27f, 0x62D7E1);
            Ornament(root, "Tier 3 armored belt", PrimitiveType.Cube,
                new Vector3(0, b.min.y + b.size.y * 0.38f, 0.33f), new Vector3(0.48f, 0.10f, 0.12f), 0xF4D574);
        }
        private void AddAuthoredTroopLevelArt(GameObject root, TroopKind kind, int level, Bounds b)
        {
            if (level < 2) return;
            float y = b.min.y + b.size.y * 0.64f;
            float side = Mathf.Clamp(b.size.x * 0.24f, 0.15f, 0.28f);
            int color = kind == TroopKind.Medic ? 0x68D9C8 : kind == TroopKind.Summoner ? 0xC3A0F0
                : kind == TroopKind.Ranger ? 0x82C875 : kind == TroopKind.Sapper ? 0xE99A61 : 0xE2B65E;
            Ornament(root, "Tier 2 left enamel insignia", PrimitiveType.Sphere,
                new Vector3(-side, y, 0), new Vector3(0.11f, 0.08f, 0.12f), color);
            Ornament(root, "Tier 2 right enamel insignia", PrimitiveType.Sphere,
                new Vector3(side, y, 0), new Vector3(0.11f, 0.08f, 0.12f), color);
            if (level < 3) return;
            Ornament(root, "Tier 3 small insignia clasp", PrimitiveType.Sphere,
                new Vector3(0, b.min.y + b.size.y * 0.48f, b.max.z * 0.34f), Vector3.one * 0.14f, 0xE8C578);
        }
        private static string BuildingArt(BuildingKind kind)
        {
            return "ThirdParty/KayKitMedieval/" + kind;
        }
        private static string TroopArt(TroopKind kind)
        {
            const string root = "ThirdParty/KayKitAdventurers/";
            switch (kind)
            {
                case TroopKind.Vanguard: return "ThirdParty/QuaterniusRPG/Warrior";
                case TroopKind.Ranger: return "ThirdParty/QuaterniusRPG/Ranger";
                case TroopKind.Guardian: return root + "Knight";
                case TroopKind.Sapper: return "ThirdParty/QuaterniusRPG/Rogue";
                case TroopKind.SkyRider: return root + "RogueHooded";
                case TroopKind.Alchemist: return root + "Mage";
                case TroopKind.Medic: return "ThirdParty/QuaterniusRPG/Cleric";
                case TroopKind.Summoner: return "ThirdParty/QuaterniusRPG/Wizard";
                default: return root + "Mage";
            }
        }
        private static bool UsesAuthoredRig(TroopKind kind)
        {
            return kind == TroopKind.Vanguard || kind == TroopKind.Ranger || kind == TroopKind.Sapper
                || kind == TroopKind.Medic || kind == TroopKind.Summoner;
        }
        private static string TroopTexture(TroopKind kind)
        {
            const string root = "ThirdParty/KayKitAdventurers/";
            switch (kind)
            {
                case TroopKind.Vanguard: return root + "barbarian_texture";
                case TroopKind.Ranger: return root + "rogue_texture";
                case TroopKind.Guardian: return root + "knight_texture";
                case TroopKind.Sapper: return root + "rogue_texture";
                case TroopKind.SkyRider: return root + "rogue_texture";
                case TroopKind.Medic: case TroopKind.Summoner: return root + "rogue_texture";
                default: return root + "mage_texture";
            }
        }
        private void ApplyExternalTexture(GameObject root, string resource)
        {
            Material replacement = ExternalMaterial(resource);
            if (replacement == null) return;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = replacement;
                renderer.sharedMaterials = materials;
            }
        }
        private void ApplyRpgTextures(GameObject root, TroopKind kind)
        {
            string prefix, weaponName;
            switch (kind)
            {
                case TroopKind.Vanguard: prefix = "Warrior"; weaponName = "Sword"; break;
                case TroopKind.Ranger: prefix = "Ranger"; weaponName = "Bow"; break;
                case TroopKind.Sapper: prefix = "Rogue"; weaponName = "Dagger"; break;
                case TroopKind.Medic: prefix = "Cleric"; weaponName = "Staff"; break;
                default: prefix = "Wizard"; weaponName = "Staff"; break;
            }
            const string folder = "ThirdParty/QuaterniusRPG/";
            Material body = ExternalMaterial(folder + prefix + "_Texture");
            Material weapon = ExternalMaterial(folder + prefix + "_" + weaponName + "_Texture");
            if (body == null || weapon == null) return;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    string importedName = materials[i] != null ? materials[i].name : "";
                    bool isWeapon = renderer.name.IndexOf(weaponName, System.StringComparison.OrdinalIgnoreCase) >= 0
                        || importedName.IndexOf(weaponName, System.StringComparison.OrdinalIgnoreCase) >= 0;
                    materials[i] = isWeapon ? weapon : body;
                }
                renderer.sharedMaterials = materials;
            }
        }
        private Material ExternalMaterial(string resource)
        {
            Texture2D texture = Resources.Load<Texture2D>(resource);
            if (texture == null) { Debug.LogError("HEARTHHOLD_EXTERNAL_TEXTURE_MISSING: " + resource); return null; }
            Material replacement;
            if (!externalTextureMaterials.TryGetValue(texture, out replacement))
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                replacement = new Material(shader) { name = "KayKit " + texture.name };
                if (replacement.HasProperty("_BaseMap")) replacement.SetTexture("_BaseMap", texture);
                if (replacement.HasProperty("_MainTex")) replacement.SetTexture("_MainTex", texture);
                replacement.color = Color.white;
                externalTextureMaterials.Add(texture, replacement);
            }
            return replacement;
        }
        private GameObject CreateExternal(string key, string resource, float targetFootprint, Vector3 groundCenter, Transform parent)
        {
            GameObject prefab = Resources.Load<GameObject>(resource);
            if (prefab == null) return null;
            if (loadedExternalResources.Add(resource)) Debug.Log("HEARTHHOLD_EXTERNAL_MODEL_READY: " + resource);
            GameObject root = new GameObject(key); root.transform.SetParent(parent, false);
            GameObject visual = Object.Instantiate(prefab, root.transform, false); visual.name = "3D model";
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            Bounds source = LocalBounds(root.transform);
            float width = Mathf.Max(source.size.x, source.size.z);
            if (width > 0.0001f) visual.transform.localScale *= targetFootprint / width;
            Bounds fitted = LocalBounds(root.transform);
            visual.transform.localPosition += new Vector3(groundCenter.x - fitted.center.x, -fitted.min.y, groundCenter.z - fitted.center.z);
            return root;
        }
        private static Bounds LocalBounds(Transform root)
        {
            return BoundsRelativeTo(root, root);
        }
        private static Bounds BoundsRelativeTo(Transform root, Transform subtree)
        {
            Renderer[] renderers = subtree.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
            bool initialized = false; Bounds result = new Bounds();
            foreach (Renderer renderer in renderers)
            {
                Bounds world = renderer.bounds;
                Vector3 min = world.min, max = world.max;
                for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++) for (int z = 0; z < 2; z++)
                {
                    Vector3 point = root.InverseTransformPoint(new Vector3(x == 0 ? min.x : max.x, y == 0 ? min.y : max.y, z == 0 ? min.z : max.z));
                    if (!initialized) { result = new Bounds(point, Vector3.zero); initialized = true; }
                    else result.Encapsulate(point);
                }
            }
            return result;
        }
        public static void Tint(GameObject root, Color color)
        {
            if (root == null) return;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (renderer.gameObject.name == "Ground contact shadow") continue;
                MaterialPropertyBlock properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(properties);
            }
        }
        private GameObject Create(string key, ModelMesh geometry, Transform parent)
        {
            Mesh mesh;
            if (!meshes.TryGetValue(key, out mesh))
            {
                List<Vector3> vertices = new List<Vector3>(), normals = new List<Vector3>();
                List<Color> colors = new List<Color>(); List<int> triangles = new List<int>();
                foreach (ModelFace face in geometry.Faces)
                {
                    int offset = vertices.Count;
                    Color color = new Color32((byte)(face.Color >> 16), (byte)(face.Color >> 8), (byte)face.Color, 255);
                    foreach (ModelPoint point in face.Points)
                    {
                        vertices.Add(new Vector3(point.X, point.Y, point.Z));
                        normals.Add(new Vector3(face.Normal.X, face.Normal.Y, face.Normal.Z)); colors.Add(color.linear);
                    }
                    for (int i = 1; i < face.Points.Length - 1; i++) { triangles.Add(offset); triangles.Add(offset + i); triangles.Add(offset + i + 1); }
                }
                mesh = new Mesh { name = key };
                if (vertices.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
                meshes.Add(key, mesh);
            }
            if (material == null)
            {
                Material template = Resources.Load<Material>("ModelPalette");
                material = template != null ? new Material(template) : new Material(Shader.Find("Hearthhold/VertexLit"));
            }
            GameObject obj = new GameObject(key); obj.transform.SetParent(parent, false);
            GameObject visual = new GameObject("3D model"); visual.transform.SetParent(obj.transform, false);
            visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = visual.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return obj;
        }
        private void AddBuildingSprite(GameObject root, BuildingKind kind)
        {
            if (kind == BuildingKind.Wall) return;
            string resource; float width, bottomTrim;
            switch (kind)
            {
                case BuildingKind.Keep: resource = "GeneratedArt/KeepV061"; width = 5.8f; bottomTrim = 0.047f; break;
                case BuildingKind.Mine: resource = "GeneratedArt/MineV061"; width = 4.35f; bottomTrim = 0.067f; break;
                case BuildingKind.Reservoir: resource = "GeneratedArt/ReservoirV061"; width = 4.15f; bottomTrim = 0.115f; break;
                case BuildingKind.Barracks: resource = "GeneratedArt/BarracksV061"; width = 4.45f; bottomTrim = 0.067f; break;
                case BuildingKind.Cannon: resource = "GeneratedArt/CannonV061"; width = 3.75f; bottomTrim = 0.083f; break;
                case BuildingKind.Watchtower: resource = "GeneratedArt/WatchtowerV061"; width = 3.25f; bottomTrim = 0.037f; break;
                case BuildingKind.TrainingCamp: resource = "GeneratedArt/TrainingCampV070"; width = 4.35f; bottomTrim = 0.071f; break;
                case BuildingKind.Laboratory: resource = "GeneratedArt/LaboratoryV070"; width = 4.2f; bottomTrim = 0.103f; break;
                default: return;
            }
            Texture2D image = Resources.Load<Texture2D>(resource);
            if (image == null) return;
            Rect crop = new Rect(0, 0, image.width, image.height * (1 - bottomTrim));
            float height = width * crop.height / image.width;
            GameObject sprite = SpriteObject("Generated " + kind + " art", image, crop, width, height,
                SpriteMaterial(image, "Generated " + kind + " material", false), root.transform);
            float size = Rules.Spec(kind).Size;
            // The painted front corner is the contact point; the ground shadow stays at the logical footprint center.
            float paintedFoot = size * 0.37f;
            sprite.transform.localPosition = new Vector3(paintedFoot, 0.03f, paintedFoot);
            root.GetComponent<MeshRenderer>().enabled = false;
            AddContactShadow(root, size * 0.92f, size * 0.75f, new Vector3(size * 0.5f, 0.025f, size * 0.5f));
            root.AddComponent<ModelActionAnimator>().Visual = sprite.transform;
        }
        private void AddTroopSprite(GameObject root, TroopKind kind)
        {
            Texture2D atlas = Resources.Load<Texture2D>("GeneratedArt/TroopAtlasV061");
            if (atlas == null) { Debug.LogError("HEARTHHOLD_TROOP_ART_MISSING: GeneratedArt/TroopAtlasV061"); return; }
            Rect crop; float width;
            switch (kind)
            {
                case TroopKind.Vanguard: crop = new Rect(0, 0, 650, 585); width = 2.1f; break;
                case TroopKind.Ranger: crop = new Rect(650, 0, 682, 600); width = 2.15f; break;
                case TroopKind.Guardian: crop = new Rect(0, 530, 680, 670); width = 2.5f; break;
                default: crop = new Rect(650, 555, 682, 645); width = 2.3f; break;
            }
            float height = width * crop.height / crop.width;
            GameObject sprite = SpriteObject("Generated " + kind + " art", atlas, crop, width, height,
                SpriteMaterial(atlas, "Generated troop material", true), root.transform);
            sprite.transform.localPosition = new Vector3(0, 0.03f, 0);
            sprite.AddComponent<FixedIsometricBillboard>();
            root.GetComponent<MeshRenderer>().enabled = false;
            AddContactShadow(root, 0.95f, 0.6f, new Vector3(0, 0.02f, 0));
            root.AddComponent<ModelActionAnimator>().Visual = sprite.transform;
        }
        private void AddContactShadow(GameObject root, float width, float depth, Vector3 position)
        {
            if (contactShadowMaterial == null)
            {
                Material template = Resources.Load<Material>("ContactShadowPalette");
                if (template == null) return;
                contactShadowMaterial = new Material(template);
            }
            if (contactShadowMesh == null)
            {
                contactShadowMesh = new Mesh { name = "Soft contact shadow" };
                contactShadowMesh.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0) };
                contactShadowMesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
                contactShadowMesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
                contactShadowMesh.RecalculateBounds();
            }
            GameObject shadow = new GameObject("Ground contact shadow");
            shadow.transform.SetParent(root.transform, false);
            shadow.transform.localPosition = position;
            shadow.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            shadow.transform.localScale = new Vector3(width, depth, 1);
            shadow.AddComponent<MeshFilter>().sharedMesh = contactShadowMesh;
            MeshRenderer renderer = shadow.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = contactShadowMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        private GameObject SpriteObject(string name, Texture2D atlas, Rect crop, float width, float height, Material spriteMaterial, Transform parent)
        {
            string key = "sprite:" + name;
            Mesh mesh;
            if (!meshes.TryGetValue(key, out mesh))
            {
                float u0 = crop.x / atlas.width, u1 = (crop.x + crop.width) / atlas.width;
                float v0 = 1 - (crop.y + crop.height) / atlas.height, v1 = 1 - crop.y / atlas.height;
                mesh = new Mesh { name = key };
                mesh.vertices = new[] { new Vector3(-width * 0.5f, 0, 0), new Vector3(width * 0.5f, 0, 0), new Vector3(-width * 0.5f, height, 0), new Vector3(width * 0.5f, height, 0) };
                mesh.uv = new[] { new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u0, v1), new Vector2(u1, v1) };
                mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
                mesh.RecalculateNormals(); mesh.RecalculateBounds(); meshes.Add(key, mesh);
            }
            GameObject sprite = new GameObject(name); sprite.transform.SetParent(parent, false);
            sprite.transform.localRotation = Quaternion.Euler(45, 45, 0);
            sprite.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = sprite.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = spriteMaterial; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            if (name.Contains("Vanguard") || name.Contains("Ranger") || name.Contains("Guardian") || name.Contains("Sapper")) renderer.sortingOrder = 20;
            return sprite;
        }
        private Material SpriteMaterial(Texture2D atlas, string name, bool readableOverlay)
        {
            Material cached;
            if (spriteMaterials.TryGetValue(atlas, out cached)) return cached;
            Material template = Resources.Load<Material>("GeneratedSpritePalette");
            Shader shader = Shader.Find("Hearthhold/ChromaKeySprite");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            Material result = template != null ? new Material(template) : new Material(shader);
            result.name = name;
            result.SetTexture("_BaseMap", atlas);
            if (result.HasProperty("_KeyColor")) result.SetColor("_KeyColor", Color.magenta);
            if (result.HasProperty("_Threshold")) result.SetFloat("_Threshold", 0.12f);
            if (result.HasProperty("_ZTest")) result.SetFloat("_ZTest", readableOverlay ? (float)CompareFunction.Always : (float)CompareFunction.LessEqual);
            if (result.HasProperty("_ZWrite")) result.SetFloat("_ZWrite", readableOverlay ? 0 : 1);
            if (readableOverlay) result.renderQueue = 3100;
            spriteMaterials.Add(atlas, result);
            return result;
        }
        public void Dispose()
        {
            foreach (Mesh mesh in meshes.Values) Object.Destroy(mesh);
            meshes.Clear(); if (material != null) Object.Destroy(material);
            if (contactShadowMaterial != null) Object.Destroy(contactShadowMaterial);
            if (contactShadowMesh != null) Object.Destroy(contactShadowMesh);
            foreach (Material spriteMaterial in spriteMaterials.Values) Object.Destroy(spriteMaterial);
            spriteMaterials.Clear();
            foreach (Material externalMaterial in externalTextureMaterials.Values) Object.Destroy(externalMaterial);
            externalTextureMaterials.Clear();
            foreach (Material levelMaterial in levelMaterials.Values) Object.Destroy(levelMaterial);
            levelMaterials.Clear();
            loadedExternalResources.Clear();
        }
    }
    internal sealed class FixedIsometricBillboard : MonoBehaviour
    {
        public float RollDegrees;
        private void LateUpdate() { transform.rotation = Quaternion.Euler(45, 45, 0) * Quaternion.Euler(0, 0, RollDegrees); }
    }

    // Moves the visible art on impact while the simulation and collider stay at their exact positions.
    internal sealed class ModelActionAnimator : MonoBehaviour
    {
        public Transform Visual;
        private ArticulatedModelAnimator articulated;
        private ImportedClipAnimator importedClips;
        private FixedIsometricBillboard billboard;
        private Vector3 restPosition, restScale, direction;
        private Quaternion restRotation;
        private float elapsed, duration;
        private bool ready, attacking, building;

        private void EnsureReady()
        {
            if (ready || Visual == null) return;
            articulated = GetComponent<ArticulatedModelAnimator>();
            importedClips = GetComponent<ImportedClipAnimator>();
            restPosition = Visual.localPosition;
            restScale = Visual.localScale;
            restRotation = Visual.localRotation;
            billboard = Visual.GetComponent<FixedIsometricBillboard>();
            building = GetComponent<BuildingHandle>() != null;
            ready = true;
        }
        public void Attack(Vector3 targetWorld)
        {
            EnsureReady();
            if (!ready) return;
            if (articulated != null) { articulated.Attack(targetWorld); return; }
            if (importedClips != null && importedClips.Attack()) return;
            direction = transform.InverseTransformDirection(targetWorld - transform.position);
            direction.y = 0;
            if (direction.sqrMagnitude < 0.001f) direction = Vector3.forward;
            direction.Normalize();
            attacking = true; elapsed = 0; duration = building ? 0.34f : 0.29f;
        }
        public void Hit()
        {
            EnsureReady();
            if (!ready || attacking) return;
            if (articulated != null) { articulated.Hit(); return; }
            if (importedClips != null && importedClips.Hit()) return;
            direction = Vector3.forward;
            attacking = false; elapsed = 0; duration = 0.22f;
        }
        private void Update()
        {
            if (!ready) EnsureReady();
            if (articulated != null || importedClips != null && importedClips.IsPlayingAction) return;
            if (!ready || duration <= 0) return;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float strength = Mathf.Sin(t * Mathf.PI);
            float move = attacking ? building ? -0.24f * strength : (t < 0.27f ? -0.2f * strength : 0.58f * strength) : -0.14f * strength;
            Visual.localPosition = restPosition + direction * move + Vector3.up * (attacking && !building ? 0.09f * strength : -0.055f * strength);
            Visual.localScale = Vector3.Scale(restScale, new Vector3(1 + (attacking ? 0.13f : -0.08f) * strength, 1 - (attacking ? 0.16f : 0.07f) * strength, 1));
            float roll = (attacking ? 9f : -5f) * strength * (direction.x >= 0 ? 1 : -1);
            if (billboard != null) billboard.RollDegrees = roll;
            else Visual.localRotation = restRotation * Quaternion.Euler(0, 0, roll);
            if (t < 1) return;
            duration = 0; attacking = false;
            Visual.localPosition = restPosition; Visual.localScale = restScale;
            if (billboard != null) billboard.RollDegrees = 0;
            else Visual.localRotation = restRotation;
        }
        private void OnDisable()
        {
            duration = 0;
            if (articulated != null) return;
            if (!ready) return;
            Visual.localPosition = restPosition; Visual.localScale = restScale;
            if (billboard != null) billboard.RollDegrees = 0;
            else Visual.localRotation = restRotation;
        }
    }

    // Source-pack animation takes are played as-is; damage timing remains in the deterministic simulation.
    internal sealed class ImportedClipAnimator : MonoBehaviour
    {
        private Animator animator;
        private AnimationClip idle, run, cast, followUp, hit, death, current;
        private PlayableGraph graph;
        private AnimationClipPlayable playable;
        private bool moving, dying, castLogged, followUpLogged;
        private TroopKind role;
        private float deathTime;
        private float attackPlaybackSpeed = 1f;
        public bool IsPlayingAction { get { return current != null && (current == cast || current == followUp || current == hit); } }

        public void Configure(string resource, TroopKind kind)
        {
            role = kind;
            animator = GetComponentInChildren<Animator>();
            if (animator == null) animator = transform.Find("3D model").gameObject.AddComponent<Animator>();
            animator.applyRootMotion = false;
            AnimationClip[] clips = Resources.LoadAll<AnimationClip>(resource);
            idle = FindClip(clips, "Idle_Weapon") ?? FindClip(clips, "Idle");
            run = FindClip(clips, "Run_Weapon") ?? FindClip(clips, "Run");
            switch (kind)
            {
                case TroopKind.Vanguard: cast = FindClip(clips, "Sword_Attack"); break;
                case TroopKind.Ranger:
                    cast = FindClip(clips, "Bow_Attack_Draw");
                    followUp = FindClip(clips, "Bow_Attack_Shoot");
                    break;
                case TroopKind.Sapper: cast = FindClip(clips, "Dagger_Attack"); break;
                default: cast = FindClip(clips, "Spell1") ?? FindClip(clips, "Staff_Attack"); break;
            }
            if (cast == null) cast = FindClip(clips, "Attack") ?? FindClip(clips, "Punch");
            hit = FindClip(clips, "RecieveHit") ?? FindClip(clips, "Hit");
            death = FindClip(clips, "Death");
            List<string> names = new List<string>();
            foreach (AnimationClip clip in clips) names.Add(clip.name);
            Debug.Log("HEARTHHOLD_IMPORTED_CLIPS: " + kind + " [" + string.Join(",", names) + "]");
            if (kind == TroopKind.Ranger && cast != null && followUp != null)
            {
                float attackInterval = (Rules.Spec(kind).Cooldown + 1) / (float)Rules.TicksPerSecond;
                attackPlaybackSpeed = Mathf.Max(1f, (cast.length + followUp.length) / (attackInterval * 0.82f));
                Debug.Log("HEARTHHOLD_BOW_TAKES: draw=" + cast.length + " shoot=" + followUp.length
                    + " playback=" + attackPlaybackSpeed.ToString("0.00") + "x");
            }
            if (idle == null || run == null || cast == null || death == null
                || (kind == TroopKind.Ranger && followUp == null))
                Debug.LogError("HEARTHHOLD_IMPORTED_CLIPS_MISSING: " + kind);
            else { Play(idle); Debug.Log("HEARTHHOLD_IMPORTED_RIG_READY: " + kind); }
        }
        private static AnimationClip FindClip(AnimationClip[] clips, string name)
        {
            foreach (AnimationClip clip in clips) if (string.Equals(clip.name, name, System.StringComparison.OrdinalIgnoreCase)) return clip;
            foreach (AnimationClip clip in clips) if (clip.name.EndsWith("|" + name, System.StringComparison.OrdinalIgnoreCase)) return clip;
            return null;
        }
        private void Play(AnimationClip clip)
        {
            if (clip == null || animator == null) return;
            if (graph.IsValid()) graph.Destroy();
            graph = PlayableGraph.Create("Hearthhold " + clip.name);
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            playable = AnimationClipPlayable.Create(graph, clip);
            if (clip == cast || clip == followUp) playable.SetSpeed(attackPlaybackSpeed);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Character animation", animator);
            output.SetSourcePlayable(playable);
            current = clip;
            graph.Play();
        }
        public void SetMoving(bool value)
        {
            moving = value;
            if (dying || IsPlayingAction) return;
            AnimationClip desired = moving ? run : idle;
            if (desired != current) Play(desired);
        }
        public bool Attack()
        {
            if (dying || cast == null) return false;
            Play(cast);
            if (!castLogged) { castLogged = true; Debug.Log("HEARTHHOLD_IMPORTED_CAST_READY: " + role + " clip=" + cast.name); }
            return true;
        }
        public bool Hit()
        {
            if (dying || hit == null) return false;
            Play(hit); return true;
        }
        public void Die()
        {
            if (dying || !gameObject.activeSelf) return;
            dying = true; deathTime = 0; Play(death);
        }
        private void Update()
        {
            if (dying)
            {
                deathTime += Time.deltaTime;
                if (deathTime >= Mathf.Max(0.7f, death != null ? death.length : 0)) gameObject.SetActive(false);
                return;
            }
            if (!graph.IsValid() || current == null) return;
            if (playable.GetTime() < current.length) return;
            if (current == cast && followUp != null)
            {
                Play(followUp);
                if (!followUpLogged) { followUpLogged = true; Debug.Log("HEARTHHOLD_IMPORTED_FOLLOWUP_READY: " + role + " clip=" + followUp.name); }
            }
            else if (IsPlayingAction) Play(moving ? run : idle);
            else playable.SetTime(0);
        }
        private void OnDestroy() { if (graph.IsValid()) graph.Destroy(); }
    }

    // Moves KayKit's imported humanoid bones and role props while leaving simulation roots and colliders fixed.
    internal sealed class ArticulatedModelAnimator : MonoBehaviour
    {
        private Transform leftArm, rightArm, leftLeg, rightLeg, head, staff, leftWing, rightWing, visual, rotor, core;
        private Quaternion leftArmRest, rightArmRest, leftLegRest, rightLegRest, headRest, staffRest, leftWingRest, rightWingRest;
        private Vector3 visualRestPosition;
        private Vector3 coreRestScale;
        private bool isTower, isMedic, moving, dying, attackLogged;
        private TroopKind role;
        private float actionTime = 10f, hitTime = 10f, deathTime, spin;
        private Vector3 targetDirection = Vector3.forward;

        public void ConfigureTroop(TroopKind kind)
        {
            role = kind;
            isMedic = kind == TroopKind.Medic;
            leftArm = FindPart(transform, "upperarm.l"); rightArm = FindPart(transform, "upperarm.r");
            leftLeg = FindPart(transform, "upperleg.l"); rightLeg = FindPart(transform, "upperleg.r");
            head = FindPart(transform, "head");
            string propName = kind == TroopKind.Guardian ? "Guardian shield joint"
                : kind == TroopKind.SkyRider ? "Sky rider crystal joint"
                : kind == TroopKind.Alchemist ? "Alchemist bomb joint"
                : isMedic ? "Medic staff joint" : "Summoner staff joint";
            staff = FindPart(transform, propName);
            leftWing = FindPart(transform, "Sky rider left wing joint");
            rightWing = FindPart(transform, "Sky rider right wing joint");
            visual = transform.Find("3D model");
            if (visual != null) visualRestPosition = visual.localPosition;
            leftArmRest = Rotation(leftArm); rightArmRest = Rotation(rightArm);
            leftLegRest = Rotation(leftLeg); rightLegRest = Rotation(rightLeg);
            headRest = Rotation(head); staffRest = Rotation(staff);
            leftWingRest = Rotation(leftWing); rightWingRest = Rotation(rightWing);
            if (leftArm == null || rightArm == null || leftLeg == null || rightLeg == null || head == null || staff == null
                || kind == TroopKind.SkyRider && (leftWing == null || rightWing == null))
            {
                List<string> parts = new List<string>();
                foreach (Transform part in GetComponentsInChildren<Transform>()) if (parts.Count < 40) parts.Add(part.name);
                Debug.LogError("HEARTHHOLD_ARTICULATED_RIG_MISSING: " + kind + " arms=" + (leftArm != null) + "/" + (rightArm != null)
                    + " legs=" + (leftLeg != null) + "/" + (rightLeg != null) + " head=" + (head != null) + " prop=" + (staff != null)
                    + " wings=" + (leftWing != null) + "/" + (rightWing != null)
                    + " parts=" + string.Join(",", parts));
            }
            else
            {
                Debug.Log("HEARTHHOLD_ARTICULATED_RIG_READY: " + kind);
                if (kind == TroopKind.SkyRider) Debug.Log("HEARTHHOLD_ARTICULATED_FLIGHT_READY: " + kind);
            }
        }
        public void ConfigureStormTower()
        {
            isTower = true;
            rotor = FindPart(transform, "Storm rotor");
            core = FindPart(transform, "Storm charge core");
            if (core != null) coreRestScale = core.localScale;
            if (rotor == null || core == null) Debug.LogError("HEARTHHOLD_STORM_RIG_MISSING");
            else Debug.Log("HEARTHHOLD_STORM_RIG_READY");
        }
        private static Transform FindPart(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                Transform found = FindPart(child, name);
                if (found != null) return found;
            }
            return null;
        }
        private static Quaternion Rotation(Transform part) { return part != null ? part.localRotation : Quaternion.identity; }
        public void SetMoving(bool value) { if (!dying) moving = value; }
        public void Attack(Vector3 targetWorld)
        {
            if (dying) return;
            targetDirection = transform.InverseTransformDirection(targetWorld - transform.position);
            targetDirection.y = 0;
            if (targetDirection.sqrMagnitude < 0.001f) targetDirection = Vector3.forward;
            targetDirection.Normalize();
            actionTime = 0;
            if (!isTower && !attackLogged) { attackLogged = true; Debug.Log("HEARTHHOLD_ARTICULATED_ATTACK_READY: " + role); }
        }
        public void Hit() { if (!dying) hitTime = 0; }
        public void Die()
        {
            if (dying || !gameObject.activeSelf) return;
            dying = true; deathTime = 0; moving = false; actionTime = 10;
        }
        private void Update()
        {
            float dt = Time.deltaTime;
            actionTime += dt; hitTime += dt;
            if (dying) deathTime += dt;
            float cast = dying ? 0 : actionTime < 0.56f ? Mathf.Sin(Mathf.PI * actionTime / 0.56f) : 0;
            float recoil = dying ? 0 : hitTime < 0.35f ? Mathf.Sin(Mathf.PI * hitTime / 0.35f) : 0;
            if (isTower)
            {
                spin += dt * (28f + cast * 720f);
                if (rotor != null) rotor.localRotation = Quaternion.Euler(0, spin, cast * 8f);
                if (core != null) core.localScale = coreRestScale * (1 + 0.11f * Mathf.Sin(Time.time * 3.4f) + cast * 0.55f + recoil * 0.14f);
                return;
            }
            bool isSkyRider = role == TroopKind.SkyRider;
            float stride = moving && !dying && !isSkyRider ? Mathf.Sin(Time.time * 10f) : 0;
            float breath = dying ? 0 : Mathf.Sin(Time.time * 2.7f);
            float collapse = dying ? Mathf.SmoothStep(0, 1, Mathf.Clamp01(deathTime / 0.48f)) : 0;
            float castingArm = isMedic ? -67f : role == TroopKind.Alchemist ? -112f : role == TroopKind.Guardian ? -58f : -88f;
            if (leftArm != null) leftArm.localRotation = leftArmRest * Quaternion.Euler(stride * 23f - cast * (role == TroopKind.Guardian ? 20f : 42f) + recoil * 28f + collapse * 50f, 0, breath * 3f - cast * 12f);
            if (rightArm != null) rightArm.localRotation = rightArmRest * Quaternion.Euler(-stride * 23f + cast * castingArm + recoil * 34f + collapse * 58f, 0, cast * (isMedic ? -18f : -32f));
            if (leftLeg != null) leftLeg.localRotation = leftLegRest * Quaternion.Euler(-stride * 20f + (isSkyRider ? 16f : 0) + collapse * 48f, 0, 0);
            if (rightLeg != null) rightLeg.localRotation = rightLegRest * Quaternion.Euler(stride * 20f + (isSkyRider ? 16f : 0) + collapse * 58f, 0, 0);
            if (head != null) head.localRotation = headRest * Quaternion.Euler(-cast * 12f + recoil * 17f + collapse * 31f, breath * 4f + targetDirection.x * cast * 10f, recoil * 8f);
            if (staff != null) staff.localRotation = staffRest * Quaternion.Euler(
                role == TroopKind.Alchemist ? -cast * 35f : -cast * 8f, 0,
                breath * 3f + cast * (role == TroopKind.Guardian ? 18f : isMedic ? -18f : -22f) + collapse * 24f);
            if (isSkyRider)
            {
                float flap = Mathf.Sin(Time.time * (moving ? 14f : 8f)) * 29f + cast * 31f;
                if (leftWing != null) leftWing.localRotation = leftWingRest * Quaternion.Euler(0, 0, flap - collapse * 60f);
                if (rightWing != null) rightWing.localRotation = rightWingRest * Quaternion.Euler(0, 0, -flap + collapse * 60f);
                if (visual != null) visual.localPosition = visualRestPosition + Vector3.up * (dying ? -collapse * 0.38f : Mathf.Sin(Time.time * 4.4f) * 0.08f + cast * 0.13f);
            }
            if (dying && deathTime >= 0.7f) gameObject.SetActive(false);
        }
    }
}
