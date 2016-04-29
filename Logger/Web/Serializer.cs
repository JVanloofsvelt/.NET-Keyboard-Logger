using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Compression;
using System.IO;
using RestSharp;


namespace Logger.Web
{
    class ObfuscatedJsonSerializer : RestSharp.Serializers.ISerializer
    {
        public string ContentType { get; set; }

        public ObfuscatedJsonSerializer()
        {
            ContentType = "application/json";
        }

        public string Serialize(object obj)
        {   
            return Obfuscate(SimpleJson.SerializeObject(obj));
        }

        string Obfuscate(string input)
        {
            byte[] key = Guid.NewGuid().ToByteArray();
            byte[] msg = Encoding.Unicode.GetBytes(input);
            byte[] encoded = XOR(Compress(msg), Cycle(key));

            return Convert.ToBase64String(key.Concat(encoded).ToArray());
        }

        byte[] XOR(IEnumerable<byte> a, IEnumerable<byte> b)
        {
            return a.Zip(b, (x, y) => (byte)(x ^ y)).ToArray();
        }

        byte[] Compress(byte[] bytes)
        {
            using (var output = new MemoryStream())
            {
                using (var input = new DeflateStream(output, CompressionLevel.Fastest))
                {
                    input.Write(bytes, 0, bytes.Length);
                    input.Flush();
                }

                return output.ToArray();
            }
        }

        IEnumerable<byte> Cycle(IEnumerable<byte> input)
        {
            while (true)
            {
                foreach (var b in input)
                    yield return b;
            }
        }

        /// <summary>
        /// Unused for JSON Serialization
        /// </summary>
        public string DateFormat { get; set; }

        /// <summary>
        /// Unused for JSON Serialization
        /// </summary>
        public string RootElement { get; set; }

        /// <summary>
        /// Unused for JSON Serialization
        /// </summary>
        public string Namespace { get; set; }
    }
}
