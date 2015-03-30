using UnityEngine;
using System.Collections;

// Available slots for the player to stand
public enum SlotPosition { Left = -1, Center, Right }

/*
* Calculate the target position/rotation of the player every frame, as well as move the objects around the player.
* This class also manages when the player is sliding/jumping, and calls any animations.
* The player has a collider which only collides with the platforms/walls. All obstacles/coins/power ups have their
* own trigger system and will call the player controller if they need to.
*/
[RequireComponent(typeof(PlayerAnimation))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
	public static PlayerController instance;
	
	public int maxCollisions = 0; // set to 0 to allow infinite collisions. In this case the game will end with the chase character attacks the player
	public InterpolatedValueList forwardSpeeds;
	public float horizontalSpeed = 15;
	public float slowRotationSpeed = 1;
	public float fastRotationSpeed = 20;
	public float stumbleSpeedDecrement = 3; // amount to decrease the speed when the player hits an obstacle
	public float stumbleDuration = 1;
	public float gravity = -15;
	public bool restrictTurns = true; // if true, can only turn on turn platforms
	public bool restrictTurnsToTurnTrigger = false; // if true, the player will only turn when the player hits a turn trigger. restrictTurns must also be enabled
	public float turnGracePeriod = 0.5f; // if restrictTurns is on, if the player swipes within the grace period before a turn then the character will turn
	public float simultaneousTurnPreventionTime = 2; // the amount of time that must elapse in between two different turns
	public Vector3 pivotOffset; // Assume the meshes pivot point is at the bottom. If it isn't use this offset.
	public Vector3 colliderCenterOffset;
	public float heightLimit = 6.0f;

	private float totalMoveDistance;
	private SlotPosition currentSlotPosition;
	private Quaternion targetRotation;
	private Vector3 targetPosition;
	private float targetHorizontalPosition;
	private bool canUpdatePosition;
	
	private float minForwardSpeed;
	private float maxForwardSpeed;
	private float forwardSpeedDelta;

	private bool isFlying;
	private bool isFlyingPending;
	private float flySpeed;

	private bool isStumbling;
	private float turnRequestTime;
	private bool turnRightRequest;
	private float turnTime;
	private bool onGround;
	private bool skipFrame;
	private int prevHitHashCode;
	
	private int platformLayer;
	private int floorLayer;
	private int wallLayer;
	private int obstacleLayer;
	
	private Vector3 startPosition;
	private Quaternion startRotation;
	private Vector3 turnOffset;
	private Vector3 curveOffset;
	private Vector3 prevTurnOffset;
	
	private PlatformObject platformObject;
	
	private Transform thisTransform;
	private CapsuleCollider capsuleCollider;
	private PlayerAnimation playerAnimation;
	
	public void Awake()
	{
		instance = this;
	}

	// Initialize function
	public void Init()
	{
		// rigidbody should no longer use gravity, be kinematic, and freeze all constraints
		Rigidbody playerRigibody = GetComponent<Rigidbody>();
		if (playerRigibody != null) {
			if (playerRigibody.useGravity) {
				Debug.LogError("The rigidbody no longer needs to use gravity. Disabling.");
				playerRigibody.useGravity = false;
			}
			if (!playerRigibody.isKinematic) {
				Debug.LogError("The rigidbody should be kinematic. Enabling.");
				playerRigibody.isKinematic = true;
			}
			if (playerRigibody.constraints != RigidbodyConstraints.FreezeAll) {
				Debug.LogError("The rigidbody should freeze all constraints. The PlayerController will take care of the physics.");
				playerRigibody.constraints = RigidbodyConstraints.FreezeAll;
			}
		}

		platformLayer = 1 << LayerMask.NameToLayer("Platform");
		floorLayer = 1 << LayerMask.NameToLayer("Floor");
		wallLayer = LayerMask.NameToLayer("Wall");
		obstacleLayer = LayerMask.NameToLayer("Obstacle");
		
		thisTransform = transform;
		capsuleCollider = GetComponent<CapsuleCollider>();
		playerAnimation = GetComponent<PlayerAnimation>();
		playerAnimation.Init();
		
		startPosition = thisTransform.position;
		startRotation = thisTransform.rotation;

		forwardSpeeds.Init();
		// determine the fastest and the slowest forward speeds
		forwardSpeeds.GetMinMaxValue(out minForwardSpeed, out maxForwardSpeed);
		forwardSpeedDelta = maxForwardSpeed - minForwardSpeed;
		if (forwardSpeedDelta == 0) {
			playerAnimation.SetRunSpeed(1, 1);
		}
		
		ResetValues();
		enabled = false;
	}

	// Reset values
	public void ResetValues()
	{
		isFlying = false;
		isFlyingPending = false;
		flySpeed = 0;

		isStumbling = false;
		onGround = true;
		prevHitHashCode = -1;
		canUpdatePosition = true;
		playerAnimation.ResetValues();
		turnTime = -simultaneousTurnPreventionTime;
		
		platformObject = null;

		currentSlotPosition = SlotPosition.Center;
		targetHorizontalPosition = (int)currentSlotPosition * ObjectGenerator.instance.slotDistance;
		totalMoveDistance = 0;
		curveOffset = Vector3.zero;
		turnOffset = prevTurnOffset = Vector3.zero;
		forwardSpeeds.ResetValues();
		
		thisTransform.position = startPosition;
		thisTransform.rotation = startRotation;
		targetRotation = startRotation;
		UpdateTargetPosition(targetRotation.eulerAngles.y);
	}

	// Start game handler
	public void StartGame()
	{
		playerAnimation.Run();
		enabled = true;
		GameController.instance.OnPauseGame += GamePaused;
	}
	
	// The character will not move. All other will moves relative to the player
	public void Update()
	{
		Vector3 moveDirection = Vector3.zero;
		float hitDistance = 0;
		RaycastHit hit;
		// cast a ray to see if we are over any platforms
		if (Physics.Raycast(thisTransform.position + colliderCenterOffset, -thisTransform.up, out hit, Mathf.Infinity, platformLayer)) {
			hitDistance = hit.distance;
			PlatformObject platform = null;
			// compare the hash code to prevent having to look up GetComponent every frame
			if (prevHitHashCode != hit.GetHashCode()) {
				prevHitHashCode = hit.GetHashCode();
				bool hasPlatform = (platform = hit.transform.GetComponent<PlatformObject>()) != null 
								||	(platform = hit.transform.parent.GetComponent<PlatformObject>()) != null;
				// update the platform object
				if (hasPlatform  && platform != platformObject) {
					platformObject = platform;
				}
			}
			// we are over a platform, determine if we are on the ground of that platform
			if (hit.distance <= capsuleCollider.height * 2.0 + pivotOffset.y + 0.5f) {
				onGround = true;
				if (isFlying) {
					if (isFlyingPending) {
						moveDirection.y += flySpeed;
						flySpeed += gravity * Time.deltaTime;
						onGround = false;
					} else {
						isFlying = false;
						if (GameController.instance.IsGameActive()) {
							playerAnimation.Run();
						}
					}
				} else {
					Vector3 position = thisTransform.position;
					position.y = hit.point.y;
					thisTransform.position = position + pivotOffset;
				}
				skipFrame = true;
				// a hit distance of -1 means that the platform is within distance
				hitDistance = -1;
			}
		} else if (Physics.Raycast(thisTransform.position + colliderCenterOffset, -thisTransform.up, out hit, Mathf.Infinity, floorLayer)) {
			hitDistance = hit.distance;
		}
		
		if (hitDistance != -1 && isFlying) {
			// a platform is beneith us but it is far away. If we are jumping apply the jump speed and gravity
			if (isFlying) {
				moveDirection.y += flySpeed;
				flySpeed += gravity * Time.deltaTime;
				
				// the jump is no longer pending if we are in the air
				if (isFlyingPending) {
					isFlyingPending = false;
				}
			} else if (!skipFrame) {
				// apply gravity if we are not jumping
				moveDirection.y = gravity;
			}
			
			if (!skipFrame && hitDistance == 0) {
				platformObject = null;
			}
			if (skipFrame) {
				skipFrame = false;
			} else if (hitDistance != 0 && thisTransform.position.y + (moveDirection.y * Time.deltaTime) < hit.point.y) {
				// this transition should be instant so ignore Time.deltaTime
				moveDirection.y = (hit.point.y - thisTransform.position.y) / Time.deltaTime;
			}
			onGround = false;
		}
		
		float xStrafe = (targetPosition.x - thisTransform.position.x) * Mathf.Abs(Mathf.Cos(targetRotation.eulerAngles.y * Mathf.Deg2Rad)) / Time.deltaTime;
		float zStrafe = (targetPosition.z - thisTransform.position.z) * Mathf.Abs(Mathf.Sin(targetRotation.eulerAngles.y * Mathf.Deg2Rad)) / Time.deltaTime;
		moveDirection.x += Mathf.Clamp(xStrafe, -horizontalSpeed, horizontalSpeed);
		moveDirection.z += Mathf.Clamp(zStrafe, -horizontalSpeed, horizontalSpeed);
		if (thisTransform.position.y + moveDirection.y * Time.deltaTime > this.heightLimit) {
			moveDirection.y = 0;
		}
		thisTransform.position += moveDirection * Time.deltaTime;
		
		// Make sure we don't run into a wall
		if (Physics.Raycast(thisTransform.position + Vector3.up, thisTransform.forward, capsuleCollider.radius, 1 << wallLayer)) {
			GameController.instance.GameOver();
		}
		
		if (!GameController.instance.IsGameActive()) {
			enabled = InAir(); // keep the character active for as long as they are in the air so gravity can keep pushing them down.
		}
	}
	
	// Move all of the objects within the LateObject to prevent jittering when the height transitions
	public void LateUpdate()
	{
		// don't move any objects if the game isn't active. The game may not be active if the character is in the air when they died
		if (!GameController.instance.IsGameActive()) {
			return;
		}
		
		float forwardSpeed = forwardSpeeds.GetValue(totalMoveDistance);
		if (isStumbling) {
			forwardSpeed -= stumbleSpeedDecrement;
		}
		
		if (thisTransform.rotation != targetRotation) {
			thisTransform.rotation = Quaternion.RotateTowards(thisTransform.rotation, targetRotation,
			                                                  Mathf.Lerp(slowRotationSpeed, fastRotationSpeed, Mathf.Clamp01(Quaternion.Angle(thisTransform.rotation, targetRotation) / 45)));
		}

		forwardSpeed *= Time.deltaTime;
		totalMoveDistance += forwardSpeed;
		ObjectGenerator.instance.MoveObjects(forwardSpeed);
	}
	
	public bool AboveTurn()
	{
		if (platformObject != null) {
			return platformObject.isRightTurn || platformObject.isLeftTurn;
		}
		
		return false;
	}
	
	// Turn left or right
	public bool Turn(bool rightTurn)
	{
		// prevent two turns from occurring really close to each other (for example, to prevent a 180 degree turn)
		if (Time.time - turnTime < simultaneousTurnPreventionTime) {
			return false;
		}
		
		RaycastHit hit;
		// ensure we are over the correct platform
		if (Physics.Raycast(thisTransform.position + colliderCenterOffset, -thisTransform.up, out hit, Mathf.Infinity, platformLayer)) {
			PlatformObject platform = null;
			bool hasPlatform = (platform = hit.transform.GetComponent<PlatformObject>()) != null 
							|| (platform = hit.transform.parent.GetComponent<PlatformObject>()) != null;
			// update the platform object
			if (hasPlatform && platform != platformObject) {
				platformObject = platform;
			}
		}
		bool isAboveTurn = AboveTurn();
		
		// if we are restricting a turn, don't turn unless we are above a turn platform
		if (restrictTurns && (!isAboveTurn || restrictTurnsToTurnTrigger)) {
			turnRequestTime = Time.time;
			turnRightRequest = rightTurn;
			return false;
		} 
		
		turnTime = Time.time;
		Vector3 direction = platformObject.GetTransform().right * (rightTurn ? 1 : -1);
		prevTurnOffset = turnOffset;
		canUpdatePosition = ObjectGenerator.instance.UpdateSpawnDirection(direction, rightTurn, isAboveTurn, out turnOffset);
		targetRotation = Quaternion.LookRotation(direction);
		curveOffset.x = (thisTransform.position.x - (startPosition.x + turnOffset.x)) * Mathf.Abs(Mathf.Sin(targetRotation.eulerAngles.y * Mathf.Deg2Rad));
		curveOffset.z = (thisTransform.position.z - (startPosition.z + turnOffset.z)) * Mathf.Abs(Mathf.Cos(targetRotation.eulerAngles.y * Mathf.Deg2Rad));
		if (isAboveTurn) {
			UpdateTargetPosition(targetRotation.eulerAngles.y);
		}

		return true;
	}

	// Make character fly
	public void Fly() 
	{
		if (FuelTracker.instance.getFuel() > 0.0f) {
			flySpeed = 5.0f;
			isFlying = isFlyingPending = true;
			playerAnimation.Hover();
			FuelTracker.instance.DecreaseFuel(0.25f);
		}
	}
	
	// There are three slots on a track. Move left or right if there is a slot available
	public void ChangeSlots(bool right)
	{
		SlotPosition targetSlot = (SlotPosition)Mathf.Clamp((int)currentSlotPosition + (right ? 1 : -1), 
		                                                    (int)SlotPosition.Left, (int)SlotPosition.Right);

		ChangeSlots(targetSlot);
	}
	
	// There are three slots on a track. The accelorometer/swipes determine the slot position
	public void ChangeSlots(SlotPosition targetSlot)
	{
		if (targetSlot == currentSlotPosition)
			return;

		currentSlotPosition = targetSlot;
		targetHorizontalPosition = (int)currentSlotPosition * ObjectGenerator.instance.slotDistance;
		
		UpdateTargetPosition(targetRotation.eulerAngles.y);
	}
	
	public SlotPosition GetCurrentSlotPosition()
	{
		return currentSlotPosition;
	}
	
	private void UpdateTargetPosition(float yAngle)
	{
		// don't update the position when the player will be moving in the wrong direction from a turn
		if (!canUpdatePosition) {
			return;
		}

		targetPosition.x = startPosition.x * Mathf.Abs(Mathf.Sin(yAngle * Mathf.Deg2Rad));
		targetPosition.z = startPosition.z * Mathf.Abs(Mathf.Cos(yAngle * Mathf.Deg2Rad));
		targetPosition += (turnOffset + curveOffset);

		targetPosition.x += targetHorizontalPosition * Mathf.Cos(yAngle * Mathf.Deg2Rad);
		targetPosition.z += targetHorizontalPosition * -Mathf.Sin(yAngle * Mathf.Deg2Rad);
	}
	
	public void UpdateForwardVector(Vector3 forward)
	{
		thisTransform.forward = forward;
		targetRotation = thisTransform.rotation;
		turnOffset = prevTurnOffset;
		
		UpdateTargetPosition(targetRotation.eulerAngles.y);
		thisTransform.position.Set(targetPosition.x, thisTransform.position.y, targetPosition.z);
	}
	
	public bool InAir()
	{
		return !onGround;
	}
	
	public void GameOver()
	{
		playerAnimation.GameOver();
		GameController.instance.OnPauseGame -= GamePaused;
		enabled = InAir();
	}
	
	// disable the script if paused to stop the objects from moving
	private void GamePaused(bool paused)
	{
		enabled = !paused;
	}
}
