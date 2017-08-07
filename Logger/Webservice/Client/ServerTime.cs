using System;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Deserializers;

namespace Logger.Webservice
{
    partial class Client
    {
        public DateTime? GetServerTime()
        {
            var result = Execute<ServerTimeResult>(new ServerTimeRequest());
            return result?.DateTime ?? null;
        }

        public async Task<DateTime> GetServerTimeAsync(CancellationToken token)
        {
            var result = await ExecuteAsync<ServerTimeResult>(new ServerTimeRequest(), token).ConfigureAwait(false);
            return result.DateTime;
        }
    }

    class ServerTimeRequest : RetrieveRequest
    {
        public ServerTimeRequest() : base("time/") { }
    }

    class ServerTimeResult
    {
        [DeserializeAs(Name = "datetime")]
        public DateTime DateTime{ get; set; }
    }
}
