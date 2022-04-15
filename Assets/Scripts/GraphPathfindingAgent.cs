using UnityEngine;

public class GraphPathfindingAgent : PathfindingAgent
{




    protected override void OnPathPointReached()
    {

    }

    public override bool RequestPath(Vector2 goal)
    {
        if (pathfindingCooldown > 0 || waitingForPath)
        {
            return false;
        }
        pathfindingCooldown = defaultPathfindingCooldown;
        endGoal = goal;
        if (Vector2.Distance(transform.position, endGoal) <= pathPositionReachedTreshold)
        {
            pathfindingCooldown = defaultPathfindingCooldown;
            return true;
        }

        waitingForPath = true;
        arrivedOnDestination = true;

        PathfindingRequest<GraphPathfindingJob> request = new PathfindingRequest<GraphPathfindingJob>();
        request.startPos = entityManager.pathfindingVolume.worldToGridPos(transform.position);
        request.endPos = entityManager.pathfindingVolume.worldToGridPos(endGoal);
        request.callback = pathReceivedAction;

        entityManager.pathfindingScheduler.RequestPath(request);
        return true;
    }

    protected virtual void OnDrawGizmosSelected()
    {

        if (pathIndexes != null)
        {
            Gizmos.color = Color.red;
            for (int i = currentIndex; i < pathIndexes.Count; i++)
            {

                Gizmos.DrawSphere(entityManager.pathfindingVolume.navNodePositionArray[pathIndexes[i]], 0.3f);
                //Debug.Log(pathIndexes[i] + " i: " + i);
            }

            if (pathIndexes.Count > 0 && currentIndex < pathIndexes.Count)
            {
                Gizmos.DrawLine(entityManager.pathfindingVolume.navNodePositionArray[pathIndexes[currentIndex]], (Vector2)transform.position);
                if (pathIndexes.Count > 1)
                {
                    //Debug.Log(currentIndex);
                    for (int i = currentIndex; i < pathIndexes.Count - 1; i++)
                    {
                        Gizmos.DrawLine(entityManager.pathfindingVolume.navNodePositionArray[pathIndexes[i + 1]], entityManager.pathfindingVolume.navNodePositionArray[pathIndexes[i]]);
                    }

                }
                if (hasPath)
                {
                    Gizmos.DrawLine(entityManager.pathfindingVolume.navNodePositionArray[pathIndexes[pathIndexes.Count - 1]], endGoal);
                }

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

    protected override Vector2 GetNextPathPosition()
    {
        return entityManager.pathfindingVolume.navNodePositionArray[pathIndexes[currentIndex]];
    }
}
