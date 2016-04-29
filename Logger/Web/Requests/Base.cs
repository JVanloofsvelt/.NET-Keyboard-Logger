using RestSharp;

namespace Logger.Web
{
    abstract class BaseRequest : RestRequest
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

        public BaseRequest() : base()
        {
            Init();
        }

        public BaseRequest(Method method) : base(method)
        {
            Init();
        }

        public BaseRequest(string resource, Method method) : base(resource, method)
        {
            Init();
        }
    }
}
