// Code language: C#
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Intyaa.Server
{
    public class Server
    {
        private static HttpListener? listener;
        public static int maxSimultaneousConnections = 10;
        private static Semaphore semaphore = new Semaphore(maxSimultaneousConnections, maxSimultaneousConnections);

        public static async Task Start() 
        {
            try
            {
                List<IPAddress> localIp = getLocalIPAddress();
                listener = initialize(localIp, 8080); 
                listener.Start();
                Console.WriteLine("Server started successfully");
                Console.WriteLine("Testing connection at http://localhost:8080/");
                RunServerTask(listener);
                Console.WriteLine("Server is running. Press Enter to exit.");
                await Task.Delay(-1);
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine($"HTTP Listener error: {ex.Message} (Error code: {ex.ErrorCode})");
                
                if (ex.ErrorCode == 5)
                {
                    Console.WriteLine("Zugriff verweigert. Versuche den Server mit Administratorrechten zu starten.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting server: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private static HttpListener initialize(List<IPAddress> localIp, int port) 
        {
            HttpListener listener = new HttpListener();
            string localHostUrl = $"http://localhost:{port}/";
            Console.WriteLine($"Adding URL: {localHostUrl}");
            listener.Prefixes.Add(localHostUrl);
            
            foreach(var ip in localIp)
            {
                string url = $"http://{ip}:{port}/";
                Console.WriteLine($"Adding URL: {url}");
                listener.Prefixes.Add(url);
            }
            
            return listener;
        }

        private static List<IPAddress> getLocalIPAddress()
        {
            try
            {
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                List<IPAddress> ipAddresses = host.AddressList
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                    .ToList();
                
                Console.WriteLine($"Found {ipAddresses.Count} local IP addresses:");
                foreach (var ip in ipAddresses)
                {
                    Console.WriteLine($"  - {ip}");
                }
                
                return ipAddresses;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting local IP addresses: {ex.Message}");
                return new List<IPAddress> { IPAddress.Loopback };
            }
        }

        private static void RunServerTask(HttpListener listener) 
        {
            Task.Run(() => 
            {
                try
                {
                    while(true)
                    {
                        semaphore.WaitOne();
                        HandleNextConnection(listener);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Server loop error: {ex.Message}");
                }
            });
        }

        private static async void HandleNextConnection(HttpListener listener)
        {
            try
            {
                HttpListenerContext context = await listener.GetContextAsync();
                Console.WriteLine("Waiting for connection...");
                Console.WriteLine($"Connection received from {context.Request.RemoteEndPoint.Address}");
                semaphore.Release();
                
                string defaultResponse = "<html><body><h1>Simple Web Server</h1><p>Es funktioniert!</p></body></html>";
                byte[] encoded = Encoding.UTF8.GetBytes(defaultResponse);
                
                context.Response.ContentType = "text/html";
                context.Response.ContentLength64 = encoded.Length;
                await context.Response.OutputStream.WriteAsync(encoded, 0, encoded.Length);
                context.Response.Close();
                
                Console.WriteLine("Response sent successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling connection: {ex.Message}");
                semaphore.Release();
            }
        }
    }
}