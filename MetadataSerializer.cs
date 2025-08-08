using System;
using System.IO;
using System.Text;

namespace SteganoAES
{
    public class ParsedMetadata
    {
        public byte Version { get; set; }
        public ushort Flags { get; set; }
        public byte[]? Salt { get; set; }
        public byte[]? Iv { get; set; }
        public byte[]? Hmac { get; set; }
        public byte[]? Ciphertext { get; set; }
    }

    public class MetadataSerializer
    {
        private static readonly byte[] MagicHeader = Encoding.ASCII.GetBytes("STEG");
        private const byte CurrentVersion = 1;

        public byte[] Pack(byte[] salt, byte[] iv, byte[] hmac, byte[] ciphertext)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                // Header
                writer.Write(MagicHeader);
                writer.Write(CurrentVersion);
                writer.Write((ushort)0); // Flags

                // Metadata
                writer.Write((ushort)salt.Length);
                writer.Write(salt);

                writer.Write((ushort)iv.Length);
                writer.Write(iv);

                writer.Write(hmac.Length);
                writer.Write(hmac);

                // Ciphertext
                writer.Write(ciphertext.Length);
                writer.Write(ciphertext);

                return ms.ToArray();
            }
        }

        public ParsedMetadata Unpack(byte[] blob)
        {
            using (var ms = new MemoryStream(blob))
            using (var reader = new BinaryReader(ms))
            {
                var header = reader.ReadBytes(4);
                if (!header.AsSpan().SequenceEqual(MagicHeader))
                {
                    throw new ArgumentException("Invalid steganography header.");
                }

                var result = new ParsedMetadata
                {
                    Version = reader.ReadByte(),
                    Flags = reader.ReadUInt16()
                };

                var saltLength = reader.ReadUInt16();
                result.Salt = reader.ReadBytes(saltLength);

                var ivLength = reader.ReadUInt16();
                result.Iv = reader.ReadBytes(ivLength);

                var hmacLength = reader.ReadInt32();
                result.Hmac = reader.ReadBytes(hmacLength);

                var ciphertextLength = reader.ReadInt32();
                result.Ciphertext = reader.ReadBytes(ciphertextLength);

                return result;
            }
        }
    }
}
