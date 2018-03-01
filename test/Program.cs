using System;
using System.Runtime.Serialization;
using ServiceStack;
using Funq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Runtime.Serialization.Json;
using System.Net;
using System.Threading;
using ICSharpCode.SharpZipLib.Zip;
using System.ServiceProcess;
using System.ServiceModel;



namespace NetBaltic.ServerProxy
{
    [Route("/Receive", "POST")]

    public class Receive : IReturn<ReceiveResponse>
    {
        [ApiMember(IsRequired = true, Description = "Object describing a receive message")]
        public OutputMessageTransportModule OutputMessage { get; set; }
    }

    public class ReceiveResponse
    {
        [ApiMember(IsRequired = true, Description = "The result of the operation")]
        [ApiAllowableValues("Result", "0", "-1")]
        public int Result { get; set; }
    }

    public class Send : IReturn<SendResponse>
    {
        public InputMessageTransportModule InputMessage { get; set; }
    }

    public class SendResponse
    {
        public int Result { get; set; }
    }

    public class RegisterWebService : IReturn<RegisterWebServiceResponse>
    {
        public string Address { get; set; }
        public WebServiceDataFormat WebServiceDataFormat { get; set; }
    }

    public class RegisterWebServiceResponse
    {
        public int Result { get; set; }
    }

    public class ServerProxyAPI : ServiceStack.Service
    {

        //TO JEST OBIEKT, KTORY ODPOWIADA ZA KOMUNIKACJE Z MOIM MODULEM
        public static IServiceClient tranportModuleClient;

        //To jest metoda, która odbiera wiadomości od modułu transportowego
        public object Any(Receive request)
        {

            //Utworzenie nowego wątku w puli wątków
            WaitCallback callback = new WaitCallback(ServerProxy.Process);
            ThreadPool.QueueUserWorkItem(callback, new object[] { request });

            return new ReceiveResponse { Result = 0 }; ;
        }
    }

    //Konfiguracja Twojego webservice
    public class ServerProxyWebService : AppSelfHostBase
    {

        public ServerProxyWebService() : base("Server Proxy - WebService Interface", typeof(ServerProxyAPI).Assembly) { }

        public override void Configure(Container container)
        {
            Feature disableFeatures = Feature.Xml | Feature.Soap;
            SetConfig(new HostConfig
            {
                EnableFeatures = Feature.All.Remove(disableFeatures), //all formats except of XML and SOAP
                DebugMode = false, //Show StackTraces in service responses during development
                WriteErrorsToResponse = true, //Disable exception handling
                AllowJsonpRequests = true //Enable JSONP requests
            });

        }

        protected override void Dispose(bool disposing)
        {
            // Needed so that when the derived class tests run the same users can be added again.
            base.Dispose(disposing);
        }
    }

    public class Program {

		#if DAEMON
		public static void Main(string[] args) {

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] { new Service() };
            ServiceBase.Run(ServicesToRun);

           
        }
		#endif

	}
}
