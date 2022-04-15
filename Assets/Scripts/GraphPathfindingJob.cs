using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct GraphPathfindingJob : IJob
{

    [ReadOnly]
    public NativeArray<NavNodeInfo> navNodeInfos;
    [ReadOnly]
    [NativeDisableContainerSafetyRestriction]
    public NativeBitArray navNodeTraversableArray;
    [ReadOnly]
    public NativeMultiHashMap<int, int> groundGroupMap;
    [ReadOnly]
    public NativeMultiHashMap<int, GraphConnectionInfo> graphConnectionInfos;
    [ReadOnly]
    public int2 startPos;
    [ReadOnly]
    public int2 endPos;
    [ReadOnly]
    public int startGroup;
    [ReadOnly]
    public int endGroup;


    public NativeArray<NavNodeInfo> workingNavNodeInfos;
    public NativeArray<int> openHeap;
    public int currentLength;
    public NativeHashSet<int> openHashset;
    public NativeHashSet<int> closedSet;
    public NativeArray<int> heapIndexes;

    public NativeList<int> path;
    public NativeArray<int> success;

    public void Execute()
    {


        for (int i = 0; i < navNodeInfos.Length; i++)
        {
            workingNavNodeInfos[i] = navNodeInfos[i];
        }


        int startNode = 0;
        int endNode = -2;
        int closestNode = 0;

        using (NativeMultiHashMap<int, int>.Enumerator nodes = groundGroupMap.GetValuesForKey(startGroup))
        {

            if (!nodes.MoveNext())
            {
                success[0] = 0;
                return;
            }
            bool found = false;

            int currentNode = nodes.Current;
            startNode = currentNode;
            float minDist = GetDistance(startPos, workingNavNodeInfos[currentNode].gridPos);

            if (navNodeTraversableArray.IsSet(currentNode))
            {
                found = true;
            }


            while (nodes.MoveNext())
            {
                currentNode = nodes.Current;

                if (!navNodeTraversableArray.IsSet(currentNode))
                {
                    continue;
                }



                float currentDist = GetDistance(startPos, workingNavNodeInfos[currentNode].gridPos);

                if (minDist > currentDist || !found)
                {
                    minDist = currentDist;
                    startNode = currentNode;
                    found = true;
                }


            }

            if (!found)
            {
                success[0] = 0;
                return;
            }

        }

        if (endGroup > 0)
        {
            using (NativeMultiHashMap<int, int>.Enumerator nodes = groundGroupMap.GetValuesForKey(endGroup))
            {

                if (!nodes.MoveNext())
                {
                    success[0] = 0;
                    return;
                }

                bool found = false;

                int currentNode = nodes.Current;
                endNode = currentNode;
                float minValidDist = GetDistance(endPos, workingNavNodeInfos[currentNode].gridPos);

                closestNode = nodes.Current;
                float minDist = minValidDist;


                if (navNodeTraversableArray.IsSet(currentNode))
                {
                    found = true;
                }

                while (nodes.MoveNext())
                {

                    currentNode = nodes.Current;

                    float currentDist = GetDistance(endPos, workingNavNodeInfos[currentNode].gridPos);

                    if (minDist > currentDist)
                    {
                        minDist = currentDist;
                        closestNode = currentNode;
                    }


                    if (!navNodeTraversableArray.IsSet(currentNode))
                    {
                        continue;
                    }

                    if (minValidDist > currentDist || !found)
                    {
                        minValidDist = currentDist;
                        endNode = currentNode;
                        found = true;

                        minDist = currentDist;
                        closestNode = currentNode;
                    }
                }

                if (!found)
                {
                    endNode = closestNode;
                }
            }
            //endPos = navNodeInfos[endNode].gridPos;
        }


        


        currentLength = 0;


        AddHeapItem(startNode);
        openHashset.Add(startNode);


        int lowestWeighedF = GetDistance(navNodeInfos[startNode].gridPos, endPos) * 10;
        int lowestWeighedFIndex = startNode;

        int current = -1;



        while (currentLength > 0)
        {

            

            int currentNode = RemoveFirst();



            openHashset.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode.Equals(endNode))
            {
                current = currentNode;
                success[0] = 1;
                break;
            }

            using (NativeMultiHashMap<int, GraphConnectionInfo>.Enumerator connections = graphConnectionInfos.GetValuesForKey(currentNode))
            {

                while (connections.MoveNext())
                {
                    if (!navNodeTraversableArray.IsSet(connections.Current.targetID) || closedSet.Contains(connections.Current.targetID))
                    {

                        continue;
                    }

                    bool contains = openHashset.Contains(connections.Current.targetID);

                    int newMovementCostToNeighbour = workingNavNodeInfos[currentNode].gCost + GetDistance(workingNavNodeInfos[currentNode].gridPos, workingNavNodeInfos[connections.Current.targetID].gridPos);
                    if (newMovementCostToNeighbour < workingNavNodeInfos[connections.Current.targetID].gCost || !contains)
                    {
                        NavNodeInfo info = workingNavNodeInfos[connections.Current.targetID];
                        info.gCost = newMovementCostToNeighbour;
                        info.hCost = GetDistance(workingNavNodeInfos[connections.Current.targetID].gridPos, endPos);
                        info.parentIndex = currentNode;
                        workingNavNodeInfos[connections.Current.targetID] = info;


                        int currentCost = workingNavNodeInfos[connections.Current.targetID].gCost + workingNavNodeInfos[connections.Current.targetID].hCost * 10;

                        if (currentCost < lowestWeighedF)
                        {
                            lowestWeighedF = currentCost;
                            lowestWeighedFIndex = connections.Current.targetID;

                            //Debug.Log(lowestF);
                            //Debug.Log(lowestFIndex);
                        }

                        //Debug.Log(workingNavNodeInfos[connections.Current.targetID].fCost);
                        //Debug.Log(connections.Current.targetID);

                        if (!contains)
                        {
                            AddHeapItem(connections.Current.targetID);
                            openHashset.Add(connections.Current.targetID);

                        }
                    }
                }
            }


        }





        if (current != endNode)
        {
            //if (lowestF >= GetDistance(workingNavNodeInfos[startNode].gridPos, endPos))
            //{
            //    lovestFIndex = startNode;
            //}
            current = lowestWeighedFIndex;
            endNode = current;
            success[0] = 0;
        }
        //Debug.Log(lowestF);
        //Debug.Log(current);
        while (current != startNode)
        {
            path.Add(current);
            current = workingNavNodeInfos[current].parentIndex;
        }


        if (path.Length > 0)
        {
            if (workingNavNodeInfos[startNode].groundGroup == workingNavNodeInfos[path[path.Length - 1]].groundGroup)
            {
                int startNodeToLast = GetDistance(workingNavNodeInfos[startNode].gridPos, workingNavNodeInfos[path[path.Length - 1]].gridPos);
                int startNodeToStartPos = GetDistance(workingNavNodeInfos[path[path.Length - 1]].gridPos, startPos);
                if (startNodeToLast < startNodeToStartPos)
                {
                    path.Add(startNode);
                }
            }
            else
            {
                path.Add(startNode);
            }
        }
        else
        {
            path.Add(startNode);
        }
        if (path.Length > 1 && startNode != endNode && success[0] == 1)
        {
            if (workingNavNodeInfos[endNode].groundGroup == workingNavNodeInfos[path[1]].groundGroup)
            {
                int endNodeToBeforeLast = GetDistance(workingNavNodeInfos[endNode].gridPos, workingNavNodeInfos[path[1]].gridPos);
                int endNodeToendPos = GetDistance(workingNavNodeInfos[path[1]].gridPos, endPos);

                if (endNodeToBeforeLast > endNodeToendPos)
                {
                    path.RemoveAt(0);
                }
            }

        }
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
                    if (workingNavNodeInfos[openHeap[childIndexRight]].fCost < workingNavNodeInfos[openHeap[childIndexLeft]].fCost)
                    {
                        swapIndex = childIndexRight;
                    }
                }


                if (workingNavNodeInfos[openHeap[swapIndex]].fCost < workingNavNodeInfos[openHeap[heapIndex]].fCost)
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

            if (workingNavNodeInfos[openHeap[parentIndex]].fCost > workingNavNodeInfos[openHeap[heapIndex]].fCost)
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
