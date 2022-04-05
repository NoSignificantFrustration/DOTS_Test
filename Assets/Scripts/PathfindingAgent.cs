using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public abstract class PathfindingAgent : MonoBehaviour
{
    [field: SerializeField, Min(0)] public float pathfindingCooldown { get; protected set;}
    [field: SerializeField, Min(0.1f)] public float pathPositionReachedTreshold { get; protected set; }

    protected EntityManager entityManager;
    protected Action<List<int>, bool> pathReceivedAction;
    protected List<int> pathPositions;
    protected int currentIndex;
    protected Vector2 nextGoal;
    protected Vector2 endGoal;
    protected bool waitingForPath = false;
    protected bool arrivedOnDestination = true;

    public Vector2 direction { get; protected set; }

    protected virtual void Awake()
    {
        pathReceivedAction += OnPathReceived;
    }

    // Start is called before the first frame update
    protected virtual void Start()
    {
        RequestPath();
    }

    // Update is called once per frame
    protected virtual void Update() { }

    protected virtual void OnPathReceived(List<int> path, bool success) 
    {
        pathPositions = path;
        currentIndex = 0;
        waitingForPath = false;
        arrivedOnDestination = false;
    }

    protected virtual void EvaluateDirection()
    {

        if (arrivedOnDestination)
        {
            direction = Vector2.zero;
            return;
        }

        if (Vector2.Distance(transform.position, nextGoal) <= pathPositionReachedTreshold)
        {
            if (endGoal.Equals(nextGoal))
            {
                currentIndex = -1;
                direction = Vector2.zero;
                arrivedOnDestination = true;
                return;
            }

            currentIndex++;

            if (currentIndex == pathPositions.Count)
            {
                nextGoal = endGoal;
            }
            else
            {
                nextGoal = entityManager.pathfindingVolume.positionArray[pathPositions[currentIndex]];
            }

            direction = ((Vector2)transform.position - nextGoal).normalized;
        }
     
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (pathPositions != null)
        {
            
            for (int i = currentIndex; i < pathPositions.Count; i++)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(entityManager.pathfindingVolume.positionArray[pathPositions[i]], 0.3f);
            }
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(endGoal, 0.3f);
        }
    }

    protected virtual void RequestPath()
    {
        if (waitingForPath)
        {
            Debug.LogWarning(gameObject.name + " wanted to request a path when it already requested one");
            return;
        }



    }
    
}
