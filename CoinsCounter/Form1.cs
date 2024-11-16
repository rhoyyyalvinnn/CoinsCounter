using Emgu.CV.Structure;
using Emgu.CV;

namespace CoinsCounter
{
    public partial class Form1 : Form
    {

        Bitmap loaded;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Show the OpenFileDialog
            openFileDialog1.ShowDialog();
        }


        private void openFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            loaded = new Bitmap(openFileDialog1.FileName);
            pictureBox1.Image = loaded;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (loaded == null)
            {
                MessageBox.Show("Please load an image first.");
                return;
            }

            // Convert the loaded image to grayscale
            Bitmap grayImage = new Bitmap(loaded.Width, loaded.Height);
            for (int y = 0; y < loaded.Height; y++)
            {
                for (int x = 0; x < loaded.Width; x++)
                {
                    Color originalColor = loaded.GetPixel(x, y);
                    int grayScale = (int)(originalColor.R * 0.3 + originalColor.G * 0.59 + originalColor.B * 0.11);
                    Color grayColor = Color.FromArgb(grayScale, grayScale, grayScale);
                    grayImage.SetPixel(x, y, grayColor);
                }
            }

            // Apply edge detection (basic version using a simple algorithm like Sobel or similar)
            Bitmap edgeImage = ApplyEdgeDetection(grayImage);

            // Display the processed image in the PictureBox (optional)
            pictureBox2.Image = edgeImage;

            // Find contours and calculate coin value
            double totalValue = FindAndEstimateCoins(edgeImage);

            MessageBox.Show($"Total estimated coin value: ${totalValue:F2}");
        }




        private Bitmap ApplyEdgeDetection(Bitmap grayImage)
        {
            Bitmap edgeImage = new Bitmap(grayImage.Width, grayImage.Height);

            // Simple edge detection (Sobel filter or custom filter)
            for (int y = 1; y < grayImage.Height - 1; y++)
            {
                for (int x = 1; x < grayImage.Width - 1; x++)
                {
                    // Calculate gradient magnitudes (basic filter example)
                    int gx = -grayImage.GetPixel(x - 1, y - 1).R + grayImage.GetPixel(x + 1, y - 1).R
                             - 2 * grayImage.GetPixel(x - 1, y).R + 2 * grayImage.GetPixel(x + 1, y).R
                             - grayImage.GetPixel(x - 1, y + 1).R + grayImage.GetPixel(x + 1, y + 1).R;

                    int gy = -grayImage.GetPixel(x - 1, y - 1).R - 2 * grayImage.GetPixel(x, y - 1).R - grayImage.GetPixel(x + 1, y - 1).R
                             + grayImage.GetPixel(x - 1, y + 1).R + 2 * grayImage.GetPixel(x, y + 1).R + grayImage.GetPixel(x + 1, y + 1).R;

                    int magnitude = (int)Math.Sqrt(gx * gx + gy * gy);
                    magnitude = Math.Clamp(magnitude, 0, 255); // Clamp value between 0 and 255

                    edgeImage.SetPixel(x, y, Color.FromArgb(magnitude, magnitude, magnitude));
                }
            }

            return edgeImage;
        }



        public static double FindAndEstimateCoins(Bitmap edgeImage)
        {
            double totalValue = 0;

            // Convert the image to grayscale (if not already grayscale)
            Bitmap grayscaleImage = ConvertToGrayscale(edgeImage);

            // Apply threshold to get binary image (edge detection or simple binary threshold)
            Bitmap thresholdedImage = ApplyThreshold(grayscaleImage, 128); // Adjust threshold value as necessary

            // Find connected components (blobs) in the binary image
            List<Rectangle> detectedRegions = FindConnectedComponents(thresholdedImage);

            // Estimate coin values based on the area of each connected region
            foreach (var region in detectedRegions)
            {
                double area = region.Width * region.Height;

                // Use area thresholds to classify the coins
                if (area > 800 && area < 2000) // Example area thresholds
                {
                    if (area >= 800 && area < 1500)
                    {
                        totalValue += 0.05; // 5 cents for smaller coins
                    }
                    else if (area >= 1500 && area < 2000)
                    {
                        totalValue += 0.10; // 10 cents for medium coins
                    }
                }
            }

            return totalValue;
        }

        // Converts the image to grayscale
        private static Bitmap ConvertToGrayscale(Bitmap original)
        {
            Bitmap grayscale = new Bitmap(original.Width, original.Height);
            for (int x = 0; x < original.Width; x++)
            {
                for (int y = 0; y < original.Height; y++)
                {
                    Color pixelColor = original.GetPixel(x, y);
                    int grayValue = (int)(0.3 * pixelColor.R + 0.59 * pixelColor.G + 0.11 * pixelColor.B);
                    grayscale.SetPixel(x, y, Color.FromArgb(grayValue, grayValue, grayValue));
                }
            }
            return grayscale;
        }

        // Applies a simple threshold to the grayscale image
        private static Bitmap ApplyThreshold(Bitmap grayscaleImage, int threshold)
        {
            Bitmap thresholdedImage = new Bitmap(grayscaleImage.Width, grayscaleImage.Height);
            for (int x = 0; x < grayscaleImage.Width; x++)
            {
                for (int y = 0; y < grayscaleImage.Height; y++)
                {
                    Color pixelColor = grayscaleImage.GetPixel(x, y);
                    int grayValue = pixelColor.R; // Since it's grayscale, R, G, and B are all the same
                    if (grayValue < threshold)
                    {
                        thresholdedImage.SetPixel(x, y, Color.Black);
                    }
                    else
                    {
                        thresholdedImage.SetPixel(x, y, Color.White);
                    }
                }
            }
            return thresholdedImage;
        }

        // Finds connected components in the thresholded image (blobs)
        private static List<Rectangle> FindConnectedComponents(Bitmap thresholdedImage)
        {
            List<Rectangle> regions = new List<Rectangle>();
            bool[,] visited = new bool[thresholdedImage.Width, thresholdedImage.Height];

            for (int x = 0; x < thresholdedImage.Width; x++)
            {
                for (int y = 0; y < thresholdedImage.Height; y++)
                {
                    if (!visited[x, y] && thresholdedImage.GetPixel(x, y).R == 255) // White pixel (potential coin)
                    {
                        Rectangle region = FloodFill(thresholdedImage, x, y, visited);
                        if (region.Width * region.Height > 0) // Only add regions with non-zero area
                        {
                            regions.Add(region);
                        }
                    }
                }
            }
            return regions;
        }

        // Performs a flood fill to find connected components (blobs)
        private static Rectangle FloodFill(Bitmap image, int startX, int startY, bool[,] visited)
        {
            int minX = startX, minY = startY, maxX = startX, maxY = startY;
            Queue<Point> pointsToVisit = new Queue<Point>();
            pointsToVisit.Enqueue(new Point(startX, startY));

            while (pointsToVisit.Count > 0)
            {
                Point current = pointsToVisit.Dequeue();
                int x = current.X;
                int y = current.Y;

                if (x < 0 || x >= image.Width || y < 0 || y >= image.Height || visited[x, y] || image.GetPixel(x, y).R != 255)
                    continue;

                visited[x, y] = true;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);

                // Check neighboring pixels (4-connectivity)
                pointsToVisit.Enqueue(new Point(x - 1, y));
                pointsToVisit.Enqueue(new Point(x + 1, y));
                pointsToVisit.Enqueue(new Point(x, y - 1));
                pointsToVisit.Enqueue(new Point(x, y + 1));
            }

            return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
    }
}
