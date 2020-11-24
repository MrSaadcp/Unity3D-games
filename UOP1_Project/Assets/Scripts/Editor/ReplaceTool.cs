﻿using System.Linq;
using UnityEngine;
using UnityEditor;

// This code has been adapted from a tutorial by Patryk Galach
// https://www.patrykgalach.com/2019/08/26/replace-tool-for-level-designers

/// <summary>
/// Replace tool window,available under the Tools menu.
/// </summary>
public class ReplaceTool : EditorWindow
{
	ReplaceData data;
	SerializedObject serializedData;
	// Prefab variable from data object. Using SerializedProperty for integrated Undo
	SerializedProperty replaceObjectField;
	// Scroll position for list of selected objects
	Vector2 selectObjectScrollPosition;

	private void InitDataIfNeeded()
	{
		if (!data)
		{
			data = ScriptableObject.CreateInstance<ReplaceData>();
			serializedData = null;
		}
		// If data was not wrapped into SerializedObject, wrap it
		if (serializedData == null)
		{
			serializedData = new SerializedObject(data);
			replaceObjectField = null;
		}
		// If prefab field was not assigned as SerializedProperty, assign it
		if (replaceObjectField == null)
		{
			replaceObjectField = serializedData.FindProperty("replacementPrefab");
		}
	}

	// Register menu item to open Window
	[MenuItem("Tools/Replace with Prefab")]
	public static void ShowWindow()
	{
		var window = GetWindow<ReplaceTool>();
		window.Show();
	}

	private void OnGUI()
	{
		InitDataIfNeeded();
		EditorGUILayout.Separator();
		EditorGUILayout.PropertyField(replaceObjectField);
		EditorGUILayout.Separator();

		EditorGUILayout.LabelField("Selected objects to replace", EditorStyles.boldLabel);
		EditorGUILayout.Separator();

		// Saving number of objects to replace.
		int objectToReplaceCount = data.objectsToReplace != null ? data.objectsToReplace.Length : 0;
		EditorGUILayout.IntField("Object count", objectToReplaceCount);
		EditorGUI.indentLevel++;

		// Printing information when no object is selected on scene.
		if (objectToReplaceCount == 0)
		{
			EditorGUILayout.Separator();
			EditorGUILayout.LabelField("Select objects in the hierarchy to replace them", EditorStyles.wordWrappedLabel);
		}

		// Read-only scroll view with selected game objects.
		selectObjectScrollPosition = EditorGUILayout.BeginScrollView(selectObjectScrollPosition);

		GUI.enabled = false;
		if (data && data.objectsToReplace != null)
		{
			foreach (var go in data.objectsToReplace)
			{
				EditorGUILayout.ObjectField(go, typeof(GameObject), true);
			}
		}
		GUI.enabled = true;

		EditorGUILayout.EndScrollView();
		EditorGUI.indentLevel--;
		EditorGUILayout.Separator();

		if (GUILayout.Button("Replace"))
		{
			// Check if replace object is assigned.
			if (!replaceObjectField.objectReferenceValue)
			{
				Debug.LogErrorFormat("{0}", "No prefab to replace with!");
				return;
			}
			// Check if there are objects to replace.
			if (data.objectsToReplace.Length == 0)
			{
				Debug.LogErrorFormat("{0}", "No objects to replace!");
				return;
			}
			ReplaceSelectedObjects(data.objectsToReplace, data.replacementPrefab);

		}

		EditorGUILayout.Separator();
		serializedData.ApplyModifiedProperties();

	}
	private void OnInspectorUpdate()
	{
		if (serializedData != null && serializedData.UpdateIfRequiredOrScript())
		{
			this.Repaint();
		}
	}
	private void OnSelectionChange()
	{
		InitDataIfNeeded();
		SelectionMode objectFilter = SelectionMode.Unfiltered ^ ~(SelectionMode.Assets | SelectionMode.DeepAssets | SelectionMode.Deep);
		Transform[] selection = Selection.GetTransforms(objectFilter);
		data.objectsToReplace = selection.Select(s => s.gameObject).ToArray();
		if (serializedData.UpdateIfRequiredOrScript())
		{
			this.Repaint();
		}
	}

	/// <summary>
	/// Replaces game objects with provided replace object.
	/// </summary>
	/// <param name="objectToReplace">Game Objects to replace.</param>
	/// <param name="replaceObject">Prefab that will be instantiated in place of the objects to replace.</param>
	private void ReplaceSelectedObjects(GameObject[] objectToReplace, GameObject replaceObject)
	{
		Debug.Log("[Replace Tool] Replace process");
		for (int i = 0; i < objectToReplace.Length; i++)
		{
			var go = objectToReplace[i];
			Undo.RegisterCompleteObjectUndo(go, "Saving game object state");
			//var inst = Instantiate(replaceObject, go.transform.position, go.transform.rotation, go.transform.parent);
			GameObject newPrefabObject = PrefabUtility.InstantiatePrefab(replaceObject, go.transform.parent) as GameObject;
			newPrefabObject.name = replaceObject.name + " (" + (i + 1) + ")";
			newPrefabObject.transform.position = go.transform.localPosition;
			newPrefabObject.transform.rotation = go.transform.localRotation;
			newPrefabObject.transform.localScale = go.transform.localScale;
			newPrefabObject.transform.localScale = go.transform.localScale;
			Undo.RegisterCreatedObjectUndo(newPrefabObject, "Replacement creation.");
			foreach (Transform child in go.transform)
			{
				Undo.SetTransformParent(child, newPrefabObject.transform, "Parent Change");
			}
			Undo.DestroyObjectImmediate(go);
		}
		Debug.LogFormat("[Replace Tool] {0} objects replaced on scene with {1}", objectToReplace.Length, replaceObject.name);
	}
}

/// <summary>
/// Data class for replace tool.
/// </summary>
public class ReplaceData : ScriptableObject
{
	public GameObject replacementPrefab;
	public GameObject[] objectsToReplace;
}
