using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SpawnProbs))]
[RequireComponent(typeof(SpawnRules))]
public class PlatformObject : BasicObject
{
	
	public delegate void PlatformDeactivationHandler();
	public event PlatformDeactivationHandler OnPlatformDeactivation;
	
	// Set this offset if the platform object's center doesn't match up with the true center 
	public Vector3 centerOffset;
	
	// True if this piece is used for section transitions
	public bool isForSectionTransition;
	// If section transition is true, this list contains the sections that it can transition from (used with the toSection list)
	public List<int> fromSection;
	// If section transition is true, this list contains the sections that it can transition to (used with the fromSection list)
	public List<int> toSection;
	
	// Direction of platform. At least one option must be true. Straight is the most common so is the default
	public bool isStraight = true;
	public bool isLeftTurn;
	public bool isRightTurn;

	// Force different collidable object types to spawn on top of the platform, such as obstacle and coin
	// (assuming the propabilities allow the object to spawn)
	public bool hasDifferentCollidables;
	
	// the number of collidable objects that can fit on one platform. The objects are spaced along the local z position of the platform
	public int numCollidables;
	
	// boolean to determine what horizontal location objects can spawn. If collidablePositions is greater than 0 then at least one
	// of these booleans must be true
	public bool collidableLeftSpawn;
	public bool collidableCenterSpawn;
	public bool collidableRightSpawn;

	private int availableSlots;
	private int numSpawnedCollidables;
	
	// Indicator if a scene object has linked to this platform. No other scene objects will be able to spawn near this object.
	private bool hasLinkedScenes;
	
	public override void Init()
	{
		base.Init();
		objectType = ObjectType.Platform;
	}
	
	public override void Awake()
	{
		base.Awake();
		numSpawnedCollidables = 0;
		hasLinkedScenes = false;
		
		availableSlots = 0;
		if (collidableLeftSpawn) {
			availableSlots |= 1;
		}
		if (collidableCenterSpawn) {
			availableSlots |= 2;
		}
		if (collidableRightSpawn) {
			availableSlots |= 4;
		}
		
		CollidableObject[] collidableObjects = GetComponentsInChildren<CollidableObject>();
		for (int i = 0; i < collidableObjects.Length; ++i) {
			collidableObjects[i].SetStartParent(collidableObjects[i].transform.parent);
		}
		
		// If this platfom doesn't have any direction enabled then it won't do anything
		if (!isStraight && !isLeftTurn && !isRightTurn) {
			Debug.LogWarning(thisGameObject.name + " has no direction set.");
			isStraight = true;
		}
	}

	// Enable the platform object linking with certain scenes
	public void EnableLinkScenes()
	{
		hasLinkedScenes = true;
	}

	// Check if the platform has linked scenes
	public bool HasLinkedScenes()
	{
		return hasLinkedScenes;
	}

	// Get the available slots for spawning children 
	public int GetSlotsAvailable()
	{
		return availableSlots;
	}
	
	// Check if the platform is able to spawn the coolidable at the given position
	public bool CanSpawnCollidable(int pos)
	{
		return (numSpawnedCollidables & (int)Mathf.Pow(2, pos)) == 0;
	}

	// Check if the platform can spawn more collidables
	public bool CanSpawnCollidable()
	{
		return numCollidables != 0 && numSpawnedCollidables != (int)Mathf.Pow(2, numCollidables) - 1;
	}

	// Spawn a collidable at the given position
	public void SpawnCollidable(int pos)
	{
		numSpawnedCollidables |= (int)Mathf.Pow(2, pos);
	}

	// Orient the platform
	public override void Orient(Vector3 position, Quaternion rotation)
	{
		base.Orient(position, rotation);
		// reset the number of collidables that have been spawned on top of the platform
		numSpawnedCollidables = 0;
	}

	// Deactivate the platform
	public override void Deactivate()
	{
		// Platforms have collidable children. Make sure they get deactivated properly
		if (OnPlatformDeactivation != null) {
			OnPlatformDeactivation();
			OnPlatformDeactivation = null;
		}
		
		base.Deactivate();
	}
}
