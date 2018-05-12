﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Zavolokas.GdiExtensions;
using Zavolokas.ImageProcessing.Inpainting;
using Zavolokas.Structures;
using Zavolokas.Utils.Processes;

namespace Reshuffle
{
    class Program
    {
        static void Main(string[] args)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            const string imagesPath = "../../../images";
            const string outputPath = "../../out";
            string resultPath = Path.Combine(outputPath, "result.png");

            const string imageName = "t058.jpg";
            const string markupName = "m058_1.png";

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            // open an image and an image with a marked area to inpaint
            var imageArgb = OpenArgbImage(Path.Combine(imagesPath, imageName));
            var markupArgb = OpenArgbImage(Path.Combine(imagesPath, markupName));

            var srcArea = markupArgb.FromArgbToArea2D();
            var dstArea = srcArea.Translate(-100, 100);

            markupArgb = dstArea
                            .ToBitmap(Color.Red, imageArgb.Width, imageArgb.Height)
                            .ToArgbImage();

            imageArgb = imageArgb.CopyFromImage(dstArea, imageArgb, srcArea);
            var reshuffler = new Reshuffler();

            reshuffler.IterationFinished += (sender, eventArgs) =>
            {
                Console.WriteLine($"Level:{eventArgs.LevelIndex}\tIteration: {eventArgs.InpaintIteration}");

                eventArgs.InpaintedLabImage
                    .FromLabToRgb()
                    .FromRgbToBitmap()
                    .CloneWithScaleTo(imageArgb.Width, imageArgb.Height, InterpolationMode.HighQualityBilinear)
                    .SaveTo(Path.Combine(outputPath, $"r{eventArgs.LevelIndex}_{eventArgs.InpaintIteration}.png"), ImageFormat.Png);
            };

            Console.WriteLine($"Begin processing ...");
            var result = reshuffler.Reshuffle(imageArgb, markupArgb);
            result
                .FromArgbToBitmap()
                .SaveTo(resultPath, ImageFormat.Png)
                .ShowFile();

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            Console.WriteLine($"Done in {elapsedMs}ms");
        }

        private static ZsImage OpenArgbImage(string path)
        {
            const double maxSize = 2048.0;

            var imageBitmap = new Bitmap(path);
            if (imageBitmap.Width > maxSize || imageBitmap.Height > maxSize)
            {
                var tmp = imageBitmap;
                double percent = imageBitmap.Width > imageBitmap.Height ? maxSize / imageBitmap.Width : maxSize / imageBitmap.Height;
                imageBitmap = imageBitmap.CloneWithScaleTo((int)(imageBitmap.Width * percent), (int)(imageBitmap.Height * percent));
                tmp.Dispose();
            }

            var image = imageBitmap.ToArgbImage();
            imageBitmap.Dispose();
            return image;
        }
    }
}
