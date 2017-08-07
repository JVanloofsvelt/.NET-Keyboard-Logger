using System;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Deserializers;

namespace Logger.Webservice
{
    
    partial class Client
    {
        public string CreateUserAccount(string username)
        {
            var result = Execute<UserAccountResult>(new CreateUserAccountRequest(username));
            return result?.AccountID ?? null;
        }

        public async Task<string> CreateUserAccountAsync(string username, CancellationToken token)
        {
            var result = await ExecuteAsync<UserAccountResult>(new CreateUserAccountRequest(username), token).ConfigureAwait(false);
            return result?.AccountID ?? null;
        }
    }

    class CreateUserAccountRequest : SubmitRequest<string>
    {
        public CreateUserAccountRequest(string username) : base("useraccount/", username) { }

        protected override object ToJSONShape(string obj)
        {
            return new
            {
                username = obj
            };
        }
    }

    class UserAccountResult
    {
        [DeserializeAs(Name = "account_id")]
        public string AccountID { get; set; }
    }
}
