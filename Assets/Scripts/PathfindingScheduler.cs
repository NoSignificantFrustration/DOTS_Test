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


    private List<PathfindingRequest<GridPathfindingJob>> gridPathfindingJobs;
    private List<PathfindingRequest<GraphPathfindingJob>> graphPathfindingJobs;

    private Queue<PathfindingRequest<GridPathfindingJob>> gridPathfindingRequests;
    public int maxGridPathfindingJobsPerFrame = 10;
    private int currentMaxGridPathfindingJobsPerFrame;

    private Queue<PathfindingRequest<GraphPathfindingJob>> graphPathfindingRequests;
    public int maxGraphPathfindingJobsPerFrame = 10;
    private int currentMaxGraphPathfindingJobsPerFrame;

    private void Awake()
    {
        gridPathfindingJobs = new List<PathfindingRequest<GridPathfindingJob>>();
        graphPathfindingJobs = new List<PathfindingRequest<GraphPathfindingJob>>();
        gridPathfindingRequests = new Queue<PathfindingRequest<GridPathfindingJob>>();
        graphPathfindingRequests = new Queue<PathfindingRequest<GraphPathfindingJob>>();

        pathfindingVolume = GetComponent<PathfindingVolume>();

        currentMaxGridPathfindingJobsPerFrame = maxGridPathfindingJobsPerFrame;
        currentMaxGraphPathfindingJobsPerFrame = maxGraphPathfindingJobsPerFrame;
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
        currentMaxGridPathfindingJobsPerFrame = maxGridPathfindingJobsPerFrame;

        for (int i = 0; i < gridPathfindingJobs.Count; i++)
        {
            PathfindingRequest<GridPathfindingJob> request = gridPathfindingJobs[i];
            if (request.jobinfo.handle.IsCompleted)
            {
                CompleteRequest(request);

                gridPathfindingJobs.RemoveAt(i);
                i--;
            }
        }
        currentMaxGraphPathfindingJobsPerFrame = maxGraphPathfindingJobsPerFrame;
        for (int i = 0; i < graphPathfindingJobs.Count; i++)
        {
            PathfindingRequest<GraphPathfindingJob> request = graphPathfindingJobs[i];
            if (request.jobinfo.handle.IsCompleted)
            {
                CompleteRequest(request);

                graphPathfindingJobs.RemoveAt(i);
                i--;
            }
        }

        //Debug.Log(gridPathfindingRequests.Count);
    }

    private void SchedulePath(PathfindingRequest<GridPathfindingJob> request)
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
        jobInfo.job.openSet = new NativeBitArray(grid.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        jobInfo.job.closedSet = new NativeBitArray(grid.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        jobInfo.job.heapIndexes = new NativeArray<int>(grid.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        jobInfo.job.success = new NativeArray<int>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        jobInfo.handle = jobInfo.job.Schedule();

        request.jobinfo = jobInfo;

        gridPathfindingJobs.Add(request);
    }

    private void SchedulePath(PathfindingRequest<GraphPathfindingJob> request)
    {
        JobInfo<GraphPathfindingJob> jobInfo = new JobInfo<GraphPathfindingJob>();
        jobInfo.job = new GraphPathfindingJob();

        jobInfo.job.navNodeInfos = navNodeInfos;
        jobInfo.job.navNodeTraversableArray = navNodeTraversableArray;
        jobInfo.job.groundGroupMap = pathfindingVolume.groundGroupMap;
        jobInfo.job.graphConnectionInfos = pathfindingVolume.graphConnectionInfos;

        jobInfo.job.startPos = request.startPos;
        jobInfo.job.endPos = request.endPos;
        jobInfo.job.startGroup = pathfindingVolume.grid[pathfindingVolume.GridposToArrayPos(request.startPos)].gridGroup;
        jobInfo.job.endGroup = pathfindingVolume.grid[pathfindingVolume.GridposToArrayPos(request.endPos)].gridGroup;

        jobInfo.job.workingNavNodeInfos = new NativeArray<NavNodeInfo>(navNodeInfos.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        jobInfo.job.openHeap = new NativeArray<int>(navNodeInfos.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        jobInfo.job.openHashset = new NativeHashSet<int>(navNodeInfos.Length, Allocator.TempJob);
        jobInfo.job.closedSet = new NativeHashSet<int>(navNodeInfos.Length, Allocator.TempJob);
        jobInfo.job.heapIndexes = new NativeArray<int>(navNodeInfos.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        jobInfo.job.success = new NativeArray<int>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        jobInfo.job.path = new NativeList<int>(Allocator.TempJob);

        jobInfo.handle = jobInfo.job.Schedule();

        request.jobinfo = jobInfo;

        graphPathfindingJobs.Add(request);
    }

    public void RequestPath(PathfindingRequest<GridPathfindingJob> request)
    {
        if (currentMaxGridPathfindingJobsPerFrame > 0)
        {
            SchedulePath(request);
            currentMaxGridPathfindingJobsPerFrame--;
        }
        else
        {
            gridPathfindingRequests.Enqueue(request);
        }
        
    }

    private void CompleteRequest(PathfindingRequest<GridPathfindingJob> request)
    {
        request.jobinfo.handle.Complete();

        request.jobinfo.job.workingGrid.Dispose();
        request.jobinfo.job.openHeap.Dispose();
        request.jobinfo.job.openSet.Dispose();
        request.jobinfo.job.closedSet.Dispose();
        request.jobinfo.job.heapIndexes.Dispose();



        List<int> path = new List<int>();
        for (int j = request.jobinfo.job.path.Length - 1; j > 1; j--)
        {
            path.Add(request.jobinfo.job.path[j]);
        }
        request.jobinfo.job.path.Dispose();


        request.callback?.Invoke(path, request.jobinfo.job.success[0] == 1);
        request.jobinfo.job.success.Dispose();
        //Debug.Log(request.jobinfo.job.success);
    }


    public void RequestPath(PathfindingRequest<GraphPathfindingJob> request)
    {
        if (currentMaxGraphPathfindingJobsPerFrame > 0)
        {
            SchedulePath(request);
            currentMaxGraphPathfindingJobsPerFrame--;
        }
        else
        {
            graphPathfindingRequests.Enqueue(request);
        }
    }

    private void CompleteRequest(PathfindingRequest<GraphPathfindingJob> request)
    {
        request.jobinfo.handle.Complete();

        request.jobinfo.job.workingNavNodeInfos.Dispose();
        request.jobinfo.job.openHeap.Dispose();
        request.jobinfo.job.openHashset.Dispose();
        request.jobinfo.job.closedSet.Dispose();
        request.jobinfo.job.heapIndexes.Dispose();

        List<int> path = new List<int>();

        for (int j = request.jobinfo.job.path.Length - 1; j > -1; j--)
        {
            path.Add(request.jobinfo.job.path[j]);
        }

        request.jobinfo.job.path.Dispose();

        request.callback?.Invoke(path, request.jobinfo.job.success[0] == 1);
        request.jobinfo.job.success.Dispose();
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
        job.openSet =  new NativeBitArray(grid.Length, Allocator.TempJob);
        job.closedSet = new NativeBitArray(grid.Length, Allocator.TempJob);
        job.heapIndexes = new NativeArray<int>(grid.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        job.success = new NativeArray<int>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        JobHandle handle = job.Schedule();
        handle.Complete();

        job.workingGrid.Dispose();
        job.openHeap.Dispose();
        job.openSet.Dispose();
        job.closedSet.Dispose();
        job.heapIndexes.Dispose();
        


        List<int> path = new List<int>();
        for (int i = job.path.Length - 1; i > -1; i--)
        {
            path.Add(job.path[i]);
        }
        job.path.Dispose();
        job.success.Dispose();

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

        job.success = new NativeArray<int>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

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
        job.success.Dispose();

        return path;
    }

    private void RefreshGridTraversableArray(Vector2Int bottomLeft, Vector2Int topRight)
    {
        for (int y = bottomLeft.y; y < topRight.y; y++)
        {
            for (int x = bottomLeft.x; x < topRight.x; x++)
            {
                int arrayPos = y * pathfindingVolume.gridSize.x + x;
                gridTraversableArray.Set(arrayPos, pathfindingVolume.gridTraversableArray[arrayPos]);
            }
        }
        //Debug.Log("Refresh: " + bottomLeft + " " + topRight);
    }

    private void LateUpdate()
    {
        while (currentMaxGridPathfindingJobsPerFrame > 0 && gridPathfindingRequests.Count > 0)
        {
            SchedulePath(gridPathfindingRequests.Dequeue());
            currentMaxGridPathfindingJobsPerFrame--;
        }

        while (currentMaxGraphPathfindingJobsPerFrame > 0 && graphPathfindingRequests.Count > 0)
        {
            SchedulePath(graphPathfindingRequests.Dequeue());
            currentMaxGraphPathfindingJobsPerFrame--;
        }
    }

    private void OnDisable()
    {
        while (gridPathfindingJobs.Count > 0)
        {
            CompleteRequest(gridPathfindingJobs[gridPathfindingJobs.Count - 1]);
            gridPathfindingJobs.RemoveAt(gridPathfindingJobs.Count - 1);
        }
        while (graphPathfindingJobs.Count > 0)
        {
            CompleteRequest(graphPathfindingJobs[graphPathfindingJobs.Count - 1]);
            graphPathfindingJobs.RemoveAt(graphPathfindingJobs.Count - 1);
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
        if (pathfindingVolume.graphConnectionInfos.IsCreated)
        {
            pathfindingVolume.graphConnectionInfos.Dispose();
        }
        if (pathfindingVolume.groundGroupMap.IsCreated)
        {
            pathfindingVolume.groundGroupMap.Dispose();
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
