using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridPathfindingAgent : PathfindingAgent
{


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
        arrivedOnDestination = true;

        PathfindingRequest<GridPathfindingJob> request = new PathfindingRequest<GridPathfindingJob>();
        request.startPos = entityManager.pathfindingVolume.worldToGridPos(transform.position);
        request.endPos = entityManager.pathfindingVolume.worldToGridPos(endGoal);
        request.callback = pathReceivedAction;

        entityManager.pathfindingScheduler.RequestPath(request);
        //Debug.Log("Requested path");
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (pathIndexes != null)
        {
            Gizmos.color = Color.red;
            for (int i = currentIndex; i < pathIndexes.Count; i++)
            {
                
                Gizmos.DrawSphere(entityManager.pathfindingVolume.positionArray[pathIndexes[i]], 0.3f);
                //Debug.Log(pathIndexes[i] + " i: " + i);
            }

            if (pathIndexes.Count > 0 && currentIndex < pathIndexes.Count )
            {
                Gizmos.DrawLine(entityManager.pathfindingVolume.positionArray[pathIndexes[currentIndex]], (Vector2)transform.position);
                if (pathIndexes.Count > 1)
                {
                    //Debug.Log(currentIndex);
                    for (int i = currentIndex; i < pathIndexes.Count - 1; i++)
                    {
                        Gizmos.DrawLine(entityManager.pathfindingVolume.positionArray[pathIndexes[i + 1]], entityManager.pathfindingVolume.positionArray[pathIndexes[i]]);
                    }
                    
                }
                Gizmos.DrawLine(entityManager.pathfindingVolume.positionArray[pathIndexes[pathIndexes.Count - 1]], endGoal);
            }
            else if (!arrivedOnDestination)
            {
                Gizmos.DrawLine(transform.position, endGoal);
            }

            if (!arrivedOnDestination)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(endGoal, 0.3f);
            }
            
        }
    }


    protected override void OnPathPointReached()
    {
        //Debug.Log("Path point reached");
    }
}
