using System.Collections.Generic;

namespace WaveFunctionCollapse.Factories
{
    public static class MatrixFactory
    {
        public static int[][][] BuildCardinalityPropagator(int numDirections, int size)
        {
            var result = new int[numDirections][][];
            for (int dir = 0; dir < numDirections; dir++)
            {
                result[dir] = new int[size][];
            }

            return result;
        }
        

        public static List<int>[][] BuildSparseCardinalityPropagator(int numDirections, int size)
        {
            var sparsePropagator = new List<int>[numDirections][];
            for (int dir = 0; dir < numDirections; dir++)
            {
                sparsePropagator[dir] = new List<int>[size];
                for (int idx = 0; idx < size; idx++)
                {
                    sparsePropagator[dir][idx] = new List<int>();
                }
            }

            return sparsePropagator;
        }

        public static bool[][][] BuildTempCardinalityPropagator(int numDirections, int size)
        {
            bool[][][] result = new bool[numDirections][][];
            for (int dir = 0; dir < numDirections; dir++)
            {
                result[dir] = new bool[size][];
                for (int idx = 0; idx < size; idx++)
                {
                    result[dir][idx] = new bool[size];
                }
            }

            return result;
        }

    }
}