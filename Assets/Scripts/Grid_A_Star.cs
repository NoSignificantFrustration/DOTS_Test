using System.Collections.Generic;
using UnityEngine;

public class Grid_A_Star : MonoBehaviour
{

    private PathfindingVolume pathfindingVolume;
    private GridCell[] grid;

    private int[] openHeap;
    private int currentLength;

    private void Awake()
    {
        pathfindingVolume = GetComponent<PathfindingVolume>();

    }

    public List<int> FindGridPath(Vector3 startPos, Vector3 endPos)
    {

        grid = (GridCell[])pathfindingVolume.grid.Clone();
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        

        int startCell = pathfindingVolume.GridposToArrayPos(pathfindingVolume.worldToGridPos(startPos));
        int endCell = pathfindingVolume.GridposToArrayPos(pathfindingVolume.worldToGridPos(endPos));

        currentLength = 0;
        openHeap = new int[pathfindingVolume.grid.Length];

        HashSet<int> closedSet = new HashSet<int>();


        openHeap[0] = startCell;
        currentLength++;

        int current = -1;
        List<int> path = new List<int>();

        if (!pathfindingVolume.traversableArray[endCell])
        {
            return path;
        }

        while (currentLength > 0)
        {

            int currentCell = RemoveFirst();


            closedSet.Add(currentCell);

            if (currentCell.Equals(endCell))
            {
                current = currentCell;
                break;
            }

            foreach (int index in pathfindingVolume.GetNeighbourFlatIndexes(grid[currentCell].gridPos))
            {

                //Debug.Log(index);
                if (!pathfindingVolume.traversableArray[index] || closedSet.Contains(index))
                {

                    continue;
                }


                bool contains = false;
                sw.Start();
                for (int i = 0; i < openHeap.Length; i++)
                {
                    if (openHeap[i] == index)
                    {
                        contains = true;
                        break;
                    }
                }
                sw.Stop();
                int newMovementCostToNeighbour = grid[currentCell].gCost + GetDistance(grid[currentCell], grid[index]);
                if (newMovementCostToNeighbour < grid[index].gCost || !contains)
                {
                    grid[index].gCost = newMovementCostToNeighbour;
                    grid[index].hCost = GetDistance(grid[index], grid[endCell]);
                    grid[index].parentIndex = pathfindingVolume.GridposToArrayPos(grid[currentCell].gridPos);

                    if (!contains)
                    {
                        AddHeapItem(index);
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
        Debug.Log(sw.ElapsedMilliseconds);
        return path;
    }

    private void AddHeapItem(int gridIndex)
    {
        openHeap[currentLength] = gridIndex;
        SortUp(currentLength);
        currentLength++;
    }

    private int RemoveFirst()
    {
        int firstItem = openHeap[0];
        currentLength--;
        openHeap[0] = openHeap[currentLength];

        SortDown(openHeap[0]);

        return firstItem;
    }

    private void SortDown(int heapIndex)
    {
        while (true)
        {
            int childIndexLeft = heapIndex * 2 + 1;
            int childIndexRight = heapIndex * 2 + 2;

            int swapIndex = 0;

            if (childIndexLeft < currentLength)
            {
                swapIndex = childIndexLeft;

                if (childIndexRight < currentLength)
                {
                    if (grid[openHeap[childIndexRight]].fCost < grid[openHeap[childIndexLeft]].fCost)
                    {
                        swapIndex = childIndexRight;
                    }
                }
                else
                {
                    return;
                }

                if (grid[openHeap[swapIndex]].fCost < grid[openHeap[heapIndex]].fCost)
                {
                    int temp = openHeap[swapIndex];
                    openHeap[swapIndex] = openHeap[heapIndex];
                    openHeap[heapIndex] = temp;
                    heapIndex = swapIndex;
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }
    }

    private void SortUp(int heapIndex)
    {
        int parentIndex = (heapIndex - 1) / 2;

        while (true)
        {

            if (grid[openHeap[parentIndex]].fCost < grid[openHeap[heapIndex]].fCost)
            {
                int temp = openHeap[parentIndex];
                openHeap[parentIndex] = openHeap[heapIndex];
                openHeap[heapIndex] = temp;
            }
            else
            {
                break;
            }

            parentIndex = (parentIndex - 1) / 2;
        }
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


