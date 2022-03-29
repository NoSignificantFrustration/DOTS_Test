using System.Collections.Generic;
using UnityEngine;

public class Grid_A_Star : MonoBehaviour
{

    private PathfindingVolume pathfindingVolume;
    [HideInInspector] public GridCell[] grid;

    private int[] openHeap;
    private int currentLength;
    private HashSet<int> openHashset;
    private int[] heapIndexes;
    private void Awake()
    {
        pathfindingVolume = GetComponent<PathfindingVolume>();

    }

    public List<int> FindGridPath(Vector3 startPos, Vector3 endPos)
    {
        
        //grid = (GridCell[])pathfindingVolume.grid.Clone();
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();


        int startCell = pathfindingVolume.GridposToArrayPos(pathfindingVolume.worldToGridPos(startPos));
        int endCell = pathfindingVolume.GridposToArrayPos(pathfindingVolume.worldToGridPos(endPos));

        currentLength = 0;
        openHeap = new int[pathfindingVolume.grid.Length];
        openHashset = new HashSet<int>();
        heapIndexes = new int[pathfindingVolume.grid.Length];

        HashSet<int> closedSet = new HashSet<int>();

        AddHeapItem(startCell);
        openHashset.Add(startCell);


        int lovestH = int.MaxValue;
        int lovestHIndex = 0;

        int current = -1;
        List<int> path = new List<int>();

        if (!pathfindingVolume.traversableArray[endCell])
        {
            return path;
        }

        while (currentLength > 0)
        {

            int currentCell = RemoveFirst();
            

            openHashset.Remove(currentCell);
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


                bool contains = openHashset.Contains(index);

                int newMovementCostToNeighbour = grid[currentCell].gCost + GetDistance(grid[currentCell], grid[index]);
                if (newMovementCostToNeighbour < grid[index].gCost || !contains)
                {
                    grid[index].gCost = newMovementCostToNeighbour;
                    grid[index].hCost = GetDistance(grid[index], grid[endCell]);
                    grid[index].parentIndex = currentCell;
                    //SortUp(heapIndexes[index]);

                    if (!contains)
                    {
                        AddHeapItem(index);
                        openHashset.Add(index);
                        if (grid[index].hCost < lovestH)
                        {
                            lovestH = grid[index].hCost;
                            lovestHIndex = index;
                        }
                    }
                }

            }
        }

        if (current != endCell)
        {
            current = lovestHIndex;
        }
       

        

        while (current != startCell)
        {
            path.Add(current);
            current = grid[current].parentIndex;
        }
        path.Reverse();
        //Debug.Log(path.Count);
        sw.Stop();
        Debug.Log(sw.ElapsedMilliseconds);
        Debug.Log("Length: " + path.Count);
        return path;
    }

    private void AddHeapItem(int gridIndex)
    {
        openHeap[currentLength] = gridIndex;
        heapIndexes[gridIndex] = currentLength; 
        SortUp(currentLength);
        currentLength++;
    }

    private int RemoveFirst()
    {
        int firstItem = openHeap[0];
        currentLength--;
        openHeap[0] = openHeap[currentLength];

        SortDown(0);

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
                

                if (grid[openHeap[swapIndex]].fCost < grid[openHeap[heapIndex]].fCost)
                {
                    int temp = openHeap[swapIndex];
                    openHeap[swapIndex] = openHeap[heapIndex];
                    openHeap[heapIndex] = temp;

                    int heapIndexTemp = heapIndexes[openHeap[swapIndex]];
                    heapIndexes[openHeap[swapIndex]] = heapIndexes[openHeap[heapIndex]];
                    heapIndexes[openHeap[heapIndex]] = heapIndexTemp;

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

            if (grid[openHeap[parentIndex]].fCost > grid[openHeap[heapIndex]].fCost)
            {
                int temp = openHeap[parentIndex];              
                openHeap[parentIndex] = openHeap[heapIndex];
                openHeap[heapIndex] = temp;

                int heapIndexTemp = heapIndexes[openHeap[parentIndex]];
                heapIndexes[openHeap[parentIndex]] = heapIndexes[openHeap[heapIndex]];
                heapIndexes[openHeap[heapIndex]] = heapIndexTemp;

                heapIndex = parentIndex;
            }
            else
            {
                break;
            }

            parentIndex = (heapIndex - 1) / 2;
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


