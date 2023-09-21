using System;
using System.Collections;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Open.Nat;
using UnityEngine;



public class NodeConnectivityHandler : NodeComponent
{
    

    private object lockObject = new object();
    private bool hasInternetAcces = false; // Determina si tenemos conexion a internet
    private const int RetryDelayInternetAcces = 1;  // Pausa entre intentos (minutos).
    private const int RetryDelayFindAvailablePort = 5;  // Pausa entre intentos (minutos).
    private ManualResetEventSlim mreInternetAcces = new ManualResetEventSlim(false); // Inicialmente seteado
    private ManualResetEventSlim mreAvailableLocalPort = new ManualResetEventSlim(false); // Inicialmente seteado
    private ManualResetEventSlim mreTCPServerReady = new ManualResetEventSlim(false); // Inicialmente seteado
    private ManualResetEventSlim mreAvailablePublicPort = new ManualResetEventSlim(false); // Inicialmente seteado

    private int localPort;
    private int? publicPort;
    private string? publicIP;

    public Action<int> OnAvailablePort;
    // Método para obtener la IP pública
    
    
    

    public async Task<bool> HasConnectivity()
    {
        using (var httpClient = new HttpClient())
        {
            try
            {
                var response = await httpClient.GetAsync("https://www.google.com/");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
    private void StartHasConnectivityCheck()
    {
        Task.Run(async () =>
        {
            while (true) // Bucle infinito
            {
                if (await HasConnectivity())
                {
                    mreInternetAcces.Set(); // Esto permitirá que la Task continúe
                }
                else 
                {
                    mreInternetAcces.Reset(); // Esto bloqueará la Task hasta que se vuelva a setear
                }

                await Task.Delay(TimeSpan.FromMinutes(RetryDelayInternetAcces)); // Pausa antes de la siguiente comprobación
            }
        });
    }

    int FindAvailablePort(int startPort = 1024, int endPort = 65535)
    {
        for (int port = startPort; port <= endPort; port++)
        {
            try
            {
                TcpListener listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                listener.Stop();
                return port;
            }
            catch (SocketException)
            {
                // Este puerto no está disponible, sigue intentando
            }
        }
        throw new ApplicationException("No se pueden encontrar puertos disponibles.");
    }
    private void StartFindAbailablePortCheck()
    {
        Task.Run(async () =>
        {
            while (true) // Bucle infinito
            {
                mreInternetAcces.Wait(); // Esto bloqueará la Task hasta tengamos conectividad

                try
                {
                    localPort = FindAvailablePort();
                    OnAvailablePort?.Invoke(localPort);
                    mreAvailableLocalPort.Set(); // Esto permitirá que la Task continúe
                }
                catch 
                {
                    mreAvailableLocalPort.Reset(); // Esto bloqueará la Task hasta que se vuelva a setear
                }                

                await Task.Delay(TimeSpan.FromMinutes(RetryDelayFindAvailablePort)); // Pausa antes de la siguiente comprobación
            }
        });
    }

    public async Task<int?> GetRedirectionPortAsync(int localListeningPort)
    {
        try
        {
            var discoverer = new NatDiscoverer();
            var cts = new System.Threading.CancellationTokenSource(5000); // Timeout de 5 segundos
            var device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);

            // Intentamos crear un mapeo para el puerto local. Si el router ya tiene ese puerto asignado,
            // podría generar una excepción o asignar un puerto diferente.
            var mapping = new Mapping(Protocol.Tcp, localListeningPort, localListeningPort, "MyApp");

            await device.CreatePortMapAsync(mapping);

            // Después de crear el mapeo, intentamos obtener el mapeo existente para ver el puerto externo asignado.
            var existingMapping = await device.GetSpecificMappingAsync(Protocol.Tcp, localListeningPort);

            return existingMapping.PublicPort; // Este es el puerto que el router utiliza para reenviar a tu aplicación.
        }
        catch
        {
            return null; // Retorna null si hay algún error. Por supuesto, puedes manejar errores específicos si es necesario.
        }
    }
    private void StartGetRedirectionPortCheck()
    {
        Task.Run(async () =>
        {
            while (true) // Bucle infinito
            {
                mreTCPServerReady.Wait(); // Esto bloqueará la Task hasta tengamos el servidor levantado
                publicPort = await GetRedirectionPortAsync(localPort);
                if (publicPort != null)
                {
                    mreAvailablePublicPort.Set(); // Esto permitirá que la Task continúe
                }
                else 
                {
                    mreAvailablePublicPort.Reset(); // Esto bloqueará la Task hasta que se vuelva a setear
                }

                await Task.Delay(TimeSpan.FromMinutes(RetryDelayFindAvailablePort)); // Pausa antes de la siguiente comprobación
            }
        });
    }

    public async Task<string> GetPublicIpAsync()
    {
        using var httpClient = new HttpClient();
        return await httpClient.GetStringAsync("http://api.ipify.org");
    }
    private void StartGetPublicIpCheck()
    {
        Task.Run(async () =>
        {
            while (true) // Bucle infinito
            {
                mreAvailablePublicPort.Wait(); // Esto bloqueará la Task hasta tengamos el servidor levantado
                publicIP = await GetPublicIpAsync();
                if (publicIP != null)
                {
                    mreAvailablePublicPort.Set(); // Esto permitirá que la Task continúe
                }
                else
                {
                    mreAvailablePublicPort.Reset(); // Esto bloqueará la Task hasta que se vuelva a setear
                }

                await Task.Delay(TimeSpan.FromMinutes(RetryDelayFindAvailablePort)); // Pausa antes de la siguiente comprobación
            }
        });
    }

    //private async Task EnsureNodeIsOnline()
    //{
    //    while (true)  // Bucle de conectividad a Internet
    //    {
    //        if (await HasInternetAccessAsync())
    //        {
    //            string publicIp;
    //            try
    //            {
    //                publicIp = await GetPublicIpAsync();
    //            }
    //            catch
    //            {
    //                OnNotification?.Invoke("Failed to retrieve public IP.");
    //                await Task.Delay(RetryDelayMs);
    //                continue;  // Vuelve al inicio del bucle de conectividad.
    //            }

    //            int availablePort;
    //            try
    //            {
    //                availablePort = FindAvailablePort();
    //            }
    //            catch
    //            {
    //                OnNotification?.Invoke("Failed to find an available port.");
    //                await Task.Delay(TimeSpan.FromMinutes(1));  // Retardo de 1 minuto.
    //                continue;  // Vuelve al inicio del bucle de conectividad
    //            }


    //            while (true)  // Bucle de búsqueda de puerto
    //            {




    //                // Intentar iniciar el servidor
    //                var serverInitTcs = new TaskCompletionSource<bool>();
    //                OnServerInitializationRequired?.Invoke(availablePort, serverInitTcs);
    //                bool serverInitialized = await serverInitTcs.Task;

    //                if (!serverInitialized)
    //                {
    //                    OnNotification?.Invoke($"Failed to initialize server on port {availablePort}.");
    //                    await Task.Delay(RetryDelayMs);
    //                    continue;  // Vuelve al inicio del bucle de búsqueda de puerto.
    //                }

    //                int? redirectionPort = await GetRouterRedirectionPortAsync(availablePort);
    //                if (redirectionPort.HasValue)
    //                {
    //                    var connectivityCheckTcs = new TaskCompletionSource<bool>();
    //                    OnCheckConnectivityRequired?.Invoke(publicIp, redirectionPort.Value, connectivityCheckTcs);
    //                    bool isConnectivityOk = await connectivityCheckTcs.Task;

    //                    if (isConnectivityOk)
    //                    {
    //                        // El nodo está online y es accesible.
    //                        Debug.Log("Node is online!");
    //                        return;  // Salimos de todas las iteraciones, ya que el nodo está online.
    //                    }
    //                }
    //                OnNotification?.Invoke("Failed to establish external connectivity.");
    //                await Task.Delay(RetryDelayMs);
    //            }
    //        }
    //        else
    //        {
    //            OnNotification?.Invoke("No internet connectivity.");
    //            await Task.Delay(RetryDelayMs);
    //        }
    //    }
    //}

    private IEnumerator GetPublicIpCoroutine()
    {
        var task = Task.Run(GetPublicIpAsync);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError($"Error obtaining public IP: {task.Exception}");
        }
        else
        {
            Debug.Log($"Public IP: {task.Result}");
        }
    }
    private IEnumerator GetAndPrintPortCoroutine()
    {
        // Invocamos el método asíncrono
        var task = GetRouterRedirectionPortAsync(7777); // Puedes cambiar 7777 al puerto que desees

        // Esperamos a que la tarea asíncrona se complete
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Result.HasValue)
        {
            Debug.Log($"Router Redirection Port: {task.Result.Value}");
        }
        else
        {
            Debug.LogError("Failed to get router redirection port.");
        }
    }
    public void Start()
    {
        StartCoroutine( GetPublicIpCoroutine());
        StartCoroutine( GetAndPrintPortCoroutine());
    }
}
