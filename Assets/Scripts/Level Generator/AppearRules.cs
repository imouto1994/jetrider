using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AppearRules : MonoBehaviour
{
	
	// Don't spawn an object if it is within a predefined distance of another object
	public List<RulesList> avoidObjectRuleMaps;
	
	protected BasicObject thisObject;
	
	public virtual void Init()
	{
		thisObject = GetComponent<BasicObject>();
		
		ObjectType objectType = thisObject.GetObjectType();
		for (int i = 0; i < avoidObjectRuleMaps.Count; ++i) {
			avoidObjectRuleMaps[i].Init(objectType);
		}
		
		for (int i = 0; i < probabilityAdjustmentMaps.Count; ++i) {
			probabilityAdjustmentMaps[i].Init(objectType);
		}
	}
	
	public virtual void AssignIndexToObject(BasicObject infiniteObject, int index)
	{
		for (int i = 0; i < avoidObjectRuleMaps.Count; ++i) {
			avoidObjectRuleMaps[i].AssignIndexToObject(infiniteObject, index);
		}
		
		for (int i = 0; i < probabilityAdjustmentMaps.Count; ++i) {
			probabilityAdjustmentMaps[i].AssignIndexToObject(infiniteObject, index);
		}
	}
	
	// Objects may not be able to be spawned if they are too close to another object, for example
	public virtual bool CanSpawnObject(float distance, ObjectSpawnData spawnData)
	{
		// can't spawn if the sections don't match up
		if (!thisObject.CanSpawnInSection(spawnData.section)) {
			return false;
		}
		
		for (int i = 0; i < avoidObjectRuleMaps.Count; ++i) {
			if (!avoidObjectRuleMaps[i].CanSpawnObject(distance)) {
				return false; // all it takes is one
			}
		}
		return true;
	}
	
	// The probability of this object occuring can be based on the previous objects spawned.
	public float ProbabilityAdjustment(float distance)
	{
		float closestObjectDistance = float.MaxValue;
		float closestProbabilityAdjustment = 1;
		float localDistance = 0;
		float probability = 0f;
		// Find the closest object within the probability adjustment map
		for (int i = 0; i < probabilityAdjustmentMaps.Count; ++i) {
			if (probabilityAdjustmentMaps[i].ProbabilityAdjustment(distance, ref localDistance, ref probability)) {
				// If the probability is equal to the maximum float value then this object must spawn
				if (probability == float.MaxValue) {
					return probability;
				}
				if (localDistance < closestObjectDistance) {
					closestObjectDistance = localDistance;
					closestProbabilityAdjustment = probability;
				}
			}
		}
		return closestProbabilityAdjustment;
	}
}