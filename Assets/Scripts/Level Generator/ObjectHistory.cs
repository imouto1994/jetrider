using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Object Direction Slots
public enum Direction { Left, Center, Right, Count }

/* 
 * SINGLETON CLASS for history of spawned objects 
 * Local index is the index of the object within its own array
 * Object index is the index of the object unique to all of the other objects (array independent)
 * Spawn index is the index of the object spawned within its own object type. 
 */
public class ObjectHistory : MonoBehaviour
{
	
	static public ObjectHistory instance;

	// The relative direction of the objects being spawned: Center, Right, Left
	private Direction activeDirection;

	// Spawn index for each object index in each direction
	private List<int>[] objectSpawnIndex;
	// Spawn index for each object type in each direction
	private int[][] objectTypeSpawnIndex;
	
	// Last local index for each given object type in each direction
	private int[][] lastLocalIndex;
	// Spawn location (distance) for each object index in each direction
	private List<float>[] lastObjectSpawnDistance;
	// Distance of the last spawned object for the each object type in each direction
	private float[][] lastObjectTypeSpawnDistance;
	
	// Angle for each direction
	private float[] objectDirectionAngle;
	
	// The total distance spawned for both platforms and scenes in each direction
	private float[] totalDistance;
	private float[] totalSceneDistance;
	
	// Distance at which the platform spawned. 
	// Indexes will be removed from this list when a scene object has spawned over it.
	private PlatformDistanceDataMap[] platformDistanceDataMap;
	
	/* Keep track of the top-most and bottom-most objects in the scene hierarchy. 
	 * When a new object is spawned, it is placed as the parent of the respective previous objects. 
	 * When the generator moves the platforms and scenes, it will only need to move the top-most object. 
	 * It will also only need to check the bottom-most object to see if it needs to be removed
	 */
	// Top & Bottom Platforms in each direction
	private BasicObject[] topPlatformObjectSpawned;
	private BasicObject[] bottomPlatformObjectSpawned;

	// Top & Bottom Scenes in each direction
	private BasicObject[] topSceneObjectSpawned;
	private BasicObject[] bottomSceneObjectSpawned;

	// Top & Bottom Turn Platform
	private BasicObject topTurnPlatformObjectSpawned;
	private BasicObject bottomTurnPlatformObjectSpawned;

	// Top & Bottom Turn Scene
	private BasicObject topTurnSceneObjectSpawned;
	private BasicObject bottomTurnSceneObjectSpawned;
	
	// The previous section that occurred for each direction 
	private int[] previousPlatformSection;
	private int[] previousSceneSection;
	private bool[] spawnedPlatformSectionTransition;
	private bool[] spawnedSceneSectionTransition;

	// Store objects for reset game
	private BasicObject savedObjects;

	public void Awake()
	{
		instance = this;
	}

	// Initialize function
	public void Init(int objectCount)
	{
		activeDirection = Direction.Center;
		objectSpawnIndex = new List<int>[(int)Direction.Count];
		objectTypeSpawnIndex = new int[(int)Direction.Count][];
		lastLocalIndex = new int[(int)Direction.Count][];
		lastObjectTypeSpawnDistance = new float[(int)Direction.Count][];
		lastObjectSpawnDistance = new List<float>[(int)Direction.Count];
		
		objectDirectionAngle = new float[(int)Direction.Count];
		
		totalDistance = new float[(int)Direction.Count];
		totalSceneDistance = new float[(int)Direction.Count];
		
		platformDistanceDataMap = new PlatformDistanceDataMap[(int)Direction.Count];
		
		topPlatformObjectSpawned = new BasicObject[(int)Direction.Count];
		bottomPlatformObjectSpawned = new BasicObject[(int)Direction.Count];
		topSceneObjectSpawned = new BasicObject[(int)Direction.Count];
		bottomSceneObjectSpawned = new BasicObject[(int)Direction.Count];
		
		previousPlatformSection = new int[(int)Direction.Count];
		previousSceneSection = new int[(int)Direction.Count];
		spawnedPlatformSectionTransition = new bool[(int)Direction.Count];
		spawnedSceneSectionTransition = new bool[(int)Direction.Count];
		
		for (int i = 0; i < (int)Direction.Count; ++i) {
			objectSpawnIndex[i] = new List<int>();
			objectTypeSpawnIndex[i] = new int[(int)ObjectType.Count];
			lastLocalIndex[i] = new int[(int)ObjectType.Count];
			lastObjectTypeSpawnDistance[i] = new float[(int)ObjectType.Count];
			
			lastObjectSpawnDistance[i] = new List<float>();
			
			platformDistanceDataMap[i] = new PlatformDistanceDataMap();
			
			for (int j = 0; j < objectCount; ++j) {
				objectSpawnIndex[i].Add(-1);
				lastObjectSpawnDistance[i].Add(0);
			}
			for (int j = 0; j < (int)ObjectType.Count; ++j) {
				objectTypeSpawnIndex[i][j] = -1;
				lastLocalIndex[i][j] = -1;
				lastObjectTypeSpawnDistance[i][j] = -1;
			}
		}
	}
	
	// Reset history data for new turn on the center
	public void ResetTurnCount()
	{
		for (int i = 0; i < objectSpawnIndex[(int)Direction.Center].Count; ++i) {
			objectSpawnIndex[(int)Direction.Left][i] = objectSpawnIndex[(int)Direction.Right][i] 
														  = objectSpawnIndex[(int)Direction.Center][i];
			lastObjectSpawnDistance[(int)Direction.Left][i] = lastObjectSpawnDistance[(int)Direction.Right][i] 
																 = lastObjectSpawnDistance[(int)Direction.Center][i];
		}
		
		for (int i = 0; i < (int)ObjectType.Count; ++i) {
			objectTypeSpawnIndex[(int)Direction.Left][i] = objectTypeSpawnIndex[(int)Direction.Right][i] 
															  = objectTypeSpawnIndex[(int)Direction.Center][i];
			lastLocalIndex[(int)Direction.Left][i] = lastLocalIndex[(int)Direction.Right][i] 
														= lastLocalIndex[(int)Direction.Center][i];
			lastObjectTypeSpawnDistance[(int)Direction.Left][i] = lastObjectTypeSpawnDistance[(int)Direction.Right][i] 
																	   = lastObjectTypeSpawnDistance[(int)Direction.Center][i];
		}
		
		totalDistance[(int)Direction.Left] = totalDistance[(int)Direction.Right] 
												= totalDistance[(int)Direction.Center];
		// on a turn, the scene catches up to the platforms, so the total scene distance equals the total distance
		totalSceneDistance[(int)Direction.Left] = totalSceneDistance[(int)Direction.Right] 
													 = totalDistance[(int)Direction.Center];
		objectDirectionAngle[(int)Direction.Left] = objectDirectionAngle[(int)Direction.Right] 
													  = objectDirectionAngle[(int)Direction.Center];
		
		platformDistanceDataMap[(int)Direction.Left].ResetValues();
		platformDistanceDataMap[(int)Direction.Right].ResetValues();
		
		previousPlatformSection[(int)Direction.Left] = previousPlatformSection[(int)Direction.Right] 
														  = previousPlatformSection[(int)Direction.Center];
		previousSceneSection[(int)Direction.Left] = previousSceneSection[(int)Direction.Right] 
													   = previousSceneSection[(int)Direction.Center];
		spawnedPlatformSectionTransition[(int)Direction.Left] = spawnedPlatformSectionTransition[(int)Direction.Right] 
																   = spawnedPlatformSectionTransition[(int)Direction.Center];
		spawnedSceneSectionTransition[(int)Direction.Left] = spawnedSceneSectionTransition[(int)Direction.Right] 
																= spawnedSceneSectionTransition[(int)Direction.Center];
	}
	
	// The player has turned. Replace the center values with the corresponding turn values if they are valid
	public void Turn(Direction direction)
	{
		for (int i = 0; i < objectSpawnIndex[(int)Direction.Center].Count; ++i) {
			lastObjectSpawnDistance[(int)Direction.Center][i] = lastObjectSpawnDistance[(int)direction][i];

			if (objectSpawnIndex[(int)direction][i] != -1) {
				objectSpawnIndex[(int)Direction.Center][i] = objectSpawnIndex[(int)direction][i];
			}
		}
		
		for (int i = 0; i < (int)ObjectType.Count; ++i) {
			if (objectTypeSpawnIndex[(int)direction][i] != -1) {
				objectTypeSpawnIndex[(int)Direction.Center][i] = objectTypeSpawnIndex[(int)direction][i];
			}
			
			lastLocalIndex[(int)Direction.Center][i] = lastLocalIndex[(int)direction][i];
			lastObjectTypeSpawnDistance[(int)Direction.Center][i] = lastObjectTypeSpawnDistance[(int)direction][i];
		}
		
		totalDistance[(int)Direction.Center] = totalDistance[(int)direction];
		totalSceneDistance[(int)Direction.Center] = totalSceneDistance[(int)direction];
		objectDirectionAngle[(int)Direction.Center] = objectDirectionAngle[(int)direction];
		
		platformDistanceDataMap[(int)Direction.Center].CopyFrom(platformDistanceDataMap[(int)direction]);
		
		previousPlatformSection[(int)Direction.Center] = previousPlatformSection[(int)direction];
		previousSceneSection[(int)Direction.Center] = previousSceneSection[(int)direction];
		spawnedPlatformSectionTransition[(int)Direction.Center] = spawnedPlatformSectionTransition[(int)direction];
		spawnedSceneSectionTransition[(int)Direction.Center] = spawnedSceneSectionTransition[(int)direction];
		
		// Use the center direction if there aren't any objects in the direction across from the turn direction
		Direction acrossDirection = (direction == Direction.Right ? Direction.Left : Direction.Right);
		if (bottomPlatformObjectSpawned[(int)acrossDirection] == null) {
			acrossDirection = Direction.Center;
		}
		
		if (topTurnPlatformObjectSpawned != null) {
			topTurnPlatformObjectSpawned.SetObjectParent(topPlatformObjectSpawned[(int)Direction.Center]);
		} else {
			bottomTurnPlatformObjectSpawned = bottomPlatformObjectSpawned[(int)acrossDirection];
		}
		topTurnPlatformObjectSpawned = topPlatformObjectSpawned[(int)Direction.Center];

		if (topTurnSceneObjectSpawned != null) {
			topTurnSceneObjectSpawned.SetObjectParent(topSceneObjectSpawned[(int)Direction.Center]);
		} else {
			bottomTurnSceneObjectSpawned = bottomSceneObjectSpawned[(int)acrossDirection];
		}
		topTurnSceneObjectSpawned = topSceneObjectSpawned[(int)Direction.Center];

		topPlatformObjectSpawned[(int)Direction.Center] = topPlatformObjectSpawned[(int)direction];
		bottomPlatformObjectSpawned[(int)Direction.Center] = bottomPlatformObjectSpawned[(int)direction];
		topSceneObjectSpawned[(int)Direction.Center] = topSceneObjectSpawned[(int)direction];
		bottomSceneObjectSpawned[(int)Direction.Center] = bottomSceneObjectSpawned[(int)direction];
		for (int i = (int)Direction.Left; i < (int)Direction.Count; i += 2) {
			topPlatformObjectSpawned[i] = null;
			bottomPlatformObjectSpawned[i] = null;
			topSceneObjectSpawned[i] = null;
			bottomSceneObjectSpawned[i] = null;
		}
	}

	// Driver function for handling spawning objects
	public BasicObject ObjectSpawned(int index, float locationOffset, Direction location, float angle, ObjectType objectType)
	{
		return ObjectSpawned(index, locationOffset, location, angle, objectType, null);
	}
	
	// Keep track of the object spawned. Returns the previous object at the top position
	public BasicObject ObjectSpawned(int index, float locationOffset, Direction direction, 
	                                 float angle, ObjectType objectType, BasicObject currentObject)
	{
		lastObjectSpawnDistance[(int)direction][index] = (objectType == ObjectType.Scene ? totalSceneDistance[(int)direction] : totalDistance[(int)direction]) + locationOffset;
		objectTypeSpawnIndex[(int)direction][(int)objectType] += 1;
		objectSpawnIndex[(int)direction][index] = objectTypeSpawnIndex[(int)direction][(int)objectType];
		lastObjectTypeSpawnDistance[(int)direction][(int)objectType] = lastObjectSpawnDistance[(int)direction][index];
		lastLocalIndex[(int)direction][(int)objectType] = ObjectPool.instance.ObjectIndexToLocalIndex(index, objectType);
		
		BasicObject prevTopObject = null;
		if (objectType == ObjectType.Platform) {
			prevTopObject = topPlatformObjectSpawned[(int)direction];
			topPlatformObjectSpawned[(int)direction] = currentObject;
			objectDirectionAngle[(int)direction] = angle;
		} else if (objectType == ObjectType.Scene) {
			prevTopObject = topSceneObjectSpawned[(int)direction];
			topSceneObjectSpawned[(int)direction] = currentObject;
		}
		
		return prevTopObject;
	}
	
	// Set bottom object
	public void SetBottomObject(Direction direction, bool isSceneObject, BasicObject currentObject)
	{
		if (isSceneObject) {
			bottomSceneObjectSpawned[(int)direction] = currentObject;
		} else {
			bottomPlatformObjectSpawned[(int)direction] = currentObject;
		}
	}

	// Remove the bottom object from the given direction and assign new bottom object
	public void ObjectRemoved(Direction direction, bool isSceneObject)
	{
		if (isSceneObject) {
			bottomSceneObjectSpawned[(int)direction] = bottomSceneObjectSpawned[(int)direction].GetObjectParent();
			if (bottomSceneObjectSpawned[(int)direction] == null) {
				topSceneObjectSpawned[(int)direction] = null;
			}
		} else {
			bottomPlatformObjectSpawned[(int)direction] = bottomPlatformObjectSpawned[(int)direction].GetObjectParent();
			if (bottomPlatformObjectSpawned[(int)direction] == null) {
				topPlatformObjectSpawned[(int)direction] = null;
			}
		}
	}

	// Remove the bottom turn object
	public void TurnObjectRemoved(bool isSceneObject)
	{
		if (isSceneObject) {
			bottomTurnSceneObjectSpawned = bottomTurnSceneObjectSpawned.GetObjectParent();
			if (bottomTurnSceneObjectSpawned == null) {
				topTurnSceneObjectSpawned = null;
			}
		} else {
			bottomTurnPlatformObjectSpawned = bottomTurnPlatformObjectSpawned.GetObjectParent();
			if (bottomTurnPlatformObjectSpawned == null) {
				topTurnPlatformObjectSpawned = null;
			}
		}
	}
	
	// Increase the distance travelled by the specified amount
	public void AddTotalDistance(float amount, Direction direction, bool isSceneObject, int section)
	{
		if (isSceneObject) {
			totalSceneDistance[(int)direction] += amount;
			totalSceneDistance[(int)direction] = ((int)(totalSceneDistance[(int)direction] * 1000f)) / 1000f;
			if (Mathf.Abs(totalSceneDistance[(int)direction] - totalDistance[(int)direction]) < 0.1f) {
				totalSceneDistance[(int)direction] = totalDistance[(int)direction];
			}
			platformDistanceDataMap[(int)direction].CheckForRemoval(totalSceneDistance[(int)direction]);
		} else {
			totalDistance[(int)direction] += amount;
			totalDistance[(int)direction] = ((int)(totalDistance[(int)direction] * 1000f)) / 1000f;
			platformDistanceDataMap[(int)direction].AddIndex(totalDistance[(int)direction], lastLocalIndex[(int)direction][(int)ObjectType.Platform], section);
		}
	}

	// Set the new active direction
	public void SetActiveDirection(Direction direction)
	{
		activeDirection = direction;
	}
	
	// Returns the spawn index for the given object type
	public int GetObjectTypeSpawnIndex(ObjectType objectType)
	{
		return objectTypeSpawnIndex[(int)activeDirection][(int)objectType];
	}
	
	// Returns the spawn index for the given object index
	public int GetObjectSpawnIndex(int index)
	{
		return objectSpawnIndex[(int)activeDirection][index];
	}
	
	// Returns the local index for the given object type
	public int GetLastLocalIndex(ObjectType objectType)
	{
		return GetLastLocalIndex(activeDirection, objectType);
	}
	
	// Returns the local index for the given object type at the object direction
	public int GetLastLocalIndex(Direction direction, ObjectType objectType)
	{
		return lastLocalIndex[(int)direction][(int)objectType];
	}
	
	// Returns the spawn direction (distance) for the given object index
	public float GetSpawnDistance(int index)
	{
		return lastObjectSpawnDistance[(int)activeDirection][index];
	}
	
	// returns the distance of the last spawned object for the given object type
	public float GetLastObjectTypeSpawnDistance(ObjectType objectType)
	{
		return lastObjectTypeSpawnDistance[(int)activeDirection][(int)objectType];
	}
	
	// Returns the angle of given direction for a scene object or platform object
	public float GetObjectDirectionAngle(Direction direction)
	{
		return objectDirectionAngle[(int)direction];
	}
	
	// Returns the total distance for a scene object or platform object
	public float GetTotalDistance(bool isSceneObject)
	{
		return (isSceneObject ? totalSceneDistance[(int)activeDirection] : totalDistance[(int)activeDirection]);
	}
	
	// Returns the local index of the first platform
	public int GetFirstPlatformIndex()
	{
		return platformDistanceDataMap[(int)activeDirection].FirstIndex();
	}
	
	// Returns the section of the first platform
	public int GetFirstPlatformSection()
	{
		return platformDistanceDataMap[(int)activeDirection].FirstSection();
	}
	
	// Returns the top-most platform or scene object
	public BasicObject GetTopInfiniteObject(Direction direction, bool isSceneObject)
	{
		return (isSceneObject ? topSceneObjectSpawned[(int)direction] : topPlatformObjectSpawned[(int)direction]);
	}
	
	// Returns the bottom-most platform or scene object
	public BasicObject GetBottomInfiniteObject(Direction direction, bool isSceneObject)
	{
		return (isSceneObject ? bottomSceneObjectSpawned[(int)direction] : bottomPlatformObjectSpawned[(int)direction]);
	}
	
	// Returns the top-most turn platform or scene object
	public BasicObject GetTopTurnInfiniteObject(bool isSceneObject)
	{
		return (isSceneObject ? topTurnSceneObjectSpawned : topTurnPlatformObjectSpawned);
	}
	
	// Returns the bottom-most turn platform or scene object
	public BasicObject GetBottomTurnInfiniteObject(bool isSceneObject)
	{
		return (isSceneObject ? bottomTurnSceneObjectSpawned : bottomTurnPlatformObjectSpawned);
	}

	// Set the previous section index for the given direction
	public void SetPreviousSection(Direction direction, bool isSceneObject, int section)
	{
		if (isSceneObject) {
			previousSceneSection[(int)direction] = section;
			spawnedSceneSectionTransition[(int)direction] = false;
		} else {
			previousPlatformSection[(int)direction] = section;
			spawnedPlatformSectionTransition[(int)direction] = false;
		}
	}

	// Returns the previous section index for the given direction
	public int GetPreviousSection(Direction direction, bool isSceneObject)
	{
		return (isSceneObject ? previousSceneSection[(int)direction] : previousPlatformSection[(int)direction]);
	}

	// Handler for spawning section transition
	public void DidSpawnSectionTransition(Direction direction, bool isSceneObject)
	{
		if (isSceneObject) {
			spawnedSceneSectionTransition[(int)direction] = true;
		} else {
			spawnedPlatformSectionTransition[(int)direction] = true;
		}
	}

	// Check whether it has spawned a section transition 
	public bool HasSpawnedSectionTransition(Direction direction, bool isSceneObject)
	{
		return (isSceneObject ? spawnedSceneSectionTransition[(int)direction] : spawnedPlatformSectionTransition[(int)direction]);
	}
}