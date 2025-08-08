using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SteganoAES
{
    public static class StegoService
    {
        public static Bitmap EmbedData(Bitmap image, byte[] data)
        {
            // Kiểm tra xem ảnh có đủ dung lượng không
            if (data.Length * 8 > image.Width * image.Height * 3)
            {
                throw new ArgumentException("Ảnh không đủ dung lượng để chứa dữ liệu");
            }

            // Tạo một bản sao của ảnh để không thay đổi ảnh gốc
            Bitmap stegoBitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(stegoBitmap))
            {
                g.DrawImage(image, 0, 0);
            }

            // Lock vùng bitmap để truy cập trực tiếp
            BitmapData stegoData = stegoBitmap.LockBits(
                new Rectangle(0, 0, stegoBitmap.Width, stegoBitmap.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format24bppRgb);

            int stride = stegoData.Stride;
            IntPtr scan0 = stegoData.Scan0;

            unsafe
            {
                byte* ptr = (byte*)scan0;
                int bitIndex = 0;

                // Nhúng độ dài của dữ liệu trước (4 bytes = 32 bits)
                int length = data.Length;
                for (int i = 0; i < 32; i++)
                {
                    bool bit = (length & (1 << (31 - i))) != 0;
                    ptr[bitIndex / 3] = (byte)((ptr[bitIndex / 3] & 0xFE) | (bit ? 1 : 0));
                    bitIndex += 3;
                }

                // Nhúng dữ liệu
                for (int byteIndex = 0; byteIndex < data.Length; byteIndex++)
                {
                    byte b = data[byteIndex];
                    for (int i = 0; i < 8; i++)
                    {
                        bool bit = (b & (1 << (7 - i))) != 0;
                        ptr[bitIndex / 3] = (byte)((ptr[bitIndex / 3] & 0xFE) | (bit ? 1 : 0));
                        bitIndex += 3;
                    }
                }
            }

            stegoBitmap.UnlockBits(stegoData);
            return stegoBitmap;
        }

        public static byte[] ExtractData(Bitmap image)
        {
            BitmapData imageData = image.LockBits(
                new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

            int stride = imageData.Stride;
            IntPtr scan0 = imageData.Scan0;
            byte[] extractedData;

            unsafe
            {
                byte* ptr = (byte*)scan0;
                int bitIndex = 0;

                // Đọc độ dài dữ liệu (32 bits đầu tiên)
                int length = 0;
                for (int i = 0; i < 32; i++)
                {
                    bool bit = (ptr[bitIndex / 3] & 1) == 1;
                    if (bit) length |= (1 << (31 - i));
                    bitIndex += 3;
                }

                if (length <= 0 || length > (image.Width * image.Height * 3 - 32) / 8)
                {
                    throw new ArgumentException("Dữ liệu không hợp lệ hoặc ảnh bị hỏng");
                }

                extractedData = new byte[length];

                // Đọc dữ liệu
                for (int byteIndex = 0; byteIndex < length; byteIndex++)
                {
                    byte b = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        bool bit = (ptr[bitIndex / 3] & 1) == 1;
                        if (bit) b |= (byte)(1 << (7 - i));
                        bitIndex += 3;
                    }
                    extractedData[byteIndex] = b;
                }
            }

            image.UnlockBits(imageData);
            return extractedData;
        }
    }
}
