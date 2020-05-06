using System.Drawing;

namespace SiebwaldeApp
{
    class ScaleBitmap
    {        
        const float WIDTH = 1000;

        public static Bitmap Scale(Bitmap bmp, float HEIGHT)
        {
            float scale = HEIGHT / bmp.Height;
            var scaleWidth = (int)(bmp.Width * scale);
            var scaleHeight = (int)(bmp.Height * scale);

            Bitmap target = new Bitmap((int)WIDTH, (int)HEIGHT);
            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(bmp,
                  new RectangleF(((int)WIDTH - scaleWidth) / 2, ((int)HEIGHT - scaleHeight) / 2, scaleWidth, scaleHeight));
            }

            return target;
        }
    }
}
