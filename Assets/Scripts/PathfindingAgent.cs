using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PathfindingAgent : MonoBehaviour
{
    [field: SerializeField, Min(0)] public float pathfindingCooldown { get; protected set;}

    protected EntityManager entityManager;
    protected Action<List<int>, bool> pathReceivedAction;
    protected List<int> pathIndexes;
    

    public Vector2 direction { get; protected set; }

    protected virtual void Awake()
    {
        pathReceivedAction += OnPathReceived;
    }

    // Start is called before the first frame update
    protected virtual void Start()
    {
        
    }

    // Update is called once per frame
    protected virtual void Update() { }

    protected virtual void OnPathReceived(List<int> path, bool success) 
    { 
        
    }
}
