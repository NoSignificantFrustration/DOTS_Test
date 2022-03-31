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



        List<int> path = new List<int>();
        path.Add(startNode);
        path.Add(endNode);

        sw.Stop();
        Debug.Log(sw.ElapsedMilliseconds + " ms");

        return path;
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
