using System.ServiceProcess;

namespace LoggerService
{
    public partial class Service : ServiceBase
    {
        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Logger.Logger.Instance.Start();
            base.OnStart(args);
        }

        protected override void OnStop()
        {
            Logger.Logger.Instance.Stop();
            base.OnStop();
        }

        protected override void OnShutdown()
        {
            Stop();
            base.OnShutdown();
        }
    }
}
