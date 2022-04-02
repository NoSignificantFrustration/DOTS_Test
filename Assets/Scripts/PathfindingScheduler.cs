using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

public class PathfindingScheduler : MonoBehaviour
{

    public PathfindingVolume pathfindingVolume { get; private set; }
    public NativeArray<GridCell> grid { get; private set; }
    public NativeBitArray gridTraversableArray { get; private set; }
    

    void Start()
    {
        if (TryGetComponent<PathfindingVolume>(out PathfindingVolume pv))
        {
            pathfindingVolume = pv;
            if (grid.IsCreated)
            {
                grid.Dispose();
            }
            grid = new NativeArray<GridCell>(pathfindingVolume.grid, Allocator.Persistent);

            if (gridTraversableArray.IsCreated)
            {
                gridTraversableArray.Dispose();
            }

            gridTraversableArray = new NativeBitArray(grid.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < gridTraversableArray.Length; i++)
            {
                gridTraversableArray.Set(i, pathfindingVolume.gridTraversableArray[i]);
            }
            pathfindingVolume.refreshGridTraversableArrayEvent.AddListener(RefreshGridTraversableArray);
        }
        else
        {
            Debug.LogWarning(gameObject.name + " lacks a PathfindingVolume");
        }
    }

    public List<int> GetGridPath(int2 startPos, int2 endPos)
    {


        GridPathfingigJob job = new GridPathfingigJob();
        job.grid = grid;
        job.gridSize = new int2(pathfindingVolume.gridSize.x, pathfindingVolume.gridSize.y);
        job.gridTraversableArray = gridTraversableArray;
        job.startPos = startPos;
        job.endPos = endPos;
        job.path = new NativeList<int>(Allocator.TempJob);

        job.workingGrid = new NativeArray<GridCell>(grid.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        job.openHeap = new NativeArray<int>(grid.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        job.openHashset =  new NativeHashSet<int>(grid.Length, Allocator.TempJob);
        job.closedSet = new NativeHashSet<int>(grid.Length, Allocator.TempJob);
        job.heapIndexes = new NativeArray<int>(grid.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);



        JobHandle handle = job.Schedule();
        handle.Complete();



        job.workingGrid.Dispose();
        job.openHeap.Dispose();
        job.openHashset.Dispose();
        job.closedSet.Dispose();
        job.heapIndexes.Dispose();



        List<int> path = new List<int>();
        for (int i = job.path.Length - 1; i > -1; i--)
        {
            path.Add(job.path[i]);
        }
        job.path.Dispose();


        return path;
    }

    private void RefreshGridTraversableArray(Vector2Int bottomLeft, Vector2Int topRight)
    {
        for (int y = bottomLeft.y; y < topRight.x; y++)
        {
            for (int x = bottomLeft.x; x < topRight.y; x++)
            {
                int arrayPos = y * pathfindingVolume.gridSize.x + x;
                gridTraversableArray.Set(arrayPos, pathfindingVolume.gridTraversableArray[arrayPos]);
            }
        }
        //Debug.Log("Refresh: " + bottomLeft + " " + topRight);
    }


    private void OnDisable()
    {
        if (grid.IsCreated)
        {
            grid.Dispose();
        }
        if (gridTraversableArray.IsCreated)
        {
            gridTraversableArray.Dispose();
        }
        pathfindingVolume.refreshGridTraversableArrayEvent.RemoveListener(RefreshGridTraversableArray);
    }
}

[BurstCompile]
public struct GridPathfingigJob : IJob
{
    [ReadOnly]
    public NativeArray<GridCell> grid;
    [ReadOnly]
    public int2 gridSize;
    [ReadOnly]
    public NativeBitArray gridTraversableArray;
    [ReadOnly]
    public int2 startPos;
    [ReadOnly]
    public int2 endPos;

    public NativeArray<GridCell> workingGrid;
    public NativeArray<int> openHeap;
    public int currentLength;
    public NativeHashSet<int> openHashset;
    public NativeHashSet<int> closedSet;
    public NativeArray<int> heapIndexes;

    public NativeList<int> path;

    public void Execute()
    {

    

        currentLength = 0;

        for (int i = 0; i < workingGrid.Length; i++)
        {
            workingGrid[i] = grid[i];
        }
        

        int startCell = GridposToArrayPos(startPos);
        int endCell = GridposToArrayPos(endPos);

        AddHeapItem(startCell);
        openHashset.Add(startCell);


        int lovestH = int.MaxValue;
        int lovestHIndex = 0;

        int current = -1;

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

            NativeList<int> neighbours = GetNeighbourFlatIndexes(workingGrid[currentCell].gridPos);

            for (int i = 0; i < neighbours.Length; i++)
            {
                int index = neighbours[i];
                
                if (!gridTraversableArray.IsSet(index) || closedSet.Contains(index))
                {

                    continue;
                }


                bool contains = openHashset.Contains(index);

                int newMovementCostToNeighbour = workingGrid[currentCell].gCost + GetDistance(workingGrid[currentCell].gridPos, workingGrid[index].gridPos);
                if (newMovementCostToNeighbour < workingGrid[index].gCost || !contains)
                {
                    GridCell cell = workingGrid[index];
                    cell.gCost = newMovementCostToNeighbour;
                    cell.hCost = GetDistance(cell.gridPos, workingGrid[endCell].gridPos);
                    cell.parentIndex = currentCell;
                    workingGrid[index] = cell;

                    if (!contains)
                    {
                        AddHeapItem(index);
                        openHashset.Add(index);
                        if (workingGrid[index].hCost < lovestH)
                        {
                            lovestH = workingGrid[index].hCost;
                            lovestHIndex = index;
                        }
                    }
                }

            }
            neighbours.Dispose();
        }

        if (current != endCell)
        {
            current = lovestHIndex;
        }




        while (current != startCell)
        {
            path.Add(current);
            current = workingGrid[current].parentIndex;
        }
        path.Add(startCell);
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
                    if (workingGrid[openHeap[childIndexRight]].fCost < workingGrid[openHeap[childIndexLeft]].fCost)
                    {
                        swapIndex = childIndexRight;
                    }
                }


                if (workingGrid[openHeap[swapIndex]].fCost < workingGrid[openHeap[heapIndex]].fCost)
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

            if (workingGrid[openHeap[parentIndex]].fCost > workingGrid[openHeap[heapIndex]].fCost)
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

    public int GridposToArrayPos(int2 gridPos)
    {
        return gridPos.y * gridSize.x + gridPos.x;
    }

    public NativeList<int> GetNeighbourFlatIndexes(int2 position)
    {
        NativeList<int> nList = new NativeList<int>(Allocator.Temp);
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }
                int2 pos = new int2(position.x + x, position.y + y);

                if (pos.x < 0 || pos.y < 0 || pos.x > gridSize.x - 1 || pos.y > gridSize.y - 1)
                {
                    continue;
                }

                nList.Add(pos.y * gridSize.x + pos.x);
            }
        }
        return nList;
    }

    private int GetDistance(int2 A, int2 B)
    {
        int distX = Mathf.Abs(A.x - B.x);
        int distY = Mathf.Abs(A.y - B.y);

        if (distX > distY)
        {
            return 14 * distY + 10 * (distX - distY);
        }
        else
        {
            return 14 * distX + 10 * (distY - distX);
        }
    }
}

[System.Serializable]
public class RefreshRangeEvent : UnityEvent<Vector2Int, Vector2Int>
{

}