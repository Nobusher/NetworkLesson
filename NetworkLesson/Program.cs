using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkLesson
{
    internal class Program
    {
        private const int DiscoveryPort = 5001;
        private const string DiscoveryRequest = "WHO_IS_THERE";

        private static async Task Main(string[] args)
        {
            Console.Write("Режим (s - сервер, c - клиент): ");
            string? mode = Console.ReadLine();

            if (mode == "s")
                await RunServerAsync();
            else if (mode == "c")
                await RunClientAsync();
            else
                Console.WriteLine("Неизвестный режим");
        }

        private static async Task RunServerAsync()
        {
            while (true)
            {
                await DiscoverClientsAsync();

                Console.WriteLine("Enter - искать снова");
                Console.ReadLine();
            }
        }

        private static async Task DiscoverClientsAsync()
        {
            using var udp = new UdpClient();
            udp.EnableBroadcast = true;

            // Отправляем запрос всем устройствам сети
            byte[] request = Encoding.UTF8.GetBytes(DiscoveryRequest);
            var broadcast = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);
            await udp.SendAsync(request, request.Length, broadcast);

            Console.WriteLine("Ищу клиентов...");

            
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            int count = 0;

            try
            {
                while (true)
                {
                    UdpReceiveResult result = await udp.ReceiveAsync(timeout.Token);
                    string name = Encoding.UTF8.GetString(result.Buffer);

                    Console.WriteLine($"  {name} - {result.RemoteEndPoint.Address}");
                    count++;
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Timeout");
            }

            Console.WriteLine($"Найдено клиентов: {count}");
        }

        private static async Task RunClientAsync()
        {
            string name = Environment.MachineName;

            
            using var udp = new UdpClient(DiscoveryPort);
            Console.WriteLine($"Клиент {name} запущен, жду запросов...");

            while (true)
            {
                UdpReceiveResult result = await udp.ReceiveAsync();
                string text = Encoding.UTF8.GetString(result.Buffer);

                if (text != DiscoveryRequest)
                    continue; 

                Console.WriteLine($"Запрос от {result.RemoteEndPoint.Address}, отвечаю");

                
                byte[] reply = Encoding.UTF8.GetBytes(name);
                await udp.SendAsync(reply, reply.Length, result.RemoteEndPoint);
            }
        }
    }
}