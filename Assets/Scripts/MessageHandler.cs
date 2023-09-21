using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageHandler : NodeComponent
{
    internal string CreateRequestClosestNodesMessage(string publicKey)
    {
        Message requestMessage = new Message
        {
            Type = "RequestClosestNodes",
            Content = publicKey
        };
        return JsonUtility.ToJson(requestMessage);
    }

    internal Message DeserializeMessage(string jsonString)
    {
        return JsonUtility.FromJson<Message>(jsonString);
    }

    internal List<NodeData> DeserializeNodeList(string response)
    {
        throw new NotImplementedException();
    }
}

[Serializable]
public class Message
{
    public string Type; // Representará el tipo de mensaje ("RequestClosestNodes" en este caso).
    public string Content; // Almacenará datos adicionales si es necesario.
}