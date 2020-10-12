/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using WaveFunctionCollapse.Extensions;

namespace WaveFunctionCollapse.Sim
{
    public abstract class Model
    {
        #region 2D-dependent variables

        // TODO: Make this dimensionally independent
        protected static int[] DX = { -1, 0, 1, 0 }; // X-Directional displacement lookup
        protected static int[] DY = { 0, 1, 0, -1 }; // X-Directional displacement lookup
        private static readonly int[] OppositeDirLookup = { 2, 3, 0, 1 }; // Opposite Direction Lookup

        // Number of nodes across in a given dimension
        protected int FMX, FMY;

        protected const int NumDirections = 4;

        #endregion

        #region Simulation Initialization

        protected bool Periodic;
        protected int NumberOfNodes;

        /// <summary>
        /// [Output Space Size][Number of Nodes]
        /// </summary>
        protected bool[][] Wave;
        
        // [Direction][Node Index][Sparse Index] with size [Cardinality][Tileset size][Node Adjacency Count] -> Adjacent Node Id
        protected int[][][] SparseCardinalNodeAdjacency;
        
        // Wave function collapse possibilities
        // [Output Space Size][Number of Nodes][Number of Directions]
        private int[][][] compatible;

        #endregion

        #region Simulation Runtime

        protected Random Random;
        private (int, int)[] stack;
        private int stacksize;

        #endregion

        #region Simulation Result
        
        /// <summary>
        /// [Output Space Size]
        /// </summary>
        protected int[] Observed;

        #endregion

        private double sumOfWeights, sumOfWeightLogWeights, startingEntropy;

        #region Node Space
        
        protected double[] Weights;
        private double[] weightLogWeights;
        

        #endregion

        #region Output Space fields

        private int[] sumsOfOnes;
        private double[] sumsOfWeights, sumsOfWeightLogWeights, entropies;
        
        private int OutputSpaceSize => FMX * FMY;

        #endregion


        protected Model(int width, int height)
        {
            FMX = width;
            FMY = height;
        }
        
        /// <summary>
        /// Initialize the simulation
        /// </summary>
        private void Init()
        {
            // Initialize the output space
            Wave = new bool[OutputSpaceSize][];
            compatible = new int[OutputSpaceSize][][];
            for (int outSpaceIdx = 0; outSpaceIdx < OutputSpaceSize; outSpaceIdx++)
            {
                Wave[outSpaceIdx] = new bool[NumberOfNodes];
                
                compatible[outSpaceIdx] = new int[NumberOfNodes][];
                for (int nodeI = 0; nodeI < NumberOfNodes; nodeI++)
                {
                    compatible[outSpaceIdx][nodeI] = new int[NumDirections];
                }
            }

            weightLogWeights = new double[NumberOfNodes];
            sumOfWeights = 0;
            sumOfWeightLogWeights = 0;

            for (int nodeI = 0; nodeI < NumberOfNodes; nodeI++)
            {
                // wi * log(wi)
                weightLogWeights[nodeI] = Weights[nodeI] * Math.Log(Weights[nodeI]);
                sumOfWeights += Weights[nodeI];
                sumOfWeightLogWeights += weightLogWeights[nodeI];
            }

            startingEntropy = Math.Log(sumOfWeights) - sumOfWeightLogWeights / sumOfWeights;

            sumsOfOnes = new int[OutputSpaceSize];
            sumsOfWeights = new double[OutputSpaceSize];
            sumsOfWeightLogWeights = new double[OutputSpaceSize];
            entropies = new double[OutputSpaceSize];

            // Number of iterations
            stack = new (int, int)[OutputSpaceSize * NumberOfNodes];
            stacksize = 0;
        }

        bool? Observe()
        {
            double min = 1E+3;
            int currentOutputSpaceTarget = -1;

            // For all the current output spaces
            for (int outputSpaceIdx = 0; outputSpaceIdx < OutputSpaceSize; outputSpaceIdx++)
            {
                // Skip the output spaces that are not within the output space?
                // TODO: Make this dimensionally agnostic
                if (OnBoundary(outputSpaceIdx % FMX, outputSpaceIdx / FMX))
                {
                    continue;
                }

                // Check if all possibles nodes failed to populate output space index
                // For some reason this node did not ever resolve
                int amount = sumsOfOnes[outputSpaceIdx];
                if (amount == 0)
                {
                    return false;
                }

                // Check if output space entry is within an acceptable range
                double entropy = entropies[outputSpaceIdx];
                if (amount > 1 && entropy <= min)
                {
                    // Output space could be resolved, adding noise to infer with the selection
                    double noise = 1E-6 * Random.NextDouble();
                    if (entropy + noise < min)
                    {
                        min = entropy + noise; // Lower the entropy threshold, we'll end up resolving the most certain selection 
                        currentOutputSpaceTarget = outputSpaceIdx;
                    }
                }
            }

            // Check if no node was selected to resolve
            if (currentOutputSpaceTarget == -1)
            {
                // Finalize observation and complete run
                Observed = new int[OutputSpaceSize];
                for (int outputSpaceIdx = 0; outputSpaceIdx < OutputSpaceSize; outputSpaceIdx++)
                {
                    for (int nodeI = 0; nodeI < NumberOfNodes; nodeI++)
                    {
                        if (Wave[outputSpaceIdx][nodeI])
                        {
                            Observed[outputSpaceIdx] = nodeI; 
                            break;
                        }
                    }
                }

                return true;
            }

            bool[] currentWave = Wave[currentOutputSpaceTarget];
            
            // Generate probability distribution for wave collapse
            double[] distribution = new double[NumberOfNodes];
            for (int nodeI = 0; nodeI < NumberOfNodes; nodeI++)
            {
                distribution[nodeI] = currentWave[nodeI] ? Weights[nodeI] : 0;
            }

            distribution.Normalize();
            int pruneNode = distribution.Random(Random.NextDouble());

            // TODO: Simplify?
            for (int nodeI = 0; nodeI < NumberOfNodes; nodeI++)
            {
                if (currentWave[nodeI] != (nodeI == pruneNode))
                {
                    PruneOption(currentOutputSpaceTarget, nodeI);
                }
            }

            return null;
        }

        protected void Propagate()
        {
            while (stacksize > 0)
            {
                var currentStackFrame = stack[stacksize - 1];
                stacksize--;

                int currentOutputSpaceIdx = currentStackFrame.Item1;
                int currentNode = currentStackFrame.Item2;
                
                // TODO: Make this dimensionally independent
                int x1 = currentOutputSpaceIdx % FMX, y1 = currentOutputSpaceIdx / FMX;

                // For each surrounding node
                for (int dir = 0; dir < NumDirections; dir++)
                {
                    int dx = DX[dir], dy = DY[dir];
                    int x2 = x1 + dx, y2 = y1 + dy;
                    if (OnBoundary(x2, y2))
                    {
                        continue;
                    }

                    // Wrapping point
                    if (x2 < 0)
                    {
                        x2 += FMX;
                    }
                    else if (x2 >= FMX)
                    {
                        x2 -= FMX;
                    }
                    
                    if (y2 < 0)
                    {
                        y2 += FMY;
                    }
                    else if (y2 >= FMY)
                    {
                        y2 -= FMY;
                    }

                    int neighborOutputSpaceIdx = AsOutputSpaceIndex(x2, y2);
                    var compat = compatible[neighborOutputSpaceIdx];

                    // Resolve around the neighbor node
                    var currentNodeAdjacency = SparseCardinalNodeAdjacency[dir][currentNode];
                    foreach (var nodeJ in currentNodeAdjacency)
                    {
                        var comp = compat[nodeJ];

                        comp[dir]--;
                        if (comp[dir] == 0)
                        {
                            PruneOption(neighborOutputSpaceIdx, nodeJ);
                        }
                    }
                }
            }
        }

        // TODO: Make dimensionally independent
        protected int AsOutputSpaceIndex(int x, int y)
        {
            return x + y * FMX;
        }

        public bool Run(int seed, int limit)
        {
            if (Wave == null)
            {
                Init();
            }

            Clear();
            Random = new Random(seed);

            // Generate until completion or the given number of frame simulations have been resolved
            for (int l = 0; l < limit || limit == 0; l++)
            {
                bool? result = Observe();
                if (result != null)
                {
                    return (bool)result;
                }

                Propagate();
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outputSpaceIndex">Index in output space</param>
        /// <param name="nodeI">Node to prune for the given output space index</param>
        protected void PruneOption(int outputSpaceIndex, int nodeI)
        {
            // We've determine that the given node is not a valid value for the given outspace coordinate
            Wave[outputSpaceIndex][nodeI] = false;

            // Prune node from all direction compatibility
            var compat = compatible[outputSpaceIndex][nodeI];
            for (int dir = 0; dir < NumDirections; dir++)
            {
                compat[dir] = 0;
            }

            // Push to stack
            stack[stacksize] = (outputSpaceIndex, nodeI);
            stacksize++;

            sumsOfOnes[outputSpaceIndex] -= 1; // Prune node
            sumsOfWeights[outputSpaceIndex] -= Weights[nodeI]; // Prune node weight
            sumsOfWeightLogWeights[outputSpaceIndex] -= weightLogWeights[nodeI]; // Prune log of node weight

            // Recalculate entropy for node
            double sum = sumsOfWeights[outputSpaceIndex];
            entropies[outputSpaceIndex] = Math.Log(sum) - sumsOfWeightLogWeights[outputSpaceIndex] / sum; 
        }

        /// <summary>
        /// Reset the state of the simulation
        /// </summary>
        protected virtual void Clear()
        {
            for (int outputSpaceIdx = 0; outputSpaceIdx < OutputSpaceSize; outputSpaceIdx++)
            {
                for (int nodeI = 0; nodeI < NumberOfNodes; nodeI++)
                {
                    Wave[outputSpaceIdx][nodeI] = true; // Wave functions can collapse to anything
                    
                    // All directions are possible
                    for (int dir = 0; dir < NumDirections; dir++)
                    {
                        compatible[outputSpaceIdx][nodeI][dir] = SparseCardinalNodeAdjacency[OppositeDirLookup[dir]][nodeI].Length;
                    }
                }

                sumsOfOnes[outputSpaceIdx] = Weights.Length;
                sumsOfWeights[outputSpaceIdx] = sumOfWeights;
                sumsOfWeightLogWeights[outputSpaceIdx] = sumOfWeightLogWeights;
                entropies[outputSpaceIdx] = startingEntropy;
            }
        }

        protected abstract bool OnBoundary(int x, int y);
        public abstract System.Drawing.Bitmap Graphics();
    }
}
