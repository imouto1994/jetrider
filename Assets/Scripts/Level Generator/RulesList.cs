using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class RulesList
{

	public BasicObject targetObject;
	public List<SingleRule> rules;
	
	private int targetObjectIndex; // the object index of the infinite object that we are interested in
	private bool targetObjectIsScene; // is the target object a scene object
	private ObjectType thisObjectType;
	
	public RulesList(BasicObject io, SingleRule r)
	{
		targetObject = io;
		rules = new List<SingleRule>();
		rules.Add(r);
	}
	
	public void Init(ObjectType objectType)
	{
		targetObjectIndex = -1;
		thisObjectType = objectType;
	}
	
	public bool AssignIndexToObject(BasicObject obj, int index)
	{
		if (targetObject == null) {
			return false;
		}
		
		if (obj == targetObject) {
			targetObjectIndex = index;
			targetObjectIsScene = targetObject.GetObjectType() == ObjectType.Scene;
			return true;
		}
		
		return false;
	}
	
	// Objects may not be able to be spawned if they are too close to another object, for example
	public bool CanSpawnObject(float distance)
	{
		for (int i = 0; i < rules.Count; ++i) {
			if (!rules[i].CanSpawnObject(distance, thisObjectType, targetObjectIndex, (targetObject != null ? targetObject.GetObjectType() : ObjectType.Last))) {
				return false;
			}
		}
		return true;
	}
}