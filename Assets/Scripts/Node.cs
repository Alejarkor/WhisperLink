using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class Node : MonoBehaviour
{
    public NodeData nodeData;
    public List<string> contactsList;
    public List<string> turnList;

    public p2pClient clientComponent;
    public P2PServer serverComponent;
    public BucketHandler bucketHandlerComponent;
    public MessageHandler messageHandlerComponent;
    public ConnectionManager connectionManagerComponent;
    public NodeConnectivityHandler iPSolver;


    #region HANDLES
    //Todos los handles para manejar diferentes eventos que ocurren en los componentes. 
    private void HandleClosestNodesRequest(string publicKey)
    {
        List<NodeData> closestNodes = bucketHandlerComponent.GetClosestNodes(publicKey);
        // Aquí puedes hacer lo que quieras con los nodos más cercanos, como enviarlos de vuelta al solicitante.
    }
    private async void HandleRequestForNewNodes(NodeData targetNode, TaskCompletionSource<List<NodeData>> tcs)
    {
        try
        {
            var result = await GetClosestNodesAsync(targetNode);
            tcs.SetResult(result);
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
        }
    }

    #endregion
    private void Awake()
    {
        //Inicializar el nodo
        NodeInitialization();
    }

    private void NodeInitialization() 
    {
        //Inicializa cada uno de los componentes pasandoles el nodo. 
        clientComponent.Initialize(this);
        
        bucketHandlerComponent.Initialize(this);
        messageHandlerComponent.Initialize(this);
        connectionManagerComponent.Initialize(this);

        //Inicializar los buckets
        NodeData bootstrap = new NodeData();
        _ = InitializeBucketsAsync(bootstrap);


        //Inicializar P2PClient
        //Inicializar p2pServer

        serverComponent.OnRequestClosestNodesReceived += HandleClosestNodesRequest;
        serverComponent.Initialize(this);
        serverComponent.Start();


        //Inicializar MessageHandler
        //Inicializar ConnectionManager

        //Anunciar mi presencia en la red
        //Iniciar rutinas de mantenimiento
    }

    public async Task InitializeBucketsAsync(NodeData bootstrap)
    {                      
        List<NodeData> nodeList = await GetClosestNodesAsync(bootstrap);

        //Subscribirme al evento de solicitud de nuevos nodos
        bucketHandlerComponent.OnRequestForNewNodes += HandleRequestForNewNodes;
        await bucketHandlerComponent.InitializeBucketsAsync(nodeList);
        bucketHandlerComponent.OnRequestForNewNodes -= HandleRequestForNewNodes;
    }

    

    private async Task<List<NodeData>> GetClosestNodesAsync(NodeData targetNode) 
    {
        // 1 y 2. Solicitar un mensaje de RequestClosestNodes y enviarlo
        string requestMessage = messageHandlerComponent.CreateRequestClosestNodesMessage(nodeData.publicKey);
        string response = await clientComponent.SendMessageAsync(requestMessage, targetNode.ipAddress, targetNode.port);

        // 3 y 4. Deserializar la respuesta y actualizar buckets
        return messageHandlerComponent.DeserializeNodeList(response);
    }

    private void Update()
    {
        // Procesar mensajes, manejar conexiones, etc.
    }  

}
