using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class PathfindingAgent : MonoBehaviour
{
    [field: SerializeField, Min(0.2f)] public float defaultPathfindingCooldown { get; protected set; }
    [field: SerializeField, Min(0.2f)] public float pathPositionReachedTreshold { get; protected set; }

    public EntityManager entityManager;
    protected Action<List<int>, bool> pathReceivedAction;
    protected List<int> pathIndexes;
    protected int currentIndex;
    protected Vector2 nextGoal;
    protected Vector2 endGoal;
    protected bool waitingForPath = false;
    protected bool arrivedOnDestination = true;
    protected bool hasPath = false;
    protected float pathfindingCooldown;

    public Vector2 direction { get; protected set; }

    protected virtual void Awake()
    {
        pathfindingCooldown = defaultPathfindingCooldown;
        pathReceivedAction += OnPathReceived;
    }

    // Start is called before the first frame update
    protected virtual void Start()
    {

    }

    // Update is called once per frame
    protected virtual void Update()
    {
        pathfindingCooldown -= Time.deltaTime;

    }

    protected virtual void OnPathReceived(List<int> path, bool success)
    {
        pathIndexes = path;
        currentIndex = 0;
        waitingForPath = false;

        pathfindingCooldown = defaultPathfindingCooldown;
        
        
        if (path.Count == 0 && !success)
        {
            arrivedOnDestination = true;
        }
        else
        {
            if (path.Count == 0)
            {
                nextGoal = endGoal;
            }
            else
            {
                nextGoal = GetNextPathPosition();
            }
            
            arrivedOnDestination = false;
        }
        
        hasPath = success;



        //Debug.Log("Path received " + pathIndexes.Count + " Success: " + hasPath);
    }

    public virtual void EvaluateDirection()
    {

        if (arrivedOnDestination || pathIndexes == null)
        {
            direction = Vector2.zero;
            return;
        }

        if (Vector2.Distance(transform.position, nextGoal) <= pathPositionReachedTreshold)
        {
            if (endGoal.Equals(nextGoal))
            {
                //currentIndex = -1;
                direction = Vector2.zero;
                arrivedOnDestination = true;
                hasPath = false;
                return;
            }

            currentIndex++;

            if (currentIndex >= pathIndexes.Count)
            {
                if (hasPath)
                {
                    nextGoal = endGoal;
                }
                else
                {
                    direction = Vector2.zero;
                    arrivedOnDestination = true;
                    hasPath = false;
                    return;
                }
                
            }
            else
            {
                OnPathPointReached();
                nextGoal = GetNextPathPosition();
            }

            direction = ((Vector2)transform.position - nextGoal).normalized;
        }

    }



    public abstract bool RequestPath(Vector2 goal);

    protected abstract void OnPathPointReached();

    protected abstract Vector2 GetNextPathPosition();
}
