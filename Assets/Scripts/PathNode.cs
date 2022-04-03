using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathNode : MonoBehaviour
{

    [field: SerializeField] public List<PathNodeConnection> connections { get; private set; }

    public PathfindingVolume pathfindingVolume;
    public int id;
    public int groundGroup;
    public bool blocked;

    private void Awake()
    {
        
    }

    private void OnDrawGizmos()
    {
        if (pathfindingVolume != null)
        {
            if (pathfindingVolume.showGraph)
            {
                if (blocked)
                {
                    Gizmos.color = Color.red;
                }
                else
                {
                    Gizmos.color = Color.blue;
                }
                
                Gizmos.DrawSphere(transform.position, 0.5f);
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [System.Serializable]
    public class PathNodeConnection
    {
        public PathNode neighbour;
        PathNodeTransitionMethod transitionMethod;
        public int maxHeight;
        public bool oneDirectional;
        
    }

    [System.Serializable]
    public enum PathNodeTransitionMethod
    {
        move, jump
    }
}
