using UnityEngine;
using System.Collections.Generic;

/* Add additional check if this object can spawn on the current platform */
public class CollidableAppearanceRules : SpawnRules
{
	// platforms in which the object cannot spawn over
	public List<PlatformLinkRule> avoidPlatforms;
	
	public override void AssignIndexToObject(BasicObject infiniteObject, int index)
	{
		base.AssignIndexToObject(infiniteObject, index);
		
		for (int i = 0; i < avoidPlatforms.Count; ++i) {
			if (avoidPlatforms[i].AssignIndexToObject(infiniteObject, index))
				break;
		}
	}
	
	public override bool CanSpawnObject(float distance, ObjectSpawnData spawnData)
	{
		if (!base.CanSpawnObject(distance, spawnData))
			return false;
		
		for (int i = 0; i < avoidPlatforms.Count; ++i) {
			if (!avoidPlatforms[i].CanSpawnObject(ObjectHistory.instance.GetLastLocalIndex(ObjectType.Platform)))
				return false;
		}
		
		// may not be able to spawn if the slots don't line up
		return (spawnData.slotPositions & ((thisObject as CollidableObject).GetSlotPositionsMask())) != 0;
	}
}