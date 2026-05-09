using UnityEngine;
using System.Collections.Generic;

namespace Territory.Core
{
    /// <summary>
    /// Min-heap for A* pathfinding. Stores (position, fScore); O(log n) Enqueue/Dequeue.
    /// Replaces List+Sort → efficient priority queue.
    /// </summary>
    public class MinHeap
    {
        private readonly List<(Vector2Int pos, int f)> heap = new List<(Vector2Int, int)>();

        public int Count => heap.Count;

        public void Enqueue(Vector2Int pos, int fScore)
        {
            heap.Add((pos, fScore));
            int i = heap.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (heap[parent].f <= heap[i].f) break;
                var tmp = heap[parent];
                heap[parent] = heap[i];
                heap[i] = tmp;
                i = parent;
            }
        }

        public Vector2Int Dequeue()
        {
            var min = heap[0];
            heap[0] = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);
            if (heap.Count == 0) return min.pos;

            int i = 0;
            while (true)
            {
                int left = 2 * i + 1, right = 2 * i + 2;
                int smallest = i;
                if (left < heap.Count && heap[left].f < heap[smallest].f) smallest = left;
                if (right < heap.Count && heap[right].f < heap[smallest].f) smallest = right;
                if (smallest == i) break;
                var tmp = heap[i];
                heap[i] = heap[smallest];
                heap[smallest] = tmp;
                i = smallest;
            }
            return min.pos;
        }
    }
}
