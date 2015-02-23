using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Inherit from SpawnRules class
// This add certain restrictions specifically for Scene 
public class SceneSpawnRules : SpawnRules
{
	
	// A list of platforms that the scene object must spawn near. A size of 0 means it can spawn near any platform
	public List<PlatformLinkRule> linkedPlatforms;
	// Width buffer between platform and scene
	private float platformSceneWidthBuffer;
	// Z Size of the scene
	private float zSize;
	
	public override void Init()
	{
		base.Init();
	}

	// Set dimensions for scene
	public void SetSizes(float buffer, float size)
	{
		platformSceneWidthBuffer = buffer;
		zSize = size;
	}

	// Assign index for object
	// This also has the responsibility of adding indices for the linked platform
	public override void AssignIndexToObject(BasicObject targetObject, int index)
	{
		base.AssignIndexToObject(targetObject, index);
		
		for (int i = 0; i < linkedPlatforms.Count; ++i) {
			if (linkedPlatforms[i].AssignIndexToObject(targetObject, index)) {
				PlatformObject platform = targetObject as PlatformObject;
				platform.EnableLinkScenes();
				(targetObject as SceneObject).isForSectionTransition = platform.isForSectionTransition;
				break;
			}
		}
	}

	// Override to add additional restriction
	public override bool CanSpawnObject(float distance, ObjectSpawnData spawnData)
	{
		if (!base.CanSpawnObject(distance, spawnData))
			return false;
		
		int platformLocalIndex = ObjectHistory.instance.GetFirstPlatformIndex();
		if (platformLocalIndex == -1)
			return false;
		
		// Get the platform that this scene is going to spawn next to. 
		// See if the platform requires linked scenes and if it does, if this scene fulfills that requirement.
		PlatformObject platform = ObjectPool.instance.GetObjectFromLocalIndex(platformLocalIndex, ObjectType.Platform) as PlatformObject;
		
		if (platform.HasLinkedScenes()) {
			for (int i = 0; i < linkedPlatforms.Count; ++i) {
				if (linkedPlatforms[i].CanSpawnObject(platformLocalIndex)) {
					return true;
				}
			}
			return false;
		} else if (linkedPlatforms.Count > 0) { // return false if this scene is linked to a platform but the platform doesn't have any linked scenes
			return false;
		}
		
		// if the platform can't fit, then don't spawn it
		float totalDistance = ObjectHistory.instance.GetTotalDistance(false);
		float largestScene = spawnData.largestScene;
		float sceneBuffer = (spawnData.useWidthBuffer ? platformSceneWidthBuffer : 0); // useWidthBuffer contains the information if we should spawn up to totalDistance
		
		if (totalDistance - distance - sceneBuffer - largestScene >= 0) {
			// largest scene of 0 means we are approaching a turn and it doesn't matter what size object is spawned as long as it fits
			if (largestScene == 0) {
				return totalDistance - distance - sceneBuffer >= zSize;
			} else {
				return largestScene >= zSize;
			}
		}
		
		return false;
	}
}