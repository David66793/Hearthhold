using System.Collections.Generic;
using Hearthhold.Core;
using UnityEngine;

namespace Hearthhold.UnityClient
{
    public sealed partial class GameBootstrap
    {
        private bool layoutEditing, layoutGridVisible = true, layoutAllSelected, layoutGroupMoving, layoutDragging;
        private int layoutPlacingId = -1;
        private readonly List<Building> layoutOriginalBuildings = new List<Building>();
        private readonly Dictionary<int, Vector2Int> layoutOriginalPositions = new Dictionary<int, Vector2Int>();
        private readonly List<Building> layoutStaged = new List<Building>();
        private readonly List<int> layoutSelectedIds = new List<int>();
        private int layoutPointerPressedId = -1, layoutPointerGrabX, layoutPointerGrabZ;
        private Vector2 layoutPointerStartScreen;
        private bool layoutPointerDragging, layoutPointerSelectingWalls, layoutPointerExtendedWalls;
        private Vector2 layoutTrayScroll;
        private GameObject layoutGridRoot, layoutSelectionBounds, layoutMoveBounds;

        private void EnterLayoutEditor()
        {
            if (session.Battle != null || layoutEditing) return;
            CloseExtraModals(); selected = moving = -1; selectedWallIds.Clear(); buildKind = null;
            layoutOriginalBuildings.Clear(); layoutOriginalPositions.Clear(); layoutStaged.Clear(); layoutSelectedIds.Clear();
            foreach (Building building in session.Village.Buildings)
            {
                layoutOriginalBuildings.Add(building);
                layoutOriginalPositions[building.Id] = new Vector2Int(building.X, building.Z);
            }
            layoutEditing = true; layoutGridVisible = true; layoutAllSelected = layoutGroupMoving = false; layoutPlacingId = -1;
            ResetLayoutPointer();
            EnsureLayoutWorld();
            layoutGridRoot.SetActive(true); layoutSelectionBounds.SetActive(false); layoutMoveBounds.SetActive(false);
            session.Notice = "阵型编辑已开启：用左侧工具和底部暂存位完成全部操作。";
        }

        private void EnsureLayoutWorld()
        {
            if (layoutGridRoot == null)
            {
                layoutGridRoot = new GameObject("Layout grid"); layoutGridRoot.transform.SetParent(transform, false);
                Color gridColor = new Color32(66, 111, 99, 255);
                for (int i = 2; i <= Rules.MapSize - 2; i++)
                {
                    Piece("Grid X " + i, PrimitiveType.Cube, new Vector3(20, 0.035f, i), new Vector3(36, 0.018f, 0.025f), gridColor, layoutGridRoot.transform);
                    Piece("Grid Z " + i, PrimitiveType.Cube, new Vector3(i, 0.035f, 20), new Vector3(0.025f, 0.018f, 36), gridColor, layoutGridRoot.transform);
                }
            }
            if (layoutSelectionBounds == null) layoutSelectionBounds = CreateLayoutBounds("Selected layout bounds", Gold);
            if (layoutMoveBounds == null) layoutMoveBounds = CreateLayoutBounds("Layout move preview", Mint);
        }

        private GameObject CreateLayoutBounds(string name, Color color)
        {
            GameObject root = new GameObject(name); root.transform.SetParent(transform, false);
            for (int i = 0; i < 4; i++) Piece("Edge " + i, PrimitiveType.Cube, Vector3.zero, Vector3.one, color, root.transform);
            root.SetActive(false); return root;
        }

        private void SetLayoutBounds(GameObject root, float minX, float minZ, float maxX, float maxZ, Color color)
        {
            if (root == null) return;
            root.SetActive(true); float width = maxX - minX, depth = maxZ - minZ, y = 0.105f, edge = 0.1f;
            Transform south = root.transform.GetChild(0), north = root.transform.GetChild(1), west = root.transform.GetChild(2), east = root.transform.GetChild(3);
            south.position = new Vector3((minX + maxX) * 0.5f, y, minZ); south.localScale = new Vector3(width + edge, 0.035f, edge);
            north.position = new Vector3((minX + maxX) * 0.5f, y, maxZ); north.localScale = new Vector3(width + edge, 0.035f, edge);
            west.position = new Vector3(minX, y, (minZ + maxZ) * 0.5f); west.localScale = new Vector3(edge, 0.035f, depth + edge);
            east.position = new Vector3(maxX, y, (minZ + maxZ) * 0.5f); east.localScale = new Vector3(edge, 0.035f, depth + edge);
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = MaterialFor(color);
        }

        private bool LayoutBounds(IList<int> ids, out int minX, out int minZ, out int maxX, out int maxZ)
        {
            minX = minZ = int.MaxValue; maxX = maxZ = int.MinValue;
            foreach (int id in ids)
            {
                Building building = session.Find(id); if (building == null) continue;
                minX = Mathf.Min(minX, building.X); minZ = Mathf.Min(minZ, building.Z);
                maxX = Mathf.Max(maxX, building.X + building.Spec.Size); maxZ = Mathf.Max(maxZ, building.Z + building.Spec.Size);
            }
            return minX != int.MaxValue;
        }

        private void SelectAllLayoutBuildings()
        {
            selected = -1; selectedWallIds.Clear();
            layoutSelectedIds.Clear(); foreach (Building building in session.Village.Buildings) layoutSelectedIds.Add(building.Id);
            layoutAllSelected = layoutSelectedIds.Count > 0; layoutGroupMoving = false;
            RefreshLayoutSelectionBounds();
            session.Notice = layoutAllSelected ? "已选择地图上的全部建筑，可点击“整体移动”。" : "地图上没有已放置建筑，请从暂存位放回。";
        }

        private void RefreshLayoutSelectionBounds()
        {
            int minX, minZ, maxX, maxZ;
            if (LayoutBounds(layoutSelectedIds, out minX, out minZ, out maxX, out maxZ))
                SetLayoutBounds(layoutSelectionBounds, minX, minZ, maxX, maxZ, Gold);
            else layoutSelectionBounds.SetActive(false);
            layoutMoveBounds.SetActive(false);
        }

        private void BeginGroupMove()
        {
            if (layoutSelectedIds.Count == 0) { SelectAllLayoutBuildings(); if (layoutSelectedIds.Count == 0) return; }
            layoutGroupMoving = true; layoutPlacingId = -1; layoutDragging = false;
            session.Notice = "移动全部建筑：点击新的左下锚点；薄荷色可放置，珊瑚色表示越界或冲突。";
        }

        private void ClearLayoutToTray()
        {
            layoutStaged.Clear(); layoutStaged.AddRange(session.Village.Buildings); session.Village.Buildings.Clear();
            layoutSelectedIds.Clear(); layoutAllSelected = layoutGroupMoving = false; layoutPlacingId = -1;
            selected = -1; selectedWallIds.Clear();
            ResetLayoutPointer();
            layoutSelectionBounds.SetActive(false); layoutMoveBounds.SetActive(false); HidePlacementPresentation();
            RebuildBuildings(); session.Notice = "全部建筑已收入暂存位。点击卡片后点地图，或把卡片拖到地图。";
        }

        private Building StagedBuilding(int id)
        {
            foreach (Building building in layoutStaged) if (building.Id == id) return building;
            return null;
        }

        private void BeginStagedPlacement(int id, bool dragging)
        {
            if (StagedBuilding(id) == null) return;
            layoutPlacingId = id; layoutDragging = dragging; layoutGroupMoving = false; layoutMoveBounds.SetActive(false);
            Building building = StagedBuilding(id);
            session.Notice = "放置" + building.Spec.Name + "：点击地图空位，或按住卡片拖到目标格。";
        }

        private void PlaceStagedBuilding(int x, int z)
        {
            Building building = StagedBuilding(layoutPlacingId); if (building == null) { layoutPlacingId = -1; return; }
            if (!session.Village.CanPlace(building.Kind, x, z, -1)) { session.Notice = "这里放不下" + building.Spec.Name + "，请选择薄荷色区域。"; return; }
            building.X = x; building.Z = z; session.Village.Buildings.Add(building); layoutStaged.Remove(building);
            layoutPlacingId = -1; layoutDragging = false; HidePlacementPresentation(); RebuildBuildings();
            session.Notice = building.Spec.Name + "已放置；暂存位剩余 " + layoutStaged.Count + " 座。";
        }

        private void FinishLayoutEditor()
        {
            if (layoutStaged.Count > 0) { session.Notice = "还有 " + layoutStaged.Count + " 座建筑在暂存位，全部放回后才能完成。"; return; }
            layoutEditing = false; layoutAllSelected = layoutGroupMoving = layoutDragging = false; layoutPlacingId = -1;
            selected = -1; selectedWallIds.Clear();
            ResetLayoutPointer();
            layoutGridRoot.SetActive(false); layoutSelectionBounds.SetActive(false); layoutMoveBounds.SetActive(false); HidePlacementPresentation();
            session.Notice = "阵型已保存。"; Save();
        }

        private void CancelLayoutEditor(bool announce = true)
        {
            if (!layoutEditing) return;
            session.Village.Buildings.Clear();
            foreach (Building building in layoutOriginalBuildings)
            {
                Vector2Int position = layoutOriginalPositions[building.Id]; building.X = position.x; building.Z = position.y;
                session.Village.Buildings.Add(building);
            }
            layoutEditing = false; layoutAllSelected = layoutGroupMoving = layoutDragging = false; layoutPlacingId = -1;
            selected = -1; selectedWallIds.Clear();
            ResetLayoutPointer();
            if (layoutGridRoot != null) layoutGridRoot.SetActive(false);
            if (layoutSelectionBounds != null) layoutSelectionBounds.SetActive(false);
            if (layoutMoveBounds != null) layoutMoveBounds.SetActive(false);
            HidePlacementPresentation(); RebuildBuildings(); if (announce) session.Notice = "已取消阵型编辑，恢复进入编辑前的布局。";
        }

        private bool OverLayoutHud()
        {
            float x = Input.mousePosition.x, y = Screen.height - Input.mousePosition.y;
            return y < 92 || x < 286 && y < 650 || y > Screen.height - 166;
        }

        private void ResetLayoutPointer()
        {
            layoutPointerPressedId = -1;
            layoutPointerDragging = layoutPointerSelectingWalls = layoutPointerExtendedWalls = false;
        }

        private void BeginLayoutPointer(Building building, int cellX, int cellZ, Vector2 screen)
        {
            selected = building.Id;
            bool selectedRow = building.Kind == BuildingKind.Wall && layoutSelectedIds.Count > 1 && layoutSelectedIds.Contains(building.Id);
            if (!layoutAllSelected && !selectedRow)
            {
                layoutSelectedIds.Clear(); layoutSelectedIds.Add(building.Id);
                RefreshLayoutSelectionBounds();
            }
            selectedWallIds.Clear();
            if (building.Kind == BuildingKind.Wall && !layoutAllSelected) selectedWallIds.AddRange(layoutSelectedIds);
            layoutPointerPressedId = building.Id;
            layoutPointerStartScreen = screen;
            layoutPointerGrabX = Mathf.Clamp(cellX - building.X, 0, building.Spec.Size - 1);
            layoutPointerGrabZ = Mathf.Clamp(cellZ - building.Z, 0, building.Spec.Size - 1);
            layoutPointerDragging = layoutPointerExtendedWalls = false;
            layoutPointerSelectingWalls = building.Kind == BuildingKind.Wall && !selectedRow && !layoutAllSelected;
        }

        private bool ExtendLayoutWalls(Building hovered)
        {
            if (hovered == null || hovered.Kind != BuildingKind.Wall || !session.ExtendWallStroke(layoutSelectedIds, hovered.Id)) return false;
            layoutPointerExtendedWalls = true;
            selectedWallIds.Clear(); selectedWallIds.AddRange(layoutSelectedIds);
            RefreshLayoutSelectionBounds();
            return true;
        }

        private bool CommitLayoutPointerAt(int targetX, int targetZ)
        {
            Building anchor = session.Find(layoutPointerPressedId);
            if (anchor == null) return false;
            int dx = targetX - anchor.X, dz = targetZ - anchor.Z;
            if (dx == 0 && dz == 0) return false;
            bool moved = session.CanMoveGroup(layoutSelectedIds, dx, dz) && session.MoveGroup(layoutSelectedIds, dx, dz);
            if (moved) { RebuildBuildings(); RefreshLayoutSelectionBounds(); }
            return moved;
        }

        private void UpdateLayoutPointer(Vector3 ground)
        {
            int cellX = Mathf.FloorToInt(ground.x), cellZ = Mathf.FloorToInt(ground.z);
            if (Input.GetMouseButtonDown(0))
            {
                Building building = BuildingUnderPointer();
                if (building == null) { layoutSelectedIds.Clear(); layoutAllSelected = false; selected = -1; selectedWallIds.Clear(); layoutSelectionBounds.SetActive(false); ResetLayoutPointer(); return; }
                BeginLayoutPointer(building, cellX, cellZ, Input.mousePosition);
            }
            if (layoutPointerPressedId < 0) return;
            Building anchor = session.Find(layoutPointerPressedId);
            if (anchor == null) { ResetLayoutPointer(); return; }
            if (Input.GetMouseButton(0) && !layoutPointerDragging && Vector2.Distance(layoutPointerStartScreen, Input.mousePosition) >= 9f)
            {
                if (layoutPointerSelectingWalls)
                {
                    ExtendLayoutWalls(session.Village.At(cellX, cellZ));
                    if (layoutPointerExtendedWalls) return;
                    if (cellX == anchor.X && cellZ == anchor.Z) return;
                }
                layoutPointerDragging = true;
            }
            if (layoutPointerDragging)
            {
                int targetX = cellX - layoutPointerGrabX, targetZ = cellZ - layoutPointerGrabZ;
                int dx = targetX - anchor.X, dz = targetZ - anchor.Z;
                bool group = layoutSelectedIds.Count > 1;
                bool valid = session.CanMoveGroup(layoutSelectedIds, dx, dz);
                if (group)
                {
                    int minX, minZ, maxX, maxZ;
                    if (LayoutBounds(layoutSelectedIds, out minX, out minZ, out maxX, out maxZ))
                        SetLayoutBounds(layoutMoveBounds, minX + dx, minZ + dz, maxX + dx, maxZ + dz, valid ? Mint : Danger);
                }
                else ShowPlacementPresentation(anchor.Kind, anchor.Level, targetX, targetZ, anchor.Spec.Size, valid);
                if (Input.GetMouseButtonUp(0))
                {
                    if (valid) CommitLayoutPointerAt(targetX, targetZ);
                    HidePlacementPresentation(); layoutMoveBounds.SetActive(false); ResetLayoutPointer();
                }
            }
            else if (Input.GetMouseButtonUp(0)) ResetLayoutPointer();
        }

        private void UpdateLayoutEditor()
        {
            if (Input.GetMouseButtonDown(1))
            {
                ResetLayoutPointer();
                layoutPlacingId = -1; layoutDragging = layoutGroupMoving = false; layoutMoveBounds.SetActive(false); HidePlacementPresentation();
                session.Notice = "已取消当前放置操作，阵型编辑仍在进行。";
            }
            layoutGridRoot.SetActive(layoutGridVisible);
            Vector3 ground;
            if (OverLayoutHud() || !GroundPoint(out ground))
            {
                if (Input.GetMouseButtonUp(0)) { layoutDragging = false; ResetLayoutPointer(); }
                HidePlacementPresentation(); if (layoutMoveBounds != null) layoutMoveBounds.SetActive(false); return;
            }
            int x = Mathf.FloorToInt(ground.x), z = Mathf.FloorToInt(ground.z);
            Building staged = StagedBuilding(layoutPlacingId);
            if (staged != null)
            {
                bool valid = session.Village.CanPlace(staged.Kind, x, z, -1);
                ShowPlacementPresentation(staged.Kind, staged.Level, x, z, staged.Spec.Size, valid);
                if ((!layoutDragging && Input.GetMouseButtonDown(0)) || (layoutDragging && Input.GetMouseButtonUp(0))) PlaceStagedBuilding(x, z);
                return;
            }
            HidePlacementPresentation();
            if (layoutGroupMoving)
            {
                int minX, minZ, maxX, maxZ;
                if (!LayoutBounds(layoutSelectedIds, out minX, out minZ, out maxX, out maxZ)) return;
                int dx = x - minX, dz = z - minZ; bool valid = session.CanMoveGroup(layoutSelectedIds, dx, dz);
                SetLayoutBounds(layoutMoveBounds, minX + dx, minZ + dz, maxX + dx, maxZ + dz, valid ? Mint : Danger);
                if (Input.GetMouseButtonDown(0) && session.MoveGroup(layoutSelectedIds, dx, dz))
                {
                    layoutGroupMoving = false; layoutMoveBounds.SetActive(false); RebuildBuildings(); RefreshLayoutSelectionBounds();
                }
                return;
            }
            UpdateLayoutPointer(ground);
        }

        private bool VerifyLayoutPointerSmoke()
        {
            Building mine = null;
            foreach (Building building in session.Village.Buildings) if (building.Kind == BuildingKind.Mine) { mine = building; break; }
            if (mine == null) return LayoutPointerSmokeFailed("mine missing");
            layoutAllSelected = false; layoutSelectedIds.Clear();
            int mineX = mine.X, mineZ = mine.Z, moveX = 0, moveZ = 0;
            bool found = false;
            for (int radius = 1; radius <= 5 && !found; radius++)
                for (int dx = -radius; dx <= radius && !found; dx++)
                    for (int dz = -radius; dz <= radius && !found; dz++)
                        if ((dx != 0 || dz != 0) && session.Village.CanPlace(mine.Kind, mineX + dx, mineZ + dz, mine.Id))
                        { moveX = dx; moveZ = dz; found = true; }
            if (!found) return LayoutPointerSmokeFailed("mine destination missing");
            BeginLayoutPointer(mine, mineX + 1, mineZ + 1, Vector2.zero);
            if (layoutPointerGrabX != 1 || layoutPointerGrabZ != 1 || CommitLayoutPointerAt(0, 0) || mine.X != mineX || mine.Z != mineZ
                || !CommitLayoutPointerAt(mineX + moveX, mineZ + moveZ))
                return LayoutPointerSmokeFailed("single building drag failed");
            ResetLayoutPointer();
            List<Building> row = null;
            foreach (Building wall in session.Village.Buildings)
                if (wall.Kind == BuildingKind.Wall && session.WallRow(wall.Id).Count >= 3)
                { row = session.WallRow(wall.Id); break; }
            if (row == null) return LayoutPointerSmokeFailed("wall row missing");
            BeginLayoutPointer(row[0], row[0].X, row[0].Z, Vector2.zero);
            if (!ExtendLayoutWalls(row[2]) || layoutSelectedIds.Count != 3 || !layoutPointerExtendedWalls)
                return LayoutPointerSmokeFailed("wall stroke did not select three contiguous walls");
            ResetLayoutPointer();
            BeginLayoutPointer(row[1], row[1].X, row[1].Z, Vector2.zero);
            if (layoutPointerSelectingWalls || layoutSelectedIds.Count != 3)
                return LayoutPointerSmokeFailed("second press did not preserve the selected row");
            int anchorX = row[1].X, anchorZ = row[1].Z, firstX = row[0].X, firstZ = row[0].Z, lastX = row[2].X, lastZ = row[2].Z;
            found = false;
            for (int radius = 1; radius <= 5 && !found; radius++)
                for (int dx = -radius; dx <= radius && !found; dx++)
                    for (int dz = -radius; dz <= radius && !found; dz++)
                        if ((dx != 0 || dz != 0) && session.CanMoveGroup(layoutSelectedIds, dx, dz))
                        { moveX = dx; moveZ = dz; found = true; }
            if (!found || CommitLayoutPointerAt(0, 0) || row[1].X != anchorX || row[1].Z != anchorZ
                || !CommitLayoutPointerAt(anchorX + moveX, anchorZ + moveZ)
                || row[1].X != anchorX + moveX || row[1].Z != anchorZ + moveZ
                || row[0].X != firstX + moveX || row[0].Z != firstZ + moveZ
                || row[2].X != lastX + moveX || row[2].Z != lastZ + moveZ)
                return LayoutPointerSmokeFailed("selected wall row did not move atomically");
            ResetLayoutPointer();
            Debug.Log("HEARTHHOLD_LAYOUT_POINTER_READY: one building dragged and three same-row walls selected and moved.");
            return true;
        }

        private static bool LayoutPointerSmokeFailed(string reason)
        {
            Debug.LogError("HEARTHHOLD_LAYOUT_POINTER_FAILED: " + reason);
            return false;
        }

        private void DrawLayoutEditorHud()
        {
            bool compact = Screen.height < 800;
            float panelHeight = compact ? 420 : 520, titleY = 126, helpY = compact ? 164 : 170;
            float firstY = compact ? 211 : 236, step = compact ? 40 : 50, buttonH = compact ? 34 : 42;
            Box(new Rect(18, 108, 264, panelHeight));
            GUI.Label(new Rect(36, titleY, 228, 42), "编辑阵型", heading);
            GUI.Label(new Rect(36, helpY, 228, compact ? 42 : 55), "按住建筑拖动；划选同排城墙\n再按住选中墙段整排移动", small);
            if (GUI.Button(new Rect(34, firstY, 232, buttonH), "全选地图建筑")) SelectAllLayoutBuildings();
            GUI.enabled = layoutSelectedIds.Count > 0;
            if (GUI.Button(new Rect(34, firstY + step, 232, buttonH), "整体移动所选建筑")) BeginGroupMove();
            GUI.enabled = true;
            if (GUI.Button(new Rect(34, firstY + step * 2, 232, buttonH), "全部清空到暂存位")) ClearLayoutToTray();
            GUI.backgroundColor = layoutGridVisible ? Mint : Color.white;
            if (GUI.Button(new Rect(34, firstY + step * 3, 232, buttonH), layoutGridVisible ? "地图网格：显示" : "地图网格：隐藏")) layoutGridVisible = !layoutGridVisible;
            GUI.backgroundColor = Color.white;
            float statusY = firstY + step * 4 + (compact ? 3 : 5);
            bool wallRowSelected = !layoutAllSelected && layoutSelectedIds.Count > 1 && session.Find(layoutSelectedIds[0]) != null && session.Find(layoutSelectedIds[0]).Kind == BuildingKind.Wall;
            string selectionStatus = wallRowSelected ? "同排城墙已选 " + layoutSelectedIds.Count + " 段" : layoutAllSelected ? "已全选地图建筑" : "全部放回后可完成";
            string status = compact ? "地图 " + session.Village.Buildings.Count + " 座 / 暂存 " + layoutStaged.Count + " 座\n" + selectionStatus
                : "地图上 " + session.Village.Buildings.Count + " 座\n暂存位 " + layoutStaged.Count + " 座\n" + selectionStatus;
            GUI.Label(new Rect(36, statusY, 228, compact ? 50 : 68), status, small);
            GUI.enabled = layoutStaged.Count == 0;
            float finishY = compact ? 434 : 520;
            if (GUI.Button(new Rect(34, finishY, 232, compact ? 36 : 42), "完成并保存阵型")) FinishLayoutEditor();
            GUI.enabled = true;
            if (GUI.Button(new Rect(34, compact ? 476 : 572, 232, 36), "取消并恢复原阵型")) CancelLayoutEditor();

            float trayY = Screen.height - 158;
            Box(new Rect(0, trayY, Screen.width, 158));
            GUI.Label(new Rect(22, trayY + 12, 170, 30), "建筑暂存位", new GUIStyle(label) { fontStyle = FontStyle.Bold });
            GUI.Label(new Rect(190, trayY + 15, Screen.width - 210, 28), layoutStaged.Count == 0 ? "暂存位为空；可先点“全部清空到暂存位”。" : "点击卡片后点地图，或按住卡片直接拖到地图。", small);
            Rect viewport = new Rect(18, trayY + 48, Screen.width - 36, 96);
            float contentWidth = Mathf.Max(viewport.width - 20, layoutStaged.Count * 192f);
            layoutTrayScroll = GUI.BeginScrollView(viewport, layoutTrayScroll, new Rect(0, 0, contentWidth, 78), false, true);
            Event current = Event.current;
            for (int i = 0; i < layoutStaged.Count; i++)
            {
                Building building = layoutStaged[i]; Rect card = new Rect(i * 192, 2, 180, 66);
                GUI.backgroundColor = layoutPlacingId == building.Id ? Gold : Color.white;
                GUI.Box(card, building.Spec.Name + "　" + building.Level + "级\n占地 " + building.Spec.Size + "×" + building.Spec.Size + "　点击或拖拽");
                if (current.type == EventType.MouseDown && current.button == 0 && card.Contains(current.mousePosition))
                { BeginStagedPlacement(building.Id, true); current.Use(); }
            }
            GUI.backgroundColor = Color.white; GUI.EndScrollView();
        }

        private void DisposeLayoutEditor()
        {
            if (layoutGridRoot != null) Destroy(layoutGridRoot);
            if (layoutSelectionBounds != null) Destroy(layoutSelectionBounds);
            if (layoutMoveBounds != null) Destroy(layoutMoveBounds);
        }
    }
}
