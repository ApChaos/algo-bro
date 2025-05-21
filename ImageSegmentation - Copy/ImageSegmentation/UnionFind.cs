using System;

namespace ImageSegmentation
{
    public class UnionFind
    {
        private int[] parent;
        private int[] rank;
        private int[] size;
        private float[] internalDifference;

        public UnionFind(int n)
        {
            parent = new int[n];
            rank = new int[n];
            size = new int[n];
            internalDifference = new float[n];

            for (int i = 0; i < n; i++)
            {
                parent[i] = i;
                rank[i] = 0;
                size[i] = 1;
                internalDifference[i] = 0;
            }
        }

        public int Find(int x)
        {
            // Path compression
            if (parent[x] != x)
                parent[x] = Find(parent[x]);
            return parent[x];
        }

        public void Union(int x, int y, float edgeWeight)
        {
            int rootX = Find(x);
            int rootY = Find(y);

            if (rootX == rootY) return;

            // Union by rank
            if (rank[rootX] < rank[rootY])
            {
                parent[rootX] = rootY;
                size[rootY] += size[rootX];
                internalDifference[rootY] = Math.Max(
                    Math.Max(internalDifference[rootX], internalDifference[rootY]),
                    edgeWeight);
            }
            else
            {
                parent[rootY] = rootX;
                size[rootX] += size[rootY];
                internalDifference[rootX] = Math.Max(
                    Math.Max(internalDifference[rootX], internalDifference[rootY]),
                    edgeWeight);

                if (rank[rootX] == rank[rootY])
                    rank[rootX]++;
            }
        }

        public int GetSize(int x)
        {
            return size[Find(x)];
        }

        public float GetInternalDifference(int x)
        {
            return internalDifference[Find(x)];
        }

        // Helper method for debugging
        public int GetNumberOfComponents()
        {
            int count = 0;
            for (int i = 0; i < parent.Length; i++)
            {
                if (parent[i] == i) count++;
            }
            return count;
        }
    }
}