using System.Collections.Generic;
using System.Linq;
using RestSharp;

namespace Logger.Webservice
{
    abstract class SubmitRequest<TObject> : RequestWithContent<TObject>
    {
        public SubmitRequest(string resource, TObject obj) : base(resource, Method.POST, obj)
        {
        }

        public SubmitRequest(string resource, IEnumerable<TObject> objects) : base(resource, Method.POST, objects)
        {
        }
    }
}
