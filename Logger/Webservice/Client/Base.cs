using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using RestSharp;

namespace Logger.Webservice
{
    partial class Client
    {
        private static readonly Uri BaseUrl = new Uri("http://localhost:8000");
        private readonly RestClient RestClient = new RestClient(BaseUrl);
        private string authenticationToken = null;
        
        public bool IsRegistered
        {
            get { return authenticationToken != null; }
        }

        public string AuthenticationToken
        {
            get { return authenticationToken; }
        }

        public Client(string authenticationToken=null)
        {
            this.authenticationToken = authenticationToken;
            RestClient.AddHandler("application/json", new ObfuscatedJsonDeserializer());
        }

        private void Execute(RestRequest request)
        {
            if (this.authenticationToken != null)
                request.AddQueryParameter("auth_token", this.authenticationToken);

            var response = this.RestClient.Execute(request);

            HandleAnyResponseError(response);
        }

        private T Execute<T>(RestRequest request) 
            where T : class, new()
        {
            request.AddQueryParameter("auth_token", this.authenticationToken);
            var response = this.RestClient.Execute<T>(request);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            HandleAnyResponseError(response);
            return response.Data;
        }

        private async Task ExecuteAsync(RestRequest request, CancellationToken token)
        {
            request.AddQueryParameter("auth_token", this.authenticationToken);
            var response = await this.RestClient.ExecuteTaskAsync(request, token).ConfigureAwait(false);

            HandleAnyResponseError(response);
        }

        private async Task<T> ExecuteAsync<T>(RestRequest request, CancellationToken token)
            where T: class, new()
        {
            request.AddQueryParameter("auth_token", this.authenticationToken);
            var response = await this.RestClient.ExecuteTaskAsync<T>(request, token).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            HandleAnyResponseError(response);
            return response.Data;
        }

        private void HandleAnyResponseError(IRestResponse response)
        {
            if (response.ErrorException != null)
                throw response.ErrorException;
        }
    }
}
