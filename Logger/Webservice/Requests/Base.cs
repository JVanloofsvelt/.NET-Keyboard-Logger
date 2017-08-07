using System.Linq;
using System.Collections.Generic;
using RestSharp;

namespace Logger.Webservice
{
    abstract class Request : RestRequest
    {
        new public DataFormat RequestFormat
        {
            get { return base.RequestFormat; }
        }

        void Init()
        {
            base.RequestFormat = DataFormat.Json;
            this.JsonSerializer = new ObfuscatedJsonSerializer();
        }

        public Request() : base()
        {
            Init();
        }

        public Request(Method method) : base(method)
        {
            Init();
        }

        public Request(string resource, Method method) : base(resource, method)
        {
            Init();
        }
    }

    abstract class RequestWithContent<TObject> : Request
    {
        public RequestWithContent(string resource, Method method, TObject obj) : base(resource, method)
        {
            this.AddJsonBody(this.ToJSONShape(obj));
        }

        public RequestWithContent(string resource, Method method, IEnumerable<TObject> objects) : base(resource, method)
        {
            this.AddJsonBody(objects.Select(obj => this.ToJSONShape(obj)));
        }

        protected abstract object ToJSONShape(TObject obj);
    }
}
