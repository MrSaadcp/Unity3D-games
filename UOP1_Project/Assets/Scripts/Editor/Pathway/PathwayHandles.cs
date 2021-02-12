﻿using UnityEngine;
using UnityEditor;


public class PathwayHandles 
{
	private Pathway _pathway;
	//private Vector3 _newPosition;

	public PathwayHandles(Pathway pathway)
	{
		_pathway = pathway;
		//_newPosition = pathway.transform.position; //to connect the movements of the points held by the pathway with the pathway movement
	}

	public void DispalyHandles()
	{
		EditorGUI.BeginChangeCheck();

		//_newPosition = _pathway.transform.position - _newPosition;

		for (int i = 0; i < _pathway.Waypoints.Count; i++)
		{
			_pathway.Waypoints[i] = Handles.PositionHandle(_pathway.Waypoints[i] /*+ _newPosition*/, Quaternion.identity);
		}

		//_newPosition = _pathway.transform.position;
	}

}
