using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeComponent 
{
    protected Node parentNode;
    public virtual void Initialize(Node parent) 
    {
        parentNode = parent;
    }
}
