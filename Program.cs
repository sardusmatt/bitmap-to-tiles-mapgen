using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace BitmapToTiles {
    class Program {
        /**
         * BitmapToTiles expects 5 command line arguments
         * - background image file
         * - boolean map file
         * - tile width
         * - tile height
         * - texturing style (either 1: classic, 2: inferno or 3: sci-fi)
         * While there are no restriction for dimensions, tile width and height should be 64|128|256|512 for best results
         * Background image and bitmap should also have same dimensions, which should be multiples of 1024|2048|4096 x 1024|2048|4096
         * The tool is expected to run in the parent folder of a 'textures' directory, containing Base, Up and Down textures for each texturing style
         * The tool will output the tiled map into an 'output' folder in the current working directory
         * E.g.: .\BitmapToTiles.exe .\testBackground.png .\testBitmap.png 128 128 1
         **/
        static void Main (string[] args) {
            int tileWidth = Convert.ToInt32(args[2]);
            int tileHeight = Convert.ToInt32(args[3]);
            int style = Convert.ToInt32(args[4]);
            Console.Out.WriteLine("TileWidth: " + tileWidth);
            Console.Out.WriteLine("TileHeight: " + tileHeight);
            Bitmap thumbnail = null;
            int thumbnailWidth = 0, thumbnailHeight = 0;
            string outputFolder = ".\\output\\";
            System.IO.Directory.CreateDirectory(outputFolder);

            for (int p = 0; p < 2; p++) {
                string prefix = (p == 0) ? "background" : "map";
                Bitmap b = new Bitmap(args[p]);
                if (p == 0) {
                    switch (b.Width) {
                        case 1024:
                            thumbnailWidth = 64;
                            break;
                        case 2048:
                            thumbnailWidth = 128;
                            break;
                        case 4096:
                            thumbnailWidth = 256;
                            break;
                    }
                    switch (b.Height) {
                        case 1024:
                            thumbnailHeight = 64;
                            break;
                        case 2048:
                            thumbnailHeight = 128;
                            break;
                        case 4096:
                            thumbnailHeight = 256;
                            break;
                    }
                    Image.GetThumbnailImageAbort myCallback = new Image.GetThumbnailImageAbort(ThumbnailCallback);
                    thumbnail = new Bitmap(b.GetThumbnailImage(thumbnailWidth, thumbnailHeight, myCallback, IntPtr.Zero));
                }
                if (p == 1) {
                    Console.Out.WriteLine("Texturing...");
                    string[] texturesPaths = new string[3];
                    string pre = null;
                    switch (style) {
                        case 1:
                            pre = "classic";
                            break;
                        case 2:
                            pre = "inferno";
                            break;
                        case 3:
                            pre = "sci-fi";
                            break;
                    }
                    texturesPaths[0] = @".\textures\" + pre + "Base.png";
                    texturesPaths[1] = @".\textures\" + pre + "Down.png";
                    texturesPaths[2] = @".\textures\" + pre + "Up.png";
                    b = TexturizeBooleanMap(b, texturesPaths[0], texturesPaths[1], texturesPaths[2]);
                    Bitmap overDraw = new Bitmap(b, thumbnailWidth, thumbnailHeight);
                    Console.Out.WriteLine("Texturing done");
                    Console.Out.WriteLine("Generating minimap thumbnail...");
                    /* Generate full thumbnail */
                    for (int x = 0; x < overDraw.Width; x++)
                        for (int y = 0; y < overDraw.Height; y++)
                            /* if not transparent, then overdraw */
                            if (overDraw.GetPixel(x, y).A != 0)
                                thumbnail.SetPixel(x, y, overDraw.GetPixel(x, y));
                    overDraw.Dispose();
                    thumbnail.Save(outputFolder + "mini.png");
                    Console.Out.WriteLine("Generating minimap thumbnail done");
                    thumbnail.Dispose();
                }
                int rowBound = (int)Math.Ceiling((float)b.Height / (float)tileHeight);
                int columnBound = (int)Math.Ceiling((float)b.Width / (float)tileWidth);
                Console.Out.WriteLine("RowBound: " + rowBound);
                Console.Out.WriteLine("ColumnBound: " + columnBound);
                PixelFormat t = b.PixelFormat;
                for (int i = 0; i < rowBound; i++) {
                    for (int j = 0; j < columnBound; j++) {
                        int width, height;
                        if ((j + 1) * tileWidth > b.Width)
                            width = b.Width - (j * tileWidth);
                        else
                            width = tileWidth;
                        if ((i + 1) * tileHeight > b.Height)
                            height = b.Height - (i * tileHeight);
                        else
                            height = tileHeight;
                        Console.Out.WriteLine("Split: (" + j * tileWidth + ", " + i * tileHeight + ") " + width + " x " + height);
                        b.Clone(new Rectangle(j * tileWidth, i * tileHeight, width, height), t).Save(outputFolder + prefix + i + j + ".png");
                    }
                }
                b.Dispose();
            }
        }

        public static bool ThumbnailCallback () {
            return false;
        }

        private static Bitmap TexturizeBooleanMap (Bitmap b, string baseTexturePath, string downTexturePath, string upTexturePath) {
            /* Starting bitmap coordinates */
            int x = 0, y = 0;
            /* Starting texture coordinates */
            int baseTextureX = 0, baseTextureY = 0;
            /* Terrain section height to be filled with up and down textures (for edge cases) */
            int textureY = 0;
            /* Used to determine when to stop in the 2nd and 3rd texturing pass */
            int sectionHeight = 0;
            /* X-axis pos for up and down textures */
            int textureX = 0;
            
            Console.Out.WriteLine("Base texture loading...");
            Bitmap baseTexture = new Bitmap(baseTexturePath);
            Console.Out.WriteLine("done");

            for (x = 0; x < b.Width; x++) {
                for (y = 0; y < b.Height; y++) {
                    /* if there is anything to draw */
                    if (b.GetPixel(x, y).A != 0) {
                        /* get the colour of the base texture pixel, to be drawn over the actual pixel in the result image */
                        b.SetPixel(x, y, baseTexture.GetPixel(baseTextureX, baseTextureY));
                    }
                    /* increment Y-axis position for the base texture */
                    baseTextureY++;
                    /* we are using seamless textures, so wrap around once it gets to the end */
                    if (baseTextureY >= baseTexture.Height)
                        baseTextureY = 0;
                }
                /* increment X-axis position and wrap around if it got to the end */
                baseTextureX++;
                if (baseTextureX >= baseTexture.Width)
                    baseTextureX = 0;
            }
            baseTexture.Dispose();

            Console.Out.WriteLine("Base texture disposing");

            Console.Out.WriteLine("Up texture loading...");
            Bitmap upTexture = new Bitmap(upTexturePath);
            Console.Out.WriteLine("done");

            /*
             * For the steps above it didn't matter, but now we want to proceed by column
             * in the matrix the positions relative to x,y are [y,x], for both the lockRectangle and the booleanMap, as it's
             * generated mapping into booleans the result of the lockRectangle
             */
            /* Starting from 1, since we are trying to find the higher end (something that has empty space above) */
            for (y = 1; y < b.Height; y++) {
                for (x = 0; x < b.Width; x++) {
                    /* if it's full */
                    if (b.GetPixel(x, y).A != 0) {
                        /* and it's the top end */
                        if (b.GetPixel(x, y - 1).A == 0) {
                            sectionHeight = 0;
                            /* then determine how far to proceed */
                            while ((y + sectionHeight) < b.Height && b.GetPixel(x, y + sectionHeight).A != 0)
                                sectionHeight++;

                            /* compute the limit */
                            if (sectionHeight / 2 >= upTexture.Height)
                                sectionHeight = upTexture.Height;
                            else
                                sectionHeight /= 2;

                            /* Texturing, column by column */
                            for (textureY = 0; textureY < sectionHeight; textureY++) {
                                b.SetPixel(x, y + textureY - 2, upTexture.GetPixel(textureX, textureY));
                            }
                            textureX++;
                            if (textureX >= upTexture.Width)
                                textureX = 0;

                        }
                    }
                }
            }
            upTexture.Dispose();
            Console.Out.WriteLine("Up texture disposing");

            Console.Out.WriteLine("Down texture loading...");
            Bitmap downTexture = new Bitmap(downTexturePath);
            Console.Out.WriteLine("done");

            /* Starting from 0, because we are looking for the lower end (something that would have empty space below) */
            for (y = 0; y < b.Height - 1; y++) {
                for (x = 0; x < b.Width; x++) {
                    /* if it's full */
                    if (b.GetPixel(x, y).A != 0) {
                        /* and it's the lower end */
                        if (b.GetPixel(x, y + 1).A == 0) {
                            sectionHeight = 0;
                            /* determine how far to proceed */
                            while ((y - sectionHeight) > 0 && b.GetPixel(x, y - sectionHeight).A != 0)
                                sectionHeight++;

                            /* compute the limit */
                            if (sectionHeight / 2 >= downTexture.Height)
                                sectionHeight = (int)(downTexture.Height * 0.75f);
                            else
                                sectionHeight /= 2;

                            /* Texturing */
                            for (textureY = 0; textureY < sectionHeight; textureY++) {
                                b.SetPixel(x, y - textureY, downTexture.GetPixel(textureX, textureY));
                            }
                            textureX++;
                            if (textureX >= downTexture.Width)
                                textureX = 0;

                        }
                    }
                }
            }

            downTexture.Dispose();
            Console.Out.WriteLine("Down texture disposing");
            return b;
        }
    }
}
