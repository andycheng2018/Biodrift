#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CaptureIcon))]
public class CaptureIconEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CaptureIcon captureIcon = (CaptureIcon)target;

        if (DrawDefaultInspector())
        {

        }

        if (GUILayout.Button("Generate Textures"))
        {
            captureIcon.captureIcon();
        }
    }
}
#endif