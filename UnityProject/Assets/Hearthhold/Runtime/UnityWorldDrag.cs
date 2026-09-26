using System.Collections.Generic;
using Hearthhold.Core;
using UnityEngine;

namespace Hearthhold.UnityClient
{
    public sealed partial class GameBootstrap
    {
        private int worldDragPressedId = -1, worldDragGrabX, worldDragGrabZ;
        private Vector2 worldDragStartScreen;
        private bool worldDragActive, worldDragSelectingWalls, worldDragExtendedWalls;

        private void ResetWorldDrag()
        {
            worldDragPressedId = -1;
            worldDragActive = worldDragSelectingWalls = worldDragExtendedWalls = false;
        }

        private Building BuildingUnderPointer()
        {
            RaycastHit hit;
            if (!Physics.Raycast(worldCamera.ScreenPointToRay(Input.mousePosition), out hit)) return null;
            BuildingHandle handle = hit.collider.GetComponentInParent<BuildingHandle>();
            return handle == null ? null : session.Find(handle.Id);
        }

        private void BeginWorldDrag(Building building, int hitX, int hitZ, Vector2 screenPosition)
        {
            bool existingRow = building.Kind == BuildingKind.Wall && selectedWallIds.Count > 1 && selectedWallIds.Contains(building.Id);
            selected = worldDragPressedId = building.Id;
            worldDragStartScreen = screenPosition;
            worldDragGrabX = Mathf.Clamp(hitX - building.X, 0, building.Spec.Size - 1);
            worldDragGrabZ = Mathf.Clamp(hitZ - building.Z, 0, building.Spec.Size - 1);
            worldDragActive = worldDragExtendedWalls = false;
            worldDragSelectingWalls = building.Kind == BuildingKind.Wall && !existingRow;
            if (!existingRow)
            {
                selectedWallIds.Clear();
                if (building.Kind == BuildingKind.Wall) selectedWallIds.Add(building.Id);
            }
        }

        private void UpdateWorldDrag()
        {
            if (session.Battle != null || layoutEditing || buildKind.HasValue || (moving >= 0 && !worldDragActive) || help || ExtraModal || modernCatalogOpen)
            {
                if (worldDragActive) { moving = -1; movingWallRow = false; HidePlacementPresentation(); }
                ResetWorldDrag();
                return;
            }
            if (Input.GetMouseButtonDown(0))
            {
                if (OverHud()) return;
                Building building = BuildingUnderPointer();
                if (building == null) { selected = -1; selectedWallIds.Clear(); ResetWorldDrag(); return; }
                Vector3 ground;
                if (!GroundPoint(out ground)) return;
                BeginWorldDrag(building, Mathf.FloorToInt(ground.x), Mathf.FloorToInt(ground.z), Input.mousePosition);
            }
            if (worldDragPressedId < 0) return;
            if (Input.GetMouseButton(0) && !worldDragActive)
            {
                if (Vector2.Distance(worldDragStartScreen, Input.mousePosition) < 9f) return;
                Vector3 ground;
                if (!OverHud() && GroundPoint(out ground))
                {
                    if (worldDragSelectingWalls)
                    {
                        Building hovered = session.Village.At(Mathf.FloorToInt(ground.x), Mathf.FloorToInt(ground.z));
                        ExtendWorldDragWalls(hovered);
                        if (worldDragExtendedWalls) return;
                        Building origin = session.Find(worldDragPressedId);
                        if (origin != null && Mathf.FloorToInt(ground.x) == origin.X && Mathf.FloorToInt(ground.z) == origin.Z) return;
                    }
                    worldDragActive = true;
                    moving = worldDragPressedId;
                    movingWallRow = selectedWallIds.Count > 1 && session.Find(moving).Kind == BuildingKind.Wall;
                    session.Notice = movingWallRow ? "拖动整组城墙，松开鼠标放置；右键取消。" : "拖动建筑，松开鼠标放置；右键取消。";
                }
            }
            if (Input.GetMouseButtonUp(0))
            {
                if (worldDragActive)
                {
                    Vector3 ground;
                    if (!OverHud() && GroundPoint(out ground))
                    {
                        int targetX = Mathf.FloorToInt(ground.x) - worldDragGrabX;
                        int targetZ = Mathf.FloorToInt(ground.z) - worldDragGrabZ;
                        CommitWorldDragAt(targetX, targetZ);
                    }
                    else session.Notice = "放置位置在地图之外，建筑保持原位。";
                    moving = -1; movingWallRow = false;
                    HidePlacementPresentation();
                }
                ResetWorldDrag();
            }
            else if (!Input.GetMouseButton(0)) ResetWorldDrag();
        }

        private bool ExtendWorldDragWalls(Building hovered)
        {
            if (hovered == null || hovered.Kind != BuildingKind.Wall || !session.ExtendWallStroke(selectedWallIds, hovered.Id)) return false;
            worldDragExtendedWalls = true;
            session.Notice = "已划选同排城墙 ×" + selectedWallIds.Count + "；松开完成选择，再按住墙段拖动整组。";
            return true;
        }

        private bool CommitWorldDragAt(int targetX, int targetZ)
        {
            Building anchor = session.Find(moving);
            if (anchor == null || targetX == anchor.X && targetZ == anchor.Z) return false;
            bool moved = movingWallRow ? session.MoveWallRow(selectedWallIds, moving, targetX, targetZ) : session.Move(moving, targetX, targetZ);
            if (moved) { RebuildBuildings(); Save(); }
            return moved;
        }

        private bool VerifyWorldDragSmoke()
        {
            Building building = null;
            foreach (Building candidate in session.Village.Buildings)
                if (candidate.Kind == BuildingKind.Mine) { building = candidate; break; }
            if (building == null) return WorldDragSmokeFailed("no test building");
            int targetX = building.X, targetZ = building.Z;
            bool found = false;
            for (int radius = 1; radius <= 5 && !found; radius++)
                for (int dx = -radius; dx <= radius && !found; dx++)
                    for (int dz = -radius; dz <= radius && !found; dz++)
                    {
                        int x = building.X + dx, z = building.Z + dz;
                        if (x == building.X && z == building.Z || !session.Village.CanPlace(building.Kind, x, z, building.Id)) continue;
                        targetX = x; targetZ = z; found = true;
                    }
            if (!found) return WorldDragSmokeFailed("no free destination for test building");
            BeginWorldDrag(building, building.X + 1, building.Z + 1, Vector2.zero);
            if (worldDragGrabX != 1 || worldDragGrabZ != 1) return WorldDragSmokeFailed("grab offset was lost");
            moving = building.Id; worldDragActive = true;
            if (!CommitWorldDragAt(targetX, targetZ) || building.X != targetX || building.Z != targetZ)
                return WorldDragSmokeFailed("building drag did not move to its destination");
            moving = -1; ResetWorldDrag(); HidePlacementPresentation();

            List<Building> row = null;
            foreach (Building candidate in session.Village.Buildings)
                if (candidate.Kind == BuildingKind.Wall && session.WallRow(candidate.Id).Count >= 3)
                { row = session.WallRow(candidate.Id); break; }
            if (row == null) return WorldDragSmokeFailed("no straight wall row");
            BeginWorldDrag(row[0], row[0].X, row[0].Z, Vector2.zero);
            if (!ExtendWorldDragWalls(row[2]) || selectedWallIds.Count != 3 || !worldDragExtendedWalls)
                return WorldDragSmokeFailed("wall stroke did not select three contiguous segments");
            ResetWorldDrag();
            BeginWorldDrag(row[1], row[1].X, row[1].Z, Vector2.zero);
            if (worldDragSelectingWalls || selectedWallIds.Count != 3)
                return WorldDragSmokeFailed("second press did not preserve the selected wall group");
            moving = row[1].Id; movingWallRow = worldDragActive = true;
            int wallTargetX = row[1].X, wallTargetZ = row[1].Z;
            found = false;
            for (int radius = 1; radius <= 5 && !found; radius++)
                for (int dx = -radius; dx <= radius && !found; dx++)
                    for (int dz = -radius; dz <= radius && !found; dz++)
                    {
                        int x = row[1].X + dx, z = row[1].Z + dz;
                        if (x == row[1].X && z == row[1].Z || !session.CanMoveWallRow(selectedWallIds, moving, x, z)) continue;
                        wallTargetX = x; wallTargetZ = z; found = true;
                    }
            if (!found) return WorldDragSmokeFailed("no free destination for selected walls");
            int oldWallX = row[1].X, oldWallZ = row[1].Z;
            int firstX = row[0].X, firstZ = row[0].Z, lastX = row[2].X, lastZ = row[2].Z;
            int untouchedX = row.Count > 3 ? row[3].X : 0, untouchedZ = row.Count > 3 ? row[3].Z : 0;
            int moveX = wallTargetX - oldWallX, moveZ = wallTargetZ - oldWallZ;
            if (!CommitWorldDragAt(wallTargetX, wallTargetZ) || row[1].X != wallTargetX || row[1].Z != wallTargetZ
                || row[0].X != firstX + moveX || row[0].Z != firstZ + moveZ
                || row[2].X != lastX + moveX || row[2].Z != lastZ + moveZ
                || row.Count > 3 && (row[3].X != untouchedX || row[3].Z != untouchedZ))
                return WorldDragSmokeFailed("selected wall group did not move atomically");
            moving = -1; movingWallRow = false; ResetWorldDrag(); HidePlacementPresentation();
            session.Notice = "拖拽验收：建筑可按住移动，同排墙段可划选后整组拖动。";
            Debug.Log("HEARTHHOLD_WORLD_DRAG_READY: building moved and three contiguous walls moved together.");
            return true;
        }

        private static bool WorldDragSmokeFailed(string reason)
        {
            Debug.LogError("HEARTHHOLD_WORLD_DRAG_FAILED: " + reason);
            return false;
        }
    }
}
