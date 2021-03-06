using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(PathfindingScheduler))]
public class PathfindingVolume : MonoBehaviour
{
    [SerializeField] public Transform origin, target;

    [SerializeField] private Vector2 areaSize;
    [SerializeField] private Vector2 areaOffset;
    [SerializeField] private List<PathNode> pathNodes;
    [SerializeField] private float cellRadius;
    [SerializeField] private int walkableHeight;
    [Min(0)]
    [SerializeField] public Vector2Int bottomLeft;
    [Min(0)]
    [SerializeField] public Vector2Int topRight;
    [SerializeField] private bool showGrid;
    [SerializeField] public bool showGraph;

    public Vector2Int gridSize { get; private set; }
    private float cellDiameter;

    private LayerMask groundMask;

    public BitArray gridTraversableArray { get; private set; }
    public BitArray walkableArray { get; private set; }
    public Vector2[] positionArray { get; private set; }
    public Vector2[] navNodePositionArray { get; private set; }

    [SerializeField] public GridCell[] grid { get; private set; }
    public Grid_A_Star gridPathfinder;
    public List<int> path;

    [HideInInspector, SerializeField] private List<GizmoConnectionInfo> gizmoConnectionInfos;
    public NavNodeInfo[] navNodeInfos { get; private set; }
    public NativeMultiHashMap<int, int> groundGroupMap { get; private set; }
    public Graph_A_Star graphPathfinder;
    public BitArray navNodeTraversableArray { get; private set; }
    public NativeMultiHashMap<int, GraphConnectionInfo> graphConnectionInfos { get; private set; }

    public List<int> graphPath;

    public RefreshRangeEvent refreshGridTraversableArrayEvent { get; private set; }

    public PathfindingScheduler scheduler { get; private set; }


    private void Awake()
    {

        
        cellDiameter = cellRadius * 2;
        int x = Mathf.RoundToInt(areaSize.x / cellDiameter);
        int y = Mathf.RoundToInt(areaSize.y / cellDiameter);
        gridSize = new Vector2Int(x, y);
        groundMask = LayerMask.GetMask("Ground");
        if (gizmoConnectionInfos == null)
        {
            gizmoConnectionInfos = new List<GizmoConnectionInfo>();
        }
        gridPathfinder = GetComponent<Grid_A_Star>();
        graphPathfinder = GetComponent<Graph_A_Star>();
        CreateGrid();
        if (Application.isEditor && pathNodes.Count > 0)
        {

            CalculateNodeConnectionGizmos();
        }
        
        GenerateGraph();

        refreshGridTraversableArrayEvent = new RefreshRangeEvent();
        scheduler = GetComponent<PathfindingScheduler>();
    }

    // Start is called before the first frame update
    void Start()
    {
        

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CalculateGridPath()
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        //path = gridPathfinder.FindGridPath(origin.position, target.position);

        path = scheduler.GetGridPath(worldToGridPos(origin.position), worldToGridPos(target.position));

        sw.Stop();
        Debug.Log(sw.ElapsedMilliseconds + "ms Length: " + path.Count);
    }

    public void CalculateGraphPath()
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        graphPath = scheduler.GetGraphPath(worldToGridPos(origin.position), worldToGridPos(target.position));

        sw.Stop();
        Debug.Log(sw.ElapsedMilliseconds + "ms Length: " + graphPath.Count);
    }

    public void CreateGrid()
    {
        cellDiameter = cellRadius * 2;
        int sizeX = Mathf.RoundToInt(areaSize.x / cellDiameter);
        int sizeY = Mathf.RoundToInt(areaSize.y / cellDiameter);
        gridSize = new Vector2Int(sizeX, sizeY);
        walkableArray = new BitArray(gridSize.x * gridSize.y);
        positionArray = new Vector2[gridSize.x * gridSize.y];
        grid = new GridCell[gridSize.x * gridSize.y];
        gridTraversableArray = new BitArray(grid.Length);
        groundMask = LayerMask.GetMask("Ground");

        Vector3 worldBottomLeft = transform.position - Vector3.right * areaSize.x / 2 - Vector3.up * areaSize.y / 2;

        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                int arrayPos = y * gridSize.x + x;

                grid[arrayPos] = new GridCell();
                grid[arrayPos].gridPos = new int2(x, y);

                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * cellDiameter + cellRadius) + Vector3.up * (y * cellDiameter + cellRadius);
                //Debug.Log(worldPoint);
                gridTraversableArray.Set(arrayPos, !Physics.CheckSphere(worldPoint, cellRadius, groundMask));
                positionArray[arrayPos] = new float2(worldPoint.x, worldPoint.y);
                bool walkable = false;
                if (y > walkableHeight - 1)
                {
                    if (!gridTraversableArray[(y - walkableHeight) * gridSize.x + x])
                    {

                        walkable = true;
                        for (int i = 0; i < walkableHeight; i++)
                        {
                            if (!gridTraversableArray[(y - i) * gridSize.x + x])
                            {
                                walkable = false;
                                break;
                            }
                        }
                    }

                    //if (traversableArray.IsSet(y * gridSize.x + x) && !traversableArray.IsSet((y - 1) * gridSize.x + x))
                    //{
                    //    walkable = true;
                    //}
                    if (walkable)
                    {
                        for (int i = 0; i < walkableHeight; i++)
                        {
                            walkableArray[(y - i) * gridSize.x + x] = true;
                        }
                    }

                }
                else
                {
                    walkableArray[arrayPos] = false;
                }


            }
        }

        EvaluateGroundGroups();
        if (gridPathfinder != null)
        {
            gridPathfinder.grid = (GridCell[])grid.Clone();
        }
        
    }

    public void RefreshGridWalkableArray(Vector2Int bottomLeftStart, Vector2Int topRightEnd)
    {
        if (bottomLeftStart.x < 0)
        {
            bottomLeftStart.x = 0;
        }

        if (bottomLeftStart.y < 0)
        {
            bottomLeftStart.y = 0;
        }

        if (topRightEnd.x > gridSize.x)
        {
            topRightEnd.x = gridSize.x;
        }

        if (topRightEnd.y > gridSize.y)
        {
            topRightEnd.y = gridSize.y;
        }
        Debug.Log(bottomLeftStart + " " + topRightEnd);

        for (int y = bottomLeftStart.y; y < topRightEnd.y; y++)
        {
            for (int x = bottomLeftStart.x; x < topRightEnd.x; x++)
            {
                int arrayPos = y * gridSize.x + x;
                Vector3 worldPoint = new Vector3(positionArray[arrayPos].x, positionArray[arrayPos].y, transform.position.z);
                gridTraversableArray.Set(arrayPos, !Physics.CheckSphere(worldPoint, cellDiameter/2, groundMask));
            }
        }

        refreshGridTraversableArrayEvent.Invoke(bottomLeftStart, topRightEnd);
    }

    void EvaluateGroundGroups()
    {
        BitArray exploredSet = new BitArray(gridSize.x * gridSize.y, false);
        Queue<int> exploreQueue = new Queue<int>();

        int currentGroup = 0;

        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                int arrayPos = y * gridSize.x + x;

                if (!gridTraversableArray[arrayPos] || exploredSet[arrayPos])
                {
                    exploredSet[arrayPos] = true;
                    continue;
                }
                else if (!walkableArray[arrayPos])
                {
                    exploredSet[arrayPos] = true;
                    grid[arrayPos].gridGroup = 0;
                    continue;
                }

                if (!exploredSet[arrayPos])
                {
                    currentGroup++;
                    grid[arrayPos].gridGroup = currentGroup;
                    exploreQueue.Enqueue(arrayPos);
                    exploredSet[arrayPos] = true;

                    while (exploreQueue.Count > 0)
                    {
                        int exploreIndex = exploreQueue.Dequeue();
                        List<int> neighbourIndexList = GetNeighbourFlatIndexes(grid[exploreIndex].gridPos);

                        for (int i = 0; i < neighbourIndexList.Count; i++)
                        {
                            if (!gridTraversableArray[neighbourIndexList[i]] || exploredSet[neighbourIndexList[i]])
                            {
                                exploredSet[neighbourIndexList[i]] = true;
                                continue;
                            }
                            else if (!walkableArray[neighbourIndexList[i]])
                            {
                                exploredSet[neighbourIndexList[i]] = true;
                                grid[neighbourIndexList[i]].gridGroup = 0;
                                neighbourIndexList.RemoveAt(i);
                                continue;
                            }

                            exploreQueue.Enqueue(neighbourIndexList[i]);
                            exploredSet[neighbourIndexList[i]] = true;
                            grid[neighbourIndexList[i]].gridGroup = currentGroup;
                        }

                    }
                }

            }
        }
        for (int i = 0; i < pathNodes.Count; i++)
        {
            pathNodes[i].groundGroup = GetCell(worldToGridPos(pathNodes[i].transform.position)).gridGroup;
        }
    }

    public List<int> GetNeighbourFlatIndexes(int2 position)
    {
        List<int> nList = new List<int>();
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

    public GridCell GetCell(int2 pos)
    {
        //Debug.Log(gridSize);
        return grid[pos.y * gridSize.x + pos.x];
    }

    public int GridposToArrayPos(int2 gridPos)
    {
        return gridPos.y * gridSize.x + gridPos.x;
    }

    public int2 worldToGridPos(Vector3 worldPos)
    {
        float percentX = (worldPos.x - cellRadius + areaSize.x / 2) / areaSize.x;
        float percentY = (worldPos.y - cellRadius + areaSize.y / 2) / areaSize.y;

        percentX = math.clamp(percentX, 0f, 1f);
        percentY = math.clamp(percentY, 0f, 1f);

        int x = Mathf.RoundToInt(Mathf.Clamp((gridSize.x) * percentX, 0, gridSize.x - 1));
        int y = Mathf.RoundToInt(Mathf.Clamp((gridSize.y) * percentY, 0, gridSize.y - 1));

        return new int2(x, y);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(new Vector3(transform.position.x + areaOffset.x, transform.position.y + areaOffset.y, 0f), new Vector3(areaSize.x, areaSize.y, 1f));

        int2 originPos = worldToGridPos(origin.position);

        if (showGrid && grid != null)
        {

            if (bottomLeft.x > gridSize.x)
            {
                bottomLeft.x = gridSize.x;
            }
            if (bottomLeft.y > gridSize.y)
            {
                bottomLeft.y = gridSize.y;
            }
            if (topRight.x > topRight.x)
            {
                topRight.x = topRight.x;
            }
            if (topRight.y > topRight.y)
            {
                topRight.y = topRight.y;
            }

            int selectedGroup = grid[originPos.y * gridSize.x + originPos.x].gridGroup;

            for (int y = bottomLeft.y; y < topRight.y; y++)
            {
                for (int x = bottomLeft.x; x < topRight.x; x++)
                {
                    int arrayPos = y * gridSize.x + x;
                    Gizmos.color = gridTraversableArray[arrayPos] ? Color.yellow : Color.red;

   
                    if ((x == originPos.x && y == originPos.y) || (grid[arrayPos].gridGroup == selectedGroup && walkableArray[arrayPos]))
                    {
                        Gizmos.color = Color.cyan;
                    }
                    else if (walkableArray[y * gridSize.x + x])
                    {
                        Gizmos.color = Color.green;
                    }
                    Gizmos.DrawCube(new Vector3(positionArray[arrayPos].x, positionArray[arrayPos].y, 0f), Vector3.one * (cellDiameter - .1f));

                }
            }
        }

        if (gizmoConnectionInfos != null && showGraph)
        {

            for (int i = 0; i < gizmoConnectionInfos.Count; i++)
            {
                if (gizmoConnectionInfos[i].bidirectional)
                {
                    Gizmos.color = Color.cyan;
                }
                else
                {
                    Gizmos.color = Color.magenta;
                }
                Gizmos.DrawLine(gizmoConnectionInfos[i].startPos, gizmoConnectionInfos[i].endPos);
                
            }
            Gizmos.color = Color.blue;
            for (int i = 0; i < pathNodes.Count; i++)
            {
                pathNodes[i].pathfindingVolume = this;
            }
        }

        if (!Application.isPlaying)
        {
            return;
        }

        if (path != null)
        {
            Gizmos.color = Color.red;
            for (int i = 1; i < path.Count; i++)
            {
                Gizmos.DrawSphere(new Vector3(positionArray[path[i - 1]].x, positionArray[path[i - 1]].y, -2f), 0.4f);
                Gizmos.DrawLine(new Vector3(positionArray[path[i - 1]].x, positionArray[path[i - 1]].y, -2f), new Vector3(positionArray[path[i]].x, positionArray[path[i]].y, -2f));
            }
        }

        if (graphPath != null && graphPath.Count > 0)
        {
            Gizmos.color = Color.red;
            
            for (int i = 1; i < graphPath.Count; i++)
            {
                Gizmos.DrawLine(pathNodes[graphPath[i - 1]].transform.position, pathNodes[graphPath[i]].transform.position);
            }
            Gizmos.DrawLine(origin.position, pathNodes[graphPath[0]].transform.position);
            Gizmos.DrawLine(pathNodes[graphPath[graphPath.Count - 1]].transform.position, target.position);
        }


    }

    public void CalculateNodeConnectionGizmos()
    {
        if (gizmoConnectionInfos == null)
        {
            gizmoConnectionInfos = new List<GizmoConnectionInfo>();
        }
        gizmoConnectionInfos.Clear();

        Queue<PathNode> workingnSet = new Queue<PathNode>();
        List<PathNode> closedSet = new List<PathNode>();
        workingnSet.Enqueue(pathNodes[0]);

        

        while (workingnSet.Count > 0)
        {


            PathNode currentNode = workingnSet.Dequeue();

            for (int i = 0; i < currentNode.connections.Count; i++)
            {
                GizmoConnectionInfo connection = new GizmoConnectionInfo();
                connection.startPos = currentNode.transform.position;

                if (currentNode.connections[i].oneDirectional)
                {
                    connection.endPos = currentNode.connections[i].neighbour.transform.position;
                    connection.bidirectional = false;
                    gizmoConnectionInfos.Add(connection);
                    closedSet.Add(currentNode);
                    continue;
                }
                else if (closedSet.Contains(currentNode.connections[i].neighbour))
                {
                    continue;
                }

                if (!workingnSet.Contains(currentNode.connections[i].neighbour))
                {
                    workingnSet.Enqueue(currentNode.connections[i].neighbour);

                }

                connection.endPos = currentNode.connections[i].neighbour.transform.position;

                connection.bidirectional = !currentNode.connections[i].oneDirectional;

                gizmoConnectionInfos.Add(connection);
                closedSet.Add(currentNode);

            }

        }

    }


    private void GenerateGraph()
    {
        navNodeInfos = new NavNodeInfo[pathNodes.Count];
        List<int> sourceList = new List<int>();
        if (graphConnectionInfos.IsCreated)
        {
            graphConnectionInfos.Dispose();
        }
        if (groundGroupMap.IsCreated)
        {
            groundGroupMap.Dispose();
        }
        groundGroupMap = new NativeMultiHashMap<int, int>(pathNodes.Count, Allocator.Persistent);
        List<GraphConnectionInfo> connectionInfoList = new List<GraphConnectionInfo>();

        navNodeTraversableArray = new BitArray(pathNodes.Count);
        navNodePositionArray = new Vector2[pathNodes.Count];

        for (int i = 0; i < pathNodes.Count; i++)
        {
            pathNodes[i].id = i;
            NavNodeInfo nodeInfo = new NavNodeInfo();
            nodeInfo.id = i;
            nodeInfo.gridPos = worldToGridPos(pathNodes[i].transform.position);
            navNodeTraversableArray[i] = !pathNodes[i].blocked;
            navNodePositionArray[i] = pathNodes[i].transform.position;

            nodeInfo.worldPos = new float2(pathNodes[i].transform.position.x, pathNodes[i].transform.position.y);

            nodeInfo.groundGroup = grid[nodeInfo.gridPos.y * gridSize.x + nodeInfo.gridPos.x].gridGroup;
            navNodeInfos[i] = nodeInfo;
            if (nodeInfo.groundGroup < 1)
            {
                Debug.LogWarning("NavNode " + i + " is not on walkable ground!");
            }
            groundGroupMap.Add(grid[GridposToArrayPos(nodeInfo.gridPos)].gridGroup, i);
        }

        for (int i = 0; i < pathNodes.Count; i++)
        {
            for (int j = 0; j < pathNodes[i].connections.Count; j++)
            {
                sourceList.Add(pathNodes[i].id);

                GraphConnectionInfo connectionInfo = new GraphConnectionInfo();
                connectionInfo.targetID = pathNodes[i].connections[j].neighbour.id;
                connectionInfo.maxHeight = pathNodes[i].connections[j].maxHeight;
                connectionInfoList.Add(connectionInfo);
            }
        }

        graphConnectionInfos = new NativeMultiHashMap<int, GraphConnectionInfo>(connectionInfoList.Count, Allocator.Persistent);
        for (int i = 0; i < connectionInfoList.Count; i++)
        {
            graphConnectionInfos.Add(sourceList[i], connectionInfoList[i]);
        }

        graphPathfinder.navNodeInfos = (NavNodeInfo[])navNodeInfos.Clone();
    }

    private void OnDisable()
    {
        
    }

    public struct GizmoConnectionInfo
    {
        public Vector3 startPos;
        public Vector3 endPos;
        public bool bidirectional;
    }
}

public struct GridCell
{

    public int gCost;
    public int hCost;
    public int fCost
    {
        get
        {
            return gCost + hCost;
        }
    }
    public int origin;

    public int gridGroup;
    public int2 gridPos;
    public int parentIndex;
    

}

public struct NavNodeInfo
{
    public int id;
    public int groundGroup;
    public int2 gridPos;
    public float2 worldPos;
    public int gCost;
    public int hCost;
    public int fCost
    {
        get
        {
            return gCost + hCost;
        }
    }

    public int parentIndex;
}

public struct GraphConnectionInfo
{
    public int targetID;
    public int maxHeight;
}