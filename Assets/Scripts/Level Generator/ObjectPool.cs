using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Basic data needed for spawning objects
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
 * SINGLETON CLASS for pooling objects
 * Used in conjuction with the object generator, the object pool keeps track of all of the objects. 
 * The infinite object generator requests a new object through getNextObjectIndex/objectFromPool 
 * and the object pool will return the object based on the appearance rules/ probability.
 */
public class ObjectPool : MonoBehaviour
{
	
	static public ObjectPool instance;
	
	// Platforms:
	public PlatformObject[] platforms;
	public Transform platformParent;
	
	// Scenes
	public SceneObject[] scenes;
	public Transform sceneParent;
	
	// Obstacles:
	public ObstacleObject[] obstacles;
	public Transform obstacleParent;
	
	// Donuts:
	public CollidableObject[] donuts;
	public Transform donutParent;

	// Fuels
	public CollidableObject[] fuels;
	public Transform fuelParent;
	
	// Power ups:
	public PowerUpObject[] powerUps;
	public Transform powerUpParent;

	// Save all of the instantiated platforms in a pool to prevent instantiating and destroying objects
	private List<List<BasicObject>> objectsPool;
	private List<int> objectPoolIndex;
	
	private List<SpawnRules> appearRules;
	private List<SpawnProbs> appearProbs;
	private List<float> probabilityCache;
	private List<bool> objectCanSpawnCache;
	
	public void Awake()
	{
		instance = this;
	}

	// Intialize function
	public void Init()
	{
		objectsPool = new List<List<BasicObject>>();
		objectPoolIndex = new List<int>();
		
		appearRules = new List<SpawnRules>();
		appearProbs = new List<SpawnProbs>();
		probabilityCache = new List<float>();
		objectCanSpawnCache = new List<bool>();
		
		int totalObjs = platforms.Length + scenes.Length + obstacles.Length + donuts.Length + powerUps.Length;
		BasicObject currentObject;
		for (int i = 0; i < totalObjs; ++i) {
			objectsPool.Add(new List<BasicObject>());
			objectPoolIndex.Add(0);
			
			probabilityCache.Add(0);
			objectCanSpawnCache.Add(false);
			
			currentObject = GetObjectFromObjectIndex(i);
			currentObject.Init();
			appearRules.Add(currentObject.GetComponent<SpawnRules>());
			appearRules[i].Init();
			appearProbs.Add(currentObject.GetComponent<SpawnProbs>());
			appearProbs[i].Init();
		}
		
		// wait until all of the appearance rules have been initialized before the object index is assigned
		for (int i = 0; i < totalObjs; ++i) {
			currentObject = GetObjectFromObjectIndex(i);
			for (int j = 0; j < totalObjs; ++j) {
				GetObjectFromObjectIndex(j).GetComponent<SpawnRules>().AssignIndexToObject(currentObject, i);
			}
		}
	}
	
	// Get the sizes of the platforms and scenes
	public void GetObjectSizes(out Vector3[] platformSizes, out Vector3[] sceneSizes, out float largestSceneLength)
	{	
		// Retrieve Platform sizes
		platformSizes = new Vector3[platforms.Length];
		for (int i = 0; i < platforms.Length; ++i) {
			Renderer platformRenderer = platforms[i].GetComponent<Renderer>();
			if (platformRenderer == null) {
				Debug.LogError("Error: platform " + platforms[i].name + " has no renderer attached");
				platformSizes[i] = Vector3.zero;
				continue;
			}
			platformSizes[i] = platforms[i].GetComponent<Renderer>().bounds.size;
		}
		
		// The parent scene object must represent the children's size
		sceneSizes = new Vector3[scenes.Length];
		largestSceneLength = 0;
		for (int i = 0; i < scenes.Length; ++i) {
			Renderer sceneRenderer = scenes[i].GetComponent<Renderer>();
			if (sceneRenderer == null) {
				Debug.LogError("Error: scene " + scenes[i].name + " has no renderer attached");
				sceneSizes[i] = Vector3.zero;
				continue;
			}
			sceneSizes[i] = scenes[i].GetComponent<Renderer>().bounds.size;
			sceneSizes[i] += scenes[i].centerOffset;
			sceneSizes[i].y = 0;

			if (largestSceneLength < sceneSizes[i].z) {
				largestSceneLength = sceneSizes[i].z;
			}
		}
		
		// The scene appearance rules need to know how much buffer space there is between the platform and scene
		if (sceneSizes.Length > 0) {
			float buffer = (sceneSizes[0].x - platformSizes[0].x) / 2 + platformSizes[0].x;
			for (int i = 0; i < scenes.Length; ++i) {
				scenes[i].GetComponent<SceneSpawnRules>().SetSizes(buffer, sceneSizes[i].z);
			}
		}
	}

	// Get the list of start positions for platform and scenes objects
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
	public BasicObject GetObjectFromPool(int localIndex, ObjectType objectType)
	{
		BasicObject obj = null;
		int objectIndex = GetObjectIndexFromLocalIndex(localIndex, objectType);
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
			objects = donuts;
			break;
		case ObjectType.PowerUp:
			objects = powerUps;
			break;
		case ObjectType.Fuel:
			objects = fuels;
			break;
		}
		
		obj = (GameObject.Instantiate(objects[localIndex].gameObject) as GameObject).GetComponent<BasicObject>();
		
		AssignParent(obj, objectType);
		obj.SetLocalIndex(localIndex);
		
		objectPool.Insert(poolIndex, obj);
		objectPoolIndex[objectIndex] = (poolIndex + 1) % objectPool.Count;
		return obj;
	}

	// Assign an abstract parent to an object
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
			currentObject.SetParent(donutParent);
			break;
		case ObjectType.Fuel:
			currentObject.SetParent(fuelParent);
			break;
		case ObjectType.PowerUp:
			currentObject.SetParent(powerUpParent);
			break;
		}
	}
	
	// Retrieve object index from the given local index 
	public int GetObjectIndexFromLocalIndex(int localIndex, ObjectType objectType)
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
			return platforms.Length + scenes.Length + obstacles.Length + donuts.Length + localIndex;
		case ObjectType.PowerUp:
			return platforms.Length + scenes.Length + obstacles.Length + donuts.Length + fuels.Length + localIndex;
		}
		return -1; // error
	}
	// Retrieve the local index from given object index
	public int GetLocalIndexFromObjectIndex(int objectIndex, ObjectType objectType)
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
			return objectIndex - platforms.Length - scenes.Length - obstacles.Length - donuts.Length;
		case ObjectType.PowerUp:
			return objectIndex - platforms.Length - scenes.Length - obstacles.Length - fuels.Length - donuts.Length;
		}
		return -1; // error	
	}

	// Retrieve object from the given local index
	public BasicObject GetObjectFromLocalIndex(int localIndex, ObjectType objectType)
	{
		switch (objectType) {
		case ObjectType.Platform:
			return platforms[localIndex];
		case ObjectType.Scene:
			return scenes[localIndex];
		case ObjectType.Obstacle:
			return obstacles[localIndex];
		case ObjectType.Donut:
			return donuts[localIndex];
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
		return platforms.Length + scenes.Length + obstacles.Length + donuts.Length + powerUps.Length;
	}
	
	// Retrieve object from the given object indexs
	private BasicObject GetObjectFromObjectIndex(int objectIndex)
	{
		if (objectIndex < platforms.Length) {
			return platforms[objectIndex];
		} else if (objectIndex < platforms.Length + scenes.Length) {
			return scenes[objectIndex - platforms.Length];
		} else if (objectIndex < platforms.Length + scenes.Length + obstacles.Length) {
			return obstacles[objectIndex - platforms.Length - scenes.Length];
		} else if (objectIndex < platforms.Length + scenes.Length + obstacles.Length + donuts.Length) {
			return donuts[objectIndex - platforms.Length - scenes.Length - obstacles.Length];
		} else if (objectIndex < platforms.Length + scenes.Length + obstacles.Length + donuts.Length + powerUps.Length) {
			return powerUps[objectIndex - platforms.Length - scenes.Length - obstacles.Length - donuts.Length];
		}
		return null;
	}
	
	// Get the index of the next object from given object type
	public int GetNextObjectIndex(ObjectType objectType, ObjectSpawnData spawnData)
	{
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
			objects = donuts;
			break;
		case ObjectType.Fuel:
			objects = fuels;
			break;
		case ObjectType.PowerUp:
			objects = powerUps;
			break;
		}
		float totalProbability = 0;
		float distance = ObjectHistory.instance.GetTotalDistance(objectType == ObjectType.Scene);
		int objectIndex;
		for (int localIndex = 0; localIndex < objects.Length; ++localIndex) {
			objectIndex = GetObjectIndexFromLocalIndex(localIndex, objectType);
			// cache the result
			objectCanSpawnCache[objectIndex] = appearRules[objectIndex].CanSpawnObject(distance, spawnData);
			if (!objectCanSpawnCache[objectIndex]) {
				continue;
			}

			probabilityCache[objectIndex] = appearProbs[objectIndex].GetProbability(distance);
			totalProbability += probabilityCache[objectIndex];
		}

		// chance of spawning nothing (especially in the case of collidable objects)
		if (totalProbability == 0) {
			return -1;
		}

		// Use probability to decide which index should be returned
		float randomValue = Random.value;
		float prevObjProbability = 0;
		float objProbability = 0;
		for (int localIndex = 0; localIndex < objects.Length - 1; ++localIndex) {
			objectIndex = GetObjectIndexFromLocalIndex(localIndex, objectType);
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