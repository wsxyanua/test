using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SteganoAES
{
    public class CryptoService
    {
        private const int KeySize = 256;
        private const int BlockSize = 128;
        private const int SaltSize = 16;
        private const int IvSize = 16;
        private const int HmacSize = 32;
        private const int Pbkdf2Iterations = 100000;

        public byte[] Encrypt(byte[] plainText, string password, out byte[] salt, out byte[] iv, out byte[] hmac)
        {
            salt = RandomNumberGenerator.GetBytes(SaltSize);
            iv = RandomNumberGenerator.GetBytes(IvSize);

            using (var aes = Aes.Create())
            {
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;

                var key = DeriveKey(password, salt);
                aes.Key = key;
                aes.IV = iv;

                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(plainText, 0, plainText.Length);
                        cryptoStream.FlushFinalBlock();
                    }
                    byte[] cipherText = memoryStream.ToArray();
                    hmac = ComputeHmac(cipherText, key);
                    return cipherText;
                }
            }
        }

        public byte[] Decrypt(byte[] cipherText, string password, byte[] salt, byte[] iv, byte[] hmac)
        {
            var key = DeriveKey(password, salt);

            var computedHmac = ComputeHmac(cipherText, key);
            if (!CryptographicOperations.FixedTimeEquals(hmac, computedHmac))
            {
                throw new CryptographicException("HMAC validation failed. The data may have been tampered with.");
            }

            using (var aes = Aes.Create())
            {
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;
                aes.Key = key;
                aes.IV = iv;

                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(cipherText, 0, cipherText.Length);
                        cryptoStream.FlushFinalBlock();
                    }
                    return memoryStream.ToArray();
                }
            }
        }

        public byte[] DeriveKey(string password, byte[] salt)
        {
            return new Rfc2898DeriveBytes(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256).GetBytes(KeySize / 8);
        }

        private byte[] ComputeHmac(byte[] data, byte[] key)
        {
            using (var hmac = new HMACSHA256(key))
            {
                return hmac.ComputeHash(data);
            }
        }
    }
}
