using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace BitmapToTiles {
    class Program {
        static void Main (string[] args) {
            int tileWidth = Convert.ToInt32(args[2]);
            int tileHeight = Convert.ToInt32(args[3]);
            int style = Convert.ToInt32(args[4]);
            Console.Out.WriteLine("TileWidth: " + tileWidth);
            Console.Out.WriteLine("TileHeight: " + tileHeight);
            Bitmap thumbnail = null;
            int thumbnailWidth = 0, thumbnailHeight = 0;
            for (int p = 0; p < 2; p++) {
                string prefix = (p ==0) ? "background" : "map";
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
                    texturesPaths[0] = @"..\..\textures\" + pre + "Base.png";
                    texturesPaths[1] = @"..\..\textures\" + pre + "Down.png";
                    texturesPaths[2] = @"..\..\textures\" + pre + "Up.png";
                    b = TexturizeBooleanMap(b, texturesPaths[0], texturesPaths[1], texturesPaths[2]);
                    Bitmap overDraw = new Bitmap(b, thumbnailWidth, thumbnailHeight);
                    Console.Out.WriteLine("Texturing done");
                    Console.Out.WriteLine("Generating minimap thumbnail...");
                    /* Genera il thumbnail completo */
                    for (int x = 0; x < overDraw.Width; x++)
                        for (int y = 0; y < overDraw.Height; y++)
                            /* Se non è trasparente sovrappone */
                            if (overDraw.GetPixel(x, y).A != 0)
                                thumbnail.SetPixel(x, y, overDraw.GetPixel(x, y));
                    overDraw.Dispose();
                    thumbnail.Save("mini.png");
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
                        b.Clone(new Rectangle(j * tileWidth, i * tileHeight, width, height), t).Save(prefix + i + j + ".png");
                    }
                }
                b.Dispose();
            }
        }

        public static bool ThumbnailCallback () {
            return false;
        }

        private static Bitmap TexturizeBooleanMap (Bitmap b, string baseTexturePath, string downTexturePath, string upTexturePath) {
            /* Coordinate bitmap di base */
            int x = 0, y = 0;
            /* Coordinate texture di base */
            int baseTextureX = 0, baseTextureY = 0;
            /* Altezza della sezione di terreno da riempire con up e down texture (per i casi limite) */
            int textureY = 0;
            /* Usato per stabilire dove fermarsi nel secondo e terzo passo di texturing (up, down) */
            int sectionHeight = 0;
            /* Posizione sull'asse orizzontale delle texture up e down */
            int textureX = 0;
            
            Console.Out.WriteLine("Base texture loading...");
            Bitmap baseTexture = new Bitmap(baseTexturePath);
            Console.Out.WriteLine("done");

            for (x = 0; x < b.Width; x++) {
                for (y = 0; y < b.Height; y++) {
                    /* Se la mappa prevede di disegnare qualcosa */
                    if (b.GetPixel(x, y).A != 0) {
                        /* Ricava il colore della texture base nel punto da disegnare sul pixel attuale */
                        b.SetPixel(x, y, baseTexture.GetPixel(baseTextureX, baseTextureY));
                    }
                    /* Incrementa contatore pixel baseTexture su asse delle Y */
                    baseTextureY++;
                    /* Se si è superata la dimensione della texture di base si riparte da 0, essendo le texture seamless */
                    if (baseTextureY >= baseTexture.Height)
                        baseTextureY = 0;
                }
                /* Incremento sull'asse orizzontale e controllo analogo sulla X */
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
             * Sopra era indifferente, ma qui vogliamo lavorare per colonne, quindi bisogna considerare
             * che le posizioni nella matrice rispetto a x,y sono [y,x], sia sui risultati della lockRectangle
             * sia sulla booleanMap, in quanto ottenuta da una mappatura su booleani del risultato di una
             * lockRectangle
             */
            /* Partenza da y = 1, perché cerchiamo qualcosa che posso avere dello spazio vuoto sopra (estremo superiore) */
            for (y = 1; y < b.Height; y++) {
                for (x = 0; x < b.Width; x++) {
                    /* Se è terreno */
                    if (b.GetPixel(x, y).A != 0) {
                        /* ...e è l'estremo superiore */
                        if (b.GetPixel(x, y - 1).A == 0) {
                            sectionHeight = 0;
                            /* Calcola sino a quanto procedere */
                            while ((y + sectionHeight) < b.Height && b.GetPixel(x, y + sectionHeight).A != 0)
                                sectionHeight++;

                            /* Calcolo del limite */
                            if (sectionHeight / 2 >= upTexture.Height)
                                sectionHeight = upTexture.Height;
                            else
                                sectionHeight /= 2;

                            /* Texturing colonna per colonna */
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

            /* Partenza da y = 0, perché cerchiamo qualcosa che posso avere dello spazio vuoto sotto (bordo inferiore) */
            for (y = 0; y < b.Height - 1; y++) {
                for (x = 0; x < b.Width; x++) {
                    /* Se è terreno */
                    if (b.GetPixel(x, y).A != 0) {
                        /* ...e è l'estremo inferiore */
                        if (b.GetPixel(x, y + 1).A == 0) {
                            sectionHeight = 0;
                            /* Calcola sino a quanto procedere */
                            while ((y - sectionHeight) > 0 && b.GetPixel(x, y - sectionHeight).A != 0)
                                sectionHeight++;

                            /* Calcolo del limite */
                            if (sectionHeight / 2 >= downTexture.Height)
                                sectionHeight = (int)(downTexture.Height * 0.75f);
                            else
                                sectionHeight /= 2;

                            /* Texturing colonna per colonna */
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
