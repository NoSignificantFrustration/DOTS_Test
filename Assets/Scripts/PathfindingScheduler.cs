using System.Collections.Generic;
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

    public NativeArray<NavNodeInfo> navNodeInfos { get; private set; }
    public NativeBitArray navNodeTraversableArray { get; private set; }


    


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

            if (navNodeInfos.IsCreated)
            {
                navNodeInfos.Dispose();
            }
            navNodeInfos = new NativeArray<NavNodeInfo>(pathfindingVolume.navNodeInfos, Allocator.Persistent);

            if (navNodeTraversableArray.IsCreated)
            {
                navNodeTraversableArray.Dispose();
            }
            navNodeTraversableArray = new NativeBitArray(navNodeInfos.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < navNodeTraversableArray.Length; i++)
            {
                navNodeTraversableArray.Set(i, pathfindingVolume.navNodeTraversableArray[i]);
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


        GridPathfindingJob job = new GridPathfindingJob();
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

    public List<int> GetGraphPath(int2 startPos, int2 endPos)
    {
        GraphPathfindingJob job = new GraphPathfindingJob();

        job.navNodeInfos = navNodeInfos;
        job.navNodeTraversableArray = navNodeTraversableArray;
        job.groundGroupMap = pathfindingVolume.groundGroupMap;
        job.graphConnectionInfos = pathfindingVolume.graphConnectionInfos;

        job.startPos = startPos;
        job.endPos = endPos;
        job.startGroup = pathfindingVolume.grid[pathfindingVolume.GridposToArrayPos(startPos)].gridGroup;
        job.endGroup = pathfindingVolume.grid[pathfindingVolume.GridposToArrayPos(endPos)].gridGroup;

        job.workingNavNodeInfos = new NativeArray<NavNodeInfo>(navNodeInfos.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        job.openHeap = new NativeArray<int>(navNodeInfos.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        job.openHashset = new NativeHashSet<int>(navNodeInfos.Length, Allocator.TempJob);
        job.closedSet = new NativeHashSet<int>(navNodeInfos.Length, Allocator.TempJob);
        job.heapIndexes = new NativeArray<int>(navNodeInfos.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        job.path = new NativeList<int>(Allocator.TempJob);

        JobHandle handle = job.Schedule();

        handle.Complete();

        job.workingNavNodeInfos.Dispose();
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
        if (navNodeInfos.IsCreated)
        {
            navNodeInfos.Dispose();
        }
        if (navNodeTraversableArray.IsCreated)
        {
            navNodeTraversableArray.Dispose();
        }
        pathfindingVolume.refreshGridTraversableArrayEvent.RemoveListener(RefreshGridTraversableArray);
    }
}



[System.Serializable]
public class RefreshRangeEvent : UnityEvent<Vector2Int, Vector2Int>
{

}