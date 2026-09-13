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
        private GameObject selectionMarker, deploymentMarker, placementPreview;
        private BuildingKind? placementPreviewKind;
        private int placementPreviewLevel;
        private Transform effectsRoot, deploymentRoot;
        private Mesh ringMesh;
        private int troopActionsPresented, defenseActionsPresented;

        private void InitializePresentation()
        {
            ringMesh = MakeRingMesh();
            selectionMarker = Ring("Selected building", Gold, transform);
            selectionMarker.SetActive(false);
            deploymentMarker = Ring("Nearest deployment point", Mint, transform);
            deploymentMarker.SetActive(false);
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
            selectionMarker.SetActive(building != null);
            if (building == null) return;
            float pulse = 1 + Mathf.Sin(Time.unscaledTime * 4) * 0.025f;
            float diameter = (building.Spec.Size + 0.9f) * pulse;
            selectionMarker.transform.position = new Vector3(building.X + building.Spec.Size / 2f, 0.08f, building.Z + building.Spec.Size / 2f);
            selectionMarker.transform.localScale = new Vector3(diameter, 1, diameter);
        }
        private void UpdateDeploymentPresentation()
        {
            bool active = session.Battle != null && !session.Battle.Finished && !heal && !OverHud();
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

        private void HidePlacementPresentation()
        {
            placement.SetActive(false);
            if (placementPreview != null) placementPreview.SetActive(false);
        }

        private void ResetBattlePresentation()
        {
            presentedEffects.Clear();
            troopActionsPresented = defenseActionsPresented = 0;
            if (effectsRoot != null) for (int i = effectsRoot.childCount - 1; i >= 0; i--) Destroy(effectsRoot.GetChild(i).gameObject);
            if (deploymentRoot != null) deploymentRoot.gameObject.SetActive(session.Battle != null);
            if (selectionMarker != null) selectionMarker.SetActive(false);
            if (deploymentMarker != null) deploymentMarker.SetActive(false);
            HidePlacementPresentation();
        }

        private void PresentBattleEffects()
        {
            if (session.Battle == null) return;
            foreach (CombatEffect effect in session.Battle.Effects)
            {
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
                    AnimateBuildingAttack(effect.X, effect.Z, end);
                    SpawnProjectile(start + Vector3.up * 0.65f, end, Ember, 0.28f, 1.35f);
                    SpawnBurst(start + Vector3.up * 0.65f, new Color32(255, 204, 104, 255), 5, 1.25f, 0.28f);
                    SpawnBurst(end, Ember, 8, 1.8f, 0.55f);
                    FlashUnit(effect.EndX, effect.EndZ, Danger);
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
                    Vector3 center = new Vector3(effect.X / 1000f, 0.12f, effect.Z / 1000f);
                    SpawnPulse(center, Mint, 0.5f, 9.6f, 0.85f);
                    SpawnPulse(center + Vector3.up * 0.04f, new Color32(184, 255, 219, 255), 0.25f, 6.8f, 0.62f);
                    SpawnMotes(center, Mint, 14);
                    foreach (Unit unit in session.Battle.Units)
                    {
                        long dx = unit.X - effect.X, dz = unit.Z - effect.Z;
                        if (unit.Health > 0 && dx * dx + dz * dz <= 25000000L) Flash(unitViews.ContainsKey(unit.Id) ? unitViews[unit.Id] : null, Mint);
                    }
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
            }
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
            int[] rows = { 17500, 19500, 21500, 23500 };
            for (int i = 0; i < 2; i++) session.Battle.Deploy(TroopKind.Guardian, 9500, rows[i]);
            for (int i = 0; i < 3; i++) session.Battle.Deploy(TroopKind.Sapper, 9500, 20500 + i * 700);
            for (int i = 0; i < 5; i++) session.Battle.Deploy(TroopKind.Vanguard, 9000, 17500 + i * 1200);
            for (int i = 0; i < 5; i++) session.Battle.Deploy(TroopKind.Ranger, 7500, 17000 + i * 1400);
            for (int i = 0; i < 240 && !session.Battle.Finished; i++) session.Battle.Step();
            session.Notice = "0.6.3 战斗验收：贴地建筑、投兵与交战动作。";
        }

        private void PrepareDeploySmoke()
        {
            session.MissionIndex = 0;
            session.BeginBattle();
            if (session.Battle == null) return;
            showBrief = false; briefSeen = true;
            RebuildBuildings();
            focus = new Vector3(20, 0, 20); worldCamera.orthographicSize = 24; MoveCamera();
            troop = TroopKind.Guardian;
            if (!TryBattleActionAt(20, 20) || session.Battle.Units.Count != 1 || !session.Battle.CanDeploy(session.Battle.Units[0].X, session.Battle.Units[0].Z))
            {
                Debug.LogError("HEARTHHOLD_DEPLOY_SMOKE_FAILED: central battlefield click did not reach a legal deployment cell.");
                Application.Quit(4);
                return;
            }
            Debug.Log("HEARTHHOLD_DEPLOY_SMOKE_READY: central battlefield click snapped to a legal deployment cell.");
            session.Notice = "0.6.3 投兵验收：点击基地中心，铁卫已自动从最近绿色战线入场。";
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

        private void PrepareTrainingSmoke()
        {
            session.Village.ArmyCounts[(int)TroopKind.Vanguard] -= 2;
            session.QueueTroop(TroopKind.Vanguard, System.DateTime.UtcNow);
            session.QueueTroop(TroopKind.Vanguard, System.DateTime.UtcNow);
            selected = -1; showTraining = true;
            session.Notice = "0.6.3 编队验收：营位、战后补齐与三种战术预设。";
        }

        private void PrepareHomeSmoke()
        {
            foreach (Building building in session.Village.Buildings)
                if (building.Kind == BuildingKind.Keep) { selected = building.Id; break; }
            session.Notice = "0.6.3 聚落验收：建筑底边与实际占地对齐。";
        }

        private void DisposePresentation()
        {
            if (ringMesh != null) Destroy(ringMesh);
        }
    }

    internal sealed class TimedWorldEffect : MonoBehaviour
    {
        private enum EffectMode { Projectile, Pulse, Debris, Mote }
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
            else
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
        private Color color;
        private float remaining;
        public void Trigger(Color flashColor) { color = flashColor; remaining = 0.2f; }
        private void Update()
        {
            if (remaining <= 0) return;
            if (targets == null)
            {
                List<Renderer> visible = new List<Renderer>();
                foreach (Renderer candidate in GetComponentsInChildren<Renderer>())
                    if (candidate.gameObject.name != "Ground contact shadow") visible.Add(candidate);
                targets = visible.ToArray();
            }
            if (targets.Length == 0) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            remaining = Mathf.Max(0, remaining - Time.deltaTime);
            float strength = Mathf.Sin(remaining / 0.2f * Mathf.PI);
            foreach (Renderer target in targets)
            {
                target.GetPropertyBlock(properties);
                properties.SetColor("_BaseColor", Color.Lerp(Color.white, color, strength * 0.8f));
                target.SetPropertyBlock(properties);
            }
        }
        private void OnDisable()
        {
            if (targets == null) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            foreach (Renderer target in targets)
            {
                target.GetPropertyBlock(properties); properties.SetColor("_BaseColor", Color.white); target.SetPropertyBlock(properties);
            }
            remaining = 0;
        }
    }
}
