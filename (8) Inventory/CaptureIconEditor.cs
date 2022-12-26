using System.Collections;
using System.Collections.Generic;
using UnityEditor;
#if UNITY_EDITOR
using UnityEngine;
#endif

#if UNITY_EDITOR
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