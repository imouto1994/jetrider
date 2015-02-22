using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public struct ObjectSpawnData
{
	public float largestScene;
	public bool useWidthBuffer;
	public int slotPositions;
	public int section;
	public bool sectionTransition;
	public int prevSection;
	public bool turnSpawned;
}

/*
 * Used in conjuction with the object generator, the object pool keeps track of all of the objects. 
 * The infinite object generator requests a new object through getNextObjectIndex/objectFromPool 
 * and the object pool will return the object based on the appearance rules/ probability.
 */
public class ObjectPool : MonoBehaviour
{
	
	static public ObjectPool instance;
	
	// Number of cached objects
	public int prespawnCache = 0;
	
	// Platforms:
	public PlatformObject[] platforms;
	public Transform platformParent;
	
	// Scenes
	public SceneObject[] scenes;
	public Transform sceneParent;
	
	// Obstacles:
	public ObstacleObject[] obstacles;
	public Transform obstacleParent;
	
	// Coins:
	public CollidableObject[] coins;
	public Transform coinParent;

	// Fuels
	public CollidableObject[] fuels;
	public Transform fuelParent;
	
	// Power ups:
	public PowerUpObject[] powerUps;
	public Transform powerUpParent;

	// Save all of the instantiated platforms in a pool to prevent instantiating and destroying objects
	private List<List<BasicObject>> objectsPool;
	private List<int> objectPoolIndex;
	
	private List<AppearRules> appearanceRules;
	private List<AppearProbs> appearanceProbability;
	private List<float> probabilityCache;
	private List<bool> objectCanSpawnCache;
	
	public void Awake()
	{
		instance = this;
	}
	
	public void Init()
	{
		objectsPool = new List<List<BasicObject>>();
		objectPoolIndex = new List<int>();
		
		appearanceRules = new List<AppearRules>();
		appearanceProbability = new List<AppearProbs>();
		probabilityCache = new List<float>();
		objectCanSpawnCache = new List<bool>();
		
		int totalObjs = platforms.Length + scenes.Length + obstacles.Length + coins.Length + powerUps.Length;
		BasicObject infiniteObject;
		for (int i = 0; i < totalObjs; ++i) {
			objectsPool.Add(new List<BasicObject>());
			objectPoolIndex.Add(0);
			
			probabilityCache.Add(0);
			objectCanSpawnCache.Add(false);
			
			infiniteObject = ObjectIndexToObject(i);
			infiniteObject.Init();
			appearanceRules.Add(infiniteObject.GetComponent<AppearRules>());
			appearanceRules[i].Init();
			appearanceProbability.Add(infiniteObject.GetComponent<AppearProbs>());
			appearanceProbability[i].Init();
		}
		
		// wait until all of the appearance rules have been initialized before the object index is assigned
		for (int i = 0; i < totalObjs; ++i) {
			infiniteObject = ObjectIndexToObject(i);
			for (int j = 0; j < totalObjs; ++j) {
				ObjectIndexToObject(j).GetComponent<AppearRules>().AssignIndexToObject(infiniteObject, i);
			}
		}
		
		// cache a fixed amount of each type of object before the game starts
		List<BasicObject> infiniteObjects = new List<BasicObject>();
		for (int i = 0; i < prespawnCache; ++i) {
			for (int j = 0; j < platforms.Length; ++j) {
				infiniteObjects.Add(ObjectFromPool(j, ObjectType.Platform));
			}
			for (int j = 0; j < scenes.Length; ++j) {
				infiniteObjects.Add(ObjectFromPool(j, ObjectType.Scene));
			}
		}
		
		for (int i = 0; i < infiniteObjects.Count; ++i) {
			infiniteObjects[i].Deactivate();
		}
	}
	
	// Measure the size of the platforms and scenes
	public void GetObjectSizes(out Vector3[] platformSizes, out Vector3[] sceneSizes, out float largestSceneLength)
	{	
		/*
		platformSizes = new Vector3[platforms.Length];
		for (int i = 0; i < platforms.Length; ++i) {
			
			if (platforms[i].overrideSize != Vector3.zero) {
				platformSizes[i] = platforms[i].overrideSize;
			} else {
				Renderer platformRenderer = platforms[i].GetComponent<Renderer>();
				if (platformRenderer == null) {
					Debug.LogError("Error: platform " + platforms[i].name + " has no renderer attached and does not override the size. Add a renderer or override the size to remove this error.");
					platformSizes[i] = Vector3.zero;
					continue;
				}
				platformSizes[i] = platforms[i].GetComponent<Renderer>().bounds.size;
				Vector3 heightChange = platformSizes[i];
				if (platforms[i].slope != PlatformSlope.None) {
					heightChange.y *= platforms[i].slope == PlatformSlope.Down ? -1 : 1;
				} else {
					heightChange.y = 0;
				}
				platformSizes[i] = heightChange;
			}
		}
		
		// the parent scene object must represent the children's size
		sceneSizes = new Vector3[scenes.Length];
		largestSceneLength = 0;
		for (int i = 0; i < scenes.Length; ++i) {
			if (scenes[i].overrideSize != Vector3.zero) {
				sceneSizes[i] = scenes[i].overrideSize;
				sceneSizes[i] += scenes[i].centerOffset;
				SceneAppearanceRules sceneAppearanceRule = scenes[i].GetComponent<SceneAppearanceRules>();
				if (sceneAppearanceRule.linkedPlatforms.Count == 1) {
					PlatformObject linkedPlatform = sceneAppearanceRule.linkedPlatforms[0].platform as PlatformObject;
					if (linkedPlatform.slope == PlatformSlope.None) {
						sceneSizes[i].y = 0;
					}
				}
			} else {
				Renderer sceneRenderer = scenes[i].GetComponent<Renderer>();
				if (sceneRenderer == null) {
					Debug.LogError("Error: scene " + scenes[i].name + " has no renderer attached and does not override the size. Add a renderer or override the size to remove this error.");
					sceneSizes[i] = Vector3.zero;
					continue;
				}
				sceneSizes[i] = scenes[i].GetComponent<Renderer>().bounds.size;
				sceneSizes[i] += scenes[i].centerOffset;
				sceneSizes[i].y = 0;
				SceneAppearanceRules sceneAppearanceRule = scenes[i].GetComponent<SceneAppearanceRules>();
				if (sceneAppearanceRule.linkedPlatforms.Count == 1) {
					PlatformObject linkedPlatform = sceneAppearanceRule.linkedPlatforms[0].platform as PlatformObject;
					if (linkedPlatform.slope != PlatformSlope.None) {
						sceneSizes[i].y = linkedPlatform.GetComponent<Renderer>().bounds.size.y * (linkedPlatform.slope == PlatformSlope.Down ? -1 : 1);
					}
				}
				
			}
			if (largestSceneLength < sceneSizes[i].z) {
				largestSceneLength = sceneSizes[i].z;
			}
		}
		
		// The scene appearance rules need to know how much buffer space there is between the platform and scene
		if (sceneSizes.Length > 0) {
			float buffer = (sceneSizes[0].x - platformSizes[0].x) / 2 + platformSizes[0].x;
			for (int i = 0; i < scenes.Length; ++i) {
				scenes[i].GetComponent<SceneAppearanceRules>().SetSizes(buffer, sceneSizes[i].z);
			}
		}
		*/
	}
	
	public void GetObjectStartPositions(out Vector3[] platformStartPosition, out Vector3[] sceneStartPosition)
	{
		platformStartPosition = new Vector3[platforms.Length];
		for (int i = 0; i < platforms.Length; ++i) {
			platformStartPosition[i] = platforms[i].GetStartPosition();
		}
		
		sceneStartPosition = new Vector3[scenes.Length];
		for (int i = 0; i < scenes.Length; ++i) {
			sceneStartPosition[i] = scenes[i].GetStartPosition();
		}
	}
	
	// Returns the specified object from the pool
	public BasicObject ObjectFromPool(int localIndex, ObjectType objectType)
	{
		BasicObject obj = null;
		int objectIndex = LocalIndexToObjectIndex(localIndex, objectType);
		List<BasicObject> objectPool = objectsPool[objectIndex];
		int poolIndex = objectPoolIndex[objectIndex];
		
		// keep a start index to prevent the constant pushing and popping from the list		
		if (objectPool.Count > 0 && objectPool[poolIndex].IsActive() == false) {
			obj = objectPool[poolIndex];
			objectPoolIndex[objectIndex] = (poolIndex + 1) % objectPool.Count;
			return obj;
		}
		
		// No inactive objects, need to instantiate a new one
		BasicObject[] objects = null;
		switch (objectType) {
		case ObjectType.Platform:
			objects = platforms;
			break;
		case ObjectType.Scene:
			objects = scenes;
			break;
		case ObjectType.Obstacle:
			objects = obstacles;
			break;
		case ObjectType.Donut:
			objects = coins;
			break;
		case ObjectType.PowerUp:
			objects = powerUps;
			break;
		case ObjectType.Fuel:
			objects = fuels;
			break;
		}
		
		obj = (GameObject.Instantiate(objects[localIndex].gameObject) as GameObject).GetComponent<InfiniteObject>();
		
		AssignParent(obj, objectType);
		obj.SetLocalIndex(localIndex);
		
		objectPool.Insert(poolIndex, obj);
		objectPoolIndex[objectIndex] = (poolIndex + 1) % objectPool.Count;
		return obj;
	}
	
	public void AssignParent(BasicObject currentObject, ObjectType objectType)
	{
		switch (objectType) {
		case ObjectType.Platform:
			currentObject.SetParent(platformParent);
			break;
		case ObjectType.Scene:
			currentObject.SetParent(sceneParent);
			break;
		case ObjectType.Obstacle:
			currentObject.SetParent(obstacleParent);
			break;
		case ObjectType.Donut:
			currentObject.SetParent(coinParent);
			break;
		case ObjectType.Fuel:
			currentObject.SetParent(fuelParent);
			break;
		case ObjectType.PowerUp:
			currentObject.SetParent(powerUpParent);
			break;
		}
	}
	
	// Converts local index to object index
	public int LocalIndexToObjectIndex(int localIndex, ObjectType objectType)
	{
		switch (objectType) {
		case ObjectType.Platform:
			return localIndex;
		case ObjectType.Scene:
			return platforms.Length + localIndex;
		case ObjectType.Obstacle:
			return platforms.Length + scenes.Length + localIndex;
		case ObjectType.Donut:
			return platforms.Length + scenes.Length + obstacles.Length + localIndex;
		case ObjectType.Fuel:
			return platforms.Length + scenes.Length + obstacles.Length + coins.Length + localIndex;
		case ObjectType.PowerUp:
			return platforms.Length + scenes.Length + obstacles.Length + coins.Length + fuels.Length + localIndex;
		}
		return -1; // error
	}
	// Converts object index to local index
	public int ObjectIndexToLocalIndex(int objectIndex, ObjectType objectType)
	{
		switch (objectType) {
		case ObjectType.Platform:
			return objectIndex;
		case ObjectType.Scene:
			return objectIndex - platforms.Length;
		case ObjectType.Obstacle:
			return objectIndex - platforms.Length - scenes.Length;
		case ObjectType.Donut:
			return objectIndex - platforms.Length - scenes.Length - obstacles.Length;
		case ObjectType.Fuel:
			return objectIndex - platforms.Length - scenes.Length - obstacles.Length - coins.Length;
		case ObjectType.PowerUp:
			return objectIndex - platforms.Length - scenes.Length - obstacles.Length - fuels.Length - coins.Length;
		}
		return -1; // error	
	}
	
	public BasicObject LocalIndexToInfiniteObject(int localIndex, ObjectType objectType)
	{
		switch (objectType) {
		case ObjectType.Platform:
			return platforms[localIndex];
		case ObjectType.Scene:
			return scenes[localIndex];
		case ObjectType.Obstacle:
			return obstacles[localIndex];
		case ObjectType.Donut:
			return coins[localIndex];
		case ObjectType.Fuel:
			return fuels[localIndex];
		case ObjectType.PowerUp:
			return powerUps[localIndex];
		}
		return null; // error	
	}
	
	// Returns the number of total objects
	public int GetTotalObjectCount()
	{
		return platforms.Length + scenes.Length + obstacles.Length + coins.Length + powerUps.Length;
	}
	
	// Converts the object index to an infinite object
	private BasicObject ObjectIndexToObject(int objectIndex)
	{
		if (objectIndex < platforms.Length) {
			return platforms[objectIndex];
		} else if (objectIndex < platforms.Length + scenes.Length) {
			return scenes[objectIndex - platforms.Length];
		} else if (objectIndex < platforms.Length + scenes.Length + obstacles.Length) {
			return obstacles[objectIndex - platforms.Length - scenes.Length];
		} else if (objectIndex < platforms.Length + scenes.Length + obstacles.Length + coins.Length) {
			return coins[objectIndex - platforms.Length - scenes.Length - obstacles.Length];
		} else if (objectIndex < platforms.Length + scenes.Length + obstacles.Length + coins.Length + powerUps.Length) {
			return powerUps[objectIndex - platforms.Length - scenes.Length - obstacles.Length - coins.Length];
		}
		return null;
	}
	
	/**
         * The next platform is determined by probabilities as well as object rules.
         * spawnData contains any extra data that is needed to make a decision if the object can be spawned
         */
	public int GetNextObjectIndex(ObjectType objectType, ObjectSpawnData spawnData)
	{
		InfiniteObject[] objects = null;
		switch (objectType) {
		case ObjectType.Platform:
			objects = platforms;
			break;
		case ObjectType.Scene:
			objects = scenes;
			break;
		case ObjectType.Obstacle:
			objects = obstacles;
			break;
		case ObjectType.Donut:
			objects = coins;
			break;
		case ObjectType.Fuel:
			objects = fuels;
			break;
		case ObjectType.PowerUp:
			objects = powerUps;
			break;
		}
		float totalProbability = 0;
		float probabilityAdjustment = 0;
		float distance = ObjectHistory.instance.GetTotalDistance(objectType == ObjectType.Scene);
		int objectIndex;
		for (int localIndex = 0; localIndex < objects.Length; ++localIndex) {
			objectIndex = LocalIndexToObjectIndex(localIndex, objectType);
			// cache the result
			objectCanSpawnCache[objectIndex] = appearanceRules[objectIndex].CanSpawnObject(distance, spawnData);
			if (!objectCanSpawnCache[objectIndex]) {
				continue;
			}
			
			probabilityAdjustment = appearanceRules[objectIndex].ProbabilityAdjustment(distance);
			// If the probability adjustment has a value of the float's max value then spawn this object no matter hwat
			if (probabilityAdjustment == float.MaxValue) {
				probabilityCache[objectIndex] = probabilityAdjustment;
				totalProbability = float.MaxValue;
				break;
			}
			
			probabilityCache[objectIndex] = appearanceProbability[objectIndex].GetProbability(distance) * probabilityAdjustment;
			totalProbability += probabilityCache[objectIndex];
		}
		
		// chance of spawning nothing (especially in the case of collidable objects)
		if (totalProbability == 0) {
			return -1;
		}
		
		float randomValue = Random.value;
		float prevObjProbability = 0;
		float objProbability = 0;
		// with the total probability we can determine a platform
		// minor optimization: don't check the last platform. If we get that far into the loop then regardless we are selecting that platform
		for (int localIndex = 0; localIndex < objects.Length - 1; ++localIndex) {
			objectIndex = LocalIndexToObjectIndex(localIndex, objectType);
			if (!objectCanSpawnCache[objectIndex]) {
				continue;
			}
			
			objProbability = probabilityCache[objectIndex];
			if (objProbability == float.MaxValue || randomValue <= (prevObjProbability + objProbability) / totalProbability) {
				return localIndex;
			}
			prevObjProbability += objProbability;
		}
		return objects.Length - 1;
	}
}