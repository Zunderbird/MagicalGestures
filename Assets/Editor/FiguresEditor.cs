using UnityEngine;
using System.Collections;
using UnityEditor;

public class FiguresEditor : EditorWindow
{
    [MenuItem("Tools/Figures Editor")]
    private static void FiguresEditorMain()
    {
        GetWindow(typeof (FiguresEditor));
    }

    private void OnGUI()
    {
        //EditorGUILayout.
    }
}
