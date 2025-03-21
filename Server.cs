using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Intyaa.SimpleWebServer
{
    public class Server
    {
        private static HtttpListener listener;
        public static int maxSimultaneousConnections = 10;+
        private static Semaphore semaphore = new Semaphore(maxSimultaneousConnections, maxSimultaneousConnections);


        private static HttpListener initialize(List<IPAddres> localIp) 
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost/");

            localIp.foreach(ip => 
            {
                Console.WriteLine("Listening on: " + ip.ToString());
                listener.Prefixes.Add("http://" + ip.ToString() + "/");
            });

            return listener;
        }


        private static List<IPAddress> getLocalIPAddress()
        {
            List<IPAddress> ipAddresses = new List<IPAddress>();
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipAddresses.Add(ip);
                }
            }
            return ipAddresses;
        }

        private static async Task Start(HttpListener listener)
        {
            listener.Start();
            Console.WriteLine("Server started");
            while (true)
            {
                semaphore.WaitOne();
                HttpListenerContext context = await listener.GetContextAsync();
                Task.Run(() => runServer(context));
            }
        }

        private static void runServer(HttpListener listener) 
        {
            while(true)
            {
                semaphore.WaitOne();
                StartConnectionListener(listener);
            }
        }

        private static async void StartConnectionListener(HttpListener listener)
        {
            HttpListenerContext context = await listener.GetContextAsync();
            semaphore.Release();
            
        }
    }
}