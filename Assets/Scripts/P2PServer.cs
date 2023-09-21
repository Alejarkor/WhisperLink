using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Experimental.GraphView;
using System;
using UnityEditor.PackageManager;
using System.IO;

//Esta clase se ocupa de gestionar las conexiones entrantes
public class P2PServer : NodeComponent
{
    private TcpListener _listener;
    private TcpClient _clientTURN;
    private NetworkStream _streamTURN;
    private const int KeepAliveIntervalMs = 5000; // 5 segundos

    private bool _isRunning;


    public Action<string> OnRequestClosestNodesReceived;



    public override void Initialize(Node parent)
    {
        base.Initialize(parent);
        _listener = new TcpListener(parentNode.nodeData.ipAddress, parentNode.nodeData.port);
    }

    public async void Start()
    {
        _listener.Start();
        _isRunning = true;

        while (_isRunning)
        {
            // Espera de manera asíncrona una conexión de cliente.
            TcpClient client = await _listener.AcceptTcpClientAsync();

            // Manejar la conexión en una nueva tarea.
            Task.Run(() => HandleClient(client));
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _listener.Stop();
    }

    private async Task HandleClient(TcpClient client)
    {
        using (client)
        using (NetworkStream stream = client.GetStream())
        {
            byte[] buffer = new byte[4096];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            Message message = parentNode.messageHandlerComponent.DeserializeMessage(receivedMessage);

            if (message.Type == "RequestClosestNodes")
            {
                OnRequestClosestNodesReceived?.Invoke(message.Content);
            }
            // Aquí podrías añadir más condiciones para otros tipos de mensajes...
        }
    }



    public async Task ConnectAsync(string serverIp, int serverPort)
    {
        _clientTURN = new TcpClient();

        while (true)
        {
            try
            {
                await _clientTURN.ConnectAsync(serverIp, serverPort);
                _streamTURN = _clientTURN.GetStream();
                Console.WriteLine("Connected to TURN server.");
                StartListening();

                while (_clientTURN.Connected)
                {
                    // Enviar paquete keep-alive
                    var keepAlivePacket = Encoding.ASCII.GetBytes("KEEP_ALIVE");
                    await _streamTURN.WriteAsync(keepAlivePacket, 0, keepAlivePacket.Length);
                    Console.WriteLine("Sent keep-alive packet.");

                    await Task.Delay(KeepAliveIntervalMs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}. Trying to reconnect...");
                await Task.Delay(1000); // Espera 1 segundo antes de intentar reconectar
            }
        }
    }
    private async void StartListening()
    {
        var buffer = new byte[1024];

        try
        {
            while (_clientTURN.Connected)
            {
                int bytesRead = await _streamTURN.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Received: {receivedMessage}");
                }
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error while listening: {ex.Message}");
        }
    }

}
