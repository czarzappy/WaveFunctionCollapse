using System.Collections.Generic;

namespace WaveFunctionCollapse.Configs
{
    public class Tileset
    {
        public int Size;
        public List<Tile> Tiles;
        public List<Neighbor> Neighbors;
    }

    public class Neighbor
    {
        public string Left;
        public string Right;
    }

    public class Tile
    {
        public string Name;
        public string Symmetry;
    }
}