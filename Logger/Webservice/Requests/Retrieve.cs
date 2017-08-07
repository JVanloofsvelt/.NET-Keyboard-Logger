using System.Collections.Generic;
using System.Linq;
using RestSharp;

namespace Logger.Webservice
{
    abstract class RetrieveRequest : Request
    {
        public RetrieveRequest(string resource) : base(resource, Method.GET)
        {
        }
    }
}
