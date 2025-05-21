using ImageSegmentation;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ImageTemplate
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        RGBPixel[,] ImageMatrix;

        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string OpenedFilePath = openFileDialog1.FileName;
                ImageMatrix = ImageOperations.OpenImage(OpenedFilePath);
                ImageOperations.DisplayImage(ImageMatrix, pictureBox1);
                txtWidth.Text = ImageOperations.GetWidth(ImageMatrix).ToString();
                txtHeight.Text = ImageOperations.GetHeight(ImageMatrix).ToString();
            }
        }

        private void btnGaussSmooth_Click(object sender, EventArgs e)
        {
            if (ImageMatrix == null)
            {
                MessageBox.Show("Please open an image first.");
                return;
            }

            double sigma = double.Parse(txtGaussSigma.Text);
            int maskSize = (int)nudMaskSize.Value;
            RGBPixel[,] smoothedImage = ImageOperations.GaussianFilter1D(ImageMatrix, maskSize, sigma);
            ImageOperations.DisplayImage(smoothedImage, pictureBox2);
        }

        private void segmentButton_Click(object sender, EventArgs e)
        {
            if (ImageMatrix == null)
            {
                MessageBox.Show("Please open an image first.");
                return;
            }

            if (!float.TryParse(kValueTextBox.Text, out float k))
            {
                MessageBox.Show("Please enter a valid K value.");
                return;
            }

            // Apply Gaussian blur (? = 0.8 as specified)
            RGBPixel[,] blurred = ImageOperations.GaussianFilter1D(ImageMatrix, 5, 0.8);
            int width = ImageOperations.GetWidth(blurred);
            int height = ImageOperations.GetHeight(blurred);

            // Process each channel separately
            var channels = GetSeparateChannels(blurred);
            var segmentations = new List<int[]>();
            var segmenter = new Segmenter(width, height, k);

            foreach (var channel in channels)
            {
                PixelGraph graph = new PixelGraph(channel);
                segmentations.Add(segmenter.Segment(graph));
            }

            // Combine results (intersection of segmentations)
            int[] finalLabels = CombineSegmentations(segmentations, width, height);

            // Color and display results
            RGBPixel[,] colored = segmenter.ColorSegments(finalLabels);
            ImageOperations.DisplayImage(colored, pictureBox2);

            // Save region sizes
            SaveRegionSizes(segmenter.GetRegionSizes(finalLabels));
        }

        private List<RGBPixel[,]> GetSeparateChannels(RGBPixel[,] image)
        {
            int width = ImageOperations.GetWidth(image);
            int height = ImageOperations.GetHeight(image);
            var channels = new List<RGBPixel[,]>();

            // Create separate images for each channel
            for (int channel = 0; channel < 3; channel++)
            {
                RGBPixel[,] channelImage = new RGBPixel[height, width];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        channelImage[y, x] = new RGBPixel
                        {
                            red = (channel == 0) ? image[y, x].red : (byte)0,
                            green = (channel == 1) ? image[y, x].green : (byte)0,
                            blue = (channel == 2) ? image[y, x].blue : (byte)0
                        };
                    }
                }
                channels.Add(channelImage);
            }
            return channels;
        }

        private int[] CombineSegmentations(List<int[]> segmentations, int width, int height)
        {
            int totalPixels = width * height;
            int[] finalLabels = new int[totalPixels];
            Dictionary<string, int> labelMap = new Dictionary<string, int>();
            int currentLabel = 1;

            for (int i = 0; i < totalPixels; i++)
            {
                // Create a composite key from all channel labels
                string compositeKey = $"{segmentations[0][i]},{segmentations[1][i]},{segmentations[2][i]}";

                if (!labelMap.ContainsKey(compositeKey))
                {
                    labelMap[compositeKey] = currentLabel++;
                }
                finalLabels[i] = labelMap[compositeKey];
            }
            return finalLabels;
        }

        private void SaveRegionSizes(List<int> sizes)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter("output.txt"))
                {
                    sw.WriteLine(sizes.Count);
                    foreach (int size in sizes)
                    {
                        sw.WriteLine(size);
                    }
                }
                MessageBox.Show("Segmentation results saved to output.txt");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving results: {ex.Message}");
            }
        }

        private void MainForm_Load(object sender, EventArgs e) { }
        private void pictureBox1_Click(object sender, EventArgs e) { }
    }
}