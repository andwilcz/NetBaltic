using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Runtime.Serialization.Json;
using System.Net;
using System.Threading;
using ICSharpCode.SharpZipLib.Zip;

namespace NetBaltic.ServerProxy{

    public class ServerProxy {

        //Metoda obsługi wątku, przyjmuje tablicę obiektów, które trzeba zrzutować na odpowiedni typ
        public static void Process(object state) {
            try
            {
                object[] array = state as object[];
                DateTime myDate = new DateTime(2018, 10, 19, 23, 30, 05);

                Console.WriteLine("Establishing connection with database");
                Postgresql dataBase = new Postgresql();
                dataBase.Open();
                Console.WriteLine("Connection with database established");

                //Rzutowanie
                Receive request = array[0] as Receive;

                //Logika zwazana z odbieraniem wiadomosci
                Console.WriteLine("Odebralem wiadomosc!");
                Console.WriteLine("Sender: " + request.OutputMessage.Sender);
                Console.WriteLine("Usluga: " + request.OutputMessage.ServiceType);
                Console.WriteLine("Ilosc bajtow wiadomosci: " + request.OutputMessage.Content.Length);

                var str = request.OutputMessage.Content;
                Console.WriteLine("Content: " + str);
                byte[] encodedDataAsBytes = System.Convert.FromBase64String(str.TrimEnd('\'').TrimStart('b', '\''));

                string returnValueStr = System.Text.ASCIIEncoding.ASCII.GetString(encodedDataAsBytes);
                string finale = @returnValueStr;
                Console.WriteLine("Content 1: " + returnValueStr);
                var details = JObject.Parse(finale);
                Console.WriteLine(details["url"] + " " + details["uid"] + " " + details["pk"] + details["cycmsg"]);
                string cycleMessage = details["cycmsg"].ToString();
                string uid = details["uid"].ToString();
                string pk = details["pk"].ToString();
                string url = details["url"].ToString();

                //int a = str.Length;
                //char[] str1 = new char[a];

                //for (int i = 13; i < a - 17; i++)
                //{
                //    str1[i - 13] += str[i];
                //}
                //string str2 = new string(str1);


                //var query = @"SELECT service_type_number, label FROM service_types;";

                //// Wykonanie zapytania
                //reader = postgresql.Sql(query);

                //// CZYTANIE WYNIKU - czyta wynik linia po linii, aż do osiągnięcia końca
                //while (reader.Read())
                //{
                //    var type = (byte)(int)reader["service_type_number"];    // Trzeba zrzutować typy
                //    var label = reader["label"] as string;                  // Tak jak wyżej inny sposób rzutowania
                //}

                //// Trzeba zamknąć obiekt reader, szczególnie jeżeli chce się wykonać potem inne zapytanie w ramach tej samej sesji połączenia z bazą
                //reader.Close();

                if (cycleMessage == "1")
                {
                    var tmpIsCyclic = 0;
                    Console.WriteLine("Start reading from DB");
                    try
                    {
                        var query = @"SELECT IsCyclic FROM requests WHERE UserID == " + uid + ";";
                        var reader = dataBase.Sql(query);

                        Console.WriteLine("Reading from DB");
                        while (reader.Read())
                        {
                            tmpIsCyclic = (int)reader["IsCyclic"];
                        }
                        Console.WriteLine("Reding from DB done");
                        reader.Close();
                    }
                    catch
                    {
                        Console.WriteLine("Reding from DB ERROR");
                    }

                    if (tmpIsCyclic == 1)
                    {
                        Console.WriteLine("Request already process!");
                    }
                    else
                    {
                        Console.WriteLine("Inserting to DB");
                        try
                        {
                            var query1 = @"INSERT INTO cyclicrequests (UserID, Address, URL, IsCyclic) VALUES (" + uid + ", " + request.OutputMessage.Sender + ", " + url + ", " + cycleMessage + ");";
                            dataBase.Sql(query1);
                        }
                        catch
                        {
                            Console.WriteLine("Inserting to DB ERROR");
                        }

                        WaitCallback callback = new WaitCallback(ServerProxy.CyclicSend);
                        ThreadPool.QueueUserWorkItem(callback, new object[] { request });
                        Console.WriteLine("Inserting to DB done and cyclic send starting process");
                    }

                }
                else if(cycleMessage == "2")
                {
                    var query2 = "UPDATE cyclicrequests SET IsCyclic = " + cycleMessage + "WHERE UserID == " + uid + ";";
                    dataBase.Sql(query2);

                    //TODO: Usuwanie wątku z puli
                }
                else
                {
                    string wgetWithUrl = " --html-extension -p -P /root/" + details["url"] + " " + details["url"];
                    Console.WriteLine("Execute:" + wgetWithUrl);

                    //////string page;
                    //////using (var client = new WebClient())
                    //////{
                    //////    //client.Credentials = new NetworkCredential("user", "password");
                    //////    page = client.DownloadString(str2);
                    //////    Console.WriteLine(page);
                    //////}

                    Process proc = new System.Diagnostics.Process();
                    proc.StartInfo.FileName = "/usr/bin/wget";
                    proc.StartInfo.Arguments = wgetWithUrl;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.Start();
                    proc.WaitForExit();
                    Console.WriteLine("Download done!");
                    String file = "Default value";
                    String jsonFile = "Default value";
                    Byte[] zipInBytes;
                    Byte[] finalZipInBytes;

                    try
                    {
                        string pathToZip = @"/root/" + details["url"];
                        string zipedPath = @"/root/zipedwebsite.zip";
                        FastZip fastZip = new FastZip();
                        bool recurse = true;  // Include all files by recursing through the directory structure
                        string filter = null; // Dont filter any files at all
                        fastZip.CreateZip(zipedPath, pathToZip, recurse, filter);
                        Console.WriteLine("Zipping done!");
                    }
                    catch
                    {
                        Console.WriteLine("Zipping issues...");
                    }
                    try
                    {
                        zipInBytes = File.ReadAllBytes("/root/zipedwebsite.zip");
                        file = Convert.ToBase64String(zipInBytes);
                        Console.WriteLine("Converting done!");
                    }
                    catch
                    {
                        Console.WriteLine("Converting issues...");
                    }
                    dynamic product = new JObject();
                    product.uid = details["uid"];
                    product.pk = details["pk"];
                    product.zip = file;
                    try
                    {
                        finalZipInBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(product.ToString());
                        jsonFile = System.Convert.ToBase64String(finalZipInBytes);
                        Console.WriteLine("Converting JSON done!");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Converting JSON issues...");
                        Console.WriteLine(e);
                    }
                    Console.WriteLine("Rozpoczynam tworzenie wiadomości");
                    InputMessageTransportModule inputMessage = new InputMessageTransportModule { Content = jsonFile, Priority = Priorities.NORMAL, Receiver = request.OutputMessage.Sender, ValidityTime = myDate, ServiceType = request.OutputMessage.ServiceType };
                    Console.WriteLine("Tworzenie wiadomości zakończone");
                    try
                    {
                        Console.WriteLine("Rozpoczynam wysyłanie...");
                        SendResponse response2 = ServerProxyAPI.tranportModuleClient.Send<SendResponse>(new Send { InputMessage = inputMessage });
                        if (response2.Result == 0)
                        {
                            Console.WriteLine("Wysylanie powodlo sie");
                        }
                    }
                    catch (Exception b)
                    {
                        Console.WriteLine("Wysylanie nie powodlo sie...");
                        Console.WriteLine(b);
                    }
                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        public static void CyclicSend(object state)
        {
            try
            {
                object[] array = state as object[];
                DateTime myDate = new DateTime(2018, 10, 19, 23, 30, 05);

                Postgresql dataBase = new Postgresql();
                dataBase.Open();

                //Rzutowanie
                Receive request = array[0] as Receive;
          
                //Rozpakowanie wiadomości na części
                var str = request.OutputMessage.Content;
                byte[] encodedDataAsBytes = System.Convert.FromBase64String(str.TrimEnd('\'').TrimStart('b', '\''));
                string returnValueStr = System.Text.ASCIIEncoding.ASCII.GetString(encodedDataAsBytes);
                string finale = @returnValueStr;
                var details = JObject.Parse(finale);
                string isCycle = details["cycmsg"].ToString();
                string uid = details["uid"].ToString();
                string pk = details["pk"].ToString();
                string url = details["url"].ToString();


                while (isCycle == "1")
                {
                    var tmpIsCyclic = 0;
                    try
                    {
                        var query = @"SELECT IsCyclic FROM requests WHERE UserID == " + uid + ";";
                        var reader = dataBase.Sql(query);

                        while (reader.Read())
                        {
                            tmpIsCyclic = (int)reader["IsCyclic"];
                        }
                        reader.Close();
                    }
                    catch
                    {
                        Console.WriteLine("Reding from DB ERROR while cycle sending");
                    }


                    if (tmpIsCyclic == 0 || tmpIsCyclic == 2)
                    {
                        Console.WriteLine("Stop processing cyclic request.");
                        isCycle = "0";
                        return;
                    }
                    else {

                        string userAddress = "";
                        string userURL = "";

                        try
                        {
                            var query1 = @"SELECT Address, URL FROM cyclicrequests WHERE UserID == " + uid + ";";
                            var reader1 = dataBase.Sql(query1);

                            while (reader1.Read())
                            {
                                userAddress = reader1["Address"] as string;
                                userURL = reader1["URL"] as string;
                            }
                            reader1.Close();
                        }
                        catch
                        {
                            Console.WriteLine("Second reading from DB ERROR while cycle sending");
                        }


                        //string userAddress = dataBase.Sql("SELECT Address FROM cyclicrequests WHERE UserID == " + details["uid"]).ToString();
                        //string userURL = dataBase.Sql("SELECT URL FROM cyclicrequests WHERE UserID == " + details["uid"]).ToString();
                        string wgetWithUrl = " --html-extension -p -P /root/" + userURL + " " + userURL;
                        Console.WriteLine("Execute:" + wgetWithUrl);

                        //////string page;
                        //////using (var client = new WebClient())
                        //////{
                        //////    //client.Credentials = new NetworkCredential("user", "password");
                        //////    page = client.DownloadString(str2);
                        //////    Console.WriteLine(page);
                        //////}

                        Process proc = new System.Diagnostics.Process();
                        proc.StartInfo.FileName = "/usr/bin/wget";
                        proc.StartInfo.Arguments = wgetWithUrl;
                        proc.StartInfo.UseShellExecute = false;
                        proc.StartInfo.RedirectStandardOutput = true;
                        proc.Start();
                        proc.WaitForExit();
                        Console.WriteLine("Download done!");
                        String file = "Default value";
                        String jsonFile = "Default value";
                        Byte[] zipInBytes;
                        Byte[] finalZipInBytes;

                        try
                        {
                            string pathToZip = @"/root/" + userURL;
                            string zipedPath = @"/root/zipedwebsite.zip";
                            FastZip fastZip = new FastZip();
                            bool recurse = true;  // Include all files by recursing through the directory structure
                            string filter = null; // Dont filter any files at all
                            fastZip.CreateZip(zipedPath, pathToZip, recurse, filter);
                            Console.WriteLine("Zipping done!");
                        }
                        catch
                        {
                            Console.WriteLine("Zipping issues...");
                        }
                        try
                        {
                            zipInBytes = File.ReadAllBytes("/root/zipedwebsite.zip");
                            file = Convert.ToBase64String(zipInBytes);
                            Console.WriteLine("Converting done!");
                        }
                        catch
                        {
                            Console.WriteLine("Converting issues...");
                        }
                        dynamic product = new JObject();
                        product.uid = details["uid"];
                        product.pk = details["pk"];
                        product.zip = file;
                        try
                        {
                            finalZipInBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(product.ToString());
                            jsonFile = System.Convert.ToBase64String(finalZipInBytes);
                            Console.WriteLine("Converting JSON done!");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Converting JSON issues...");
                            Console.WriteLine(e);
                        }
                        Console.WriteLine("Rozpoczynam tworzenie wiadomości");
                        InputMessageTransportModule inputMessage = new InputMessageTransportModule { Content = jsonFile, Priority = Priorities.NORMAL, Receiver = userAddress, ValidityTime = myDate, ServiceType = request.OutputMessage.ServiceType };
                        Console.WriteLine("Tworzenie wiadomości zakończone");
                        try
                        {
                            Console.WriteLine("Rozpoczynam wysyłanie...");
                            SendResponse response2 = ServerProxyAPI.tranportModuleClient.Send<SendResponse>(new Send { InputMessage = inputMessage });
                            if (response2.Result == 0)
                            {
                                Console.WriteLine("Wysylanie powodlo sie");
                            }
                        }
                        catch (Exception b)
                        {
                            Console.WriteLine("Wysylanie nie powodlo sie...");
                            Console.WriteLine(b);
                        }
 
                        System.Threading.Thread.Sleep(10000);
                    }

                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public ServerProxy()
		{
		}
	}
}
