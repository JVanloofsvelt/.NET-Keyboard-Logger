using System;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Deserializers;

namespace Logger.Webservice
{
    partial class Client
    {
        public string Register(string registrationToken, RegistrationInfo info)
        {
            var result = Execute<RegistrationResult>(new RegistrationRequest(registrationToken, info));
            return HandleRegistrationResult(result);
        }

        public async Task<string> RegisterAsync(string registrationToken, RegistrationInfo info, CancellationToken token)
        {
            var result = await ExecuteAsync<RegistrationResult>(new RegistrationRequest(registrationToken, info), token).ConfigureAwait(false);
            return HandleRegistrationResult(result);
        }

        string HandleRegistrationResult(RegistrationResult result)
        {
            this.authenticationToken = result.AuthenticationToken;
            return result?.AuthenticationToken ?? null;
        }
    }
    
    struct RegistrationInfo
    {
        public string Hostname { get; set; }
        public string FileVersion { get; set; }
    }

    class RegistrationRequest : SubmitRequest<RegistrationInfo>
    {
        public RegistrationRequest(string registrationToken, RegistrationInfo info) : base("logger/", info)
        {
            this.AddQueryParameter("registration_token", registrationToken);
        }

        protected override object ToJSONShape(RegistrationInfo obj)
        {
            return new
            {
                hostname = obj.Hostname,
                file_version = obj.FileVersion
            };
        }
    }

    class RegistrationResult
    {
        [DeserializeAs(Name="auth_token")]
        public string AuthenticationToken { get; set; }
    }
}
