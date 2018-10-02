using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GG;

[CustomEditor(typeof(ComStateMachine))]
public class ComStateMachineEditor : Editor 
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI ();

		ComStateMachine script = target as ComStateMachine;
		if (GUILayout.Button ("next")) 
		{
			if (null != script) 
			{
				script.next ();
			}
		}

		int key = 0;
		if(null != script)
		{
			key = script.Key;
		}
		EditorGUILayout.LabelField("Current Status: [" + key.ToString() + "]", GUILayout.MinWidth(60));
	}
}