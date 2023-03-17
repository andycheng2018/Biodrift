using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
	public override void OnInspectorGUI()
	{
		MapGenerator mapGen = (MapGenerator)target;

		if (DrawDefaultInspector())
		{
			
        }

		if (GUILayout.Button("Generate Map"))
		{
			mapGen.DrawMapInEditor();
		}
	}
}

#endif