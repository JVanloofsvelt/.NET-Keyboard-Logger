using System;
using System.Threading.Tasks;
using RestSharp;

namespace Logger.Web
{
    partial class Webservice
    {
        public string Register()
        {
            var result = Execute<RegistrationResult>(new RegistrationRequest());
            this.authenticationToken = result.AuthenticationToken;
            return this.authenticationToken;
        }

        public async Task<string> RegisterAsync()
        {
            var result = await ExecuteAsync<RegistrationResult>(new RegistrationRequest());
            return result.AuthenticationToken;
        }
    }
    
    class RegistrationRequest : BaseRequest
    {
        public RegistrationRequest() : base("loggers/", Method.POST) { }
    }

    class RegistrationResult
    {
        [RestSharp.Deserializers.DeserializeAs(Name="auth_token")]
        public string AuthenticationToken { get; set; }
    }
}
