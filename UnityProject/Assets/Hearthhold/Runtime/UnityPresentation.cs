using System.Collections.Generic;
using Hearthhold.Core;
using UnityEngine;

namespace Hearthhold.UnityClient
{
    public sealed partial class GameBootstrap
    {
        private static readonly Color Danger = new Color32(235, 91, 78, 255);
        private static readonly Color Ember = new Color32(255, 139, 67, 255);
        private readonly HashSet<CombatEffect> presentedEffects = new HashSet<CombatEffect>();
        private GameObject selectionMarker, deploymentMarker, focusMarker, placementPreview;
        private readonly List<GameObject> wallSelectionMarkers = new List<GameObject>();
        private readonly List<GameObject> wallPlacementMarkers = new List<GameObject>();
        private Mesh tileOutlineMesh;
        private Material tileMintMaterial, tileDangerMaterial;
        private BuildingKind? placementPreviewKind;
        private int placementPreviewLevel;
        private Transform effectsRoot, deploymentRoot;
        private Mesh ringMesh;
        private int troopActionsPresented, defenseActionsPresented;

        private void InitializePresentation()
        {
            ringMesh = MakeRingMesh();
            tileOutlineMesh = MakeTileOutlineMesh();
            selectionMarker = Ring("Selected building", Gold, transform);
            selectionMarker.SetActive(false);
            deploymentMarker = Ring("Nearest deployment point", Mint, transform);
            deploymentMarker.SetActive(false);
            focusMarker = Ring("Ordered target", Gold, transform);
            focusMarker.SetActive(false);
            effectsRoot = new GameObject("Battle effects").transform;
            effectsRoot.SetParent(transform, false);
            deploymentRoot = new GameObject("Deployment boundary").transform;
            deploymentRoot.SetParent(sceneryRoot, false);
            Color boundary = new Color32(102, 232, 173, 255);
            Piece("South deployment line", PrimitiveType.Cube, new Vector3(20.5f, 0.07f, 10.5f), new Vector3(20, 0.05f, 0.2f), boundary, deploymentRoot);
            Piece("North deployment line", PrimitiveType.Cube, new Vector3(20.5f, 0.07f, 31.5f), new Vector3(20, 0.05f, 0.2f), boundary, deploymentRoot);
            Piece("West deployment line", PrimitiveType.Cube, new Vector3(10.5f, 0.07f, 21), new Vector3(0.2f, 0.05f, 21), boundary, deploymentRoot);
            Piece("East deployment line", PrimitiveType.Cube, new Vector3(30.5f, 0.07f, 21), new Vector3(0.2f, 0.05f, 21), boundary, deploymentRoot);
            deploymentRoot.gameObject.SetActive(false);
        }

        private Mesh MakeRingMesh()
        {
            const int segments = 48;
            Vector3[] vertices = new Vector3[segments * 2];
            Vector3[] normals = new Vector3[vertices.Length];
            int[] triangles = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                vertices[i * 2] = direction * 0.5f;
                vertices[i * 2 + 1] = direction * 0.41f;
                normals[i * 2] = normals[i * 2 + 1] = Vector3.up;
                int next = (i + 1) % segments, t = i * 6;
                triangles[t] = i * 2; triangles[t + 1] = i * 2 + 1; triangles[t + 2] = next * 2;
                triangles[t + 3] = next * 2; triangles[t + 4] = i * 2 + 1; triangles[t + 5] = next * 2 + 1;
            }
            Mesh mesh = new Mesh { name = "Hearthhold ring" };
            mesh.vertices = vertices; mesh.normals = normals; mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
        private Mesh MakeTileOutlineMesh()
        {
            const float outer = 0.5f, inner = 0.425f;
            Vector3[] vertices = {
                new Vector3(-outer, 0, -outer), new Vector3(outer, 0, -outer), new Vector3(outer, 0, outer), new Vector3(-outer, 0, outer),
                new Vector3(-inner, 0, -inner), new Vector3(inner, 0, -inner), new Vector3(inner, 0, inner), new Vector3(-inner, 0, inner)
            };
            int[] triangles = { 0,4,1, 1,4,5, 1,5,2, 2,5,6, 2,6,3, 3,6,7, 3,7,0, 0,7,4 };
            Mesh mesh = new Mesh { name = "Hearthhold tile outline" };
            mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
        private Material TileMarkerMaterial(Color color)
        {
            bool danger = color == Danger;
            Material cached = danger ? tileDangerMaterial : tileMintMaterial;
            if (cached != null) return cached;
            Material template = Resources.Load<Material>("OverlayPalette");
            cached = template != null ? new Material(template) : new Material(MaterialFor(color));
            cached.color = color;
            if (cached.HasProperty("_BaseColor")) cached.SetColor("_BaseColor", color);
            if (danger) tileDangerMaterial = cached; else tileMintMaterial = cached;
            return cached;
        }
        private GameObject TileMarker(string markerName, Color color)
        {
            GameObject result = new GameObject(markerName); result.transform.SetParent(transform, false);
            result.AddComponent<MeshFilter>().sharedMesh = tileOutlineMesh;
            result.AddComponent<MeshRenderer>().sharedMaterial = TileMarkerMaterial(color);
            return result;
        }

        private GameObject Ring(string name, Color color, Transform parent)
        {
            GameObject result = new GameObject(name);
            result.transform.SetParent(parent, false);
            result.AddComponent<MeshFilter>().sharedMesh = ringMesh;
            result.AddComponent<MeshRenderer>().sharedMaterial = MaterialFor(color);
            return result;
        }

        private void UpdateSelectionPresentation()
        {
            Building building = session.Battle == null && selected >= 0 && moving < 0 ? session.Find(selected) : null;
            bool wall = building != null && building.Kind == BuildingKind.Wall;
            selectionMarker.SetActive(building != null && !wall);
            int wallMarkers = wall ? Mathf.Max(1, selectedWallIds.Count) : 0;
            while (wallSelectionMarkers.Count < wallMarkers) wallSelectionMarkers.Add(TileMarker("Selected wall cell", Mint));
            for (int i = 0; i < wallSelectionMarkers.Count; i++) wallSelectionMarkers[i].SetActive(i < wallMarkers);
            if (building == null) return;
            float pulse = 1 + Mathf.Sin(Time.unscaledTime * 4) * 0.025f;
            if (wall)
            {
                for (int i = 0; i < wallMarkers; i++)
                {
                    Building segment = session.Find(selectedWallIds.Count > i ? selectedWallIds[i] : building.Id);
                    if (segment == null) continue;
                    wallSelectionMarkers[i].transform.position = new Vector3(segment.X + 0.5f, 0.085f, segment.Z + 0.5f);
                    wallSelectionMarkers[i].transform.localScale = Vector3.one * pulse;
                }
                return;
            }
            float diameter = (building.Spec.Size + 0.9f) * pulse;
            selectionMarker.transform.position = new Vector3(building.X + building.Spec.Size / 2f, 0.08f, building.Z + building.Spec.Size / 2f);
            selectionMarker.transform.localScale = new Vector3(diameter, 1, diameter);
        }
        private void UpdateDeploymentPresentation()
        {
            Building ordered = null;
            if (session.Battle != null && session.Battle.FocusTicks > 0)
                foreach (Building building in session.Battle.Buildings)
                    if (building.Id == session.Battle.FocusTargetId && building.Health > 0) { ordered = building; break; }
            focusMarker.SetActive(ordered != null);
            if (ordered != null)
            {
                float size = ordered.Spec.Size + 0.9f + Mathf.Sin(Time.unscaledTime * 8) * 0.08f;
                focusMarker.transform.position = new Vector3(ordered.CenterX / 1000f, 0.09f, ordered.CenterZ / 1000f);
                focusMarker.transform.localScale = new Vector3(size, 1, size);
            }
            bool active = session.Battle != null && !session.Battle.Finished && !heal && !fury && !freeze && !breach && !focusOrder && !OverHud();
            Vector3 point; int x = 0, z = 0;
            active = active && GroundPoint(out point) && session.Battle.NearestDeployment(Mathf.FloorToInt(point.x) * 1000 + 500, Mathf.FloorToInt(point.z) * 1000 + 500, out x, out z);
            deploymentMarker.SetActive(active);
            if (!active) return;
            float pulse = 1.35f + Mathf.Sin(Time.unscaledTime * 7) * 0.12f;
            deploymentMarker.transform.position = new Vector3(x / 1000f, 0.1f, z / 1000f);
            deploymentMarker.transform.localScale = new Vector3(pulse, 1, pulse);
        }

        private void ShowPlacementPresentation(BuildingKind kind, int level, int x, int z, int size, bool valid)
        {
            placement.SetActive(true);
            placement.transform.position = new Vector3(x + size / 2f, 0.055f, z + size / 2f);
            placement.transform.localScale = new Vector3(size, 0.035f, size);
            placement.GetComponent<Renderer>().sharedMaterial = MaterialFor(valid ? Mint : Danger);
            if (placementPreview == null || placementPreviewKind != kind || placementPreviewLevel != level)
            {
                if (placementPreview != null) Destroy(placementPreview);
                placementPreview = modelViews.BuildingPreview(kind, level, transform);
                placementPreviewKind = kind; placementPreviewLevel = level;
            }
            placementPreview.SetActive(true);
            placementPreview.transform.position = new Vector3(x, 0, z);
            float pulse = 1 + Mathf.Sin(Time.unscaledTime * 5) * 0.012f;
            placementPreview.transform.localScale = Vector3.one * pulse;
            ModelViews.Tint(placementPreview, valid ? new Color(0.72f, 1, 0.82f) : new Color(1, 0.3f, 0.27f));
        }

        private void ShowWallRowPlacement(int targetX, int targetZ, bool valid)
        {
            if (placementPreview != null) placementPreview.SetActive(false);
            Building anchor = session.Find(moving);
            if (anchor == null) return;
            while (wallPlacementMarkers.Count < selectedWallIds.Count)
                wallPlacementMarkers.Add(TileMarker("Wall row placement cell", valid ? Mint : Danger));
            int dx = targetX - anchor.X, dz = targetZ - anchor.Z;
            for (int i = 0; i < wallPlacementMarkers.Count; i++)
            {
                bool active = i < selectedWallIds.Count; GameObject marker = wallPlacementMarkers[i]; marker.SetActive(active);
                if (!active) continue;
                Building wall = session.Find(selectedWallIds[i]);
                if (wall == null) { marker.SetActive(false); continue; }
                marker.GetComponent<MeshRenderer>().sharedMaterial = TileMarkerMaterial(valid ? Mint : Danger);
                marker.transform.position = new Vector3(wall.X + dx + 0.5f, 0.09f, wall.Z + dz + 0.5f);
                marker.transform.localScale = Vector3.one;
            }
        }

        private void HidePlacementPresentation()
        {
            placement.SetActive(false);
            if (placementPreview != null) placementPreview.SetActive(false);
            foreach (GameObject marker in wallPlacementMarkers) if (marker != null) marker.SetActive(false);
        }

        private void ResetBattlePresentation()
        {
            presentedEffects.Clear();
            troopActionsPresented = defenseActionsPresented = 0;
            if (effectsRoot != null) for (int i = effectsRoot.childCount - 1; i >= 0; i--) Destroy(effectsRoot.GetChild(i).gameObject);
            if (deploymentRoot != null) deploymentRoot.gameObject.SetActive(session.Battle != null);
            if (selectionMarker != null) selectionMarker.SetActive(false);
            foreach (GameObject marker in wallSelectionMarkers) if (marker != null) marker.SetActive(false);
            foreach (GameObject marker in wallPlacementMarkers) if (marker != null) marker.SetActive(false);
            if (deploymentMarker != null) deploymentMarker.SetActive(false);
            if (focusMarker != null) focusMarker.SetActive(false);
            HidePlacementPresentation();
        }

        private void PresentBattleEffects()
        {
            if (session.Battle == null) return;
            presentedEffects.RemoveWhere(effect => effect.Ticks <= 0);
            foreach (CombatEffect effect in session.Battle.Effects)
            {
                if (effect.Ticks <= 0) continue;
                if (!presentedEffects.Add(effect)) continue;
                Vector3 start = new Vector3(effect.X / 1000f, 1.15f, effect.Z / 1000f);
                Vector3 end = new Vector3(effect.EndX / 1000f, 1.15f, effect.EndZ / 1000f);
                if (effect.Kind == 0)
                {
                    AnimateUnitAttack(effect.X, effect.Z, end);
                    SpawnArrow(start + Vector3.up * 0.15f, end, Gold, 0.2f, 0.42f);
                    SpawnBurst(end, Gold, 3, 0.7f, 0.32f);
                    FlashBuilding(effect.EndX, effect.EndZ, Color.white);
                }
                else if (effect.Kind == 1)
                {
                    PresentDefenseAttackEffect(effect, start, end);
                }
                else if (effect.Kind == 2)
                {
                    AnimateUnitAttack(effect.X, effect.Z, end);
                    SpawnPulse(end + Vector3.down, Ember, 0.2f, 0.95f, 0.16f);
                    SpawnBurst(end, new Color32(255, 214, 126, 255), 4, 0.8f, 0.28f);
                    FlashBuilding(effect.EndX, effect.EndZ, Danger);
                }
                else if (effect.Kind == 3)
                {
                    PresentHealEffect(effect);
                }
                else if (effect.Kind == 4)
                {
                    SpawnDestruction(new Vector3(effect.X / 1000f, 0.3f, effect.Z / 1000f));
                    FlashBuilding(effect.EndX, effect.EndZ, Ember);
                }
                else if (effect.Kind == 5)
                {
                    AnimateBuildingAttack(effect.X, effect.Z, end);
                    SpawnArrow(start + Vector3.up * 1.75f, end, new Color32(255, 196, 83, 255), 0.17f, 0.3f);
                    SpawnBurst(start + Vector3.up * 1.75f, Gold, 3, 0.65f, 0.2f);
                    SpawnBurst(end, Gold, 3, 0.75f, 0.3f);
                    FlashUnit(effect.EndX, effect.EndZ, Danger);
                }
                else if (effect.Kind == 6)
                {
                    Vector3 center = new Vector3(effect.X / 1000f, 0.15f, effect.Z / 1000f);
                    SpawnPulse(center, Gold, 0.5f, 4.4f, 0.65f);
                    SpawnMotes(center + Vector3.up * 1.3f, Gold, 10);
                    FlashBuilding(effect.X, effect.Z, Gold);
                }
                else if (effect.Kind == 7)
                {
                    PresentFuryEffect(effect);
                }
                else if (effect.Kind == 8)
                {
                    PresentFreezeEffect(effect);
                }
                else if (effect.Kind == 9)
                {
                    PresentBreachEffect(effect);
                }
                else if (effect.Kind == 10)
                {
                    Vector3 center = new Vector3(effect.X / 1000f, 0.2f, effect.Z / 1000f);
                    SpawnPulse(center, new Color32(174, 120, 235, 255), 0.2f, 2.2f, 0.5f); SpawnMotes(center, new Color32(174, 120, 235, 255), 8);
                }
                else if (effect.Kind == 11)
                {
                    AnimateUnitAttack(effect.X, effect.Z, end);
                    SpawnPulse(end, Mint, 0.2f, 1.5f, 0.35f); SpawnMotes(end, Mint, 6); FlashUnit(effect.EndX, effect.EndZ, Mint);
                }
                else if (effect.Kind == 12)
                {
                    SpawnHeroCommand(effect);
                }
            }
        }

        private void SpawnHeroCommand(CombatEffect effect)
        {
            Unit hero = null;
            foreach (Unit unit in session.Battle.Units)
                if (unit.IsHero && unit.Health > 0 && unit.X == effect.X && unit.Z == effect.Z) { hero = unit; break; }
            if (hero == null || !unitViews.ContainsKey(hero.Id)) return;
            Transform heroRoot = unitViews[hero.Id].transform;
            GameObject aura = new GameObject("Hero command aura");
            aura.transform.SetParent(heroRoot, false);
            aura.transform.localPosition = Vector3.up * 0.12f;
            Transform outer = Ring("Ember command boundary", Ember, aura.transform).transform;
            Transform inner = Ring("Bronze command seal", Gold, aura.transform).transform;
            inner.localPosition = Vector3.up * 0.06f;
            Material auraMaterial = Resources.Load<Material>("SpellAura");
            if (auraMaterial != null)
            {
                TintSpellRenderer(outer.GetComponent<Renderer>(), auraMaterial, Ember, 0.34f, 0f);
                TintSpellRenderer(inner.GetComponent<Renderer>(), auraMaterial, Gold, 0.24f, 0f);
            }
            Transform[] embers = new Transform[8];
            for (int i = 0; i < embers.Length; i++)
                embers[i] = Piece("Orbiting command ember", PrimitiveType.Sphere, Vector3.zero,
                    Vector3.one * (i % 2 == 0 ? 0.16f : 0.11f), i % 2 == 0 ? Ember : Gold, aura.transform).transform;
            aura.AddComponent<HeroCommandVisual>().Configure(effect, outer, inner, embers);
            Vector3 center = heroRoot.position + Vector3.up * 0.17f;
            SpawnSoftPulse(center, Gold, 0.6f, 8f, 0.58f, 0.34f);
            SpawnSoftPulse(center + Vector3.up * 0.08f, Ember, 0.35f, 4.8f, 0.42f, 0.25f);
            SpawnBurst(center + Vector3.up * 1.15f, Ember, 12, 2.1f, 0.65f);
            foreach (Unit ally in session.Battle.Units)
            {
                if (ally.Id == hero.Id || ally.Health <= 0 || ally.FuryTicks <= 0) continue;
                long dx = ally.X - hero.X, dz = ally.Z - hero.Z;
                if (dx * dx + dz * dz > 65000000L) continue;
                SpawnPulse(new Vector3(ally.X / 1000f, 0.14f, ally.Z / 1000f), Gold, 0.2f, 1.7f, 0.4f);
            }
            Debug.Log("HEARTHHOLD_HERO_COMMAND_VISUAL_READY: following aura and allied activation pulses.");
        }

        private void MarkFrozenDefense(Building building, GameObject view)
        {
            if (view.transform.Find("Frozen defense seal") != null) return;
            Color ice = new Color32(122, 218, 255, 255);
            GameObject seal = new GameObject("Frozen defense seal");
            seal.transform.SetParent(view.transform, false);
            seal.transform.localPosition = new Vector3(building.Spec.Size * 0.5f, 0.14f, building.Spec.Size * 0.5f);
            Transform ring = Ring("Frozen defense boundary", ice, seal.transform).transform;
            Material auraMaterial = Resources.Load<Material>("SpellAura");
            if (auraMaterial != null) TintSpellRenderer(ring.GetComponent<Renderer>(), auraMaterial, ice, 0.48f, 0f);
            Transform[] crystals = new Transform[4];
            for (int i = 0; i < crystals.Length; i++)
            {
                float angle = (i + 0.5f) * Mathf.PI * 0.5f;
                crystals[i] = Piece("Frozen defense crystal", PrimitiveType.Cube,
                    new Vector3(Mathf.Cos(angle) * (building.Spec.Size * 0.43f), 0.28f,
                        Mathf.Sin(angle) * (building.Spec.Size * 0.43f)),
                    new Vector3(0.15f, 0.48f, 0.15f), ice, seal.transform).transform;
                crystals[i].localRotation = Quaternion.Euler(0, 45f, 16f);
            }
            seal.AddComponent<FrozenDefenseVisual>().Configure(building, ring, crystals, building.Spec.Size + 0.8f);
        }

        private void SpawnProjectile(Vector3 start, Vector3 end, Color color, float duration, float arc)
        {
            GameObject projectile = new GameObject("Heavy projectile"); projectile.transform.SetParent(effectsRoot, false);
            projectile.transform.position = start; projectile.transform.rotation = Quaternion.LookRotation(end - start);
            Piece("Hot core", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.24f, new Color32(255, 225, 151, 255), projectile.transform);
            Piece("Ember shell", PrimitiveType.Sphere, new Vector3(0, 0, -0.15f), Vector3.one * 0.31f, color, projectile.transform);
            Piece("Smoke trail", PrimitiveType.Sphere, new Vector3(0, 0, -0.38f), Vector3.one * 0.25f, new Color32(82, 76, 69, 255), projectile.transform);
            projectile.AddComponent<TimedWorldEffect>().Projectile(start, end, duration, arc);
        }

        private void SpawnArrow(Vector3 start, Vector3 end, Color color, float duration, float arc)
        {
            GameObject arrow = new GameObject("Arrow streak"); arrow.transform.SetParent(effectsRoot, false);
            arrow.transform.position = start; arrow.transform.rotation = Quaternion.LookRotation(end - start);
            Piece("Shaft", PrimitiveType.Cube, new Vector3(0, 0, -0.18f), new Vector3(0.055f, 0.055f, 0.68f), color, arrow.transform);
            Piece("Arrow head", PrimitiveType.Sphere, new Vector3(0, 0, 0.19f), Vector3.one * 0.13f, new Color32(255, 236, 183, 255), arrow.transform);
            arrow.AddComponent<TimedWorldEffect>().Projectile(start, end, duration, arc);
        }

        private void SpawnPulse(Vector3 position, Color color, float startScale, float endScale, float duration)
        {
            GameObject pulse = Ring("Impact ring", color, effectsRoot);
            pulse.transform.position = position;
            pulse.AddComponent<TimedWorldEffect>().Pulse(startScale, endScale, duration);
        }

        private void SpawnSoftPulse(Vector3 position, Color color, float startScale, float endScale, float duration, float alpha)
        {
            GameObject pulse = Ring("Soft command wave", color, effectsRoot);
            pulse.transform.position = position;
            Material auraMaterial = Resources.Load<Material>("SpellAura");
            if (auraMaterial != null) TintSpellRenderer(pulse.GetComponent<Renderer>(), auraMaterial, color, alpha, 0f);
            pulse.AddComponent<TimedWorldEffect>().Pulse(startScale, endScale, duration);
        }

        private void SpawnSpellField(Vector3 center, Color primary, Color secondary, float radius, CombatEffect effect, int style)
        {
            GameObject field = new GameObject("Sustained spell field");
            field.transform.SetParent(effectsRoot, false); field.transform.position = center;
            Material auraMaterial = Resources.Load<Material>("SpellAura");
            GameObject wash = Piece("Soft spell light", PrimitiveType.Plane, Vector3.down * 0.085f,
                new Vector3(radius / 5f, 1f, radius / 5f), primary, field.transform);
            Transform outer = Ring("Spell boundary", primary, field.transform).transform;
            Transform inner = Ring("Spell resonance", secondary, field.transform).transform;
            outer.localPosition = Vector3.zero; inner.localPosition = Vector3.up * 0.045f;
            if (auraMaterial != null)
            {
                TintSpellRenderer(wash.GetComponent<Renderer>(), auraMaterial, primary, 0.17f, 1f);
                TintSpellRenderer(outer.GetComponent<Renderer>(), auraMaterial, primary, 0.32f, 0f);
                TintSpellRenderer(inner.GetComponent<Renderer>(), auraMaterial, secondary, 0.17f, 0f);
            }
            Transform[] motes = new Transform[12];
            for (int i = 0; i < motes.Length; i++)
            {
                Vector3 size = style == 2 ? new Vector3(0.16f, 0.52f, 0.16f) : style == 1 ? new Vector3(0.2f, 0.38f, 0.2f) : new Vector3(0.16f, 0.16f, 0.16f);
                motes[i] = Piece(style == 2 ? "Frost crystal" : style == 1 ? "Ember rune" : "Rising 3D healing mote", style == 2 ? PrimitiveType.Cube : PrimitiveType.Sphere, Vector3.zero, size, i % 3 == 0 ? secondary : primary, field.transform).transform;
            }
            if (style == 0)
            {
                Piece("Healing fountain lower core", PrimitiveType.Sphere, new Vector3(0, 0.30f, 0), new Vector3(0.70f, 0.26f, 0.70f), primary, field.transform);
                Piece("Healing fountain upper core", PrimitiveType.Sphere, new Vector3(0, 1.16f, 0), new Vector3(0.30f, 0.87f, 0.30f), secondary, field.transform);
                for (int i = 0; i < 5; i++)
                {
                    float angle = i * Mathf.PI * 2f / 5f;
                    GameObject petal = Piece("Raised healing petal", PrimitiveType.Capsule,
                        new Vector3(Mathf.Cos(angle) * 0.73f, 0.45f, Mathf.Sin(angle) * 0.73f),
                        new Vector3(0.22f, 0.48f, 0.22f), i % 2 == 0 ? secondary : primary, field.transform);
                    petal.transform.localRotation = Quaternion.Euler(Mathf.Sin(angle) * 38f, 0, -Mathf.Cos(angle) * 38f);
                }
                Debug.Log("HEARTHHOLD_ART_PROTOTYPE_HEAL_3D: volumetric fountain and twelve orbiting motes");
            }
            else if (style == 1)
            {
                Piece("Fury ember heart", PrimitiveType.Sphere, new Vector3(0, 0.62f, 0), new Vector3(0.47f, 0.78f, 0.47f), secondary, field.transform);
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * Mathf.PI * 0.5f;
                    GameObject flame = Piece("Rising fury flame", PrimitiveType.Capsule,
                        new Vector3(Mathf.Cos(angle) * radius * 0.38f, 0.66f, Mathf.Sin(angle) * radius * 0.38f),
                        new Vector3(0.27f, 0.83f + i % 2 * 0.20f, 0.27f), primary, field.transform);
                    flame.transform.localRotation = Quaternion.Euler(Mathf.Sin(angle) * -16f, 0, Mathf.Cos(angle) * 16f);
                }
            }
            else
            {
                Piece("Frost heart", PrimitiveType.Cube, new Vector3(0, 0.81f, 0), new Vector3(0.47f, 1.32f, 0.47f), secondary, field.transform)
                    .transform.localRotation = Quaternion.Euler(0, 45, 0);
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * Mathf.PI * 2f / 6f;
                    GameObject crystal = Piece("Frost spire", PrimitiveType.Cube,
                        new Vector3(Mathf.Cos(angle) * radius * 0.57f, 0.63f, Mathf.Sin(angle) * radius * 0.57f),
                        new Vector3(0.27f, 1.05f + i % 2 * 0.36f, 0.27f), i % 2 == 0 ? primary : secondary, field.transform);
                    crystal.transform.localRotation = Quaternion.Euler(0, 45, i % 2 == 0 ? 12 : -16);
                }
            }
            field.AddComponent<SpellFieldVisual>().Configure(wash.transform, outer, inner, motes, radius, effect, style);
        }

        private static void TintSpellRenderer(Renderer renderer, Material material, Color tint, float alpha, float softness)
        {
            renderer.sharedMaterial = material;
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            properties.SetColor("_Color", new Color(tint.r, tint.g, tint.b, alpha));
            properties.SetFloat("_Softness", softness);
            renderer.SetPropertyBlock(properties);
        }

        private void SpawnDestruction(Vector3 position)
        {
            SpawnPulse(position, new Color32(205, 151, 92, 255), 0.7f, 4.8f, 0.62f);
            SpawnBurst(position + Vector3.up * 0.2f, new Color32(208, 169, 111, 255), 14, 2.2f, 0.95f);
            for (int i = 0; i < 11; i++)
            {
                float angle = i * Mathf.PI * 2 / 11;
                GameObject debris = Piece("Rubble", PrimitiveType.Cube, position + Vector3.up * 0.25f, Vector3.one * (0.18f + (i % 3) * 0.05f), i % 2 == 0 ? Ember : new Color32(116, 91, 70, 255), effectsRoot);
                Vector3 velocity = new Vector3(Mathf.Cos(angle) * 2.1f, 2.4f + (i % 3) * 0.3f, Mathf.Sin(angle) * 2.1f);
                debris.AddComponent<TimedWorldEffect>().Debris(velocity, 0.85f);
            }
        }

        private void SpawnBurst(Vector3 position, Color color, int count, float force, float duration)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2 / Mathf.Max(1, count) + (i % 2) * 0.21f;
                GameObject spark = Piece("Impact spark", i % 3 == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube, position, Vector3.one * (0.08f + i % 3 * 0.025f), color, effectsRoot);
                Vector3 velocity = new Vector3(Mathf.Cos(angle) * force, 0.65f + (i % 4) * force * 0.24f, Mathf.Sin(angle) * force);
                spark.AddComponent<TimedWorldEffect>().Debris(velocity, duration);
            }
        }

        private void SpawnMotes(Vector3 center, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2 / count, radius = 0.7f + i % 5 * 0.78f;
                Vector3 position = center + new Vector3(Mathf.Cos(angle) * radius, 0.06f + i % 3 * 0.08f, Mathf.Sin(angle) * radius);
                GameObject mote = Piece("Healing mote", PrimitiveType.Sphere, position, Vector3.one * (0.08f + i % 2 * 0.035f), color, effectsRoot);
                mote.AddComponent<TimedWorldEffect>().Mote(new Vector3(Mathf.Cos(angle) * 0.18f, 1.2f + i % 4 * 0.2f, Mathf.Sin(angle) * 0.18f), 0.9f);
            }
        }

        private void FlashBuilding(int x, int z, Color color)
        {
            foreach (Building building in session.Battle.Buildings)
                if (building.CenterX == x && building.CenterZ == z && buildingViews.ContainsKey(building.Id))
                {
                    GameObject view = buildingViews[building.Id];
                    Flash(view, color);
                    ModelActionAnimator action = view.GetComponent<ModelActionAnimator>(); if (action != null) action.Hit();
                    return;
                }
        }

        private void AnimateBuildingAttack(int x, int z, Vector3 target)
        {
            foreach (Building building in session.Battle.Buildings)
                if (building.CenterX == x && building.CenterZ == z && buildingViews.ContainsKey(building.Id))
                {
                    ModelActionAnimator action = buildingViews[building.Id].GetComponent<ModelActionAnimator>();
                    if (action != null) { action.Attack(target); defenseActionsPresented++; }
                    return;
                }
        }

        private void AnimateUnitAttack(int x, int z, Vector3 target)
        {
            Unit nearest = null; long best = long.MaxValue;
            foreach (Unit unit in session.Battle.Units)
            {
                long dx = unit.X - x, dz = unit.Z - z, distance = dx * dx + dz * dz;
                if (distance < best) { best = distance; nearest = unit; }
            }
            if (nearest == null || best > 1000000L || !unitViews.ContainsKey(nearest.Id)) return;
            ModelActionAnimator action = unitViews[nearest.Id].GetComponent<ModelActionAnimator>();
            if (action != null) { action.Attack(target); troopActionsPresented++; }
        }

        private void FlashUnit(int x, int z, Color color)
        {
            Unit nearest = null; long best = long.MaxValue;
            foreach (Unit unit in session.Battle.Units)
            {
                long dx = unit.X - x, dz = unit.Z - z, distance = dx * dx + dz * dz;
                if (distance < best) { best = distance; nearest = unit; }
            }
            if (nearest != null && best < 4000000L && unitViews.ContainsKey(nearest.Id))
            {
                GameObject view = unitViews[nearest.Id];
                Flash(view, color);
                ModelActionAnimator action = view.GetComponent<ModelActionAnimator>(); if (action != null) action.Hit();
            }
        }

        private static void Flash(GameObject target, Color color)
        {
            if (target == null) return;
            ModelHitFlash flash = target.GetComponent<ModelHitFlash>();
            if (flash == null) flash = target.AddComponent<ModelHitFlash>();
            flash.Trigger(color);
        }

        private void PrepareBattleSmoke()
        {
            // The battle presentation fixture intentionally instantiates all eight troop roles and four spells.
            foreach (Building building in session.Village.Buildings) if (building.Kind == BuildingKind.TrainingCamp) { building.Level = 3; building.Health = building.MaxHealth; }
            foreach (Building building in session.Village.Buildings) if (building.Kind == BuildingKind.Keep) { building.Level = 4; building.Health = building.MaxHealth; }
            if (session.Village.Count(BuildingKind.HeroHall) == 0) session.Village.Add(BuildingKind.HeroHall, 5, 5);
            if (session.Village.Count(BuildingKind.PetLodge) == 0) session.Village.Add(BuildingKind.PetLodge, 9, 5);
            session.Village.EnsureHeroes(); session.Village.HeroLevels[(int)HeroKind.EmberWarden] = 2; session.Village.PetLevels[(int)PetKind.CinderFox] = 2;
            session.Village.HeroPetAssignments[(int)HeroKind.EmberWarden] = (int)PetKind.CinderFox;
            session.Village.EnsureTechnology();
            for (int i = 0; i < session.Village.SpellLevels.Count; i++) session.Village.SpellLevels[i] = 3;
            session.Village.HealLevel = 3;
            for (int i = 0; i < Rules.FormationCounts[4].Length; i++) session.Village.ArmyCounts[i] = Rules.FormationCounts[4][i];
            for (int i = 0; i < Missions.Count - 1; i++) session.Village.RecordMission(i, 1, 55 + i * 3);
            session.MissionIndex = Missions.Count - 1;
            session.BeginBattle();
            if (session.Battle == null) return;
            showBrief = false; briefSeen = true;
            RebuildBuildings();
            troop = TroopKind.Guardian;
            int before = session.Battle.Units.Count;
            if (!TryBattleActionAt(20, 20) || session.Battle.Units.Count != before + 1 || !session.Battle.CanDeploy(session.Battle.Units[before].X, session.Battle.Units[before].Z))
            {
                Debug.LogError("HEARTHHOLD_DEPLOY_SMOKE_FAILED: central battlefield click did not reach a legal deployment cell.");
                Application.Quit(4);
                return;
            }
            Debug.Log("HEARTHHOLD_DEPLOY_SMOKE_READY: central battlefield click snapped to a legal deployment cell.");
            for (int i = 0; i < Rules.Troops.Length; i++) session.Battle.Deploy((TroopKind)i, 9500, 15000 + i * 2100);
            bool heroReady = session.Battle.DeployHeroNearest(HeroKind.EmberWarden, 9500, 30000) && session.Battle.CastHeroSkill(HeroKind.EmberWarden);
            Unit petSmoke = null; foreach (Unit unit in session.Battle.Units) if (unit.IsPet) petSmoke = unit;
            if (!heroReady || petSmoke == null || petSmoke.BondedHeroUnitId < 0) { Debug.LogError("HEARTHHOLD_HERO_SMOKE_FAILED: hero, bound pet or active skill failed."); Application.Quit(4); return; }
            Debug.Log("HEARTHHOLD_HERO_SMOKE_READY: Ember Warden deployed with a bound independent pet and cast its skill.");
            session.Battle.Deploy(TroopKind.Guardian, 9500, 19000);
            session.Battle.Deploy(TroopKind.Ranger, 8500, 20500);
            bool breachReady = session.Battle.CastBreach(11500, 20000);
            bool furyReady = session.Battle.CastFury(9500, 22000);
            bool freezeReady = session.Battle.CastFreeze(18000, 15000);
            if (!breachReady || !furyReady || !freezeReady)
            {
                Debug.LogError("HEARTHHOLD_SPELL_SMOKE_FAILED: one or more 0.9 tactical spells had no valid target.");
                Application.Quit(4); return;
            }
            Debug.Log("HEARTHHOLD_SPELL_SMOKE_READY: breach, fury and freeze accepted valid targets.");
            for (int i = 0; i < 240 && !session.Battle.Finished; i++) session.Battle.Step();
            Building ordered = null;
            foreach (Building building in session.Battle.Buildings)
                if (building.Health > 0 && building.Kind != BuildingKind.Wall) { ordered = building; break; }
            if (ordered == null || !session.Battle.CastFocus(ordered.CenterX, ordered.CenterZ))
            {
                Debug.LogError("HEARTHHOLD_FOCUS_SMOKE_FAILED: no live target could receive the command.");
                Application.Quit(4);
                return;
            }
            Debug.Log("HEARTHHOLD_FOCUS_SMOKE_READY: tactical target accepted.");
            session.Notice = "0.9 战斗验收：八兵种、六种防御与四类法术。";
        }

        private void PrepareSpellFieldSmoke()
        {
            PrepareBattleSmoke();
            if (session.Battle == null || session.Battle.Finished) return;
            session.Battle.SpellCharges = session.Battle.FuryCharges = session.Battle.FreezeCharges = 1;
            if (!session.Battle.CastHeal(10500, 26000) || !session.Battle.CastFury(20500, 24000)
                || !session.Battle.CastFreeze(30000, 15000))
                Debug.LogError("HEARTHHOLD_SPELL_FIELDS_FAILED: could not cast all three sustained fields.");
        }

        private int feedbackHeroId = -1, feedbackPetId = -1, feedbackFrozenDefenseId = -1;
        private void PrepareCombatFeedbackSmoke()
        {
            if (session.Village.Count(BuildingKind.HeroHall) == 0) session.Village.Add(BuildingKind.HeroHall, 5, 5);
            if (session.Village.Count(BuildingKind.PetLodge) == 0) session.Village.Add(BuildingKind.PetLodge, 9, 5);
            session.Village.EnsureHeroes();
            session.Village.HeroLevels[0] = 2; session.Village.PetLevels[0] = 2; session.Village.HeroPetAssignments[0] = 0;
            session.Village.EnsureTechnology(); session.Village.SpellLevels[(int)SpellKind.Freeze] = 1;
            session.BeginBattle();
            if (session.Battle == null || !session.Battle.DeployHeroNearest(HeroKind.EmberWarden, 9500, 30000)
                || !session.Battle.CastHeroSkill(HeroKind.EmberWarden))
            { Debug.LogError("HEARTHHOLD_COMBAT_FEEDBACK_FAILED: hero skill could not be activated."); return; }
            foreach (Unit unit in session.Battle.Units)
            {
                if (unit.IsHero) feedbackHeroId = unit.Id;
                if (unit.IsPet) feedbackPetId = unit.Id;
            }
            foreach (Building building in session.Battle.Buildings)
                if (building.Spec.Damage > 0) { feedbackFrozenDefenseId = building.Id; session.Battle.CastFreeze(building.CenterX, building.CenterZ); break; }
            showBrief = false; briefSeen = true;
            RebuildBuildings(); SyncBattle();
            Unit pet = session.Battle.Units.Find(delegate(Unit unit) { return unit.Id == feedbackPetId; });
            if (pet != null) { FlashUnit(pet.X, pet.Z, Danger); FlashUnit(pet.X, pet.Z, Danger); }
            focus = new Vector3(13, 0, 27); worldCamera.orthographicSize = 11.5f; MoveCamera();
        }

        private bool VerifyCombatFeedbackSmoke()
        {
            GameObject hero, pet, defense;
            bool ready = unitViews.TryGetValue(feedbackHeroId, out hero) && hero != null
                && hero.transform.Find("Hero command aura") != null
                && unitViews.TryGetValue(feedbackPetId, out pet) && pet != null
                && pet.GetComponent<ModelHitFlash>() != null && pet.GetComponent<ModelHitFlash>().AppearanceRestored()
                && buildingViews.TryGetValue(feedbackFrozenDefenseId, out defense) && defense != null
                && defense.transform.Find("Frozen defense seal") != null;
            if (ready) Debug.Log("HEARTHHOLD_COMBAT_FEEDBACK_READY: pet tint restored, hero aura active, frozen defense marked.");
            else Debug.LogError("HEARTHHOLD_COMBAT_FEEDBACK_FAILED: a combat visual is absent or pet tint was not restored.");
            return ready;
        }

        private GameObject artProofVanguard, artProofPet, artProofField;
        private void PrepareArtProofSmoke(bool rotated)
        {
            PrepareHomeSmoke();
            Building keep = session.Village.Buildings.Find(delegate(Building b) { return b.Kind == BuildingKind.Keep; });
            if (keep == null) return;
            // Isolate the four specimens for honest visual comparison; gameplay villages remain untouched.
            session.Village.Buildings.RemoveAll(delegate(Building b) { return b.Kind != BuildingKind.Keep; });
            keep.Level = 3; keep.Health = keep.MaxHealth; RebuildBuildings();
            artProofVanguard = modelViews.Troop(new Unit { Kind = TroopKind.Vanguard }, unitsRoot, 2);
            artProofVanguard.transform.position = rotated ? new Vector3(26f, 0, 22f) : new Vector3(11f, 0, 21f);
            artProofVanguard.transform.rotation = Quaternion.Euler(0, -28, 0);
            artProofPet = modelViews.Troop(new Unit { IsPet = true, PetKind = PetKind.CinderFox, Kind = TroopKind.Sapper }, unitsRoot, 2);
            artProofPet.transform.position = rotated ? new Vector3(12f, 0, 14f) : new Vector3(17f, 0, 18f);
            artProofPet.transform.rotation = Quaternion.Euler(0, -25, 0);
            Vector3 spellCenter = rotated ? new Vector3(15f, 0.12f, 22f) : new Vector3(23.5f, 0.12f, 12.5f);
            CombatEffect source = new CombatEffect(Mathf.RoundToInt(spellCenter.x * 1000f), Mathf.RoundToInt(spellCenter.z * 1000f), Mathf.RoundToInt(spellCenter.x * 1000f), Mathf.RoundToInt(spellCenter.z * 1000f), 300, 3);
            SpawnSpellField(spellCenter, Mint, new Color32(188, 255, 213, 255), 2.8f, source, 0);
            artProofField = effectsRoot.GetChild(effectsRoot.childCount - 1).gameObject;
            artProofField.AddComponent<ArtProofEffectClock>().Source = source;
            focus = new Vector3(19, 0, 17f); worldCamera.orthographicSize = 9.8f;
            cameraYaw = rotated ? 135 : 45; worldCamera.transform.rotation = Quaternion.Euler(45, cameraYaw, 0); MoveCamera();
            selected = -1; showBrief = false; briefSeen = true;
        }
        private bool VerifyArtProofSmoke()
        {
            bool ready = artProofVanguard != null && artProofPet != null && artProofField != null
                && artProofVanguard.GetComponentInChildren<SkinnedMeshRenderer>() != null
                && artProofPet.GetComponentsInChildren<MeshFilter>().Length >= 15
                && artProofPet.GetComponent<CinderFoxArtAnimator>() != null
                && artProofPet.transform.Find("3D model/Fox tail joint/Sculpted tapering fire tail") != null
                && artProofField.transform.Find("Healing fountain upper core") != null
                && artProofField.transform.Find("Raised healing petal") != null
                && sceneryRoot.Find("Painted meadow surface") != null
                && artProofVanguard.GetComponentInChildren<FixedIsometricBillboard>() == null
                && artProofPet.GetComponentInChildren<FixedIsometricBillboard>() == null;
            if (ready) Debug.Log("HEARTHHOLD_ART_PROOF_READY: real 3D keep, rigged troop, articulated pet, volumetric heal; yaw=" + cameraYaw);
            else Debug.LogError("HEARTHHOLD_ART_PROOF_FAILED: 3D specimen incomplete");
            return ready;
        }

        private bool VerifySpellFieldSmoke()
        {
            int fields = 0, distinctCores = 0;
            foreach (Transform child in effectsRoot)
            {
                if (child.name != "Sustained spell field" || !child.gameObject.activeInHierarchy) continue;
                fields++;
                if (child.Find("Healing fountain upper core") != null || child.Find("Fury ember heart") != null || child.Find("Frost heart") != null) distinctCores++;
            }
            if (session.Battle == null || session.Battle.SpellZones.Count != 3 || fields != 3 || distinctCores != 3)
            {
                Debug.LogError("HEARTHHOLD_SPELL_FIELDS_FAILED: zones=" + (session.Battle == null ? -1 : session.Battle.SpellZones.Count) + " visuals=" + fields + " distinctCores=" + distinctCores);
                return false;
            }
            Debug.Log("HEARTHHOLD_SPELL_FIELDS_READY: three sustained rule zones have distinct living world visuals.");
            return true;
        }

        private void PrepareDeploySmoke()
        {
            session.MissionIndex = 0;
            session.BeginBattle();
            if (session.Battle == null) return;
            showBrief = false; briefSeen = true;
            RebuildBuildings();
            focus = new Vector3(20, 0, 20); worldCamera.orthographicSize = 20; MoveCamera();
            troop = TroopKind.Vanguard;
            if (!TryBattleActionAt(20, 20) || session.Battle.Units.Count != 1 || !session.Battle.CanDeploy(session.Battle.Units[0].X, session.Battle.Units[0].Z))
            {
                Debug.LogError("HEARTHHOLD_DEPLOY_SMOKE_FAILED: central battlefield click did not reach a legal deployment cell.");
                Application.Quit(4);
                return;
            }
            Debug.Log("HEARTHHOLD_DEPLOY_SMOKE_READY: central battlefield click snapped to a legal deployment cell.");
            session.Notice = "0.9 投兵验收：八兵种均可从绿色战线部署。";
        }

        private void PrepareCampaignSmoke()
        {
            session.Village.RecordMission(0, 3, 100);
            session.Village.RecordMission(1, 2, 78);
            session.Village.RecordMission(2, 1, 56);
            session.Village.Wins = 3;
            session.MissionIndex = 3;
            selected = -1; showCampaign = true;
            session.Notice = "0.6.3 战役进度验收：逐关解锁、最佳纪录与成就奖励。";
        }
        private void PrepareBuildCatalogSmoke()
        {
            PrepareHomeSmoke(); showBuildCatalog = true;
            session.Notice = "建造目录验收：价格、用途、解锁和数量上限集中展示。";
        }
        private void PrepareLayoutEditorSmoke(bool tray)
        {
            PrepareHomeSmoke(); EnterLayoutEditor();
            if (tray)
            {
                ClearLayoutToTray(); int before = layoutStaged.Count;
                if (before > 0) { BeginStagedPlacement(layoutStaged[0].Id, false); PlaceStagedBuilding(2, 2); }
                if (session.Village.Buildings.Count != 1 || layoutStaged.Count != before - 1)
                    Debug.LogError("HEARTHHOLD_LAYOUT_PLACEMENT_SMOKE_FAILED: click placement did not transfer one building from tray to map.");
                else Debug.Log("HEARTHHOLD_LAYOUT_PLACEMENT_SMOKE_READY: placed=1 staged=" + layoutStaged.Count);
            }
            else { SelectAllLayoutBuildings(); if (smokeLayoutPointer) VerifyLayoutPointerSmoke(); }
            Debug.Log("HEARTHHOLD_LAYOUT_EDITOR_SMOKE_READY: tray=" + tray + " placed=" + session.Village.Buildings.Count + " staged=" + layoutStaged.Count);
        }
        private void PrepareWallRowSmoke()
        {
            selectedWallIds.Clear();
            foreach (Building building in session.Village.Buildings)
            {
                if (building.Kind != BuildingKind.Wall || session.WallRow(building.Id).Count < 3) continue;
                selected = building.Id; ToggleWallRowSelection(building); break;
            }
            if (selectedWallIds.Count < 3)
            {
                Debug.LogError("HEARTHHOLD_WALL_ROW_SMOKE_FAILED: no continuous wall row was selected.");
                Application.Quit(4); return;
            }
            Debug.Log("HEARTHHOLD_WALL_ROW_SMOKE_READY: selected=" + selectedWallIds.Count);
            session.Notice = "城墙坐标验收：每段墙体与青色 1×1 格点框共用同一中心。";
        }
        private void PrepareWallAxesSmoke()
        {
            for (int z = 15; z <= 20; z++) session.Village.Add(BuildingKind.Wall, 25, z);
            RebuildBuildings(); selectedWallIds.Clear();
            foreach (Building building in session.Village.Buildings)
                if (building.Kind == BuildingKind.Wall) { selectedWallIds.Add(building.Id); if (selected < 0) selected = building.Id; }
            focus = new Vector3(21.5f, 0, 20); worldCamera.orthographicSize = 9.5f; MoveCamera();
            session.Notice = "城墙双轴验收：横向与纵向模型均与各自青色格框同心。";
            Debug.Log("HEARTHHOLD_WALL_AXES_SMOKE_READY: selected=" + selectedWallIds.Count);
        }
        private void PrepareHeroesSmoke(bool showPet)
        {
            Building keep = session.Find(1); keep.Level = 4; keep.Health = keep.MaxHealth;
            Building hall = session.Village.Add(BuildingKind.HeroHall, 5, 5);
            Building lodge = session.Village.Add(BuildingKind.PetLodge, 31, 5);
            hall.Level = lodge.Level = 2; hall.Health = hall.MaxHealth; lodge.Health = lodge.MaxHealth;
            session.Village.EnsureHeroes(); session.Village.HeroLevels[0] = 1; session.Village.PetLevels[0] = 1;
            session.Village.HeroPetAssignments[0] = 0; session.Village.Gold = session.Village.Capacity; session.Village.Crystal = session.Village.Capacity;
            if (smokeEquipment)
            {
                session.Village.EnsureEquipment(); session.Village.Stardust = 240; session.Village.CoreSigils = 1; session.Village.HeroHallPermit = true;
                session.Village.EquipmentLevels[0] = 1; session.Village.EquipmentLevels[1] = 1; session.Village.EquipmentLevels[2] = 1;
                session.Village.HeroEquipmentSlots[0] = 0; session.Village.HeroEquipmentSlots[1] = 2;
                heroEquipmentTab = true; selectedEquipmentKind = 2;
            }
            RebuildBuildings(); rosterSelection = showPet ? 1 : 0; showHeroes = true;
            session.Notice = "英雄殿堂验收：动态名册、实时动作、数值详情、灵契与升级入口。";
            Debug.Log("HEARTHHOLD_HERO_HALL_SMOKE_READY: hall=2 lodge=2 selected=" + (showPet ? "pet" : "hero"));
        }

        private void PrepareTrainingSmoke()
        {
            session.Village.ArmyCounts[(int)TroopKind.Vanguard] -= 2;
            session.QueueTroop(TroopKind.Vanguard, System.DateTime.UtcNow);
            session.QueueTroop(TroopKind.Vanguard, System.DateTime.UtcNow);
            selected = -1; showTraining = true;
            session.Notice = "0.9 编队验收：八兵种、营位与五种战术预设。";
        }

        private void PrepareResearchSmoke()
        {
            Building lab = null;
            foreach (Building building in session.Village.Buildings) if (building.Kind == BuildingKind.Laboratory) { lab = building; break; }
            session.Village.Gold = session.Village.Capacity; session.Village.Crystal = session.Village.Capacity;
            session.Upgrade(1);
            if (lab != null) session.Upgrade(lab.Id);
            session.ResearchTroop(TroopKind.Vanguard);
            session.ResearchHeal();
            session.ResearchSpell(SpellKind.Fury);
            session.ResearchSpell(SpellKind.Freeze);
            selected = lab == null ? -1 : lab.Id; showResearch = true;
            session.Notice = "0.9 科技验收：八兵种与四类法术共享规则数据。";
        }

        private void PrepareProgressionSmoke()
        {
            selected = -1; showProgression = true;
            session.Notice = "发展路线验收：议事堡1—3级实装内容与4—8级规划。";
        }

        private void PrepareHomeSmoke()
        {
            foreach (Building building in session.Village.Buildings)
                if (building.Kind == BuildingKind.TrainingCamp) { selected = building.Id; break; }
            session.Notice = "0.9 聚落验收：十三类建筑以三维网格贴地显示。";
        }

        private void DisposePresentation()
        {
            if (ringMesh != null) Destroy(ringMesh);
            if (tileOutlineMesh != null) Destroy(tileOutlineMesh);
            if (tileMintMaterial != null) Destroy(tileMintMaterial);
            if (tileDangerMaterial != null) Destroy(tileDangerMaterial);
        }
    }

    internal sealed class FrozenDefenseVisual : MonoBehaviour
    {
        private Building building;
        private Transform ring;
        private Transform[] crystals;
        private float diameter, elapsed;

        public void Configure(Building target, Transform boundary, Transform[] iceCrystals, float ringDiameter)
        { building = target; ring = boundary; crystals = iceCrystals; diameter = ringDiameter; }

        private void Update()
        {
            if (building == null || building.Health <= 0 || building.FrozenTicks <= 0) { Destroy(gameObject); return; }
            elapsed += Time.deltaTime;
            ring.localScale = Vector3.one * diameter * (1f + Mathf.Sin(elapsed * 2.8f) * 0.035f);
            ring.Rotate(0, -Time.deltaTime * 13f, 0);
            for (int i = 0; i < crystals.Length; i++)
                crystals[i].localPosition = new Vector3(crystals[i].localPosition.x,
                    0.28f + Mathf.Sin(elapsed * 2f + i * 1.3f) * 0.08f, crystals[i].localPosition.z);
        }
    }

    internal sealed class HeroCommandVisual : MonoBehaviour
    {
        private CombatEffect source;
        private Transform outer, inner;
        private Transform[] embers;
        private int initialTicks;

        public void Configure(CombatEffect effect, Transform boundary, Transform seal, Transform[] orbitingEmbers)
        { source = effect; initialTicks = effect.Ticks; outer = boundary; inner = seal; embers = orbitingEmbers; }

        private void Update()
        {
            if (source == null || source.Ticks <= 0) { Destroy(gameObject); return; }
            float elapsed = (initialTicks - source.Ticks) / (float)Rules.TicksPerSecond;
            float presence = Mathf.Clamp01(elapsed / 0.22f) * Mathf.Clamp01(source.Ticks / (Rules.TicksPerSecond * 0.5f));
            float breath = 1f + Mathf.Sin(elapsed * 4.2f) * 0.035f;
            outer.localScale = Vector3.one * (5.5f * breath * presence);
            inner.localScale = Vector3.one * (3.1f * (2f - breath) * presence);
            outer.Rotate(0, Time.deltaTime * 20f, 0);
            inner.Rotate(0, -Time.deltaTime * 34f, 0);
            for (int i = 0; i < embers.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / embers.Length + elapsed * 1.1f;
                float radius = 1.5f + i % 2 * 0.25f;
                embers[i].localPosition = new Vector3(Mathf.Cos(angle) * radius,
                    0.27f + i % 3 * 0.16f + Mathf.Sin(elapsed * 4f + i) * 0.11f, Mathf.Sin(angle) * radius);
                embers[i].localScale = Vector3.one * (i % 2 == 0 ? 0.16f : 0.11f) * presence;
            }
        }
    }

    internal sealed class SpellFieldVisual : MonoBehaviour
    {
        private Transform wash, outer, inner;
        private Transform[] motes;
        private Vector3[] moteScales;
        private float radius, elapsed;
        private int initialTicks;
        private CombatEffect source;
        private int style;

        public void Configure(Transform groundWash, Transform boundary, Transform resonance, Transform[] particles, float fieldRadius, CombatEffect effect, int fieldStyle)
        {
            wash = groundWash; outer = boundary; inner = resonance; motes = particles; radius = fieldRadius; source = effect; initialTicks = effect.Ticks; style = fieldStyle;
            moteScales = new Vector3[particles.Length];
            for (int i = 0; i < particles.Length; i++) moteScales[i] = particles[i].localScale;
        }

        private void Update()
        {
            if (source == null || source.Ticks <= 0) { Destroy(gameObject); return; }
            elapsed = (initialTicks - source.Ticks) / (float)Rules.TicksPerSecond;
            float reveal = Mathf.Clamp01(elapsed / 0.28f);
            float retreat = Mathf.Clamp01(source.Ticks / (Rules.TicksPerSecond * 0.45f));
            float presence = reveal * retreat;
            float beat = 1f + Mathf.Sin(elapsed * (style == 2 ? 2.8f : 4f)) * 0.025f;
            wash.localScale = new Vector3(radius / 5f * beat * presence, 1f, radius / 5f * beat * presence);
            outer.localScale = Vector3.one * (radius * 2f * beat * presence);
            inner.localScale = Vector3.one * (radius * 1.46f * (2f - beat) * presence);
            outer.Rotate(0, Time.deltaTime * (style == 2 ? -12f : 18f), 0);
            inner.Rotate(0, Time.deltaTime * (style == 1 ? -30f : 24f), 0);
            for (int i = 0; i < motes.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / motes.Length + elapsed * (style == 1 ? 0.28f : -0.17f);
                float distance = radius * (style == 0 ? 0.20f + i % 4 * 0.085f : 0.72f + i % 3 * 0.085f);
                float lift = style == 0 ? 0.33f + i % 4 * 0.28f + Mathf.Sin(elapsed * 3.4f + i * 0.8f) * 0.13f
                    : style == 1 ? 0.35f + Mathf.Sin(elapsed * 5f + i * 0.7f) * 0.18f
                    : 0.34f + Mathf.Sin(elapsed * 1.5f + i) * 0.08f;
                motes[i].localPosition = new Vector3(Mathf.Cos(angle) * distance, lift, Mathf.Sin(angle) * distance);
                motes[i].localScale = moteScales[i] * presence;
                motes[i].Rotate(0, Time.deltaTime * (style == 2 ? 45f : 90f), 0);
            }
        }
    }

    internal sealed class ArtProofEffectClock : MonoBehaviour
    {
        public CombatEffect Source;
        private void Update() { if (Source != null && Source.Ticks > 0) Source.Ticks--; }
    }

    internal sealed class TimedWorldEffect : MonoBehaviour
    {
        private enum EffectMode { Projectile, Pulse, Debris, Mote, Hold }
        private EffectMode mode;
        private Vector3 start, end, velocity;
        private float elapsed, duration, arc, startScale, endScale;

        public void Projectile(Vector3 from, Vector3 to, float seconds, float height)
        { mode = EffectMode.Projectile; start = from; end = to; duration = seconds; arc = height; transform.position = from; }
        public void Pulse(float from, float to, float seconds)
        { mode = EffectMode.Pulse; startScale = from; endScale = to; duration = seconds; transform.localScale = Vector3.one * from; }
        public void Debris(Vector3 initialVelocity, float seconds)
        { mode = EffectMode.Debris; velocity = initialVelocity; duration = seconds; }
        public void Mote(Vector3 initialVelocity, float seconds)
        { mode = EffectMode.Mote; velocity = initialVelocity; duration = seconds; }
        public void Hold(float seconds)
        { mode = EffectMode.Hold; duration = seconds; }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            if (mode == EffectMode.Projectile)
                transform.position = Vector3.Lerp(start, end, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * arc;
            else if (mode == EffectMode.Pulse)
            {
                float scale = Mathf.Lerp(startScale, endScale, 1 - (1 - t) * (1 - t));
                transform.localScale = new Vector3(scale, 1, scale);
                transform.Rotate(0, Time.deltaTime * 90, 0);
            }
            else if (mode == EffectMode.Debris)
            {
                velocity += Vector3.down * 7.5f * Time.deltaTime;
                transform.position += velocity * Time.deltaTime;
                transform.Rotate(180 * Time.deltaTime, 260 * Time.deltaTime, 110 * Time.deltaTime);
            }
            else if (mode == EffectMode.Mote)
            {
                transform.position += velocity * Time.deltaTime;
                transform.Rotate(0, 150 * Time.deltaTime, 0);
                transform.localScale *= 1 - Time.deltaTime * 0.55f;
            }
            if (elapsed >= duration) Destroy(gameObject);
        }
    }

    internal sealed class ModelHitFlash : MonoBehaviour
    {
        private Renderer[] targets;
        private MaterialPropertyBlock properties;
        private MaterialPropertyBlock[] originalProperties;
        private Color[] originalColors;
        private Color color;
        private float remaining;
        public void Trigger(Color flashColor)
        {
            if (targets == null) CaptureOriginalAppearance();
            color = flashColor; remaining = 0.2f;
            Apply(1f);
        }
        private void CaptureOriginalAppearance()
        {
            List<Renderer> visible = new List<Renderer>();
            foreach (Renderer candidate in GetComponentsInChildren<Renderer>())
                if (candidate.gameObject.name != "Ground contact shadow") visible.Add(candidate);
            targets = visible.ToArray();
            originalProperties = new MaterialPropertyBlock[targets.Length];
            originalColors = new Color[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                originalProperties[i] = new MaterialPropertyBlock();
                targets[i].GetPropertyBlock(originalProperties[i]);
                Material material = targets[i].sharedMaterial;
                Color baseColor = material != null && material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor")
                    : material != null && material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                Color overrideColor = originalProperties[i].GetColor("_BaseColor");
                originalColors[i] = overrideColor.a > 0.001f ? overrideColor : baseColor;
            }
        }
        private void Update()
        {
            if (remaining <= 0) return;
            remaining = Mathf.Max(0, remaining - Time.deltaTime);
            if (remaining <= 0) { Restore(); return; }
            Apply(remaining / 0.2f);
        }
        private void Apply(float strength)
        {
            if (targets == null) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            for (int i = 0; i < targets.Length; i++)
            {
                Renderer target = targets[i];
                if (target == null) continue;
                target.GetPropertyBlock(properties);
                properties.SetColor("_BaseColor", Color.Lerp(originalColors[i], color, strength * 0.65f));
                target.SetPropertyBlock(properties);
            }
        }
        private void Restore()
        {
            if (targets == null) return;
            for (int i = 0; i < targets.Length; i++) if (targets[i] != null) targets[i].SetPropertyBlock(originalProperties[i]);
            remaining = 0;
        }
        public bool AppearanceRestored()
        {
            if (remaining > 0 || targets == null || targets.Length == 0) return false;
            MaterialPropertyBlock current = new MaterialPropertyBlock();
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;
                targets[i].GetPropertyBlock(current);
                if (current.GetColor("_BaseColor") != originalProperties[i].GetColor("_BaseColor")) return false;
            }
            return true;
        }
        private void OnDisable() { Restore(); }
    }
}
