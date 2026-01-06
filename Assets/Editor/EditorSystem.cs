// Assets/Editor/EditorSystem.cs
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AdministartorScript))]
public class EditorSystem : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Delete All PlayerPrefs"))
        {
            PlayerPrefs.DeleteAll();
        }
    }
}
