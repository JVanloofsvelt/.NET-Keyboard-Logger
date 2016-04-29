using System.Collections.Generic;
using System.Linq;
using RestSharp;

namespace Logger.Web
{
    abstract class SubmitRequest<T> : BaseRequest
    {
        public SubmitRequest(T obj) : base(Method.POST)
        {
            this.AddJsonBody(this.ToJSONShape(obj));
        }

        public SubmitRequest(string resource, T obj) : base(resource, Method.POST)
        {
            this.AddJsonBody(this.ToJSONShape(obj));
        }

        public SubmitRequest(IEnumerable<T> objects) : base(Method.POST)
        {
            this.AddJsonBody(objects.Select(obj => this.ToJSONShape(obj)));
        }

        public SubmitRequest(string resource, IEnumerable<T> objects) : base(resource, Method.POST)
        {
            this.AddJsonBody(objects.Select(obj => this.ToJSONShape(obj)));
        }

        protected abstract object ToJSONShape(T obj);
    }
}
