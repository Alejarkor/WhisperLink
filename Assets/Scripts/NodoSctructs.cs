using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

[Serializable]
public struct NodeData
{  
    public string publicKey;
    public NodeState state;
    public IPAddress ipAddress;
    public int port;
    public bool isTurn;
    public bool isNated;


    public string Serialize() 
    {
        return JsonUtility.ToJson(this);
    }

    public static NodeData Deserialize(string json) 
    {
        return JsonUtility.FromJson<NodeData>(json);
    }

    public static string SerializeNodeList(List<NodeData> nodeList)
    {
        NodeDataListWrapper wrapper = new NodeDataListWrapper();
        wrapper.nodeList = nodeList;
        return JsonUtility.ToJson(wrapper);
    }

    public static List<NodeData> DeserializeNodeList(string json) 
    {        
        return JsonUtility.FromJson<NodeDataListWrapper>(json).nodeList;
    }
}

[Serializable]
public class NodeDataListWrapper
{
    public List<NodeData> nodeList;
}

public enum NodeState 
{
    Online,
    Offline,
    Unknown
}

