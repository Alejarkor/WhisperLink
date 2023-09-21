using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BucketHandler : NodeComponent
{
    //Lista de buckets
    public List<Bucket> buckets;
    public Action<NodeData, TaskCompletionSource<List<NodeData>>> OnRequestForNewNodes;

    //private TaskCompletionSource<List<NodeData>> tcs;

    //Este metodo sirve para actualizar los buckets con nuevos nodos recibidos
    public void UpdateReceivedNodes(List<NodeData> receivedNodes)
    {
        foreach (var receivedNode in receivedNodes)
        {
            ulong distance = Bucket.CalculateXORDistance(parentNode.nodeData.publicKey, receivedNode.publicKey);

            int bucketIndex = GetBucketIndexForDistance(distance);

            if (bucketIndex >= 0 && bucketIndex < buckets.Count)
            {
                Bucket bucket = buckets[bucketIndex];

                // Eliminar nodos desconectados
                List<string> nodesToRemove = new List<string>();
                foreach (var node in bucket.Nodes)
                {
                    if (node.Value.state == NodeState.Offline)
                        nodesToRemove.Add(node.Key);
                }
                foreach (var key in nodesToRemove)
                {
                    bucket.Nodes.Remove(key);
                }

                // Si el nodo no está en el bucket, lo intentamos añadir
                if (!bucket.Nodes.ContainsKey(receivedNode.publicKey))
                {
                    // La función AddNode devuelve false si el bucket ya está lleno
                    if (!bucket.AddNode(receivedNode.publicKey, receivedNode))
                    {
                        // Si el bucket está lleno, encontramos el nodo más "lejano" en términos de distancia XOR
                        ulong farthestDistance = 0;
                        string farthestKey = null;
                        foreach (var node in bucket.Nodes)
                        {
                            ulong currentDistance = Bucket.CalculateXORDistance(parentNode.nodeData.publicKey, node.Value.publicKey);
                            if (currentDistance > farthestDistance)
                            {
                                farthestDistance = currentDistance;
                                farthestKey = node.Key;
                            }
                        }

                        // Si el nodo recibido está más "cerca" que el nodo más "lejano", reemplazamos el nodo "lejano"
                        if (distance < farthestDistance)
                        {
                            bucket.Nodes.Remove(farthestKey);
                            bucket.AddNode(receivedNode.publicKey, receivedNode);
                        }
                    }
                }
            }
        }
    }

    public async Task InitializeBucketsAsync(List<NodeData> targetNodes)
    {
        UpdateReceivedNodes(targetNodes);
              

        int iterationCount = 0;
        while (iterationCount < 5)
        {            
            foreach (var bucket in buckets)
            {
                if (bucket.Nodes.Count < Bucket.BUCKET_SIZE)
                {
                    // Tomar un nodo al azar del bucket y pedirle su lista de nodos cercanos
                    NodeData? tempRandomNode = bucket.GetRandomNode();                    
                    if (tempRandomNode != null)
                    {
                        var tcs = new TaskCompletionSource<List<NodeData>>();
                        OnRequestForNewNodes?.Invoke((NodeData)tempRandomNode, tcs);
                        var newNodesList = await tcs.Task; // Esto pausará la ejecución hasta que la tarea se complete
                        UpdateReceivedNodes(newNodesList);
                    }
                }
            }
            iterationCount++;
        }
    }

    internal void UpdateBuckets(object nodeList)
    {
        throw new NotImplementedException();
    }

    //Devuelve el bucket en funcion de la distancia XOR
    private int GetBucketIndexForDistance(ulong distance)
    {
        // Aquí, vamos a determinar en qué bucket cae una distancia dada.
        // Suponiendo que los buckets están ordenados por distancia.
        for (int i = 0; i < buckets.Count; i++)
        {
            // Para simplificar, asumimos que el rango de cada bucket es 2^index
            // Es decir, el bucket 0 tiene nodos con distancia [0, 2^0), 
            // el bucket 1 tiene [2^0, 2^1), etc.
            if (distance < (1UL << i))
            {
                return i;
            }
        }
        return -1; // Si no se encuentra un bucket adecuado, retorna -1
    }

    internal List<NodeData> GetClosestNodes(string publicKey)
    {
        throw new NotImplementedException();
    }
}
