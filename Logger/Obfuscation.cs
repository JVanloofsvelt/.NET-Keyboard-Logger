using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;

namespace Logger
{
    static class Obfuscation
    {
        static readonly int KEY_SIZE = 16;

        public static string Encode(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(Encode(bytes));
        }

        public static byte[] Encode(byte[] input)
        {
            byte[] key = Guid.NewGuid().ToByteArray();
            var compressed = Compress(input);
            byte[] encoded = XOR(compressed, key.Cycle());
            return key.Concat(encoded).ToArray();
        }

        public static string Decode(string input)
        {
            var bytes = Convert.FromBase64String(input);
            return Encoding.UTF8.GetString(Decode(bytes));
        }

        public static byte[] Decode(byte[] input)
        {
            var key = input.Take(KEY_SIZE);
            var encoded = input.Skip(KEY_SIZE);
            var decoded = XOR(encoded, key.Cycle());
            return Decompress(decoded);
        }

        private static byte[] XOR(IEnumerable<byte> a, IEnumerable<byte> b)
        {
            return a.Zip(b, (x, y) => (byte)(x ^ y)).ToArray();
        }

        private static byte[] Compress(byte[] bytes)
        {
            using (var output = new MemoryStream())
            {
                using (var compressor = new DeflateStream(output, CompressionLevel.Fastest))
                {
                    compressor.Write(bytes, 0, bytes.Length);
                    compressor.Flush();
                }

                return output.ToArray();
            }
        }

        private static byte[] Decompress(byte[] bytes)
        {
            using (var input = new MemoryStream(bytes))
            using (var output = new MemoryStream())
            {
                using (var decompressor = new DeflateStream(input, CompressionMode.Decompress))
                {
                    decompressor.CopyTo(output);
                }

                return output.ToArray();
            }
        }
    }
}
