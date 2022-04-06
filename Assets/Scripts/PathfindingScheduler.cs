using System;
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


    private List<PathfindingRequest<GridPathfindingJob>> gridPathfindingRequests;
    private List<PathfindingRequest<GraphPathfindingJob>> graphPathfindingRequests;


    private void Awake()
    {
        gridPathfindingRequests = new List<PathfindingRequest<GridPathfindingJob>>();
        graphPathfindingRequests = new List<PathfindingRequest<GraphPathfindingJob>>();
        pathfindingVolume = GetComponent<PathfindingVolume>();
    }

    void Start()
    {
        if (pathfindingVolume != null)
        {
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

    private void Update()
    {
        for (int i = 0; i < gridPathfindingRequests.Count; i++)
        {
            PathfindingRequest<GridPathfindingJob> request = gridPathfindingRequests[i];
            if (request.jobinfo.handle.IsCompleted)
            {
                CompleteRequest(request);

                gridPathfindingRequests.RemoveAt(i);
                i--;
            }
        }

        //Debug.Log(gridPathfindingRequests.Count);
    }

    public void RequestPath(PathfindingRequest<GridPathfindingJob> request)
    {
        JobInfo<GridPathfindingJob> jobInfo = new JobInfo<GridPathfindingJob>();
        jobInfo.job = new GridPathfindingJob();

        jobInfo.job.grid = grid;
        jobInfo.job.gridSize = new int2(pathfindingVolume.gridSize.x, pathfindingVolume.gridSize.y);
        jobInfo.job.gridTraversableArray = gridTraversableArray;
        jobInfo.job.startPos = request.startPos;
        jobInfo.job.endPos = request.endPos;
        jobInfo.job.path = new NativeList<int>(Allocator.TempJob);

        jobInfo.job.workingGrid = new NativeArray<GridCell>(grid.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        jobInfo.job.openHeap = new NativeArray<int>(grid.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        jobInfo.job.openHashset = new NativeHashSet<int>(grid.Length, Allocator.TempJob);
        jobInfo.job.closedSet = new NativeHashSet<int>(grid.Length, Allocator.TempJob);
        jobInfo.job.heapIndexes = new NativeArray<int>(grid.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        
        jobInfo.handle = jobInfo.job.Schedule();

        request.jobinfo = jobInfo;

        gridPathfindingRequests.Add(request);
    }

    private void CompleteRequest(PathfindingRequest<GridPathfindingJob> request)
    {
        request.jobinfo.handle.Complete();

        request.jobinfo.job.workingGrid.Dispose();
        request.jobinfo.job.openHeap.Dispose();
        request.jobinfo.job.openHashset.Dispose();
        request.jobinfo.job.closedSet.Dispose();
        request.jobinfo.job.heapIndexes.Dispose();



        List<int> path = new List<int>();
        for (int j = request.jobinfo.job.path.Length - 1; j > 1; j--)
        {
            path.Add(request.jobinfo.job.path[j]);
        }
        request.jobinfo.job.path.Dispose();


        request.callback?.Invoke(path, true);
    }


    public void RequestPath(PathfindingRequest<GraphPathfindingJob> request)
    {

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
        for (int i = job.path.Length - 1; i > 1; i--)
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
        while (gridPathfindingRequests.Count > 0)
        {
            CompleteRequest(gridPathfindingRequests[gridPathfindingRequests.Count - 1]);
            gridPathfindingRequests.RemoveAt(gridPathfindingRequests.Count - 1);
        }

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

public class PathfindingRequest<T>
{
    public int2 startPos;
    public int2 endPos;
    public Action<List<int>, bool> callback;
    public JobInfo<T> jobinfo;
    
}

public class JobInfo<T>
{
    public JobHandle handle;
    public T job;
}
