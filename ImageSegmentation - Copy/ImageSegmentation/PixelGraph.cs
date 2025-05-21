using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageTemplate;

namespace ImageSegmentation
{
    public class PixelNode
    {
        public int Index; // 1D index (y * width + x)
        public int X, Y;
        public byte R, G, B;

        public PixelNode(int index, int x, int y, byte r, byte g, byte b)
        {
            Index = index;
            X = x;
            Y = y;
            R = r;
            G = g;
            B = b;
        }

        // 5D Euclidean distance
        public float DistanceTo(PixelNode other)
        {
            float dr = this.R - other.R;
            float dg = this.G - other.G;
            float db = this.B - other.B;

            return (float)Math.Sqrt(dr * dr + dg * dg + db * db);
        }

    }

    public class Edge : IComparable<Edge>
    {
        public int From;
        public int To;
        public float Weight;

        public Edge(int from, int to, float weight)
        {
            From = from;
            To = to;
            Weight = weight;
        }

        public int CompareTo(Edge other)
        {
            return this.Weight.CompareTo(other.Weight);
        }
    }

    public class PixelGraph
    {
        public List<Edge> Edges;
        public PixelNode[] Nodes;
        private int width, height;

        public PixelGraph(RGBPixel[,] image)
        {
            width = image.GetLength(1);
            height = image.GetLength(0);
            int totalPixels = width * height;

            Nodes = new PixelNode[totalPixels];
            Edges = new List<Edge>();

            // Create nodes
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    Nodes[index] = new PixelNode(index, x, y,
                        image[y, x].red,
                        image[y, x].green,
                        image[y, x].blue);
                }
            }

            // 8-connected neighborhood with proper edge weights
            int[,] directions = {
                {-1,-1}, {-1,0}, {-1,1},
                {0,-1},          {0,1},
                {1,-1},  {1,0},  {1,1}
            };

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int currentIdx = y * width + x;
                    var current = Nodes[currentIdx];

                    for (int d = 0; d < directions.GetLength(0); d++)
                    {
                        int nx = x + directions[d, 1];
                        int ny = y + directions[d, 0];

                        if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                        {
                            int neighborIdx = ny * width + nx;
                            var neighbor = Nodes[neighborIdx];

                            // Use L2 norm for color difference (as in reference)
                            float diffR = current.R - neighbor.R;
                            float diffG = current.G - neighbor.G;
                            float diffB = current.B - neighbor.B;
                            float weight = (float)Math.Sqrt(diffR * diffR + diffG * diffG + diffB * diffB);

                            Edges.Add(new Edge(currentIdx, neighborIdx, weight));
                        }
                    }
                }
            }

            Edges.Sort();
        }
    }
}