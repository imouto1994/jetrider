using UnityEngine;

/* Rules of linking an object with specific platforms */
[System.Serializable]
public class PlatformLinkRule
{
	// Target linked platform
	public BasicObject platform;
	// Indicator whether the object can spawn with this platform 
	public bool canSpawn;
	// The local index of this platform
	private int platformIndex;
	
	public PlatformLinkRule(BasicObject p, bool c)
	{
		platform = p;
		canSpawn = c;
	}

	// Check if it is able to assign the local index to the target platform
	public bool AssignIndexToObject(BasicObject targetObject, int index)
	{
		if (targetObject == platform) {
			platformIndex = index;
			return true;
		}
		return false;
	}

	// Check if the rule will pass for the given local index of platform
	public bool CanSpawnObject(int index)
	{
		if (index == platformIndex) {
			return canSpawn;
		}
		return !canSpawn;
	}
}