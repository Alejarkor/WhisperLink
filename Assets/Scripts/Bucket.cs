using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public class Bucket : MonoBehaviour
{    
    public const int BUCKET_SIZE = 20;
    public int XORRangeLevel; // Identificar el nivel de distancia XOR
    public Dictionary<string, NodeData> Nodes;

    public Bucket()
    {
        Nodes = new Dictionary<string, NodeData>(BUCKET_SIZE);
    }

    public bool AddNode(string publicKey, NodeData nodeData)
    {
        if (Nodes.Count < BUCKET_SIZE)
        {
            if (!Nodes.ContainsKey(publicKey))
            {
                Nodes.Add(publicKey, nodeData);
                return true;
            }
        }
        return false;
    }

    public bool RemoveNode(string publicKey)
    {
        return Nodes.Remove(publicKey);
    }

    public NodeData? GetRandomNode() 
    {
        if (Nodes.Count == 0)
            return null;

        int randomIndex = new System.Random().Next(0, Nodes.Count);
        foreach (var node in Nodes.Values)
        {
            if (--randomIndex < 0)
                return node;
        }
        return null; // Nunca debería llegar aquí
    }

    public static ulong CalculateXORDistance(string key1, string key2)
    {
        // Asume que los keys son representaciones string de números, por simplicidad.
        ulong id1 = Convert.ToUInt64(key1, 16);
        ulong id2 = Convert.ToUInt64(key2, 16);

        return id1 ^ id2;
    }
}
