using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct GridPathfindingJob : IJob
{
    [ReadOnly]
    public NativeArray<GridCell> grid;
    [ReadOnly]
    public int2 gridSize;
    [ReadOnly]
    [NativeDisableContainerSafetyRestriction]
    public NativeBitArray gridTraversableArray;
    [ReadOnly]
    public int2 startPos;
    [ReadOnly]
    public int2 endPos;

    public NativeArray<GridCell> workingGrid;
    public NativeArray<int> openHeap;
    public int currentLength;
    public NativeBitArray openSet;
    public NativeBitArray closedSet;
    public NativeArray<int> heapIndexes;

    public NativeList<int> path;
    public NativeArray<int> success;

    public void Execute()
    {



        currentLength = 0;

        //for (int i = 0; i < workingGrid.Length; i++)
        //{
        //    workingGrid[i] = grid[i];
        //}


        openSet.SetBits(0, false, workingGrid.Length);
        closedSet.SetBits(0, false, workingGrid.Length);



        int startCell = GridposToArrayPos(startPos);
        int endCell = GridposToArrayPos(endPos);

        AddHeapItem(startCell);
        openSet.Set(startCell, true);
        workingGrid[startCell] = grid[startCell];

        int lovestH = int.MaxValue;
        int lovestHIndex = startCell;

        int current = startCell;

        while (currentLength > 0)
        {

            int currentCell = RemoveFirst();



            openSet.Set(currentCell, false);
            closedSet.Set(currentCell, true);

            if (currentCell == endCell)
            {
                current = currentCell;
                break;
            }

            NativeList<int> neighbours = GetNeighbourFlatIndexes(workingGrid[currentCell].gridPos);

            for (int i = 0; i < neighbours.Length; i++)
            {
                int index = neighbours[i];

                if (!gridTraversableArray.IsSet(index) || closedSet.IsSet(index))
                {
                    continue;
                }

                bool contains = openSet.IsSet(index);

                int2 direction = grid[index].gridPos - grid[currentCell].gridPos;

                if (math.abs(direction.x) + math.abs(direction.y) == 2)
                {
                    int2 currentPos = grid[currentCell].gridPos;
                    if (!gridTraversableArray.IsSet(GridposToArrayPos(new int2(currentPos.x + direction.x, currentPos.y))) || !gridTraversableArray.IsSet(GridposToArrayPos(new int2(currentPos.x, currentPos.y + direction.y))))
                    {

                        continue;
                    }
                }


                int newMovementCostToNeighbour = workingGrid[currentCell].gCost + GetDistance(workingGrid[currentCell].gridPos, grid[index].gridPos);
                if (newMovementCostToNeighbour < workingGrid[index].gCost || !contains)
                {
                    GridCell cell = grid[index];
                    cell.gCost = newMovementCostToNeighbour;
                    cell.hCost = GetDistance(cell.gridPos, grid[endCell].gridPos);
                    cell.parentIndex = currentCell;
                    workingGrid[index] = cell;

                    if (!contains)
                    {
                        AddHeapItem(index);
                        openSet.Set(index, true);
                        if (workingGrid[index].hCost < lovestH)
                        {
                            lovestH = workingGrid[index].hCost;
                            lovestHIndex = index;
                        }
                    }
                    else
                    {
                        SortUp(heapIndexes[index]);
                    }
                }

            }
            neighbours.Dispose();
        }

        if (current != endCell)
        {
            current = lovestHIndex;
            success[0] = 0;
        }
        else
        {
            success[0] = 1;
        }
        //Debug.Log("Current: " + current + " end: " + endCell + " success: " + success);
        if (current == startCell)
        {
            return;
        }

        


        int prevCell = current;
        int2 prevDir = new int2(2, 2);

        while (current != startCell)
        {
            
            int2 direction = workingGrid[prevCell].gridPos - workingGrid[current].gridPos;
            if (!prevDir.Equals(direction))
            {
                path.Add(prevCell);
                prevDir = direction;
            }
            prevCell = current;
            current = workingGrid[current].parentIndex;
            //Debug.Log(current);
        }

        //path.RemoveAt(0);
        //path.RemoveAt(0);
        //path.Add(startCell);


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
        int distX = math.abs(A.x - B.x);
        int distY = math.abs(A.y - B.y);

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
