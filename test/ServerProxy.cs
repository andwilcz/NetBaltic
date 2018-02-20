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
        public static void Process(object state)
        {
            try
            {
                object[] array = state as object[];
                DateTime myDate = new DateTime(2018, 10, 19, 23, 30, 05);

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

                //int a = str.Length;
                //char[] str1 = new char[a];

                //for (int i = 13; i < a - 17; i++)
                //{
                //    str1[i - 13] += str[i];
                //}
                //string str2 = new string(str1);

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
