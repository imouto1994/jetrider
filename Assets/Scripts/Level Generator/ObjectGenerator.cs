using UnityEngine;
using System.Collections;

public class InfiniteObjectGenerator : MonoBehaviour
{
	static public InfiniteObjectGenerator instance;
	
	// How far out in the distance objects spawn (squared)
	public float sqrHorizon = 38000;
	// The distance behind the camera that the objects will be removed and added back to the object pool
	public float removeHorizon = -25;
	// the number of units between the slots in the track
	public float slotDistance = 2;
	// Spawn the full length of objects, useful when creating a tutorial or startup objects
	public bool spawnFullLength;
	// Do we want to reposition on height changes?
	public bool heightReposition;
	// The amount of distance to move back when the player is reviving
	public float reviveMoveBackDistance = -20;
	
	// the probability that no collidables will spawn on the platform
	[HideInInspector]
	public InterpolatedValueList noCollidableProbability;
	
	private SectionController sectionSelection;
	
	private Vector3 moveDirection;
	private Vector3 spawnDirection;
	private float wrongMoveDistance;
	
	private PlatformObject[] turnPlatform;
	private int[] turnIndex;
	private SceneObject[] turnScene;
	private int[] sceneTurnIndex;
	
	private Vector3[] platformSizes;
	private Vector3[] sceneSizes;
	private float largestSceneLength;
	private Vector3[] platformStartPosition;
	private Vector3[] sceneStartPosition;
	
	private bool stopObjectSpawns;
	private ObjectSpawnData spawnData;

	private Transform playerTransform;

	public void Awake()
	{
		instance = this;
	}
	
	public void Start()
	{
		ObjectPool.instance.Init();
		ObjectHistory.instance.Init(ObjectPool.instance.GetTotalObjectCount());
		
		moveDirection = Vector3.forward;
		spawnDirection = Vector3.forward;
		turnPlatform = new PlatformObject[(int)Direction.Count];
		turnIndex = new int[(int)Direction.Count];
		turnScene = new SceneObject[(int)Direction.Count];
		sceneTurnIndex = new int[(int)Direction.Count];
		
		ObjectPool.instance.GetObjectSizes(out platformSizes, out sceneSizes, out largestSceneLength);
		ObjectPool.instance.GetObjectStartPositions(out platformStartPosition, out sceneStartPosition);
		
		stopObjectSpawns = false;
		spawnData = new ObjectSpawnData();
		spawnData.largestScene = largestSceneLength;
		spawnData.useWidthBuffer = true;
		spawnData.section = 0;
		spawnData.sectionTransition = false;
		
		noCollidableProbability.Init();
		
		SpawnObjectRun(true);
		
		GameController.instance.OnStartGame += StartGame;
	}

	private void StartGame()
	{
		playerTransform = PlayerController.instance.transform;
	}
	
	// An object run contains many platforms strung together with collidables: obstacles, power ups, and coins. If spawnObjectRun encounters a turn,
	// it will spawn the objects in the correct direction
	public void SpawnObjectRun(bool activateImmediately)
	{
		// spawn the center objects
		BasicObject prevPlatform = ObjectHistory.instance.GetTopInfiniteObject(Direction.Center, false);
		while ((prevPlatform == null || (Vector3.Scale(prevPlatform.GetTransform().position, spawnDirection)).sqrMagnitude < sqrHorizon) && (turnPlatform[(int)Direction.Center] == null || turnPlatform[(int)Direction.Center].straight)) {
			Vector3 position = Vector3.zero;
			if (prevPlatform != null) {
				int prevPlatformIndex = ObjectHistory.instance.GetLastLocalIndex(Direction.Center, ObjectType.Platform);
				position = prevPlatform.GetTransform().position - GetPrevPlatformStartPosition(prevPlatform, prevPlatformIndex, spawnDirection) + platformSizes[prevPlatformIndex].z / 2 * spawnDirection + platformSizes[prevPlatformIndex].y * Vector3.up;
			}
			PlatformObject platform = SpawnObjects(Direction.Center, position, spawnDirection, activateImmediately);
			
			if (platform == null)
				return;
			
			PlatformSpawned(platform, Direction.Center, spawnDirection, activateImmediately);
			prevPlatform = ObjectHistory.instance.GetTopInfiniteObject(Direction.Center, false);
			
			if (spawnFullLength)
				SpawnObjectRun(activateImmediately);
		}
		
		// spawn the left and right objects
		if (turnPlatform[(int)Direction.Center] != null) {
			Vector3 turnDirection = turnPlatform[(int)Direction.Center].GetTransform().right;
			
			// spawn the platform and scene objects for the left and right turns
			for (int i = 0; i < 2; ++i) {
				Direction location = (i == 0 ? Direction.Right : Direction.Left);
				
				bool canSpawn = (location == Direction.Right && turnPlatform[(int)Direction.Center].rightTurn) ||
					(location == Direction.Left && turnPlatform[(int)Direction.Center].leftTurn);
				if (canSpawn && turnPlatform[(int)location] == null) {
					prevPlatform = ObjectHistory.instance.GetTopInfiniteObject(location, false);
					if (prevPlatform == null || (Vector3.Scale(prevPlatform.GetTransform().position, turnDirection)).sqrMagnitude < sqrHorizon) {
						ObjectHistory.instance.SetActiveDirection(location);
						Vector3 position = Vector3.zero;
						if (prevPlatform != null) {
							int prevPlatformIndex = ObjectHistory.instance.GetLastLocalIndex(location, ObjectType.Platform);
							position = prevPlatform.GetTransform().position - GetPrevPlatformStartPosition(prevPlatform, prevPlatformIndex, turnDirection) +
								platformSizes[prevPlatformIndex].z / 2 * turnDirection + platformSizes[prevPlatformIndex].y * Vector3.up;
						} else {
							PlatformObject centerTurn = turnPlatform[(int)Direction.Center];
							int centerTurnIndex = turnIndex[(int)Direction.Center];
							position = centerTurn.GetTransform().position - platformStartPosition[centerTurnIndex].x * turnDirection - Vector3.up * platformStartPosition[centerTurnIndex].y -
								platformStartPosition[centerTurnIndex].z * spawnDirection + centerTurn.centerOffset.x * turnDirection + centerTurn.centerOffset.z * spawnDirection +
									platformSizes[centerTurnIndex].y * Vector3.up;
						}
						
						PlatformObject platform = SpawnObjects(location, position, turnDirection, activateImmediately);
						if (platform == null)
							return;
						
						PlatformSpawned(platform, location, turnDirection, activateImmediately);
					}
				}
				turnDirection *= -1;
			}
			
			// reset
			ObjectHistory.instance.SetActiveDirection(Direction.Center);
		}
	}
	
	// it is a lot of work to adjust for the previous platform start position
	private Vector3 GetPrevPlatformStartPosition(BasicObject platform, int platformIndex, Vector3 direction)
	{
		return platformStartPosition[platformIndex].x * platform.GetTransform().right + platformStartPosition[platformIndex].y * Vector3.up +
			platformStartPosition[platformIndex].z * direction;
	}
	
	// spawn the platforms, obstacles, power ups, and coins
	private PlatformObject SpawnObjects(Direction location, Vector3 position, Vector3 direction, bool activateImmediately)
	{
		SetupSection(location, false);
		spawnData.turnSpawned = turnPlatform[(int)location] != null;
		int localIndex = ObjectPool.instance.GetNextObjectIndex(ObjectType.Platform, spawnData);
		if (localIndex == -1) {
			print("Unable to spawn platform. No platforms can be spawned based on the probability rules at distance " +
			      ObjectHistory.instance.GetTotalDistance(false) + " within section " + spawnData.section + (spawnData.sectionTransition ? (" (Transitioning from section " + spawnData.prevSection + ")") : ""));
			return null;
		}
		PlatformObject platform = SpawnPlatform(localIndex, location, position, direction, activateImmediately);
		
		if (platform.CanSpawnCollidable() && Random.value >= noCollidableProbability.GetValue(ObjectHistory.instance.GetTotalDistance(false))) {
			// First try to spawn an obstacle. If there is any space remaining on the platform, then try to spawn a coin.
			// If there is still some space remaing, try to spawn a powerup.
			// An extension of this would be to randomize the order of ObjectType, but this way works if the probabilities
			// are setup fairly
			SpawnCollidable(ObjectType.Obstacle, position, direction, location, platform, localIndex, activateImmediately);
			if (platform.CanSpawnCollidable()) {
				SpawnCollidable(ObjectType.Coin, position, direction, location, platform, localIndex, activateImmediately);
				if (platform.CanSpawnCollidable()) {
					SpawnCollidable(ObjectType.PowerUp, position, direction, location, platform, localIndex, activateImmediately);
				}
			}
		}
		
		return platform;
	}
	
	// returns the length of the created platform
	private PlatformObject SpawnPlatform(int localIndex, Direction location, Vector3 position, Vector3 direction, bool activateImmediately)
	{
		PlatformObject platform = (PlatformObject)ObjectPool.instance.ObjectFromPool(localIndex, ObjectType.Platform);
		Quaternion lookRotation = Quaternion.LookRotation(direction);
		platform.Orient(position + (direction * platformSizes[localIndex].z / 2), lookRotation);
		if (activateImmediately)
			platform.Activate();
		
		int objectIndex = ObjectPool.instance.LocalIndexToObjectIndex(localIndex, ObjectType.Platform);
		BasicObject prevTopPlatform = ObjectHistory.instance.ObjectSpawned(objectIndex, 0, location, lookRotation.eulerAngles.y, ObjectType.Platform, platform);
		// the current platform now becames the parent of the previous top platform
		if (prevTopPlatform != null) {
			prevTopPlatform.SetObjectParent(platform);
		} else {
			ObjectHistory.instance.SetBottomObject(location, false, platform);
		}
		ObjectHistory.instance.AddTotalDistance(platformSizes[localIndex].z, location, false, spawnData.section);
		
		return platform;
	}
	
	// a platform has been spawned, now spawn the scene objects and setup for a turn if needed
	private void PlatformSpawned(PlatformObject platform, Direction location, Vector3 direction, bool activateImmediately)
	{
		bool isTurn = platform.leftTurn || platform.rightTurn;
		if (isTurn || spawnFullLength) {
			// set largestScene to 0 to prevent the scene spawner from waiting for space for the largest scene object
			spawnData.largestScene = 0;
			spawnData.useWidthBuffer = false;
		}
		
		// spawn all of the scene objects until we have spawned enough scene objects
		SetupSection(location, true);
		int localIndex;
		SceneObject scene = null;
		while ((localIndex = ObjectPool.instance.GetNextObjectIndex(ObjectType.Scene, spawnData)) != -1) {
			Vector3 position = Vector3.zero;
			SceneObject prevScene = ObjectHistory.instance.GetTopInfiniteObject(location, true) as SceneObject;
			bool useZSize = true;
			int prevSceneIndex;
			// may be null if coming from a turn
			if (prevScene == null) {
				if (location != Direction.Center) {
					prevSceneIndex = sceneTurnIndex[(int)Direction.Center];
					prevScene = turnScene[(int)Direction.Center];
				} else {
					prevScene = ObjectHistory.instance.GetTopInfiniteObject(location, true) as SceneObject;
					prevSceneIndex = ObjectHistory.instance.GetLastLocalIndex(location, ObjectType.Scene);
				}
				useZSize = false;
			} else {
				prevSceneIndex = ObjectHistory.instance.GetLastLocalIndex(location, ObjectType.Scene);
			}
			if (prevScene) {
				position = prevScene.GetTransform().position - sceneStartPosition[prevSceneIndex] + (useZSize ? sceneSizes[prevSceneIndex].z : sceneSizes[prevSceneIndex].x) / 2 * direction + sceneSizes[prevSceneIndex].y * Vector3.up;
			}
			scene = SpawnSceneObject(localIndex, location, position, direction, activateImmediately);
			// the section may change because of the newly spawned scene object
			SetupSection(location, true);
		}
		
		if (isTurn) {
			spawnData.largestScene = largestSceneLength;
			spawnData.useWidthBuffer = true;
			
			turnPlatform[(int)location] = platform;
			turnIndex[(int)location] = ObjectHistory.instance.GetLastLocalIndex(location, ObjectType.Platform);
			
			turnScene[(int)location] = scene;
			sceneTurnIndex[(int)location] = ObjectHistory.instance.GetLastLocalIndex(location, ObjectType.Scene);
			
			if (location == Direction.Center) {
				ObjectHistory.instance.ResetTurnCount();
			}
		} else if (platform.sectionTransition) {
			ObjectHistory.instance.DidSpawnSectionTranition(location, false);
		}
	}
	
	// before platforms are about to be spawned setup the section data to ensure the correct platforms are spawned
	private void SetupSection(Direction location, bool isSceneObject)
	{
		int prevSection = ObjectHistory.instance.GetPreviousSection(location, isSceneObject);
		spawnData.section = sectionSelection.GetSection(ObjectHistory.instance.GetTotalDistance(isSceneObject), isSceneObject);
		if (sectionSelection.useSectionTransitions) {
			if (spawnData.section != prevSection && !ObjectHistory.instance.HasSpawnedSectionTransition(location, isSceneObject)) {
				spawnData.sectionTransition = true;
				spawnData.prevSection = prevSection;
			} else {
				spawnData.sectionTransition = false;
				if (spawnData.section != prevSection && ObjectHistory.instance.HasSpawnedSectionTransition(location, isSceneObject))
					ObjectHistory.instance.SetPreviousSection(location, isSceneObject, spawnData.section);
			}
		}
	}
	
	// returns true if there is still space on the platform for a collidable object to spawn
	private void SpawnCollidable(ObjectType objectType, Vector3 position, Vector3 direction, Direction location, PlatformObject platform, int platformLocalIndex, bool activateImmediately)
	{
		int collidablePositions = platform.collidablePositions;
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
					CollidableObject collidable = ObjectPool.instance.ObjectFromPool(localIndex, objectType) as CollidableObject;
					Quaternion lookRotation = Quaternion.LookRotation(direction);
					Vector3 spawnSlot = collidable.GetSpawnSlot(platform.GetTransform().right * slotDistance, spawnData.slotPositions);
					collidable.Orient(platform, position + (offset.z + ((i + 1) * zDelta)) * direction + spawnSlot, lookRotation);
					if (activateImmediately)
						collidable.Activate();
					
					int objectIndex = ObjectPool.instance.LocalIndexToObjectIndex(localIndex, objectType);
					ObjectHistory.instance.ObjectSpawned(objectIndex, (offset.z + ((i + 1) * zDelta)), location, lookRotation.eulerAngles.y, objectType);
					platform.CollidableSpawned(i);
					
					// don't allow any more of the same collidable type if we are forcing a different collidable
					if (platform.forceDifferentCollidableTypes)
						break;
				}
			}
		}
		spawnData.slotPositions = 0;
	}
	
	// spawn a scene object at the specified location
	private SceneObject SpawnSceneObject(int localIndex, Direction location, Vector3 position, Vector3 direction, bool activateImmediately)
	{
		SceneObject scene = (SceneObject)ObjectPool.instance.ObjectFromPool(localIndex, ObjectType.Scene);
		Quaternion lookRotation = Quaternion.LookRotation(direction);
		scene.Orient(position + direction * sceneSizes[localIndex].z / 2, lookRotation);
		if (activateImmediately)
			scene.Activate();
		
		int objectIndex = ObjectPool.instance.LocalIndexToObjectIndex(localIndex, ObjectType.Scene);
		BasicObject prevTopScene = ObjectHistory.instance.ObjectSpawned(objectIndex, 0, location, lookRotation.eulerAngles.y, ObjectType.Scene, scene);
		// the current scene now becames the parent of the previous top scene
		if (prevTopScene != null) {
			prevTopScene.SetObjectParent(scene);
		} else {
			ObjectHistory.instance.SetBottomObject(location, true, scene);
		}
		
		ObjectHistory.instance.AddTotalDistance(sceneSizes[localIndex].z, location, true, spawnData.section);
		if (scene.sectionTransition) {
			ObjectHistory.instance.DidSpawnSectionTranition(location, true);
		}
		
		return scene;
	}
	
	// move all of the active objects
	public void MoveObjects(float moveDistance)
	{
		if (moveDistance == 0)
			return;
		
		// the distance to move the objects
		Vector3 delta = moveDirection * moveDistance;
		if (moveDirection != spawnDirection) {
			wrongMoveDistance += moveDistance;
		}
		
		// only move the top most platform/scene of each ObjectLocation because all of the other objects are children of these two
		// objects. Only have to check the bottom-most platform/scene as well to determine if it should be removed
		BasicObject infiniteObject = null;
		Transform objectTransform = null;
		PlatformObject platformObject = null;
		for (int i = 0; i < 2; ++i) { // loop through the platform and scenes
			for (int j = 0; j < (int)Direction.Count; ++j) {
				// move
				infiniteObject = ObjectHistory.instance.GetTopInfiniteObject((Direction)j, i == 0);
				if (infiniteObject != null) {
					objectTransform = infiniteObject.GetTransform();
					Vector3 pos = objectTransform.position;
					pos -= delta;
					objectTransform.position = pos;
					
					// check for removal.. there will always be a bottom object if there is a top object
					infiniteObject = ObjectHistory.instance.GetBottomInfiniteObject((Direction)j, i == 0);
					if (playerTransform.InverseTransformPoint(infiniteObject.GetTransform().position).z < removeHorizon) {
						if (turnPlatform[j] == infiniteObject) {
							turnPlatform[j] = null;
						}
						ObjectHistory.instance.ObjectRemoved((Direction)j, i == 0);
						infiniteObject.Deactivate();
					}
				}
			}
			
			// loop through all of the turn objects
			infiniteObject = ObjectHistory.instance.GetTopTurnInfiniteObject(i == 0);
			if (infiniteObject != null) {
				objectTransform = infiniteObject.GetTransform();
				Vector3 pos = objectTransform.position;
				pos -= delta;
				objectTransform.position = pos;
				
				infiniteObject = ObjectHistory.instance.GetBottomTurnInfiniteObject(i == 0);
				if (playerTransform.InverseTransformPoint(infiniteObject.GetTransform().position).z < removeHorizon) {
					ObjectHistory.instance.TurnObjectRemoved(i == 0);
					infiniteObject.Deactivate();
				}
			}
		}
		
		if (!stopObjectSpawns) {
			//dataManager.AddToScore(moveDistance);
			SpawnObjectRun(true);
		}
	}
	
	// When a platform with delta height is removed, move all of the objects back to their original heights to reduce the chances
	// of floating point errors
	private void TransitionHeight(float amount)
	{
		// Move the position of every object by -amount
		BasicObject infiniteObject;
		Transform infiniteObjectTransform;
		Vector3 position;
		for (int i = 0; i < 2; ++i) { // loop through the platform and scenes
			for (int j = 0; j < (int)Direction.Count; ++j) {
				infiniteObject = ObjectHistory.instance.GetTopInfiniteObject((Direction)j, i == 0);
				if (infiniteObject != null) {
					position = (infiniteObjectTransform = infiniteObject.GetTransform()).position;
					position.y -= amount;
					infiniteObjectTransform.position = position;
				}
			}
		}
		
		PlayerController.instance.TransitionHeight(amount);
	}
	
	// gradually turn the player for a curve
	public void SetMoveDirection(Vector3 newDirection)
	{
		moveDirection = newDirection;
	}
	
	public Vector3 GetMoveDirection()
	{
		return moveDirection;
	}
	
	// the player hit a turn, start generating new objects
	public bool UpdateSpawnDirection(Vector3 newDirection, bool setMoveDirection, bool rightTurn, bool playerAboveTurn, out Vector3 turnOffset)
	{
		turnOffset = Vector3.zero;
		// Don't set the move direction above a curve because the cuve will set the direction
		if (setMoveDirection) {
			moveDirection = newDirection;
		}
		
		// don't change spawn directions if the player isn't above a turn. The game is about to be over anyway so there isn't a reason to keep generating objects
		if (!playerAboveTurn) {
			stopObjectSpawns = true;
			return false;
		}
		
		float yAngle = Quaternion.LookRotation(newDirection).eulerAngles.y;
		if ((rightTurn && Mathf.Abs(yAngle - ObjectHistory.instance.GetObjectDirectionAngle(Direction.Right)) < 0.01f) ||
		    (!rightTurn && Mathf.Abs(yAngle - ObjectHistory.instance.GetObjectDirectionAngle(Direction.Left)) < 0.01f)) {
			spawnDirection = newDirection;
			wrongMoveDistance = 0;
			Direction turnLocation = (rightTurn ? Direction.Right : Direction.Left);
			turnPlatform[(int)Direction.Center] = turnPlatform[(int)turnLocation];
			turnPlatform[(int)Direction.Right] = turnPlatform[(int)Direction.Left] = null;
			turnIndex[(int)Direction.Center] = turnIndex[(int)turnLocation];
			turnScene[(int)Direction.Center] = turnScene[(int)turnLocation];
			turnScene[(int)Direction.Right] = turnScene[(int)Direction.Left] = null;
			sceneTurnIndex[(int)Direction.Center] = sceneTurnIndex[(int)turnLocation];
			
			// The center objects and the objects in the location opposite of turn are grouped together with the center object being the top most object
			for (int i = 0; i < 2; ++i) {
				BasicObject infiniteObject = ObjectHistory.instance.GetTopInfiniteObject((turnLocation == Direction.Right ? Direction.Left : Direction.Right), i == 0);
				// may be null if the turn only turns one direction
				if (infiniteObject != null) {
					BasicObject centerObject = ObjectHistory.instance.GetBottomInfiniteObject(Direction.Center, i == 0);
					infiniteObject.SetObjectParent(centerObject);
				}
			}
			
			ObjectHistory.instance.Turn(turnLocation);
			if (turnPlatform[(int)Direction.Center] != null) {
				ObjectHistory.instance.ResetTurnCount();
			}
			
			turnOffset = GetTurnOffset();
			return true;
		}
		
		// Set the move direction even if the turn is a curve so the infinite objects will move in the opposite direciton of the player
		moveDirection = newDirection;
		return false;
	}
	
	public Vector3 GetTurnOffset()
	{
		// add an offset so the character is always in the correct slot after a turn
		PlatformObject topPlatform = ObjectHistory.instance.GetTopInfiniteObject(Direction.Center, false) as PlatformObject;
		Vector3 offset = Vector3.zero;
		Vector3 position = topPlatform.GetTransform().position;
		int topPlatformIndex = ObjectHistory.instance.GetLastLocalIndex(Direction.Center, ObjectType.Platform);
		Quaternion lookRotation = Quaternion.LookRotation(spawnDirection);
		offset.x = (position.x + platformStartPosition[topPlatformIndex].x * (spawnDirection.z > 0 ? -1 : 1)) * -Mathf.Cos(lookRotation.eulerAngles.y * Mathf.Deg2Rad) * (spawnDirection.z > 0 ? -1 : 1);
		offset.z = (position.z + platformStartPosition[topPlatformIndex].x * (spawnDirection.x < 0 ? -1 : 1)) * Mathf.Sin(lookRotation.eulerAngles.y * Mathf.Deg2Rad) * (spawnDirection.x < 0 ? -1 : 1);
		return offset;
	}
	
	// the player is being revived from where they died. Move the track back a set amount of distance. You should modify this method if you want a more
	// advanced algorithm to determine how far back the platforms should move
	public void PrepareForRevive(bool retry)
	{
		// May need to retry because the previous position placed the character above a jump or non-platform object. Move the objects forward by a small amount
		if (retry) {
			MoveObjects(-reviveMoveBackDistance * 0.1f);
		} else {
			MoveObjects(-wrongMoveDistance);
			moveDirection = spawnDirection;
			MoveObjects(reviveMoveBackDistance);
		}
	}
	
	// clear everything out and reset the generator back to the beginning, keeping the current set of objects activated before new objects are generated
	public void ResetValues()
	{
		moveDirection = Vector3.forward;
		spawnDirection = Vector3.forward;
		wrongMoveDistance = 0;
		
		for (int i = 0; i < (int)Direction.Count; ++i) {
			turnPlatform[i] = null;
		}
		
		stopObjectSpawns = false;
		spawnData.largestScene = largestSceneLength;
		spawnData.useWidthBuffer = true;
		spawnData.section = 0;
		spawnData.sectionTransition = false;
		
		ObjectHistory.instance.SaveObjectsReset();
		sectionSelection.ResetValues();
	}
	
	// remove the saved infinite objects and activate the set of objects for the next game
	public void ReadyFromReset()
	{
		// deactivate the saved infinite objects from the previous game
		BasicObject infiniteObject = ObjectHistory.instance.GetSavedObjects();
		BasicObject[] childObjects = null;
		for (int i = 0; i < 2; ++i) { // loop through the platform and scenes
			if (i == 0) { // scene
				childObjects = infiniteObject.GetComponentsInChildren<SceneObject>(true);
			} else {
				childObjects = infiniteObject.GetComponentsInChildren<PlatformObject>(true);
			}
			
			for (int j = 0; j < childObjects.Length; ++j) {
				childObjects[j].Deactivate();
			}
		}
		
		// activate the objects for the current game
		for (int i = 0; i < 2; ++i) { // loop through the platform and scenes
			for (int j = 0; j < (int)Direction.Count; ++j) {
				infiniteObject = ObjectHistory.instance.GetTopInfiniteObject((Direction)j, i == 0);
				if (infiniteObject != null) {
					if (i == 0) { // scene
						childObjects = infiniteObject.GetComponentsInChildren<SceneObject>(true);
					} else {
						childObjects = infiniteObject.GetComponentsInChildren<PlatformObject>(true);
					}
					
					for (int k = 0; k < childObjects.Length; ++k) {
						childObjects[k].Activate();
					}
				}
			}
		}
	}
}
