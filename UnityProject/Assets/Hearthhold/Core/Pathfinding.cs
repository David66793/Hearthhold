using System;
using System.Collections.Generic;

namespace Hearthhold.Core
{
    // Integer-cost A*. Destructible walls carry a traversal cost; units must actually
    // destroy a wall before taking the corresponding step. Ranged goals use footprints.
    public static class Pathfinder
    {
        private static readonly int[] DX = { 1, 0, -1, 0 };
        private static readonly int[] DZ = { 0, 1, 0, -1 };
        private struct Entry
        {
            public int Id, Cost;
            public Entry(int id, int cost) { Id = id; Cost = cost; }
        }
        private static bool Before(Entry a, Entry b) { return a.Cost < b.Cost || a.Cost == b.Cost && a.Id < b.Id; }
        private static void Push(List<Entry> heap, Entry value)
        {
            int child = heap.Count; heap.Add(value);
            while (child > 0)
            {
                int parent = (child - 1) / 2;
                if (!Before(value, heap[parent])) break;
                heap[child] = heap[parent]; child = parent;
            }
            heap[child] = value;
        }
        private static Entry Pop(List<Entry> heap)
        {
            Entry result = heap[0], last = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);
            if (heap.Count == 0) return result;
            int parent = 0;
            while (parent * 2 + 1 < heap.Count)
            {
                int child = parent * 2 + 1;
                if (child + 1 < heap.Count && Before(heap[child + 1], heap[child])) child++;
                if (!Before(heap[child], last)) break;
                heap[parent] = heap[child]; parent = child;
            }
            heap[parent] = last;
            return result;
        }
        private static int StepCost(Building block, bool sapper)
        { return 10 + (block != null && block.Health > 0 && block.Kind == BuildingKind.Wall ? sapper ? 12 : 65 : 0); }
        // Full weighted-distance field lets target choice compare real approaches rather than straight-line distance.
        // Both this field and Find charge the same extra cost for a wall that must be broken.
        public static int[] Costs(Building[,] occupied, int sx, int sz, bool sapper)
        {
            int n = Rules.MapSize, count = n * n;
            int[] costs = new int[count];
            for (int i = 0; i < count; i++) costs[i] = int.MaxValue;
            sx = Math.Max(0, Math.Min(n - 1, sx)); sz = Math.Max(0, Math.Min(n - 1, sz));
            int start = sx + sz * n;
            costs[start] = 0;
            List<Entry> heap = new List<Entry>(); Push(heap, new Entry(start, 0));
            while (heap.Count > 0)
            {
                Entry current = Pop(heap);
                if (current.Cost != costs[current.Id]) continue;
                int cx = current.Id % n, cz = current.Id / n;
                for (int d = 0; d < 4; d++)
                {
                    int x = cx + DX[d], z = cz + DZ[d];
                    if (x < 0 || z < 0 || x >= n || z >= n) continue;
                    Building block = occupied[x, z];
                    if (block != null && block.Health > 0 && block.Kind != BuildingKind.Wall) continue;
                    int id = x + z * n, candidate = current.Cost + StepCost(block, sapper);
                    if (candidate >= costs[id]) continue;
                    costs[id] = candidate; Push(heap, new Entry(id, candidate));
                }
            }
            return costs;
        }
        public static int ApproachCost(int[] costs, Building target, int range)
        {
            int radius = (range + Rules.Scale - 1) / Rules.Scale + 1, n = Rules.MapSize, best = int.MaxValue;
            int minX = Math.Max(0, target.X - radius), maxX = Math.Min(n - 1, target.X + target.Spec.Size + radius);
            int minZ = Math.Max(0, target.Z - radius), maxZ = Math.Min(n - 1, target.Z + target.Spec.Size + radius);
            for (int z = minZ; z <= maxZ; z++)
                for (int x = minX; x <= maxX; x++)
                    if (costs[x + z * n] < best && target.DistanceSquared(x * Rules.Scale + Rules.Scale / 2, z * Rules.Scale + Rules.Scale / 2) <= (long)range * range)
                        best = costs[x + z * n];
            return best;
        }
        public static List<Cell> Find(Building[,] occupied, int sx, int sz, Building target, int range, bool sapper)
        {
            int n = Rules.MapSize, count = n * n;
            int[] costs = new int[count], parents = new int[count];
            bool[] closed = new bool[count];
            for (int i = 0; i < count; i++) { costs[i] = int.MaxValue; parents[i] = -1; }
            sx = Math.Max(0, Math.Min(n - 1, sx)); sz = Math.Max(0, Math.Min(n - 1, sz));
            int start = sx + sz * n;
            costs[start] = 0;
            List<int> open = new List<int>(); open.Add(start);
            int found = -1;
            while (open.Count > 0)
            {
                int bestIndex = 0, bestScore = int.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    int id = open[i];
                    int score = costs[id] + Heuristic(id % n, id / n, target, range);
                    if (score < bestScore || (score == bestScore && id < open[bestIndex])) { bestIndex = i; bestScore = score; }
                }
                int current = open[bestIndex]; open.RemoveAt(bestIndex);
                if (closed[current]) continue;
                closed[current] = true;
                int cx = current % n, cz = current / n;
                if (target.DistanceSquared(cx * 1000 + 500, cz * 1000 + 500) <= (long)range * range)
                { found = current; break; }
                for (int d = 0; d < 4; d++)
                {
                    int x = cx + DX[d], z = cz + DZ[d];
                    if (x < 0 || z < 0 || x >= n || z >= n) continue;
                    int id = x + z * n;
                    if (closed[id]) continue;
                    Building block = occupied[x, z];
                    if (block != null && block.Health > 0 && block.Kind != BuildingKind.Wall) continue;
                    int candidate = costs[current] + StepCost(block, sapper);
                    if (candidate < costs[id]) { costs[id] = candidate; parents[id] = current; open.Add(id); }
                }
            }
            List<Cell> result = new List<Cell>();
            if (found < 0) return result;
            while (found != start && found >= 0) { result.Add(new Cell(found % n, found / n)); found = parents[found]; }
            result.Reverse();
            return result;
        }
        private static int Heuristic(int x, int z, Building target, int range)
        {
            int dx = Math.Max(target.X - x, Math.Max(0, x - target.X - target.Spec.Size));
            int dz = Math.Max(target.Z - z, Math.Max(0, z - target.Z - target.Spec.Size));
            return Math.Max(0, dx + dz - (range + 999) / 1000 - 1) * 10;
        }
    }
}
