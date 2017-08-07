using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RestSharp;
using RestSharp.Deserializers;

namespace Logger.Webservice
{
    class ObfuscatedJsonDeserializer : RestSharp.Deserializers.IDeserializer
    {
        private JsonDeserializer JsonDeserializer;

        public ObfuscatedJsonDeserializer()
        {
            JsonDeserializer = new JsonDeserializer();
        }

        public T Deserialize<T>(IRestResponse response)
        {
            response.Content = Obfuscation.Decode(response.Content);
            return JsonDeserializer.Deserialize<T>(response);
        }
        

        /// <summary>
        /// Unused for JSON Deserialization
        /// </summary>
        public string DateFormat { get; set; }

        /// <summary>
        /// Unused for JSON Deserialization
        /// </summary>
        public string RootElement { get; set; }

        /// <summary>
        /// Unused for JSON Deserialization
        /// </summary>
        public string Namespace { get; set; }
    }
}
