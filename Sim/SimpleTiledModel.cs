/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;
using WaveFunctionCollapse.Configs;
using WaveFunctionCollapse.Extensions;
using WaveFunctionCollapse.Factories;
using WaveFunctionCollapse.Xml;

namespace WaveFunctionCollapse.Sim
{
    public class SimpleTiledModel : Model
    {
        #region Simulation Initialization
        
        private SimpleTiledConfig config;

        private Tileset tileset;

        #endregion

        public SimpleTiledModel(SimpleTiledConfig config) : base(config.width, config.height)
        {
            this.config = config;
            this.Periodic = config.periodic;
            
            tileset = new Tileset();

            XElement xroot = config.LoadSampleXml();
            tileset.TileSize = xroot.Get("size", 16);
            bool unique = xroot.Get("unique", false);

            List<string> subset = config.LoadTileSubset(xroot);

            tileset.Tiles = new List<Color[]>();
            tileset.TileNames = new List<string>();
            
            var tempStationary = new List<double>();
            var action = new List<int[]>();
            var firstOccurrence = new Dictionary<string, int>();

            var tileElements = xroot.Element("tiles").Elements("tile");
            foreach (var xtile in tileElements)
            {
                string tilename = xtile.Get<string>("name");
                if (subset != null && !subset.Contains(tilename))
                {
                    continue;
                }

                TileParseUtils.ParseSymmetry(xtile, out int cardinality, out var a, out var b);

                NumberOfNodes = action.Count;
                firstOccurrence.Add(tilename, NumberOfNodes);

                var map = new int[cardinality][];
                for (int t = 0; t < cardinality; t++)
                {
                    // Number of combinations for rotations and reflection
                    // 4 rotational orientations
                    // 2 reflection orientations
                    map[t] = new int[8]; 

                    map[t][0] = t;
                    map[t][1] = a(t);
                    map[t][2] = a(a(t));
                    map[t][3] = a(a(a(t)));
                    
                    map[t][4] = b(t);
                    map[t][5] = b(a(t));
                    map[t][6] = b(a(a(t)));
                    map[t][7] = b(a(a(a(t))));

                    // Mapping neighbors
                    for (int s = 0; s < 8; s++)
                    {
                        map[t][s] += NumberOfNodes;
                    }

                    action.Add(map[t]);
                }

                if (unique)
                {
                    for (int dir = 0; dir < cardinality; dir++)
                    {
                        var bitmap = new Bitmap($"samples/{config.name}/{tilename} {dir}.png");
                        tileset.LoadUniqueTile(bitmap, tilename, dir);
                    }
                }
                else
                {
                    var bitmap = new Bitmap($"samples/{config.name}/{tilename}.png");
                    tileset.LoadTile(bitmap, tilename, cardinality, NumberOfNodes);
                }

                float weight = xtile.Get("weight", 1.0f);
                for (int t = 0; t < cardinality; t++)
                {
                    tempStationary.Add(weight);
                }
            }

            NumberOfNodes = action.Count;
            Weights = tempStationary.ToArray();

            SparseCardinalNodeAdjacency = MatrixFactory.BuildCardinalityPropagator(NumDirections, NumberOfNodes);
            
            // TODO: Make dimensionally independent
            // [Number of Directions][NodeI][NodeJ] = is adjacent
            var cardinalityAdjacencyMatrix = MatrixFactory.BuildTempCardinalityPropagator(NumDirections, NumberOfNodes);

            foreach (XElement xneighbor in xroot.Element("neighbors").Elements("neighbor"))
            {
                ParseAdjacencyNode(xneighbor, subset, firstOccurrence, cardinalityAdjacencyMatrix, action);
            }

            // Flip adjacency matrix for relationship between Node J to Node I
            for (int nodeJ = 0; nodeJ < NumberOfNodes; nodeJ++)
            {
                for (int nodeI = 0; nodeI < NumberOfNodes; nodeI++)
                {
                    cardinalityAdjacencyMatrix[2][nodeJ][nodeI] = cardinalityAdjacencyMatrix[0][nodeI][nodeJ];
                    cardinalityAdjacencyMatrix[3][nodeJ][nodeI] = cardinalityAdjacencyMatrix[1][nodeI][nodeJ];
                }
            }
            
            var sp = new List<int>();
            for (int dir = 0; dir < NumDirections; dir++)
            {
                for (int nodeI = 0; nodeI < NumberOfNodes; nodeI++)
                {
                    sp.Clear();
                    // All the nodes that Node I is adjacent to
                    var nodeIAdjacency = cardinalityAdjacencyMatrix[dir][nodeI];

                    for (int nodeJ = 0; nodeJ < NumberOfNodes; nodeJ++)
                    {
                        // Is NodeI connected to NodeJ?
                        if (nodeIAdjacency[nodeJ])
                        {
                            // Add NodeJ to sparse list
                            sp.Add(nodeJ);
                        }
                    }

                    int adjacencyCount = sp.Count;
                    if (adjacencyCount == 0)
                    {
                        Console.WriteLine($"ERROR: tile {tileset.TileNames[nodeI]} has no neighbors in direction {dir}");
                    }

                    SparseCardinalNodeAdjacency[dir][nodeI] = sp.ToArray();
                }
            }
        }

        // Builds up adjacency matrix
        // 
        // 
        private static void ParseAdjacencyNode(XElement xneighbor, 
            List<string> subset,
            Dictionary<string, int> firstOccurrence, 
            bool[][][] tempPropagator, 
            List<int[]> action)
        {
            var nodeIArgs = xneighbor.Get<string>("left").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var nodeJArgs = xneighbor.Get<string>("right").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            string nodeITileName = nodeIArgs[0];
            string nodeJTileName = nodeJArgs[0];
            if (subset != null && (!subset.Contains(nodeITileName) || !subset.Contains(nodeJTileName)))
            {
                return;
            }

            int nodeI = firstOccurrence[nodeITileName];
            int nodeIDir = nodeIArgs.Length == 1 ? 0 : int.Parse(nodeIArgs[1]);
            
            int nodeJ = firstOccurrence[nodeJTileName];
            int nodeJDir = nodeJArgs.Length == 1 ? 0 : int.Parse(nodeJArgs[1]);
            
            // TODO: Understand how this affects the adjacency matrix
            
            // [Ni] <-> [Nj]
            int L = action[nodeI][nodeIDir];
            int D = action[L][1];
            int R = action[nodeJ][nodeJDir];
            int U = action[R][1];

            tempPropagator[0][R][L] = true;
            tempPropagator[0][ action[R][6] ][ action[L][6] ] = true;
            tempPropagator[0][ action[L][4] ][ action[R][4] ] = true;
            tempPropagator[0][ action[L][2] ][ action[R][2] ] = true;

            tempPropagator[1][U][D] = true;
            tempPropagator[1][ action[D][6] ][ action[U][6] ] = true;
            tempPropagator[1][ action[U][4] ][ action[D][4] ] = true;
            tempPropagator[1][ action[D][2] ][ action[U][2] ] = true;
        }

        protected override bool OnBoundary(int x, int y) => !Periodic && (x < 0 || y < 0 || x >= FMX || y >= FMY);

        private int PixelIndex(int x, int y, int tileX, int tileY)
        {
            // return outputSpaceIdx * tileset.TileSize + tileX + tileY * FMX * tileset.TileSize;
            return x * tileset.TileSize + tileX + (y * tileset.TileSize + tileY) * FMX * tileset.TileSize;
        }
        
        public override Bitmap Graphics()
        {
            int imageWidth = FMX * tileset.TileSize;
            int imageHeight = FMY * tileset.TileSize;
            
            int[] bitmapData = new int[imageWidth * imageHeight];

            // If we have a fully observed solution, show it
            if (Observed != null)
            {
                for (int x = 0; x < FMX; x++)
                {
                    for (int y = 0; y < FMY; y++)
                    {
                        int outputSpaceIdx = AsOutputSpaceIndex(x, y);
                        
                        var tile = tileset.Tiles[Observed[outputSpaceIdx]];
                        for (int yt = 0; yt < tileset.TileSize; yt++)
                        {
                            for (int xt = 0; xt < tileset.TileSize; xt++)
                            {
                                Color c = tile[xt + yt * tileset.TileSize];

                                double xPercent = 1f - (1f * x) / FMX;
                                double yPercent = 1f - (1f * y) / FMY;

                                byte threshold = 2;

                                byte greyness = 32;
                                int variance = 128 - greyness;
                                double r = c.R >= threshold ? (yPercent * variance) + greyness : c.R;
                                double g = c.R >= threshold ? greyness : c.G;
                                double b = c.R >= threshold ? (xPercent * variance) + greyness : c.B;

                                int pixelIdx = PixelIndex(x, y, xt, yt);
                                bitmapData[pixelIdx] =
                                    unchecked((int)0xff000000 | ((int)r << 16) | ((int)g << 8) | (int) b);
                            }
                        }
                    }
                }
            }
            else
            {
                // TODO: Make dimensionally agnostic
                for (int x = 0; x < FMX; x++)
                {
                    for (int y = 0; y < FMY; y++)
                    {
                        int outputSpaceIdx = AsOutputSpaceIndex(x, y);
                        
                        bool[] outputSpaceWave = Wave[outputSpaceIdx];
                        int amount = (from b in outputSpaceWave where b select 1).Sum();
                        double lambda = 1.0 / (from nodeI in Enumerable.Range(0, NumberOfNodes) where outputSpaceWave[nodeI] select Weights[nodeI]).Sum();

                        for (int yt = 0; yt < tileset.TileSize; yt++)
                        {
                            for (int xt = 0; xt < tileset.TileSize; xt++)
                            {
                                int tileIdx = xt + yt * tileset.TileSize;
                                int pixelIdx = PixelIndex(x, y, xt, yt);
                                
                                // If wave not resolved and config allows for black, set pixels to black
                                if (config.black && amount == NumberOfNodes) 
                                {
                                    bitmapData[pixelIdx] = unchecked((int)0xff000000);
                                }
                                else
                                {
                                    // Blend all possible values together
                                    double r = 0, g = 0, b = 0;
                                    for (int nodeI = 0; nodeI < NumberOfNodes; nodeI++)
                                    {
                                        if (outputSpaceWave[nodeI])
                                        {
                                            var c = tileset.Tiles[nodeI][tileIdx];
                                            r += c.R * Weights[nodeI] * lambda;
                                            g += c.G * Weights[nodeI] * lambda;
                                            b += c.B * Weights[nodeI] * lambda;
                                        }
                                    }

                                    bitmapData[pixelIdx] = unchecked((int)0xff000000 | ((int)r << 16) | ((int)g << 8) | (int)b);
                                }
                            }
                        }
                    }
                }
            }

            return BitmapFactory.FromColorData(imageWidth, imageHeight, bitmapData);
        }

        public string TextOutput()
        {
            var result = new System.Text.StringBuilder();

            for (int y = 0; y < FMY; y++)
            {
                for (int x = 0; x < FMX; x++)
                {
                    int outputSpaceIdx = AsOutputSpaceIndex(x, y);
                    
                    result.Append($"{tileset.TileNames[Observed[outputSpaceIdx]]}, ");
                }
                result.Append(Environment.NewLine);
            }

            return result.ToString();
        }
    }
}
