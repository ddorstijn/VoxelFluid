using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlaneGeneration))]
public class PlaneGenerationEditor : Editor {

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PlaneGeneration generator = (PlaneGeneration)target;
        if (GUILayout.Button("Build Object"))
        {
            generator.GeneratePlane();
        }
    }
}
