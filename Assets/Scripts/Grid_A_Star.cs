using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid_A_Star : MonoBehaviour
{

    private PathfindingVolume pathfindingVolume;

    private void Awake()
    {
        pathfindingVolume = GetComponent<PathfindingVolume>();
    }

    public List<int> FindGridPath(Vector3 startPos, Vector3 endPos)
    {
        GridCell[] grid = (GridCell[])pathfindingVolume.grid.Clone();

        int startCell = pathfindingVolume.GridposToArrayPos(pathfindingVolume.worldToGridPos(startPos));
        int endCell = pathfindingVolume.GridposToArrayPos(pathfindingVolume.worldToGridPos(endPos));

        int currentLength = 0;
        int[] openHeap = new int[pathfindingVolume.grid.Length];

        List<int> openSet = new List<int>();
        HashSet<int> closedSet = new HashSet<int>();

        openSet.Add(startCell);

        openHeap[currentLength] = startCell;
        currentLength++;

        int current = -1;
        List<int> path = new List<int>();

        if (!pathfindingVolume.traversableArray[endCell])
        {
            return path;
        }

        while (openSet.Count > 0)
        {

            int currentCell = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (grid[openSet[i]].fCost < grid[currentCell].fCost || grid[openSet[i]].fCost == grid[currentCell].fCost && grid[openSet[i]].hCost < grid[currentCell].hCost)
                {
                    currentCell = openSet[i];
                }
            }
            openSet.Remove(currentCell);
            closedSet.Add(currentCell);

            if (currentCell.Equals(endCell))
            {
                current = currentCell;
                break;
            }

            foreach (int index in pathfindingVolume.GetNeighbourFlatIndexes(grid[currentCell].gridPos))
            {
                try
                {
                    //Debug.Log(index);
                    if (!pathfindingVolume.traversableArray[index] || closedSet.Contains(index))
                    {

                        continue;
                    }
                }
                catch (System.Exception)
                {
                    Debug.LogError(grid[currentCell].gridPos + " " + index);
                    throw;
                }
                

                int newMovementCostToNeighbour = grid[currentCell].gCost + GetDistance(grid[currentCell], grid[index]);
                if (newMovementCostToNeighbour < grid[index].gCost || !openSet.Contains(index))
                {
                    grid[index].gCost = newMovementCostToNeighbour;
                    grid[index].hCost = GetDistance(grid[index], grid[endCell]);
                    grid[index].parentIndex = pathfindingVolume.GridposToArrayPos(grid[currentCell].gridPos);

                    if (!openSet.Contains(index))
                    {
                        openSet.Add(index);
                    }
                }

            }
        }

        if (current != endCell)
        {
            return path;
        }

        current = endCell;

        while (current != startCell)
        {
            path.Add(current);
            current = grid[current].parentIndex;
        }
        path.Reverse();
        //Debug.Log(path.Count);
        return path;
    }



    private int GetDistance(GridCell A, GridCell B)
    {
        int distX = Mathf.Abs(A.gridPos.x - B.gridPos.x);
        int distY = Mathf.Abs(A.gridPos.y - B.gridPos.y);

        if (distX > distY)
        {
            return 14 * distY + 10 * (distX - distY);
        }
        else
        {
            return 14 * distX + 10 * (distY - distX);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}


