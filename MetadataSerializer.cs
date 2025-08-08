using System;

namespace SteganoAES
{
    public class MetadataSerializer
    {
        private const int MAGIC = 0x47455453; // "STEG" in hex
        private const byte VERSION = 1;

        public class Metadata
        {
            public byte[] Salt { get; set; }
            public byte[] IV { get; set; }
        }

        public static byte[] Pack(byte[] salt, byte[] iv, byte[] encryptedData)
        {
            // Calculate total size
            int totalSize = 
                4 + // Magic
                1 + // Version
                2 + // Salt length
                salt.Length +
                2 + // IV length
                iv.Length +
                4 + // Data length
                encryptedData.Length;

            byte[] result = new byte[totalSize];
            int offset = 0;

            // Write magic number
            Buffer.BlockCopy(BitConverter.GetBytes(MAGIC), 0, result, offset, 4);
            offset += 4;

            // Write version
            result[offset++] = VERSION;

            // Write salt
            Buffer.BlockCopy(BitConverter.GetBytes((short)salt.Length), 0, result, offset, 2);
            offset += 2;
            Buffer.BlockCopy(salt, 0, result, offset, salt.Length);
            offset += salt.Length;

            // Write IV
            Buffer.BlockCopy(BitConverter.GetBytes((short)iv.Length), 0, result, offset, 2);
            offset += 2;
            Buffer.BlockCopy(iv, 0, result, offset, iv.Length);
            offset += iv.Length;

            // Write encrypted data
            Buffer.BlockCopy(BitConverter.GetBytes(encryptedData.Length), 0, result, offset, 4);
            offset += 4;
            Buffer.BlockCopy(encryptedData, 0, result, offset, encryptedData.Length);

            return result;
        }

        public static Metadata Unpack(byte[] blob, out byte[] encryptedData)
        {
            int offset = 0;

            // Verify magic number
            int magic = BitConverter.ToInt32(blob, offset);
            if (magic != MAGIC)
                throw new ArgumentException("Invalid metadata format");
            offset += 4;

            // Verify version
            byte version = blob[offset++];
            if (version != VERSION)
                throw new ArgumentException("Unsupported version");

            // Read salt
            int saltLength = BitConverter.ToInt16(blob, offset);
            offset += 2;
            byte[] salt = new byte[saltLength];
            Buffer.BlockCopy(blob, offset, salt, 0, saltLength);
            offset += saltLength;

            // Read IV
            int ivLength = BitConverter.ToInt16(blob, offset);
            offset += 2;
            byte[] iv = new byte[ivLength];
            Buffer.BlockCopy(blob, offset, iv, 0, ivLength);
            offset += ivLength;

            // Read encrypted data
            int dataLength = BitConverter.ToInt32(blob, offset);
            offset += 4;
            encryptedData = new byte[dataLength];
            Buffer.BlockCopy(blob, offset, encryptedData, 0, dataLength);

            return new Metadata { Salt = salt, IV = iv };
        }
    }
}
