using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* List of single rule target certain object */
[System.Serializable]
public class RulesList
{
	// The target object for all these rules
	public BasicObject targetObject;
	// List of rules
	public List<SingleRule> rules;
	// The object index of the infinite object that we are interested in
	private int targetObjectIndex; 
	// Indicator if the target object is a scene object
	private bool isSceneObject;
	// The object type of the object that this script is attached to
	private ObjectType thisObjectType;
	
	public RulesList(BasicObject targetObject, SingleRule rule)
	{
		this.targetObject = targetObject;
		rules = new List<SingleRule>();
		rules.Add(rule);
	}
	
	public void Init(ObjectType objectType)
	{
		targetObjectIndex = -1;
		thisObjectType = objectType;
	}

	// Assign the object index for target object
	public bool AssignIndexToObject(BasicObject obj, int index)
	{
		if (targetObject == null) {
			return false;
		}
		
		if (obj == targetObject) {
			targetObjectIndex = index;
			isSceneObject = targetObject.GetObjectType() == ObjectType.Scene;
			return true;
		}
		
		return false;
	}
	
	// Check whether this object can be spawned if all rules are passed
	public bool CanSpawnObject(float distance)
	{
		for (int i = 0; i < rules.Count; ++i) {
			ObjectType targetObjectType = targetObject != null ? targetObject.GetObjectType() : ObjectType.Count;
			if (!rules[i].CanSpawnObject(distance, thisObjectType, targetObjectIndex, targetObjectType)) {
				return false;
			}
		}
		return true;
	}
}