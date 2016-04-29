using System;
using System.Threading.Tasks;
using RestSharp;

namespace Logger.Web
{
    partial class Webservice
    {
        private static readonly Uri BaseUrl = new Uri("http://localhost:8000");
        private RestClient client = new RestClient(BaseUrl);
        private string authenticationToken = null;
        
        public Webservice(string authenticationToken=null)
        {
            this.authenticationToken = authenticationToken;
        }

        private void Execute(RestRequest request)
        {
            request.AddQueryParameter("auth_token", this.authenticationToken);
            var response = this.client.Execute(request);

            if (response.ErrorException != null)
                throw response.ErrorException;
        }

        private T Execute<T>(RestRequest request) where T : new()
        {
            request.AddQueryParameter("auth_token", this.authenticationToken);
            var response = this.client.Execute<T>(request);

            if (response.ErrorException != null)
                throw response.ErrorException;

            return response.Data;
        }

        private async Task ExecuteAsync(RestRequest request)
        {
            request.AddQueryParameter("auth_token", this.authenticationToken);
            var response = await this.client.ExecuteTaskAsync(request);
            
            if (response.ErrorException != null)
                throw response.ErrorException;
        }

        private async Task<T> ExecuteAsync<T>(RestRequest request) where T: new()
        {
            request.AddQueryParameter("auth_token", this.authenticationToken);
            var response = await this.client.ExecuteTaskAsync<T>(request);
            
            if (response.ErrorException != null)
                throw response.ErrorException;

            return response.Data;
        }
    }
}
