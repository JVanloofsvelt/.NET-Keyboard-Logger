using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Logger
{
    static class Serialization
    {
        public static byte[] Serialize(object graph)
        {
            byte[] serialized;

            using (var ms = new MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, graph);
                serialized = ms.ToArray();
            }

            return Obfuscation.Encode(serialized);
        }

        public static object Deserialize(byte[] bytes)
        {
            var decoded = Obfuscation.Decode(bytes);

            using (var ms = new MemoryStream(decoded))
            {
                return new BinaryFormatter().Deserialize(ms);
            }
        }
    }
}
