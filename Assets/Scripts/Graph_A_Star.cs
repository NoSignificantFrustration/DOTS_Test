using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class Graph_A_Star : MonoBehaviour
{
    private PathfindingVolume pathfindingVolume;
    [HideInInspector] public NavNodeInfo[] navNodeInfos;

    private int[] openHeap;
    private int currentLength;
    private HashSet<int> openHashset;
    private int[] heapIndexes;



    private void Awake()
    {
        pathfindingVolume = GetComponent<PathfindingVolume>();
    }

    public List<int> FindGraphPath(Vector3 start, Vector3 end)
    {

        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        int startGroup = pathfindingVolume.grid[pathfindingVolume.GridposToArrayPos(pathfindingVolume.worldToGridPos(start))].gridGroup;
        int endGroup = pathfindingVolume.grid[pathfindingVolume.GridposToArrayPos(pathfindingVolume.worldToGridPos(end))].gridGroup;

        //sw.Stop();
        //Debug.Log("Determine start and end ground groups: " + sw.ElapsedMilliseconds + "ms");
        //sw.Restart();

        int startNode = 0;
        int endNode = 0;

        using (NativeMultiHashMap<int, int>.Enumerator nodes = pathfindingVolume.groundGroupMap.GetValuesForKey(startGroup))
        {
            //Debug.Log("Start:" + +nodes.Current);
            if (!nodes.MoveNext())
            {
                return null;
            }
            int currentNode = nodes.Current;
            startNode = currentNode;
            float minDist = Mathf.Abs((start.x - navNodeInfos[currentNode].worldPos.x) + (start.y - navNodeInfos[currentNode].worldPos.y));
            //Debug.Log(navNodeInfos[currentNode].id);
            //Debug.Log("Curr: " + currentNode + " Dist: " + minDist);
            while (nodes.MoveNext())
            {

                currentNode = nodes.Current;

                float currentDist = Mathf.Abs((start.x - navNodeInfos[currentNode].worldPos.x) + (start.y - navNodeInfos[currentNode].worldPos.y));
                //Debug.Log(navNodeInfos[currentNode].gridPos);
                //Debug.Log("Curr: " + currentNode + " Dist: " + currentDist);
                if (minDist > currentDist)
                {
                    minDist = currentDist;
                    startNode = currentNode;
                }
            }



        }

        //sw.Stop();
        //Debug.Log("Determine start node: " + sw.ElapsedMilliseconds + "ms");
        //sw.Restart();

        using (NativeMultiHashMap<int, int>.Enumerator nodes = pathfindingVolume.groundGroupMap.GetValuesForKey(endGroup))
        {
            //Debug.Log("Start:" + +nodes.Current);
            if (!nodes.MoveNext())
            {
                return null;
            }
            int currentNode = nodes.Current;
            endNode = currentNode;
            float minDist = Mathf.Abs((end.x - navNodeInfos[currentNode].worldPos.x) + (end.y - navNodeInfos[currentNode].worldPos.y));
            //Debug.Log(navNodeInfos[currentNode].id);
            //Debug.Log("Curr: " + currentNode + " Dist: " + minDist);
            while (nodes.MoveNext())
            {

                currentNode = nodes.Current;

                float currentDist = Mathf.Abs((end.x - navNodeInfos[currentNode].worldPos.x) + (end.y - navNodeInfos[currentNode].worldPos.y));
                //Debug.Log(navNodeInfos[currentNode].gridPos);
                //Debug.Log("Curr: " + currentNode + " Dist: " + currentDist);
                if (minDist > currentDist)
                {
                    minDist = currentDist;
                    endNode = currentNode;
                }
            }

        }

        //sw.Stop();
        //Debug.Log("Determine end node: " + sw.ElapsedMilliseconds + "ms");
        //sw.Restart();

        currentLength = 0;
        openHeap = new int[pathfindingVolume.navNodeInfos.Length];
        openHashset = new HashSet<int>();
        heapIndexes = new int[pathfindingVolume.navNodeInfos.Length];

        HashSet<int> closedSet = new HashSet<int>();

        AddHeapItem(startNode);
        openHashset.Add(startNode);


        int lovestH = int.MaxValue;
        int lovestHIndex = 0;

        int current = -1;
        List<int> path = new List<int>();


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

            using (NativeMultiHashMap<int, GraphConnectionInfo>.Enumerator connections = pathfindingVolume.graphConnectionInfos.GetValuesForKey(currentNode))
            {

                while (connections.MoveNext())
                {
                    if (!pathfindingVolume.navNodeTraversableArray[connections.Current.targetID] || closedSet.Contains(connections.Current.targetID))
                    {
                        continue;
                    }

                    bool contains = openHashset.Contains(connections.Current.targetID);

                    int newMovementCostToNeighbour = navNodeInfos[currentNode].gCost + GetDistance(navNodeInfos[currentNode].gridPos, navNodeInfos[connections.Current.targetID].gridPos);
                    if (newMovementCostToNeighbour < navNodeInfos[connections.Current.targetID].gCost || !contains)
                    {
                        navNodeInfos[connections.Current.targetID].gCost = newMovementCostToNeighbour;
                        navNodeInfos[connections.Current.targetID].hCost = GetDistance(navNodeInfos[connections.Current.targetID].gridPos, navNodeInfos[endNode].gridPos);
                        navNodeInfos[connections.Current.targetID].parentIndex = currentNode;

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

        //Debug.Log("Start: " + pathfindingVolume.navNodeInfos[startNode].id + " End: " + pathfindingVolume.navNodeInfos[endNode].id);

        if (current != endNode)
        {
            current = lovestHIndex;
            //Debug.Log("Fail");
        }
       

        while (current != startNode)
        {
            path.Add(current);
            //Debug.Log("Current: " + current );
            current = navNodeInfos[current].parentIndex;
        }

        path.Add(startNode);


        sw.Stop();
        Debug.Log("Pathfinding:" + sw.ElapsedMilliseconds + " ms");
        Debug.Log("Length: " + path.Count);

        path.Reverse();
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
                    if (pathfindingVolume.navNodeInfos[openHeap[childIndexRight]].fCost < pathfindingVolume.navNodeInfos[openHeap[childIndexLeft]].fCost)
                    {
                        swapIndex = childIndexRight;
                    }
                }


                if (pathfindingVolume.navNodeInfos[openHeap[swapIndex]].fCost < pathfindingVolume.navNodeInfos[openHeap[heapIndex]].fCost)
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

            if (pathfindingVolume.navNodeInfos[openHeap[parentIndex]].fCost > pathfindingVolume.navNodeInfos[openHeap[heapIndex]].fCost)
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
