using System.Drawing;
using System.Drawing.Imaging;

namespace WaveFunctionCollapse.Factories
{
    public static class BitmapFactory
    {
        public static Bitmap FromColorData(int imageWidth, int imageHeight, int[] bitmapData)
        {
            Bitmap result = new Bitmap(imageWidth, imageHeight);
            var bits = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            System.Runtime.InteropServices.Marshal.Copy(bitmapData, 0, bits.Scan0, bitmapData.Length);
            result.UnlockBits(bits);

            return result;
        }
    }
}