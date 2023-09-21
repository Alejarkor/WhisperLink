using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


//Esta clase se ocupa de las conexiones salientes
public class p2pClient : NodeComponent
{   
    private float requestInterval = 10f; // Por ejemplo, cada 10 segundos       

    public async Task<string> SendMessageAsync(string message, IPAddress ipAddress, int port)
    {
        using (TcpClient client = new TcpClient())
        {
            try
            {
                // Conectar al servidor de forma asíncrona.
                await client.ConnectAsync(ipAddress, port);

                // Obtener el stream de la conexión y enviar el mensaje de forma asíncrona.
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                    await stream.WriteAsync(messageBytes, 0, messageBytes.Length);

                    // Leer la respuesta de forma asíncrona.
                    byte[] responseBuffer = new byte[4096];  // Asumiendo que la respuesta será menor que 4096 bytes.
                    int bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);

                    return Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error al enviar el mensaje: {ex.Message}");
                return null;
            }
        }
    }

    public bool TestTcpConnection(string ipAddress, int port, int timeoutMilliseconds = 2000)
    {
        using TcpClient tcpClient = new TcpClient();

        try
        {
            var asyncResult = tcpClient.BeginConnect(ipAddress, port, null, null);
            asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeoutMilliseconds));

            return tcpClient.Connected;
        }
        catch
        {
            // Hubo un error al intentar conectar.
            return false;
        }
    }

    private IEnumerator NodeInfoRequestRoutine(List<NodeData> knownNodes)
    {
        while (true)
        {
            foreach (var node in knownNodes)
            {
                SendNodeInfoRequest(node);
            }

            yield return new WaitForSeconds(requestInterval);
        }
    }

    private void SendNodeInfoRequest(NodeData targetNode)
    {
        // Aquí pones la lógica de envío del mensaje NodeInfoRequest al nodo targetNode
        // Por ejemplo, podría usar una función TCP como las que discutimos anteriormente.
    }
}
