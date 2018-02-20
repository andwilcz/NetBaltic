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
    class Program {

		public static void Main(string[] args) {

            Console.WriteLine("Server Proxy...");
            //ServerProxyAPI
            //Start webservice'su, z którym będę mógł się komunikować jako moduł transportowy
            var webService = new ServerProxyWebService();
            webService.Init();
            try
            {
                webService.Start("http://188.166.10.162:8090/"); // <-- pod tym adresem jest twój webservice, otwórz ten link w przeglądarce, wyswietli się strona metadata, na której są opisane metody twojego webservice'su. Szablon tej stronki jest w katalogu Template, w lokalizacji pliku exe, jak zobaczysz jest już przygotowany
                Console.WriteLine("Connecting...");
            }
            catch (Exception e)
            {
                Console.WriteLine("Starting error");
                Console.WriteLine(e);
            }
            // Utworzenie klienta webservice'su, żeby podłączyć się do mojego modułu transportowego, tzn. do webservice'su mojego modułu
            //którego oczywiście pod tym adresem nie ma, bo to jest adres localhost, który mi slużył do testów, dlatego wszystko co jest na dole jest zakomentowane, bo się i tak nie odpali na razie
            ServerProxyAPI.tranportModuleClient = new JsonServiceClient("http://lec.ddns.name:64102/");


            //Uwierzytelnianie / logowanie do modułu
            try
            {
                var authResponse = ServerProxyAPI.tranportModuleClient.Post(new Authenticate
                {
                    provider = "credentials", //= credentials
                    UserName = "serverproxy",
                    Password = "p@ssw0rd",
                    RememberMe = true
                });
            }
            catch
            {
                Console.WriteLine("Authentication Error");
            }

            //Tutaj jest rejestrowanie Twojego websevice'su, tak żebym mógł sie z tobą komunikować i wiedzieć jak coś do Ciebie wysłać, w polu Address jest adres Twojego webservice'su pod którym go uruchomiłeś
            //To drugie pole jest wskazaniem w jakim formacie będziemy się komunikować, ustawione na JSON
            RegisterWebServiceResponse response = ServerProxyAPI.tranportModuleClient.Send(new RegisterWebService { Address = "http://188.166.10.162:8090/", WebServiceDataFormat = WebServiceDataFormat.JSON });
            if (response.Result == 0)
            {
                Console.WriteLine("Rejestracja webservice'su przebiegla pomyslnie...");
            }

            //Postgresql dataBase = new Postgresql(/*host, user, password, database*/);

            //dataBase.Open();
            //dataBase.Sql("CREATE DATABASE ProxyDB");
            //dataBase.Sql("CREATE TABLE Requests (UserID int, Address vrchar, URL varchar, IsCyclic bit)");

            //Wylogowanie z mojego webservice'su
            //tranportModuleClient.Post(new Authenticate { provider = "logout" });

            Console.ReadKey();



        }
    }
}
