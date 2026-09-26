using Hearthhold.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hearthhold.UnityClient
{
    // Owns preview models, cameras and render textures; callers only choose content and visibility.
    internal sealed class ModelPreviewService
    {
        private readonly ModelViews modelViews;
        private GameObject detailStage, detailModel;
        private Camera detailCamera;
        private RenderTexture detailTexture;
        private string detailKey;
        private float detailActionTimer;
        private float detailYaw = 318f, detailPitch = 27f;
        private GameObject[] rosterStages, rosterModels;
        private Camera[] rosterCameras;
        private RenderTexture[] rosterTextures;
        private int[] rosterLevels;
        private float rosterActionTimer;
        private float[] rosterYaw, rosterPitch;
        private bool rosterReadyLogged;

        internal RenderTexture DetailTexture { get { return detailTexture; } }
        internal RenderTexture[] RosterTextures { get { return rosterTextures; } }
        internal float DetailYaw { get { return detailYaw; } }
        internal float RosterYaw(int index) { return rosterYaw != null && index >= 0 && index < rosterYaw.Length ? rosterYaw[index] : 0; }

        internal ModelPreviewService(ModelViews views) { modelViews = views; }

        private static void SetPreviewLayer(Transform node, int layer)
        {
            node.gameObject.layer = layer;
            foreach (Transform child in node) SetPreviewLayer(child, layer);
        }

        internal void EnsureRoster(VillageData village)
        {
            int count = 1 + Rules.Pets.Length;
            if (rosterStages == null)
            {
                rosterStages = new GameObject[count]; rosterModels = new GameObject[count]; rosterCameras = new Camera[count];
                rosterTextures = new RenderTexture[count]; rosterLevels = new int[count];
                rosterYaw = new float[count]; rosterPitch = new float[count];
                for (int i = 0; i < count; i++) { rosterLevels[i] = -1; rosterYaw[i] = 320f; rosterPitch[i] = 26f; }
            }
            for (int i = 0; i < count; i++)
            {
                int level = i == 0 ? village.HeroLevels[0] : village.PetLevels[i - 1];
                int visualLevel = Mathf.Max(1, level);
                if (rosterStages[i] == null)
                {
                    int layer = 27 + i;
                    rosterStages[i] = new GameObject("Roster preview stage " + i);
                    rosterStages[i].transform.position = new Vector3(1100 + i * 40, 0, 1100);
                    rosterTextures[i] = new RenderTexture(512, 512, 16) { name = "Roster animated preview " + i };
                    GameObject cameraObject = new GameObject("Roster preview camera " + i);
                    rosterCameras[i] = cameraObject.AddComponent<Camera>(); rosterCameras[i].targetTexture = rosterTextures[i];
                    rosterCameras[i].cullingMask = 1 << layer; rosterCameras[i].clearFlags = CameraClearFlags.SolidColor;
                    rosterCameras[i].backgroundColor = i == 0 ? new Color(0.18f, 0.12f, 0.08f) : new Color(0.08f, 0.19f, 0.17f);
                    rosterCameras[i].orthographic = true;
                }
                if (rosterModels[i] != null && rosterLevels[i] == visualLevel) continue;
                if (rosterModels[i] != null) Object.Destroy(rosterModels[i]);
                rosterModels[i] = i == 0 ? modelViews.HeroPreview(HeroKind.EmberWarden, visualLevel, rosterStages[i].transform)
                    : modelViews.PetPreview((PetKind)(i - 1), visualLevel, rosterStages[i].transform);
                SetPreviewLayer(rosterModels[i].transform, 27 + i);
                Renderer[] renderers = rosterModels[i].GetComponentsInChildren<Renderer>(); bool started = false;
                Bounds bounds = new Bounds(rosterStages[i].transform.position, Vector3.one);
                foreach (Renderer renderer in renderers)
                {
                    if (renderer.gameObject.name == "Ground contact shadow") continue;
                    if (!started) { bounds = renderer.bounds; started = true; } else bounds.Encapsulate(renderer.bounds);
                }
                rosterModels[i].transform.position -= bounds.center - rosterStages[i].transform.position;
                rosterModels[i].transform.rotation = Quaternion.Euler(0, 145, 0);
                float span = Mathf.Max(bounds.size.x, bounds.size.z);
                rosterCameras[i].orthographicSize = Mathf.Max(1.05f, bounds.size.y * 0.68f, span * 0.72f);
                PlaceOrbitCamera(rosterCameras[i], rosterStages[i].transform.position, rosterYaw[i], rosterPitch[i], 8.7f);
                rosterLevels[i] = visualLevel;
            }
            if (!rosterReadyLogged)
            {
                rosterReadyLogged = true;
                Debug.Log("HEARTHHOLD_ROSTER_PREVIEW_READY: hero=" + Rules.Heroes.Length + " pets=" + Rules.Pets.Length);
            }
        }

        // Returns true when a new model was created; the smoke host uses that to time animation capture.
        internal bool EnsureDetail(bool building, int kind, int level)
        {
            string key = (building ? "building:" : "troop:") + kind + ":" + level;
            if (detailKey == key && detailModel != null) return false;
            if (detailStage == null)
            {
                detailStage = new GameObject("Detail preview stage");
                detailStage.transform.position = new Vector3(1000, 0, 1000);
                detailTexture = new RenderTexture(512, 512, 16) { name = "Animated detail preview" };
                GameObject cameraObject = new GameObject("Detail preview camera");
                detailCamera = cameraObject.AddComponent<Camera>();
                detailCamera.targetTexture = detailTexture;
                detailCamera.cullingMask = 1 << 30;
                detailCamera.clearFlags = CameraClearFlags.SolidColor;
                detailCamera.backgroundColor = new Color(0.13f, 0.20f, 0.19f);
                detailCamera.orthographic = true;
            }
            if (detailModel != null) Object.Destroy(detailModel);
            detailModel = building ? modelViews.BuildingPreview((BuildingKind)kind, level, detailStage.transform) : modelViews.TroopPreview((TroopKind)kind, level, detailStage.transform);
            SetPreviewLayer(detailModel.transform, 30);
            Renderer[] renderers = detailModel.GetComponentsInChildren<Renderer>();
            Bounds bounds = new Bounds(detailStage.transform.position, Vector3.one);
            bool started = false;
            foreach (Renderer renderer in renderers)
            {
                if (renderer.gameObject.name == "Ground contact shadow") continue;
                if (!started) { bounds = renderer.bounds; started = true; } else bounds.Encapsulate(renderer.bounds);
            }
            detailModel.transform.position -= bounds.center - detailStage.transform.position;
            if (!building) detailModel.transform.rotation = Quaternion.Euler(0, 140f, 0);
            float span = Mathf.Max(bounds.size.x, bounds.size.z);
            detailCamera.orthographicSize = building ? Mathf.Max(1.4f, bounds.size.y * 0.85f, span * 0.86f) : Mathf.Max(1.1f, bounds.size.y * 0.65f, span * 0.72f);
            detailYaw = 318f; detailPitch = 27f;
            PlaceOrbitCamera(detailCamera, detailStage.transform.position, detailYaw, detailPitch, 13.5f);
            detailKey = key;
            detailActionTimer = 1.8f;
            Debug.Log("HEARTHHOLD_DETAIL_PREVIEW_READY: " + key);
            return true;
        }

        internal void Update(VillageData village, bool showRoster, bool showDetail)
        {
            if (showRoster)
            {
                EnsureRoster(village); rosterActionTimer += Time.unscaledDeltaTime;
                for (int i = 0; i < rosterModels.Length; i++)
                {
                    if (rosterCameras[i] != null) rosterCameras[i].enabled = true;
                    if (rosterModels[i] == null) continue;
                    float pulse = 1 + Mathf.Sin(Time.unscaledTime * 2.2f + i) * 0.012f;
                    rosterModels[i].transform.localScale = Vector3.one * pulse;
                }
                if (rosterActionTimer >= 2.4f)
                {
                    rosterActionTimer = 0;
                    ModelActionAnimator action = rosterModels[0] == null ? null : rosterModels[0].GetComponent<ModelActionAnimator>();
                    if (action != null) action.Attack(rosterModels[0].transform.position + rosterModels[0].transform.forward * 3f);
                }
            }
            else if (rosterCameras != null) foreach (Camera camera in rosterCameras) if (camera != null) camera.enabled = false;
            if (detailCamera != null) detailCamera.enabled = showDetail;
            if (detailModel != null && showDetail)
            {
                ModelActionAnimator action = detailModel.GetComponent<ModelActionAnimator>();
                ImportedClipAnimator imported = detailModel.GetComponent<ImportedClipAnimator>();
                if (action != null && (detailModel.GetComponent<ArticulatedModelAnimator>() != null || imported != null))
                {
                    detailActionTimer += Time.unscaledDeltaTime;
                    if (detailActionTimer >= 2f && (imported == null || !imported.IsPlayingAction))
                    {
                        detailActionTimer = 0;
                        action.Attack(detailModel.transform.position + detailModel.transform.forward * 3f);
                    }
                }
            }
        }

        internal void RotateDetail(Vector2 drag)
        {
            if (detailCamera == null || detailModel == null) return;
            detailYaw = Mathf.Repeat(detailYaw - drag.x * 0.55f, 360f);
            detailPitch = Mathf.Clamp(detailPitch + drag.y * 0.3f, 10f, 65f);
            PlaceOrbitCamera(detailCamera, detailStage.transform.position, detailYaw, detailPitch, 13.5f);
        }

        internal void RotateRoster(int index, Vector2 drag)
        {
            if (rosterCameras == null || index < 0 || index >= rosterCameras.Length || rosterModels[index] == null) return;
            rosterYaw[index] = Mathf.Repeat(rosterYaw[index] - drag.x * 0.55f, 360f);
            rosterPitch[index] = Mathf.Clamp(rosterPitch[index] + drag.y * 0.3f, 10f, 65f);
            PlaceOrbitCamera(rosterCameras[index], rosterStages[index].transform.position, rosterYaw[index], rosterPitch[index], 8.7f);
        }

        private static void PlaceOrbitCamera(Camera camera, Vector3 center, float yaw, float pitch, float radius)
        {
            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0);
            camera.transform.position = center + orbit * (Vector3.back * radius);
            camera.transform.LookAt(center);
        }

        internal void Dispose()
        {
            if (detailModel != null) Object.Destroy(detailModel);
            if (detailStage != null) Object.Destroy(detailStage);
            if (detailCamera != null) Object.Destroy(detailCamera.gameObject);
            if (detailTexture != null) { detailTexture.Release(); Object.Destroy(detailTexture); }
            if (rosterModels != null) foreach (GameObject model in rosterModels) if (model != null) Object.Destroy(model);
            if (rosterStages != null) foreach (GameObject stage in rosterStages) if (stage != null) Object.Destroy(stage);
            if (rosterCameras != null) foreach (Camera camera in rosterCameras) if (camera != null) Object.Destroy(camera.gameObject);
            if (rosterTextures != null) foreach (RenderTexture texture in rosterTextures) if (texture != null) { texture.Release(); Object.Destroy(texture); }
        }
    }

    internal sealed class PreviewDragHandle : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private System.Action<Vector2> rotate;
        private bool dragging;
        internal void Configure(System.Action<Vector2> onDrag) { rotate = onDrag; }
        public void OnPointerDown(PointerEventData eventData) { dragging = eventData.button == PointerEventData.InputButton.Left; }
        public void OnDrag(PointerEventData eventData)
        { if (dragging && eventData.button == PointerEventData.InputButton.Left && rotate != null) rotate(eventData.delta); }
        public void OnPointerUp(PointerEventData eventData) { dragging = false; }
    }
}
