using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;

namespace SteganoAES
{
    public class StegoService
    {
        private const int BitsPerChannel = 1;
        private const int ChannelsPerPixel = 3; // R, G, B

        public Bitmap Embed(Bitmap image, byte[] blob)
        {
            long requiredBits = blob.Length * 8L;
            long capacity = image.Width * image.Height * ChannelsPerPixel * BitsPerChannel;

            if (requiredBits > capacity)
            {
                throw new ArgumentException("The image does not have enough capacity to hold the data.");
            }

            Bitmap newImage = new Bitmap(image);
            BitArray bitBlob = new BitArray(blob);
            int bitIndex = 0;

            for (int y = 0; y < newImage.Height && bitIndex < bitBlob.Length; y++)
            {
                for (int x = 0; x < newImage.Width && bitIndex < bitBlob.Length; x++)
                {
                    Color pixel = newImage.GetPixel(x, y);
                    int r = pixel.R;
                    int g = pixel.G;
                    int b = pixel.B;

                    if (bitIndex < bitBlob.Length)
                    {
                        r = SetLsb(r, bitBlob[bitIndex++]);
                    }
                    if (bitIndex < bitBlob.Length)
                    {
                        g = SetLsb(g, bitBlob[bitIndex++]);
                    }
                    if (bitIndex < bitBlob.Length)
                    {
                        b = SetLsb(b, bitBlob[bitIndex++]);
                    }

                    newImage.SetPixel(x, y, Color.FromArgb(pixel.A, r, g, b));
                }
            }

            return newImage;
        }

        public byte[] Extract(Bitmap image, int blobSizeInBytes)
        {
            int totalBitsToExtract = blobSizeInBytes * 8;
            BitArray extractedBits = new BitArray(totalBitsToExtract);
            int bitIndex = 0;

            for (int y = 0; y < image.Height && bitIndex < totalBitsToExtract; y++)
            {
                for (int x = 0; x < image.Width && bitIndex < totalBitsToExtract; x++)
                {
                    Color pixel = image.GetPixel(x, y);

                    if (bitIndex < totalBitsToExtract)
                    {
                        extractedBits[bitIndex++] = GetLsb(pixel.R);
                    }
                    if (bitIndex < totalBitsToExtract)
                    {
                        extractedBits[bitIndex++] = GetLsb(pixel.G);
                    }
                    if (bitIndex < totalBitsToExtract)
                    {
                        extractedBits[bitIndex++] = GetLsb(pixel.B);
                    }
                }
            }

            byte[] extractedBytes = new byte[blobSizeInBytes];
            extractedBits.CopyTo(extractedBytes, 0);
            return extractedBytes;
        }

        private int SetLsb(int value, bool bit)
        {
            return bit ? (value | 1) : (value & ~1);
        }

        private bool GetLsb(int value)
        {
            return (value & 1) == 1;
        }
    }
}
