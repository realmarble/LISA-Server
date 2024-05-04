using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class ChatServer
{
    private TcpListener _listener;
    private ConcurrentDictionary<string, TcpClient> _clients = new ConcurrentDictionary<string, TcpClient>();

    public ChatServer(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public void Start()
    {
        _listener.Start();
        Console.WriteLine("Server has started on {0}:{1}.", ((IPEndPoint)_listener.LocalEndpoint).Address, ((IPEndPoint)_listener.LocalEndpoint).Port);

        try
        {
            while (true)
            {
                TcpClient client = _listener.AcceptTcpClient();

                // Start a new thread to handle the connection
                Thread clientThread = new Thread(HandleClient);
                clientThread.Start(client);

            }
        }
        finally
        {
            _listener.Stop();
        }
    }

    private void HandleClient(object clientObj)
    {
        TcpClient client = (TcpClient)clientObj;
        string clientId = Guid.NewGuid().ToString();
        _clients.TryAdd(clientId, client);
        Console.WriteLine($"Accepted new client: {clientId}");
        BroadcastMessage(clientId, $"User {clientId} has joined");

        try
        {
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            {
                writer.AutoFlush = true;
                while (true)
                {
                    try
                    {
                        string message = reader.ReadLine();
                        Console.WriteLine($"{clientId}: {message}");
                        BroadcastMessage(clientId, $"{clientId}: {message}");

                    }
                    catch (System.IO.IOException)
                    {
                        Console.WriteLine($"User {clientId} has disconnected");
                        break;
                    }


                }
            }
        }
        finally
        {
            TcpClient removedClient;
            _clients.TryRemove(clientId, out removedClient);
            client.Close();
            BroadcastMessage(null, $"User {clientId} has disconnected");
        }
    }

    private void BroadcastMessage(string senderId, string message)
    {
        foreach (var client in _clients)
        {
            if (client.Key == senderId) continue;

            try
            {
                StreamWriter writer = new StreamWriter(client.Value.GetStream()) { AutoFlush = true };
                writer.WriteLine(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error broadcasting message: {0}", ex.Message);
            }
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        ChatServer server = new ChatServer(8888);
        server.Start();
    }
}
