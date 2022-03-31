using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
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
        int startGroup = pathfindingVolume.grid[pathfindingVolume.GridposToArrayPos(pathfindingVolume.worldToGridPos(start))].gridGroup;

        int startNode = 0;

        using (NativeMultiHashMap<int, int>.Enumerator nodes = pathfindingVolume.groundGroupMap.GetValuesForKey(startGroup))
        {
            
            
            int currentNode = nodes.Current;
            startNode = currentNode;
            float minDist = (start.x - navNodeInfos[currentNode].worldPos.x) + (start.y - navNodeInfos[currentNode].worldPos.y);

            while (nodes.MoveNext())
            {
                Debug.Log("Curr: " + currentNode);
                currentNode = nodes.Current;
                float currentDist = (start.x - navNodeInfos[currentNode].worldPos.x) + (start.y - navNodeInfos[currentNode].worldPos.y);
                if (minDist > currentDist)
                {
                    minDist = currentDist;
                    startNode = currentNode;
                }
            }

        }
        Debug.Log(startNode);
        return new List<int>();
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
