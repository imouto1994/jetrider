using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// The list of rules for an object
public class SpawnRules : MonoBehaviour
{
	// Don't spawn an object if it is within a predefined distance of another object
	public List<RulesList> avoidObjectRuleMaps;

	// The object that the script is attached to
	protected BasicObject thisObject;
	
	public virtual void Init()
	{
		thisObject = GetComponent<BasicObject>();
		
		ObjectType objectType = thisObject.GetObjectType();
		for (int i = 0; i < avoidObjectRuleMaps.Count; ++i) {
			avoidObjectRuleMaps[i].Init(objectType);
		}
	}

	// Assign index to rules have target object similar to the given object
	public virtual void AssignIndexToObject(BasicObject targetObject, int index)
	{
		for (int i = 0; i < avoidObjectRuleMaps.Count; ++i) {
			avoidObjectRuleMaps[i].AssignIndexToObject(targetObject, index);
		}
	}
	
	// Check if the object is able to be spawned
	public virtual bool CanSpawnObject(float distance, ObjectSpawnData spawnData)
	{
		// Cannot spawn if the object is unable to spawn in the given ection
		if (!thisObject.CanSpawnInSection(spawnData.section)) {
			return false;
		}

		// Rules check
		for (int i = 0; i < avoidObjectRuleMaps.Count; ++i) {
			if (!avoidObjectRuleMaps[i].CanSpawnObject(distance)) {
				return false; 
			}
		}
		return true;
	}
}