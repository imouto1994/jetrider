using UnityEngine;

// Inherit from SpawnRules class
// Add certain additional restrictions specifically for Platform
public class PlatformSpawnRules : SpawnRules
{
	// The attached platform object
	private PlatformObject platformObject;
	
	public override void Init()
	{
		base.Init();
		
		platformObject = GetComponent<PlatformObject>();
	}

	public override bool CanSpawnObject(float distance, ObjectSpawnData spawnData)
	{
		if (!base.CanSpawnObject(distance, spawnData))
			return false;
		
		// If section transition is true a transition object must be found
		if (spawnData.sectionTransition) {
			if (platformObject.isForSectionTransition) {
				// any transition is a section transition if there are no specific section transitions defined
				if (platformObject.fromSection.Count == 0) {
					return true;
				}
				// return true if the from section equals the previous section and matches up with the to section which equals the current section
				// fromSection and toSection must be equal in size
				for (int i = 0; i < platformObject.fromSection.Count; ++i) {
					if (platformObject.fromSection[i] == spawnData.prevSection && platformObject.toSection[i] == spawnData.section) {
						return true;
					}
				}
			}
			return false;
		}
		
		// Prevent multiple turns from spawning within the same object location.
		if (spawnData.turnSpawned && (platformObject.isLeftTurn || platformObject.isRightTurn)) {
			return false;
		}
		
		return !platformObject.isForSectionTransition;
	}
}