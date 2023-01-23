using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;

namespace ReverseTileGen {
    class Program {

        static bool ForceCopyInitialTiles = true;
        static bool ForceGenerateTiles = true;
        static bool KeepInputFormat = false;
        static string InputDir;
        static string OutputDir;
        static string InputFormat = "png";
        static string OutputFormat = "jpg";
        static int Resolution = 768;
        static int MaxZoom = 10;
        static int MinZoom = 4;

        static List<Layer> Layers = new List<Layer>();

        internal class Layer {
            public int Zoom;
            public List<Tile> Tiles = new List<Tile>();


            public int MaxX {
                get {
                    if (Tiles.Count == 0) return 0;
                    return Tiles.Max(t => t.X);
                }
            }
            public int MaxY {
                get {
                    if (Tiles.Count == 0) return 0;
                    return Tiles.Max(t => t.Y);
                }
            }
        }

        internal class Tile {
            public int Zoom;
            public int X;
            public int Y;
            public string Path {
                get {
                    return $"{Zoom}\\{X}\\{Y}.png";
                }
            }
            public string FullPath {
                get {
                    return $"{OutputDir}\\{this.Path}";
                }
            }
        }

        public void Run() {
            InputDir = "C:\\Users\\dankrusi\\Code\\stable-diffusion-webui\\outputs\\txt2img-images";
            OutputDir = "C:\\Users\\dankrusi\\Code\\ReverseTileGen\\Output";
            var outputImageFormat = System.Drawing.Imaging.ImageFormat.Jpeg;
            if(OutputFormat == "png") outputImageFormat = System.Drawing.Imaging.ImageFormat.Png;
            else if(OutputFormat == "jpg") outputImageFormat = System.Drawing.Imaging.ImageFormat.Jpeg;

            var images = Directory.GetFiles(InputDir).ToList();
            //images = images.Take(64).ToList(); //test

            int imagesX = (int)Math.Floor(Math.Sqrt(images.Count()));
            int imagesY = (int)Math.Floor(Math.Sqrt(images.Count()));


            Console.WriteLine($"images total: {images.Count}");
            Console.WriteLine($"images x: {imagesX}");
            Console.WriteLine($"images y: {imagesY}");

            // Make sure we get a squarable number of images
            images = images.Take(imagesX * imagesY).ToList();
            imagesX = (int)Math.Floor(Math.Sqrt(images.Count()));
            imagesY = (int)Math.Floor(Math.Sqrt(images.Count()));

            Console.WriteLine($"images total: {images.Count}");
            Console.WriteLine($"images x: {imagesX}");
            Console.WriteLine($"images y: {imagesY}");



            //Console.WriteLine($"Press any key to continue...");
            //Console.ReadKey();

            // Initial layer
            Console.WriteLine($"Creating initial layer zoom level {MaxZoom}...");
            var initialLayer = new Layer();
            initialLayer.Zoom = MaxZoom;
            Layers.Add(initialLayer);
            {
                
                int currentTileX = 0;
                int currentTileY = 0;
                foreach (var image in images) {
                    var tile = new Tile();
                    tile.X = currentTileX;
                    tile.Y = currentTileY;
                    tile.Zoom = initialLayer.Zoom;
                    initialLayer.Tiles.Add(tile);

                    Console.WriteLine($"  {tile.Path}");
                    Directory.CreateDirectory(Directory.GetParent(tile.FullPath).FullName);
                    if (ForceCopyInitialTiles || !File.Exists(tile.FullPath)) {
                        File.Copy(image, tile.FullPath, true);
                    }

                    if(OutputFormat != InputFormat) {
                        var pathOutputFormat = tile.Path.Replace("." + InputFormat, "." + OutputFormat);
                        var fullPathOutputFormat = tile.FullPath.Replace("." + InputFormat, "." + OutputFormat);
                        Console.WriteLine($"  {pathOutputFormat}");
                        var imageToConvert = new Bitmap(tile.FullPath);
                        imageToConvert.Save(fullPathOutputFormat, outputImageFormat);
                        imageToConvert.Dispose();
                    }

                    // Next tile...
                    currentTileY++;
                    if (currentTileY >= imagesY) {
                        currentTileY = 0;
                        currentTileX++;
                    }
                }
            }

            for(var zoom = initialLayer.Zoom-1; zoom >= MinZoom; zoom--) {
                Console.WriteLine($"Creating layer zoom level {zoom}...");
                var currentlayer = new Layer();
                currentlayer.Zoom = zoom;
                Layers.Add(currentlayer);
                var sourceLayer = Layers.Single(l => l.Zoom == zoom + 1);
                Console.WriteLine($"  Based on zoom level {sourceLayer.Zoom}");
                int tilesX = (int)Math.Ceiling((sourceLayer.MaxX+1) / 2.0);
                int tilesY = (int)Math.Ceiling((sourceLayer.MaxY+1) / 2.0);
                Console.WriteLine($"  tiles x: {tilesX}");
                Console.WriteLine($"  tiles y: {tilesY}");
                for(var currentTileY = 0; currentTileY < tilesY; currentTileY++) {
                    for (var currentTileX = 0; currentTileX < tilesX; currentTileX++) {
                        var tile = new Tile();
                        tile.X = currentTileX;
                        tile.Y = currentTileY;
                        tile.Zoom = currentlayer.Zoom;
                        currentlayer.Tiles.Add(tile);

                        Directory.CreateDirectory(Directory.GetParent(tile.FullPath).FullName);

                        Console.WriteLine($"  {tile.Path}");

                        // Get source tiles for this
                        //                                     
                        // AB                                    
                        // CD                                    
                        //                                     
                        //                                     
                        var sourceTiles = new List<Tile>();
                        var sourceTopLeftX = ((tile.X - 0) * 2 + 0);
                        var sourceTopLeftY = ((tile.Y - 0) * 2 + 0);
                        // A
                        sourceTiles.Add(sourceLayer.Tiles.SingleOrDefault(t => t.X == (sourceTopLeftX + 0) && t.Y == (sourceTopLeftY+0)));
                        // B
                        sourceTiles.Add(sourceLayer.Tiles.SingleOrDefault(t => t.X == (sourceTopLeftX + 1) && t.Y == (sourceTopLeftY+0)));
                        // C
                        sourceTiles.Add(sourceLayer.Tiles.SingleOrDefault(t => t.X == (sourceTopLeftX + 0) && t.Y == (sourceTopLeftY+1)));
                        // D
                        sourceTiles.Add(sourceLayer.Tiles.SingleOrDefault(t => t.X == (sourceTopLeftX + 1) && t.Y == (sourceTopLeftY+1)));
                        foreach (var sourceTile in sourceTiles) {
                            if (sourceTile == null) continue;
                            Console.WriteLine($"    {sourceTile.Path}");
                        }

                        // Generate new tile
                        if(!File.Exists(tile.FullPath) || ForceGenerateTiles) {
                            // Create image.
                            var newImage = new Bitmap(Resolution, Resolution);
                            //var newImage = new Bitmap(sourceTiles.First().FullPath);
                            var currentDrawX = 0;
                            var currentDrawY = 0;
                            var graphics = Graphics.FromImage(newImage);
                            foreach (var sourceTile in sourceTiles) {
                                if (sourceTile != null) {
                                    var sourceImage = new Bitmap(sourceTile.FullPath);
                                    var drawW = Resolution / 2;
                                    var drawH = Resolution / 2;
                                    var drawX = currentDrawX * drawW;
                                    var drawY = currentDrawY * drawH;
                                    graphics.DrawImage(sourceImage, drawX, drawY, drawW, drawH);
                                    sourceImage.Dispose();
                                }

                                currentDrawX++;
                                if (currentDrawX > 1) {
                                    currentDrawX = 0;
                                    currentDrawY++;
                                }
                            }
                            Console.WriteLine($"    saving {tile.Path}...");
                            newImage.Save(tile.FullPath);
                            if (OutputFormat != InputFormat) {
                                var pathOutputFormat = tile.Path.Replace("." + InputFormat, "." + OutputFormat);
                                var fullpathOutputFormat = tile.FullPath.Replace("." + InputFormat, "." + OutputFormat);
                                Console.WriteLine($"    saving {pathOutputFormat}...");
                                newImage.Save(fullpathOutputFormat, outputImageFormat);
                            }
                            newImage.Dispose();
                        }

                        
                        
                    }
                }
            }

            // Remove the input format tile?
            if (KeepInputFormat == false && OutputFormat != InputFormat) {
                foreach (var layer in Layers) {
                    foreach(var tile in layer.Tiles) {
                        if (File.Exists(tile.FullPath)) {
                            File.Delete(tile.FullPath);
                        }
                    }
                }
            }

            foreach (var layer in Layers) {
                Console.WriteLine($"Layer {layer.Zoom}: {layer.Tiles.Count}");
            }

        }

        static void Main(string[] args) {

            Program prog = new Program();
            prog.Run();

            Console.WriteLine($"Done");
            Console.ReadKey();
        }
    }
}
