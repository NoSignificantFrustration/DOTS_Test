using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct GraphPathfindingJob : IJob
{

    [ReadOnly]
    public NativeArray<NavNodeInfo> navNodeInfos;
    [ReadOnly]
    public int2 gridSize;
    [ReadOnly]
    public NativeBitArray navNodeTraversableArray;
    [ReadOnly]
    NativeMultiHashMap<int, int> groundGroupMap;
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
    public void Execute()
    {

        

        //sw.Stop();
        //Debug.Log("Determine start and end ground groups: " + sw.ElapsedMilliseconds + "ms");
        //sw.Restart();

        int startNode = 0;
        int endNode = 0;
        int closestNode = 0;

        using (NativeMultiHashMap<int, int>.Enumerator nodes = groundGroupMap.GetValuesForKey(startGroup))
        {
            //Debug.Log("Start:" + +nodes.Current);
            if (!nodes.MoveNext())
            {
                return;
            }
            bool found = false;

            int currentNode = nodes.Current;
            startNode = currentNode;
            float minDist = Mathf.Abs((start.x - navNodeInfos[currentNode].worldPos.x) + (start.y - navNodeInfos[currentNode].worldPos.y));

            if (navNodeTraversableArray.IsSet(currentNode))
            {
                found = true;
            }
            //Debug.Log(navNodeInfos[currentNode].id);
            //Debug.Log("Curr: " + currentNode + " Dist: " + minDist);
            while (nodes.MoveNext())
            {
                currentNode = nodes.Current;

                if (!navNodeTraversableArray.IsSet(currentNode))
                {
                    continue;
                }



                float currentDist = Mathf.Abs((start.x - navNodeInfos[currentNode].worldPos.x) + (start.y - navNodeInfos[currentNode].worldPos.y));
                //Debug.Log(navNodeInfos[currentNode].gridPos);
                //Debug.Log("Curr: " + currentNode + " Dist: " + currentDist);
                if (minDist > currentDist || !found)
                {
                    minDist = currentDist;
                    startNode = currentNode;
                    found = true;
                }


            }

            if (!found)
            {
                return;
            }

        }

        //sw.Stop();
        //Debug.Log("Determine start node: " + sw.ElapsedMilliseconds + "ms");
        //sw.Restart();

        using (NativeMultiHashMap<int, int>.Enumerator nodes = groundGroupMap.GetValuesForKey(endGroup))
        {
            //Debug.Log("Start:" + +nodes.Current);
            if (!nodes.MoveNext())
            {
                return;
            }

            bool found = false;

            int currentNode = nodes.Current;
            endNode = currentNode;
            float minValidDist = Mathf.Abs((end.x - navNodeInfos[currentNode].worldPos.x) + (end.y - navNodeInfos[currentNode].worldPos.y));

            closestNode = nodes.Current;
            float minDist = minValidDist;

            //Debug.Log("First: " + endNode + " " + pathfindingVolume.navNodeTraversableArray[currentNode]);

            if (navNodeTraversableArray.IsSet(currentNode))
            {
                found = true;
            }

            //Debug.Log(navNodeInfos[currentNode].id);
            //Debug.Log("Curr: " + currentNode + " Dist: " + minDist);
            while (nodes.MoveNext())
            {

                currentNode = nodes.Current;

                float currentDist = Mathf.Abs((end.x - navNodeInfos[currentNode].worldPos.x) + (end.y - navNodeInfos[currentNode].worldPos.y));

                if (minDist > currentDist)
                {
                    minDist = currentDist;
                    closestNode = currentNode;
                }


                if (!navNodeTraversableArray.IsSet(currentNode))
                {
                    continue;
                }

                //Debug.Log("Current: " + currentNode + " Found: " + found);


                //Debug.Log(navNodeInfos[currentNode].gridPos);
                //Debug.Log("Curr: " + currentNode + " Dist: " + currentDist);
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

        //sw.Stop();
        //Debug.Log("Determine end node: " + sw.ElapsedMilliseconds + "ms");
        //sw.Restart();



        currentLength = 0;


        AddHeapItem(startNode);
        openHashset.Add(startNode);


        int lovestH = int.MaxValue;
        int lovestHIndex = 0;

        int current = -1;



        while (currentLength > 0)
        {

            int currentNode = RemoveFirst();



            openHashset.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode.Equals(endNode))
            {
                current = currentNode;
                //Debug.Log("Success");
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

                    int newMovementCostToNeighbour = navNodeInfos[currentNode].gCost + GetDistance(navNodeInfos[currentNode].gridPos, navNodeInfos[connections.Current.targetID].gridPos);
                    if (newMovementCostToNeighbour < navNodeInfos[connections.Current.targetID].gCost || !contains)
                    {
                        NavNodeInfo info = workingNavNodeInfos[connections.Current.targetID];
                        info.gCost = newMovementCostToNeighbour;
                        info.hCost = GetDistance(navNodeInfos[connections.Current.targetID].gridPos, navNodeInfos[closestNode].gridPos);
                        info.parentIndex = currentNode;
                        workingNavNodeInfos[connections.Current.targetID] = info;

                        if (!contains)
                        {
                            AddHeapItem(connections.Current.targetID);
                            openHashset.Add(connections.Current.targetID);
                            if (navNodeInfos[connections.Current.targetID].hCost < lovestH)
                            {
                                lovestH = navNodeInfos[connections.Current.targetID].hCost;
                                lovestHIndex = connections.Current.targetID;
                            }
                            //Debug.Log(currentNode);
                        }
                    }
                }
            }


        }


        //Debug.Log("Start: " + startNode + " End: " + endNode);


        if (current != endNode)
        {
            if (navNodeInfos[lovestHIndex].hCost >= GetDistance(navNodeInfos[startNode].gridPos, navNodeInfos[endNode].gridPos))
            {
                lovestHIndex = startNode;
            }
            current = lovestHIndex;
            //Debug.Log("Fail");
        }


        while (current != startNode)
        {
            path.Add(current);
            //Debug.Log("Current: " + current );
            current = navNodeInfos[current].parentIndex;
        }


        if (path.Length > 0)
        {
            if (navNodeInfos[startNode].groundGroup == navNodeInfos[path[path.Length - 1]].groundGroup)
            {
                int startNodeToLast = GetDistance(navNodeInfos[startNode].gridPos, navNodeInfos[path[path.Length - 1]].gridPos);
                int startNodeToStartPos = GetDistance(navNodeInfos[path[path.Length - 1]].gridPos, startPos);
                //Debug.Log("startNodeToLast: " + startNodeToLast + " startNodeToStartPos: " + startNodeToStartPos);
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

        if (path.Length > 1 && startNode != endNode)
        {
            if (navNodeInfos[endNode].groundGroup == navNodeInfos[path[1]].groundGroup)
            {
                int endNodeToBeforeLast = GetDistance(navNodeInfos[endNode].gridPos, navNodeInfos[path[1]].gridPos);
                int endNodeToendPos = GetDistance(navNodeInfos[path[1]].gridPos, endPos);

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
                    if (navNodeInfos[openHeap[childIndexRight]].fCost < navNodeInfos[openHeap[childIndexLeft]].fCost)
                    {
                        swapIndex = childIndexRight;
                    }
                }


                if (navNodeInfos[openHeap[swapIndex]].fCost < navNodeInfos[openHeap[heapIndex]].fCost)
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

            if (navNodeInfos[openHeap[parentIndex]].fCost > navNodeInfos[openHeap[heapIndex]].fCost)
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
