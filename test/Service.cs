using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using ServiceStack;
using Funq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Threading;
using ICSharpCode.SharpZipLib.Zip;


namespace NetBaltic.ServerProxy
{
    public class Service : ServiceBase
    {
        protected override void OnStart(string[] args)
        {
            Console.WriteLine("Server Proxy...");
            ConnectionEstablish();
        }
        
        void ConnectionEstablish()
        { 
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

			// Utworzenie obiektu - dobrze
            //Postgresql dataBase = new Postgresql();
			// Otwarcie bazy - dobrze
           // dataBase.Open();

			// Źle - nie możesz utworzyć bazy, skoro już się do niej zalogowałeś, zalogowałeś się do bazy netbaltic, musisz ją utworzyć sam, poniższe polecenie jest niepoprawne
			/*Zobacz do pliku Postgresql.cs
			public Postgresql () {
			connectionString =
				"Host=localhost;" +				//Adres hosta
				"Username=netbaltic;" +			//Nazwa użytkownika - musi być utworzony
				"Password=netbaltic;" +			//Hasło użytkownika
				"Database=netbaltic;";			//Nazwa bazy - musi istnieć
			dbcon = new NpgsqlConnection(connectionString);
			}

			*/


            /*Musisz ręcznie zalogować sie do bazy wpisując najpierw 'su - postgres' a następnie 'psql' będziesz zalogowany do bazy jako użytkownik postgres,
			następnie wpisujesz 
			\password postgres	#ustawianie hasła użytkownika postgres
			CREATE USER netbaltic WITH PASSWORD '$database_pass';	#Tworzenei użytkownika netbaltic
			CREATE DATABASE netbaltic;	#Utworzenie bazy netbaltic
			GRANT ALL PRIVILEGES ON DATABASE netbaltic to netbaltic;	#Ustawienie uprawnień dla użytkownika netbaltic do bazy netbaltic
			\c netbaltic netbaltic	#Zalogowanie się do bazy netbaltic jako użytkownik netbaltic
			I tutaj wykonujesz tworzenie bazy - tabele etc.
			*/

			/*Jeżeli byłyby problemy z logowanie dla bezpieczeństwa wejdź do pliku /var/lib/pgsql/data/pg_hba.conf
			i zamień wszędzie wartości peer i ident na trust, to powinno dać możliwość logowania bez hasła nawet w przypadku podawania hasła
			*/

			/*Korzystanie z metody Sql
			Ta metoda zwraca obiekt NpgsqlDataReader
			nie można zrobić ToString() bo to Ci zwróci jakąś bzdurę
			Poprawne korzystanie, PRZYKŁADOWE:

			// Utworzenie zapytania
			var query = @"SELECT service_type_number, label FROM service_types;";

			// Wykonanie zapytania
			reader = postgresql.Sql (query);

			// CZYTANIE WYNIKU - czyta wynik linia po linii, aż do osiągnięcia końca
			while (reader.Read ()) {
				var type = (byte)(int)reader ["service_type_number"];	// Trzeba zrzutować typy
				var label = reader ["label"] as string;					// Tak jak wyżej inny sposób rzutowania
			}

			// Trzeba zamknąć obiekt reader, szczególnie jeżeli chce się wykonać potem inne zapytanie w ramach tej samej sesji połączenia z bazą
			reader.Close ();
			albo zamykać za każdym razem polączenie za bazą postgresql.Close() i potem otwierać ponownie
			*/

			// Źle - tabele powinny być już utworzone w bazie, w tym momencie, przy każdym uruchomieniu tworzysz tabelę, która już po 2 uruchomieniu programu istnieje


			//Brakuje zamknięcia połączenia z baząe
			//dataBase.Close();

            //Wylogowanie z mojego webservice'su
            //tranportModuleClient.Post(new Authenticate { provider = "logout" });

            Console.ReadKey();

        }

        protected override void OnStop()
        {
            //Wylogowanie z mojego webservice'su
            //tranportModuleClient.Post(new Authenticate { provider = "logout" });
        }

		#if NODAEMON
		// This method is for debugging of OnStart() method only.
		// Switch to Debug config, set a breakpoint here and a breakpoint in OnStart()
		// How to: Debug the OnStart Method http://msdn.microsoft.com/en-us/library/cktt23yw.aspx
		// How to: Debug Windows Service Applications http://msdn.microsoft.com/en-us/library/7a50syb3%28v=vs.110%29.aspx
		public static void Main (String [] args) {
			
			(new Service ()).OnStart (new string [1]);
			(new Service ()).OnStop ();
		}
		#endif
    }
}
