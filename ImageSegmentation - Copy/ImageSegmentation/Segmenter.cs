using ImageTemplate;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageSegmentation
{
    public class Segmenter
    {
        private int width, height;
        private float k;
        private const float MinRegionSize = 20; // Adjusted for better matching

        public Segmenter(int width, int height, float k)
        {
            this.width = width;
            this.height = height;
            this.k = k;
        }

        private float Tau(int size) => k / size;

        public int[] Segment(PixelGraph graph)
        {
            int totalPixels = width * height;
            UnionFind uf = new UnionFind(totalPixels);

            // Phase 1: Initial segmentation
            foreach (var edge in graph.Edges)
            {
                int root1 = uf.Find(edge.From);
                int root2 = uf.Find(edge.To);

                if (root1 != root2)
                {
                    float mInt = Math.Min(
                        uf.GetInternalDifference(root1) + Tau(uf.GetSize(root1)),
                        uf.GetInternalDifference(root2) + Tau(uf.GetSize(root2)));

                    if (edge.Weight <= mInt)
                    {
                        uf.Union(root1, root2, edge.Weight);
                    }
                }
            }

            // Phase 2: Post-processing
            return PostProcess(uf, graph);
        }

        private int[] PostProcess(UnionFind uf, PixelGraph graph)
        {
            Dictionary<int, int> componentSizes = new Dictionary<int, int>();
            Dictionary<int, List<int>> adjacents = new Dictionary<int, List<int>>();

            // Build adjacency map and count sizes
            for (int i = 0; i < width * height; i++)
            {
                int root = uf.Find(i);
                if (!componentSizes.ContainsKey(root))
                {
                    componentSizes[root] = 0;
                    adjacents[root] = new List<int>();
                }
                componentSizes[root]++;
            }

            // Find adjacent components - SAFER IMPLEMENTATION
            foreach (var edge in graph.Edges)
            {
                int root1 = uf.Find(edge.From);
                int root2 = uf.Find(edge.To);

                if (root1 != root2)
                {
                    // Ensure both roots exist in adjacents
                    if (!adjacents.ContainsKey(root1)) adjacents[root1] = new List<int>();
                    if (!adjacents.ContainsKey(root2)) adjacents[root2] = new List<int>();

                    if (!adjacents[root1].Contains(root2))
                        adjacents[root1].Add(root2);
                    if (!adjacents[root2].Contains(root1))
                        adjacents[root2].Add(root1);
                }
            }

            // Merge small regions - SAFER IMPLEMENTATION
            var componentsToProcess = componentSizes.Keys.ToList();
            foreach (var component in componentsToProcess)
            {
                if (componentSizes.ContainsKey(component) &&
                    componentSizes[component] < MinRegionSize &&
                    adjacents.ContainsKey(component))
                {
                    // Find largest adjacent component that still exists
                    var validNeighbors = adjacents[component]
                        .Where(n => componentSizes.ContainsKey(n))
                        .OrderByDescending(n => componentSizes[n])
                        .ToList();

                    if (validNeighbors.Count > 0)
                    {
                        int bestNeighbor = validNeighbors.First();
                        uf.Union(component, bestNeighbor, float.MaxValue);
                        componentSizes[bestNeighbor] += componentSizes[component];
                        componentSizes.Remove(component);
                    }
                }
            }

            // Final labeling
            int[] labels = new int[width * height];
            Dictionary<int, int> labelMap = new Dictionary<int, int>();
            int currentLabel = 1;

            for (int i = 0; i < width * height; i++)
            {
                int root = uf.Find(i);
                if (!labelMap.ContainsKey(root))
                {
                    labelMap[root] = currentLabel++;
                }
                labels[i] = labelMap[root];
            }

            return labels;
        }

        public RGBPixel[,] ColorSegments(int[] labels)
        {
            RGBPixel[,] output = new RGBPixel[height, width];
            Dictionary<int, RGBPixel> regionColors = new Dictionary<int, RGBPixel>();
            Random rand = new Random(123); // Fixed seed for consistent colors

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    int label = labels[index];

                    if (!regionColors.ContainsKey(label))
                    {
                        regionColors[label] = new RGBPixel
                        {
                            red = (byte)rand.Next(100, 256),
                            green = (byte)rand.Next(100, 256),
                            blue = (byte)rand.Next(100, 256)
                        };
                    }
                    output[y, x] = regionColors[label];
                }
            }
            return output;
        }

        public List<int> GetRegionSizes(int[] labels)
        {
            Dictionary<int, int> regionCounts = new Dictionary<int, int>();
            foreach (int label in labels)
            {
                if (!regionCounts.ContainsKey(label))
                    regionCounts[label] = 0;
                regionCounts[label]++;
            }

            List<int> sizes = new List<int>(regionCounts.Values);
            sizes.Sort((a, b) => b.CompareTo(a));
            return sizes;
        }
    }
}