using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RestSharp;

namespace Logger.Webservice
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
            var json = SimpleJson.SerializeObject(obj);
            return Obfuscation.Encode(json);
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
