using System.Linq;

namespace WaveFunctionCollapse.Extensions
{
    public static class ArrayExt
    {
        // Normalize all values of the given array
        public static void Normalize(this double[] a)
        {
            double sum = a.Sum();
            for (int j = 0; j < a.Length; j++)
            {
                a[j] /= sum;
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="a">The given array</param>
        /// <param name="r">A random floating-point value between 0 and 1</param>
        /// <returns>Index of the selected element in the given array</returns>
        public static int Random(this double[] a, double r)
        {
            int i = 0;
            double x = 0;

            while (i < a.Length)
            {
                x += a[i];
                if (r <= x)
                {
                    return i;
                }

                i++;
            }

            return 0;
        }
    }
}