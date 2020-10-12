using System.Xml.Linq;
using WaveFunctionCollapse.Extensions;

namespace WaveFunctionCollapse.Xml
{
    public static class TileParseUtils
    {
        private static Lambdas.Transform Identity = i => i; // 0 => 0, 1 => 1, 2 => 2, 3 => 3
        private static Lambdas.Transform Descent = i => 1 - i; // 0 => 1, 1 => 0, 2 => -1 (3), 3 => -2 (2)
        
        private static Lambdas.Transform Ascent2D = i => (i + 1) % 4; // 0 => 1, 1 => 2, 2 => 3, 3 => 0
        
        
        // TODO: Make this dimension agnostic
        // Cardinality is the number of possible orientations
        public static void ParseSymmetry(XElement xtile, out int cardinality, out Lambdas.Transform a, out Lambdas.Transform b)
        {
            char sym = xtile.Get("symmetry", 'X');
            
            switch (sym)
            {
                case 'L': // L-shaped symmetry
                    cardinality = 4;
                    a = Ascent2D;
                    b = i => i % 2 == 0 ? i + 1 : i - 1; // 0 => 1, 1 => 0, 2 => 3, 3 => 2 
                    break;
                case 'T': // T-shaped symmetry
                    cardinality = 4;
                    a = Ascent2D;
                    b = i => i % 2 == 0 ? i : 4 - i; // 0 => 0, 1 => 3, 2 => 2, 3 => 1
                    break;
                case 'I': // Vertical symmetry
                    cardinality = 2;
                    a = Descent; 
                    b = Identity;
                    break;
                case '\\': // Diagonal symmetry
                    cardinality = 2;
                    a = Descent;
                    b = Descent;
                    break;
                case 'F': // No symmetry
                    cardinality = 8;
                    
                    // 0 => 1, 1 => 2, 2 => 3, 3 => 0, 4 => 7, 5 => 4, 6 => 5, 7 => 6
                    a = i => i < 4 ? (i + 1) % 4 : 4 + (i - 1) % 4;
                    
                    // 0 => 4, 1 => 5, 2 => 6, 3 => 7, 4 => 0, 5 => 1, 6 => 2, 7 => 3
                    b = i => i < 4 ? i + 4 : i - 4;
                    break;
                default:
                    cardinality = 1; // Fully symmetrical tile
                    a = Identity;
                    b = Identity;
                    break;
            }
        }
    }
}