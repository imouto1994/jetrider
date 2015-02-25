using UnityEngine;
using System.Collections;

/*
 * SINGLETON CLASS for generating objects  
 * This instance has the responsibility of spawning objects and moving them in the game
 */
public class ObjectGenerator : MonoBehaviour
{
	static public ObjectGenerator instance;

	private const bool IS_PLATFORM = false;
	private const bool IS_SCENE = true;

	// How far out in the distance objects spawn (squared)
	public float sqrHorizon = 38000;
	// The distance behind the camera that the objects will be removed and added back to the object pool
	public float removeHorizon = -25;
	// the number of units between the slots in the track
	public float slotDistance = 2;

	private Vector3 moveDirection;
	private Vector3 spawnDirection;
	
	private PlatformObject[] turnPlatform;
	private int[] platformTurnIndex;
	private SceneObject[] turnScene;
	private int[] sceneTurnIndex;
	
	private Vector3[] platformSizes;
	private Vector3[] sceneSizes;
	private float largestSceneLength;
	private Vector3[] platformStartPositions;
	private Vector3[] sceneStartPositions;
	
	private bool stopSpawning;
	private ObjectSpawnData spawnData;

	private Transform playerTransform;

	public void Awake()
	{
		instance = this;
	}

	// Initialize for instances
	public void Start()
	{
		ObjectPool.instance.Init();
		ObjectHistory.instance.Init(ObjectPool.instance.GetTotalObjectCount());
		
		moveDirection = Vector3.forward;
		spawnDirection = Vector3.forward;

		turnPlatform = new PlatformObject[(int)Direction.Count];
		platformTurnIndex = new int[(int)Direction.Count];
		turnScene = new SceneObject[(int)Direction.Count];
		sceneTurnIndex = new int[(int)Direction.Count];
		
		ObjectPool.instance.GetObjectSizes(out platformSizes, out sceneSizes, out largestSceneLength);
		ObjectPool.instance.GetObjectStartPositions(out platformStartPositions, out sceneStartPositions);
		
		stopSpawning = false;
		spawnData = new ObjectSpawnData();
		spawnData.largestScene = largestSceneLength;
		spawnData.useWidthBuffer = true;
		spawnData.section = 0;
		spawnData.sectionTransition = false;

		this.SpawnObjects(true);
		
		GameController.instance.OnStartGame += StartGame;
	}

	// Game has started. Retrieve the player character's transform
	private void StartGame()
	{
		playerTransform = PlayerController.instance.transform;
	}
	
	// Spawn objects
	public void SpawnObjects(bool activateImmediately)
	{
		// Spawn objects in center direction
		BasicObject prevPlatform = ObjectHistory.instance.GetTopObject(Direction.Center, IS_PLATFORM);
		bool isWithinSpawnDistance = prevPlatform == null || 
									Vector3.Scale(prevPlatform.GetTransform().position, spawnDirection).sqrMagnitude < sqrHorizon;
		bool hasStraightPath = turnPlatform[(int)Direction.Center] == null || turnPlatform[(int)Direction.Center].isStraight;
	
		while (isWithinSpawnDistance && hasStraightPath) {
			Vector3 position = Vector3.zero;
			if (prevPlatform != null) {
				int prevPlatformIndex = ObjectHistory.instance.GetLastLocalIndex(Direction.Center, ObjectType.Platform);
				position = prevPlatform.GetTransform().position 
							- GetPrevPlatformStartPosition(prevPlatform, prevPlatformIndex, spawnDirection) 
							+ platformSizes[prevPlatformIndex].z / 2 * spawnDirection 
							+ platformSizes[prevPlatformIndex].y * Vector3.up;
			}
			PlatformObject platform = SpawnPlatformAndCollidables(Direction.Center, position, spawnDirection, activateImmediately);
			
			if (platform == null)
				return;
			
			SpawnSceneForPlatform(platform, Direction.Center, spawnDirection, activateImmediately);
			prevPlatform = ObjectHistory.instance.GetTopObject(Direction.Center, IS_PLATFORM);

			// Refresh conditions
			isWithinSpawnDistance = prevPlatform == null || 
									Vector3.Scale(prevPlatform.GetTransform().position, spawnDirection).sqrMagnitude < sqrHorizon;
			hasStraightPath = turnPlatform[(int)Direction.Center] == null || turnPlatform[(int)Direction.Center].isStraight;
		}


		// Spawn objects in the left and right direction
		if (turnPlatform[(int)Direction.Center] != null) {
			Vector3 turnDirection = turnPlatform[(int)Direction.Center].GetTransform().right;
			
			// spawn the platform and scene objects for the left and right turns
			for (int i = 0; i < 2; ++i) {
				Direction location = (i == 0 ? Direction.Right : Direction.Left);
				
				bool canSpawn = (location == Direction.Right && turnPlatform[(int)Direction.Center].isRightTurn) ||
								(location == Direction.Left && turnPlatform[(int)Direction.Center].isLeftTurn);
				if (canSpawn && turnPlatform[(int)location] == null) {
					prevPlatform = ObjectHistory.instance.GetTopObject(location, IS_PLATFORM);
					isWithinSpawnDistance = prevPlatform == null || 
											Vector3.Scale(prevPlatform.GetTransform().position, turnDirection).sqrMagnitude < sqrHorizon;
					if (isWithinSpawnDistance) {
						ObjectHistory.instance.SetActiveDirection(location);
						Vector3 position = Vector3.zero;
						if (prevPlatform != null) {
							int prevPlatformIndex = ObjectHistory.instance.GetLastLocalIndex(location, ObjectType.Platform);
							position = prevPlatform.GetTransform().position 
										- GetPrevPlatformStartPosition(prevPlatform, prevPlatformIndex, turnDirection) 
										+ platformSizes[prevPlatformIndex].z / 2 * turnDirection 
										+ platformSizes[prevPlatformIndex].y * Vector3.up;
						} else {
							PlatformObject centerTurn = turnPlatform[(int)Direction.Center];
							int centerTurnIndex = platformTurnIndex[(int)Direction.Center];
							position = centerTurn.GetTransform().position 
										- platformStartPositions[centerTurnIndex].x * turnDirection 
										- Vector3.up * platformStartPositions[centerTurnIndex].y 
										- platformStartPositions[centerTurnIndex].z * spawnDirection 
										+ centerTurn.centerOffset.x * turnDirection + centerTurn.centerOffset.z * spawnDirection 
										+ platformSizes[centerTurnIndex].y * Vector3.up;
						}
						
						PlatformObject platform = SpawnPlatformAndCollidables(location, position, turnDirection, activateImmediately);
						if (platform == null)
							return;
						
						SpawnSceneForPlatform(platform, location, turnDirection, activateImmediately);
					}
				}
				turnDirection *= -1;
			}
			
			// Reset
			ObjectHistory.instance.SetActiveDirection(Direction.Center);
		}
	}
	
	// it is a lot of work to adjust for the previous platform start position
	private Vector3 GetPrevPlatformStartPosition(BasicObject platform, int platformIndex, Vector3 direction)
	{
		return platformStartPositions[platformIndex].x * platform.GetTransform().right 
				+ platformStartPositions[platformIndex].y * Vector3.up 
				+ platformStartPositions[platformIndex].z * direction;
	}
	
	// Spawn platforms and their attached collidables
	private PlatformObject SpawnPlatformAndCollidables(Direction locationDirection, Vector3 position, 
	                                                   Vector3 direction, bool activateImmediately)
	{
		SetupSection(locationDirection, false);
		spawnData.turnSpawned = turnPlatform[(int)locationDirection] != null;
		int localIndex = ObjectPool.instance.GetNextObjectIndex(ObjectType.Platform, spawnData);
		if (localIndex == -1) {
			return null;
		}
		PlatformObject platform = SpawnPlatform(localIndex, locationDirection, position, direction, activateImmediately);
		
		if (platform.CanSpawnCollidable()) {
			// First try to spawn an obstacle. 
			// If there is any space remaining on the platform, then try to spawn a donut.
			// If there is any space remaining on the platform, then try to spawn a fuel.
			// If there is any space remaining on the platform, then try to spawn a powerup.
			SpawnCollidable(ObjectType.Obstacle, position, direction, locationDirection, platform, localIndex, activateImmediately);
			if (platform.CanSpawnCollidable()) {
				SpawnCollidable(ObjectType.Donut, position, direction, locationDirection, platform, localIndex, activateImmediately);
				if (platform.CanSpawnCollidable()) {
					SpawnCollidable(ObjectType.Fuel, position, direction, locationDirection, platform, localIndex, activateImmediately);
					if(platform.CanSpawnCollidable()) {
						SpawnCollidable(ObjectType.PowerUp, position, direction, locationDirection, platform, localIndex, activateImmediately);
					}
				}
			}
		}
		
		return platform;
	}
	
	// Spawn platform
	private PlatformObject SpawnPlatform(int localIndex, Direction locationDirection, Vector3 position, 
	                                     Vector3 direction, bool activateImmediately)
	{
		PlatformObject platform = (PlatformObject)ObjectPool.instance.GetObjectFromPool(localIndex, ObjectType.Platform);
		Quaternion lookRotation = Quaternion.LookRotation(direction);
		platform.Orient(position + (direction * platformSizes[localIndex].z / 2), lookRotation);
		if (activateImmediately)
			platform.Activate();
		
		int objectIndex = ObjectPool.instance.GetObjectIndexFromLocalIndex(localIndex, ObjectType.Platform);
		BasicObject prevTopPlatform = ObjectHistory.instance.ObjectSpawned(objectIndex, 0, locationDirection, 
		                                                                   lookRotation.eulerAngles.y, ObjectType.Platform, platform);
		// the current platform now becames the parent of the previous top platform
		if (prevTopPlatform != null) {
			prevTopPlatform.SetObjectParent(platform);
		} else {
			ObjectHistory.instance.SetBottomObject(locationDirection, IS_PLATFORM, platform);
		}
		ObjectHistory.instance.AddTotalDistance(platformSizes[localIndex].z, locationDirection, IS_PLATFORM, spawnData.section);
		
		return platform;
	}
	
	// Spawn scene for platform
	private void SpawnSceneForPlatform(PlatformObject platform, Direction locationDirection, 
	                                   Vector3 direction, bool activateImmediately)
	{
		bool isTurn = platform.isLeftTurn || platform.isRightTurn;
		if (isTurn) {
			// set largestScene to 0 to prevent the scene spawner from waiting for space for the largest scene object
			spawnData.largestScene = 0;
			spawnData.useWidthBuffer = false;
		}
		
		// spawn all of the scene objects until we have spawned enough scene objects
		SetupSection(locationDirection, IS_SCENE);
		int localIndex;
		SceneObject scene = null;
		while ((localIndex = ObjectPool.instance.GetNextObjectIndex(ObjectType.Scene, spawnData)) != -1) {
			Vector3 position = Vector3.zero;
			SceneObject prevScene = ObjectHistory.instance.GetTopObject(locationDirection, IS_SCENE) as SceneObject;
			bool useZSize = true;
			int prevSceneIndex;
			// may be null if coming from a turn
			if (prevScene == null) {
				if (locationDirection != Direction.Center) {
					prevSceneIndex = sceneTurnIndex[(int)Direction.Center];
					prevScene = turnScene[(int)Direction.Center];
				} else {
					prevScene = ObjectHistory.instance.GetTopObject(locationDirection, IS_SCENE) as SceneObject;
					prevSceneIndex = ObjectHistory.instance.GetLastLocalIndex(locationDirection, ObjectType.Scene);
				}
				useZSize = false;
			} else {
				prevSceneIndex = ObjectHistory.instance.GetLastLocalIndex(locationDirection, ObjectType.Scene);
			}
			if (prevScene) {
				position = prevScene.GetTransform().position - sceneStartPositions[prevSceneIndex] 
							+ (useZSize ? sceneSizes[prevSceneIndex].z : sceneSizes[prevSceneIndex].x) / 2 * direction 
							+ sceneSizes[prevSceneIndex].y * Vector3.up;
			}
			scene = SpawnScene(localIndex, locationDirection, position, direction, activateImmediately);
			// the section may change because of the newly spawned scene object
			SetupSection(locationDirection, IS_SCENE);
		}
		
		if (isTurn) {
			spawnData.largestScene = largestSceneLength;
			spawnData.useWidthBuffer = true;
			
			turnPlatform[(int)locationDirection] = platform;
			platformTurnIndex[(int)locationDirection] = ObjectHistory.instance.GetLastLocalIndex(locationDirection, ObjectType.Platform);
			
			turnScene[(int)locationDirection] = scene;
			sceneTurnIndex[(int)locationDirection] = ObjectHistory.instance.GetLastLocalIndex(locationDirection, ObjectType.Scene);
			
			if (locationDirection == Direction.Center) {
				ObjectHistory.instance.ResetTurnCount();
			}
		} else if (platform.isForSectionTransition) {
			ObjectHistory.instance.DidSpawnSectionTransition(locationDirection, IS_PLATFORM);
		}
	}
	
	// Setup section for platform to be spawned correctly
	private void SetupSection(Direction locationDirection, bool isSceneObject)
	{
		int prevSection = ObjectHistory.instance.GetPreviousSection(locationDirection, isSceneObject);
		spawnData.section = SectionController.instance.GetSection(ObjectHistory.instance.GetTotalDistance(isSceneObject), isSceneObject);
		if (SectionController.instance.useSectionTransitions) {
			if (spawnData.section != prevSection && !ObjectHistory.instance.HasSpawnedSectionTransition(locationDirection, isSceneObject)) {
				spawnData.sectionTransition = true;
				spawnData.prevSection = prevSection;
			} else {
				spawnData.sectionTransition = false;
				if (spawnData.section != prevSection && ObjectHistory.instance.HasSpawnedSectionTransition(locationDirection, isSceneObject))
					ObjectHistory.instance.SetPreviousSection(locationDirection, isSceneObject, spawnData.section);
			}
		}
	}
	
	// returns true if there is still space on the platform for a collidable object to spawn
	private void SpawnCollidable(ObjectType objectType, Vector3 position, Vector3 direction, Direction locationDirection, 
	                             PlatformObject platform, int platformLocalIndex, bool activateImmediately)
	{
		int collidablePositions = platform.numCollidables;
		// can't do anything if the platform doesn't accept any collidable object spawns
		if (collidablePositions == 0)
			return;
		
		Vector3 offset = platformSizes[platformLocalIndex] * 0.1f;
		float zDelta = platformSizes[platformLocalIndex].z * .8f / (1 + collidablePositions);
		
		for (int i = 0; i < collidablePositions; ++i) {
			if (platform.CanSpawnCollidable(i)) {
				spawnData.slotPositions = platform.GetSlotsAvailable();
				int localIndex = ObjectPool.instance.GetNextObjectIndex(objectType, spawnData);
				if (localIndex != -1) {
					CollidableObject collidable = ObjectPool.instance.GetObjectFromPool(localIndex, objectType) as CollidableObject;
					Quaternion lookRotation = Quaternion.LookRotation(direction);
					Vector3 spawnSlot = collidable.GetSpawnSlot(platform.GetTransform().right * slotDistance, spawnData.slotPositions);
					collidable.Orient(platform, position + (offset.z + ((i + 1) * zDelta)) * direction + spawnSlot, lookRotation);
					if (activateImmediately)
						collidable.Activate();
					
					int objectIndex = ObjectPool.instance.GetObjectIndexFromLocalIndex(localIndex, objectType);
					ObjectHistory.instance.ObjectSpawned(objectIndex, (offset.z + ((i + 1) * zDelta)), locationDirection, 
					                                     lookRotation.eulerAngles.y, objectType);
					platform.SpawnCollidable(i);
					
					// don't allow any more of the same collidable type if we are forcing a different collidable
					if (platform.hasDifferentCollidables)
						break;
				}
			}
		}
		spawnData.slotPositions = 0;
	}
	
	// spawn a scene object at the specified location
	private SceneObject SpawnScene(int localIndex, Direction locationDirection, Vector3 position, 
	                               Vector3 direction, bool activateImmediately)
	{
		SceneObject scene = (SceneObject)ObjectPool.instance.GetObjectFromPool(localIndex, ObjectType.Scene);
		Quaternion lookRotation = Quaternion.LookRotation(direction);
		scene.Orient(position + direction * sceneSizes[localIndex].z / 2, lookRotation);
		if (activateImmediately)
			scene.Activate();
		
		int objectIndex = ObjectPool.instance.GetObjectIndexFromLocalIndex(localIndex, ObjectType.Scene);
		BasicObject prevTopScene = ObjectHistory.instance.ObjectSpawned(objectIndex, 0, locationDirection, 
		                                                                lookRotation.eulerAngles.y, ObjectType.Scene, scene);
		// the current scene now becames the parent of the previous top scene
		if (prevTopScene != null) {
			prevTopScene.SetObjectParent(scene);
		} else {
			ObjectHistory.instance.SetBottomObject(locationDirection, IS_SCENE, scene);
		}
		
		ObjectHistory.instance.AddTotalDistance(sceneSizes[localIndex].z, locationDirection, IS_SCENE, spawnData.section);
		if (scene.isForSectionTransition) {
			ObjectHistory.instance.DidSpawnSectionTransition(locationDirection, IS_SCENE);
		}
		
		return scene;
	}
	
	// Move all of the active objects 
	public void MoveObjects(float moveDistance)
	{	

		if (moveDistance == 0)
			return;
		// The distance vector to move the objects
		Vector3 delta = moveDirection * moveDistance;
		
		// Only move the top most platform/scene of each ObjectLocation because all of the other objects are children of these two
		// objects. Only have to check the bottom-most platform/scene as well to determine if it should be removed
		BasicObject currentObject = null;
		Transform objectTransform = null;
		for (int i = 0; i < 2; ++i) { // loop through the platform and scenes
			bool isScene = i == 0;
			for (int j = 0; j < (int)Direction.Count; ++j) {
				// Move objects
				currentObject = ObjectHistory.instance.GetTopObject((Direction)j, isScene);
				if (currentObject != null) {
					objectTransform = currentObject.GetTransform();
					Vector3 pos = objectTransform.position;
					pos -= delta;
					objectTransform.position = pos;
					
					// Check for removing bottom object if it is out of range
					currentObject = ObjectHistory.instance.GetBottomObject((Direction)j, isScene);
					if (playerTransform.InverseTransformPoint(currentObject.GetTransform().position).z < removeHorizon) {
						if (turnPlatform[j] == currentObject) {
							turnPlatform[j] = null;
						}
						ObjectHistory.instance.ObjectRemoved((Direction)j, isScene);
						currentObject.Deactivate();
					}
				}
			}
			
			// Move turn objects
			currentObject = ObjectHistory.instance.GetTopTurnObject(isScene);
			if (currentObject != null) {
				objectTransform = currentObject.GetTransform();
				Vector3 pos = objectTransform.position;
				pos -= delta;
				objectTransform.position = pos;
				
				currentObject = ObjectHistory.instance.GetBottomTurnObject(isScene);
				if (playerTransform.InverseTransformPoint(currentObject.GetTransform().position).z < removeHorizon) {
					ObjectHistory.instance.TurnObjectRemoved(isScene);
					currentObject.Deactivate();
				}
			}
		}
		
		if (!stopSpawning) {
			SpawnObjects(true);
		}
	}
	
	// Set move direction vector
	public void SetMoveDirection(Vector3 newDirection)
	{
		moveDirection = newDirection;
	}

	// Retrieve the move direction vector
	public Vector3 GetMoveDirection()
	{
		return moveDirection;
	}
	
	// The player has turned. Update the spawn direction to continue spawning objects
	public bool UpdateSpawnDirection(Vector3 newDirection, bool rightTurn, 
	                                 bool playerAboveTurn, out Vector3 turnOffset)
	{
		turnOffset = Vector3.zero;
		moveDirection = newDirection;
		
		// Terminate if the player is not above turn. Stop spawning objects as the game is about to be over.
		if (!playerAboveTurn) {
			stopSpawning = true;
			return false;
		}
		
		float yAngle = Quaternion.LookRotation(newDirection).eulerAngles.y;
		if ((rightTurn && Mathf.Abs(yAngle - ObjectHistory.instance.GetObjectDirectionAngle(Direction.Right)) < 0.01f) ||
		    (!rightTurn && Mathf.Abs(yAngle - ObjectHistory.instance.GetObjectDirectionAngle(Direction.Left)) < 0.01f)) {
			spawnDirection = newDirection;
			Direction turnDirection = (rightTurn ? Direction.Right : Direction.Left);
			turnPlatform[(int)Direction.Center] = turnPlatform[(int)turnDirection];
			turnPlatform[(int)Direction.Right] = turnPlatform[(int)Direction.Left] = null;
			platformTurnIndex[(int)Direction.Center] = platformTurnIndex[(int)turnDirection];
			turnScene[(int)Direction.Center] = turnScene[(int)turnDirection];
			turnScene[(int)Direction.Right] = turnScene[(int)Direction.Left] = null;
			sceneTurnIndex[(int)Direction.Center] = sceneTurnIndex[(int)turnDirection];
			
			// The center objects and the objects in the location opposite of turn are grouped together with the center object being the top most object
			for (int i = 0; i < 2; ++i) {
				Direction direction = turnDirection == Direction.Right ? Direction.Left : Direction.Right;
				BasicObject infiniteObject = ObjectHistory.instance.GetTopObject(direction, i == 0);
				// may be null if the turn only turns one direction
				if (infiniteObject != null) {
					BasicObject centerObject = ObjectHistory.instance.GetBottomObject(Direction.Center, i == 0);
					infiniteObject.SetObjectParent(centerObject);
				}
			}
			
			ObjectHistory.instance.Turn(turnDirection);
			if (turnPlatform[(int)Direction.Center] != null) {
				ObjectHistory.instance.ResetTurnCount();
			}
			
			turnOffset = GetTurnOffset();
			return true;
		}

		return false;
	}

	// Retrieve the necessary turn offset
	public Vector3 GetTurnOffset()
	{
		// Add an offset so the character is always in the correct slot after a turn
		PlatformObject topPlatform = ObjectHistory.instance.GetTopObject(Direction.Center, IS_PLATFORM) as PlatformObject;
		Vector3 offset = Vector3.zero;
		Vector3 position = topPlatform.GetTransform().position;
		int topPlatformIndex = ObjectHistory.instance.GetLastLocalIndex(Direction.Center, ObjectType.Platform);
		Quaternion lookRotation = Quaternion.LookRotation(spawnDirection);
		offset.x = (position.x + platformStartPositions[topPlatformIndex].x * (spawnDirection.z > 0 ? -1 : 1)) 
					* -Mathf.Cos(lookRotation.eulerAngles.y * Mathf.Deg2Rad) * (spawnDirection.z > 0 ? -1 : 1);
		offset.z = (position.z + platformStartPositions[topPlatformIndex].x * (spawnDirection.x < 0 ? -1 : 1)) 
					* Mathf.Sin(lookRotation.eulerAngles.y * Mathf.Deg2Rad) * (spawnDirection.x < 0 ? -1 : 1);
		return offset;
	}
}
