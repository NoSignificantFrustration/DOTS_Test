using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityManager : MonoBehaviour
{

    [field: SerializeField] public PathfindingVolume pathfindingVolume { get; private set; }
    [field: SerializeField] public List<PathfindingAgent> pathfindingAgents { get; private set; }
    [field: SerializeField] public PathfindingScheduler pathfindingScheduler { get; private set; }

    private void Awake()
    {
        foreach (PathfindingAgent item in pathfindingAgents)
        {
            item.entityManager = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < pathfindingAgents.Count; i++)
        {
            pathfindingAgents[i].RequestPath(pathfindingVolume.target.position);
        }
    }
}
