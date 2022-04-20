using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vector_Pararellity_Test : MonoBehaviour
{

    [SerializeField] public Transform start;
    [SerializeField] public Transform end;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float dot = Vector2.Dot(Vector2.right, (end.position - start.position).normalized);
        Debug.Log(dot);
    }
}
