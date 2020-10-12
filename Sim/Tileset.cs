using System;
using System.Collections.Generic;
using System.Drawing;

namespace WaveFunctionCollapse.Sim
{
    public class Tileset
    {
        
        // Width and Height for any given tile
        public int TileSize;
        public List<Color[]> Tiles;

        #region For Debugging

        public List<string> TileNames;

        #endregion

        private Color[] TileColorMap(Func<int, int, Color> f)
        {
            var result = new Color[TileSize * TileSize];
            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    result[x + y * TileSize] = f(x, y);
                }
            }

            return result;
        }

        private Color[] Rotate(Color[] array) => TileColorMap((x, y) => array[TileSize - 1 - y + x * TileSize]);
        private Color[] Reflect(Color[] array) => TileColorMap((x, y) => array[TileSize - 1 - x + y * TileSize]);

        public void LoadTile(Bitmap bitmap, string tilename, int cardinality, int currentTileIdx)
        {
            Tiles.Add(TileColorMap((x, y) => bitmap.GetPixel(x, y)));
            TileNames.Add($"{tilename} 0");

            for (int dir = 1; dir < cardinality; dir++)
            {
                if (dir <= 3)
                {
                    Tiles.Add(Rotate(Tiles[currentTileIdx + dir - 1]));
                }
                
                if (dir >= 4)
                {
                    Tiles.Add(Reflect(Tiles[currentTileIdx + dir - 4]));
                }
                
                TileNames.Add($"{tilename} {dir}");
            }
        }
        
        public void LoadUniqueTile(Bitmap bitmap, string tilename, int dir)
        {
            Tiles.Add(TileColorMap((x, y) => bitmap.GetPixel(x, y)));
            TileNames.Add($"{tilename} {dir}");
        }
    }
}