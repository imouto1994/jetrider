using UnityEngine;
using System.Collections;

// SINGLETON CLASS TO CONTROL THE CAMERA
public class CameraController : MonoBehaviour {

	static public CameraController instance;

	// Target position and rotation of camera when in game
	public Vector3 inGamePosition;
	public Vector3 inGameRotation;

	// Properties of camera
	public float smoothMoveTime;
	public float moveSpeed;
	public float rotationSpeed;

	//TODO: Add shake effect later

	// Target position and rotation of the camera
	private Vector3 targetPosition;
	private Quaternion targetRotation;

	// Indicator whether the camera is transitioning or not
	private bool isTransitioning;
	// Indicator whether the game is active or not
	private bool isGameActive;

	// Initial position of the camera
	private Vector3 startPosition;
	private Quaternion startRotation;

	// Component references
	private Transform cameraTransform;
	private PlayerController playerController;
	private Transform playerTransform;

	// Pause time
	private float pauseTime;

	public void Awake() 
	{
		instance = this;

		startPosition = transform.position;
		startRotation = transform.rotation;
	}

	public void Start()
	{
		cameraTransform = transform;
		// Use these if we want to display initial scene of the game
		//targetPosition = cameraTransform.position;
		//targetRotation = cameraTransform.rotation;
	}

	public void StartGame(bool isRestart)
	{
		playerController = PlayerController.instance;
		playerTransform = playerController.transform;
		if (isRestart) {
			targetPosition = inGamePosition;
			targetRotation.eulerAngles = inGameRotation;

			cameraTransform.position = playerTransform.TransformPoint(inGamePosition);
			Vector3 relativeTargetRotationVec = inGameRotation;
			relativeTargetRotationVec.y += playerTransform.eulerAngles.y;
			Quaternion relativeTargetQuaternion = cameraTransform.rotation;
			relativeTargetQuaternion.eulerAngles = relativeTargetRotationVec;
			cameraTransform.rotation = relativeTargetQuaternion;

			isTransitioning = false;
		} else {
			targetPosition = inGamePosition;
			targetRotation.eulerAngles = inGameRotation;
			isTransitioning = true;
		}
		isGameActive = true;
		GameController.instance.OnPauseGame += GamePaused;
	}

	// Check function whether the camera is in-game
	public bool IsInGameTransform() 
	{
		return targetPosition == inGamePosition && targetRotation.eulerAngles == inGameRotation;
	}

	// Handler for game over
	public void GameOver() 
	{
		isGameActive = false;
		GameController.instance.OnPauseGame -= GamePaused;
	}

	// Update the position and transform of the camera
	public void LateUpdate() 
	{
		if (!isGameActive) {
			return;
		}

		Vector3 relativeTargetPosition = playerTransform.TransformPoint(targetPosition);
		Vector3 relativeTargetRotationVec = targetRotation.eulerAngles;
    	relativeTargetRotationVec.y += playerTransform.eulerAngles.y;
    	Quaternion relativeTargetRotation = cameraTransform.rotation;
    	relativeTargetRotation.eulerAngles = relativeTargetRotationVec;

	    // if the camera is transitioning from one position to another then use the regular move towards / rotate towards.
	    if (isTransitioning) {
	        bool isTransitioned = true;
	        if ((cameraTransform.position - relativeTargetPosition).sqrMagnitude > 0.01f) {
	            isTransitioned = false;
	            cameraTransform.position = Vector3.MoveTowards(cameraTransform.position, relativeTargetPosition, moveSpeed);
	        }
	        if (Quaternion.Angle(cameraTransform.rotation, relativeTargetRotation) > 0.01f) {
	            isTransitioned = false;
	            cameraTransform.rotation = Quaternion.RotateTowards(cameraTransform.rotation, relativeTargetRotation, rotationSpeed);
			}
	        isTransitioning = !isTransitioned;
	    } else {
	        Vector3 currentVelocity = Vector3.zero;
	        cameraTransform.position = Vector3.SmoothDamp(cameraTransform.position, relativeTargetPosition, ref currentVelocity, smoothMoveTime);
	        cameraTransform.rotation = Quaternion.RotateTowards(cameraTransform.rotation, relativeTargetRotation, rotationSpeed);
	    }
	}

	// Reset the transform of the camera back to the initial state
	public void ResetValues()
	{
		cameraTransform.position = startPosition;
		cameraTransform.rotation = startRotation;
	}

	// Handler for pausing game
	public void GamePaused(bool paused)
	{
		enabled = !paused;
	}
}
