using UnityEngine;
using System.Collections;

// Rule of restriction for an object to be considered for spawning
[System.Serializable]
public class SingleRule
{
	// Indicator that rule applies to any object of the same type with this object
	public bool minDistanceSameObjectType;
	
	// Minimum distance is the minimum distance that two objects can spawn next to each other.
	public int minDistance;
	
	// The number of objects which must be in between two objects
	public int minObjectSeparation;

	// The range of distance that this rule will apply
	public InterpolatedValue range;

	// Constructor 
	public SingleRule(int minDistance, bool minDistanceSameObjectType, int minObjectSeparation, InterpolatedValue range)
	{
		this.minDistance = minDistance;
		this.minDistanceSameObjectType = minDistanceSameObjectType;
		this.minObjectSeparation = minObjectSeparation;
		this.range = range;
	}

	// Indicator whether this object passes this rule
	public bool CanSpawnObject(float distance, ObjectType thisObjectType, int targetObjectIndex, ObjectType targetObjectType)
	{
		// return true if the parameters do not apply to the current distance
		if (!range.IsWithinRange(distance))
			return true;
		
		// The target object doesn't matter if we are using objects of the same object type
		float totalDistance = ObjectHistory.instance.GetTotalDistance(thisObjectType == ObjectType.Scene);
		if (minDistanceSameObjectType) {
			float lastSpawnDistance = ObjectHistory.instance.GetLastObjectTypeSpawnDistance(thisObjectType);
			if (totalDistance - lastSpawnDistance <= minDistance) {
				return false;
			}
		}
		
		// The rest of the tests need the target object, so if there is no target object then we are done early
		if (targetObjectIndex == -1)
			return true;

		int objectSpawnIndex = ObjectHistory.instance.GetObjectSpawnIndex(targetObjectIndex);
		// can always spawn if the object hasn't been spawned before and it is within the probabilities
		if (objectSpawnIndex == -1)
			return true;

		int latestSpawnIndex = ObjectHistory.instance.GetObjectTypeSpawnIndex(targetObjectType);
		// can't spawn if there isn't enough object separation
		if (latestSpawnIndex - objectSpawnIndex <= minObjectSeparation)
			return false;

		float objectLastDistance = ObjectHistory.instance.GetSpawnDistance(targetObjectIndex);
		// can't spawn if we are too close to another object
		if (totalDistance - objectLastDistance <= minDistance)
			return false;

		return true;
	}
}