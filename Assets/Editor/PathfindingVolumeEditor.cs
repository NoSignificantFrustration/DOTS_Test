using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PathfindingVolume))]
public class PathfindingVolumeEditor : Editor
{


    SerializedProperty origin;
    SerializedProperty targetPos;
    SerializedProperty areaSize;
    SerializedProperty areaOffset;
    SerializedProperty pathNodes;
    SerializedProperty cellRadius;
    SerializedProperty walkableHeight;
    SerializedProperty bottomLeft;
    SerializedProperty topRight;
    SerializedProperty showGrid;
    SerializedProperty showGraph;


    SerializedProperty grid;





    void OnEnable()
    {
        origin = serializedObject.FindProperty("origin");
        targetPos = serializedObject.FindProperty("target");
        areaSize = serializedObject.FindProperty("areaSize");
        areaOffset = serializedObject.FindProperty("areaOffset");
        pathNodes = serializedObject.FindProperty("pathNodes");
        cellRadius = serializedObject.FindProperty("cellRadius");
        walkableHeight = serializedObject.FindProperty("walkableHeight");
        bottomLeft = serializedObject.FindProperty("bottomLeft");
        topRight = serializedObject.FindProperty("topRight");
        showGrid = serializedObject.FindProperty("showGrid");
        showGraph = serializedObject.FindProperty("showGraph");


        grid = serializedObject.FindProperty("grid");




    }

    public override void OnInspectorGUI()
    {
        PathfindingVolume volume = (PathfindingVolume)target;

        serializedObject.Update();

        EditorGUILayout.PropertyField(origin); 
        EditorGUILayout.PropertyField(targetPos);

        EditorGUILayout.LabelField("Position and size");

        EditorGUILayout.PropertyField(areaSize);
        EditorGUILayout.PropertyField(areaOffset);

        EditorGUILayout.LabelField("Parameters");

        EditorGUILayout.PropertyField(pathNodes);
        EditorGUILayout.PropertyField(cellRadius);
        EditorGUILayout.PropertyField(walkableHeight);

        EditorGUILayout.LabelField("Debug");
        //EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(bottomLeft);
        EditorGUILayout.PropertyField(topRight);
        //EditorGUILayout.EndHorizontal();
        EditorGUILayout.PropertyField(showGrid);
        EditorGUILayout.PropertyField(showGraph);

        if (GUILayout.Button("Refresh Grid"))
        {
            if (Application.isPlaying)
            {
                volume.RefreshGridWalkableArray(volume.bottomLeft, volume.topRight);
            }
            else
            {
                volume.CreateGrid();
                
            }
            
            
        }

        if (GUILayout.Button("Refresh Graph Gizmos"))
        {
            volume.CalculateNodeConnectionGizmos();
        }

        if (GUILayout.Button("Refresh Grid Path"))
        {
            volume.CalculateGridPath();
        }

        if (GUILayout.Button("Refresh Graph Path"))
        {
            volume.CalculateGraphPath();
        }
        serializedObject.ApplyModifiedProperties();
    }

    
}
