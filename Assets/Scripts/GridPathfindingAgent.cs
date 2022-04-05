using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridPathfindingAgent : PathfindingAgent
{

    protected override void Awake()
    {
        base.Awake();
    }

    // Start is called before the first frame update
    override protected void Start()
    {
        base.Start();
        
        
    }

    // Update is called once per frame
    override protected void Update()
    {
        base.Update();
        //Debug.Log(pathfindingCooldown);
        if (!waitingForPath)
        {
            RequestPath();
        }
        
        
        EvaluateDirection();
        //if (pathIndexes != null)
        //{
        //    //Debug.Log(Vector2.Distance((Vector2)transform.position, nextGoal));
        //    Debug.Log(nextGoal);
        //}

    }

    protected override void RequestPath()
    {

        if (pathfindingCooldown > 0 || waitingForPath)
        {
            return;
        }
        pathfindingCooldown = defaultPathfindingCooldown;
        if (Vector2.Distance(transform.position, endGoal) <= pathPositionReachedTreshold)
        {
            pathfindingCooldown = defaultPathfindingCooldown;
            return;
        }
        
        endGoal = entityManager.pathfindingVolume.target.position;
        waitingForPath = true;
        arrivedOnDestination = false;

        PathfindingRequest<GridPathfindingJob> request = new PathfindingRequest<GridPathfindingJob>();
        request.startPos = entityManager.pathfindingVolume.worldToGridPos(transform.position);
        request.endPos = entityManager.pathfindingVolume.worldToGridPos(endGoal);
        request.callback = pathReceivedAction;

        entityManager.pathfindingScheduler.RequestPath(request);
        Debug.Log("Requested path");
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (pathIndexes != null)
        {

            for (int i = currentIndex; i < pathIndexes.Count; i++)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(entityManager.pathfindingVolume.positionArray[pathIndexes[i]], 0.3f);
                //Debug.Log(pathIndexes[i] + " i: " + i);
            }
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(endGoal, 0.3f);
        }
    }

    protected override Vector2 GetNextPathPosition()
    {
        return entityManager.pathfindingVolume.positionArray[pathIndexes[currentIndex]];
    }

    protected override void OnPathPointReached()
    {
        Debug.Log("Path point reached");
    }
}
