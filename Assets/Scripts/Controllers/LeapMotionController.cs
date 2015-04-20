using UnityEngine;
using System.Collections;
using Leap;

public class LeapMotionController : MonoBehaviour
{

	enum Dir {left, right, none};

	static public LeapMotionController instance;


	[SerializeField]
	private float swipeMaxAngle; // in degrees
	[SerializeField]
	private float minPitch; // in radians
	[SerializeField]
	private float maxPitch; // in radians


	private const float swipeMinLength = 30;
	private const float swipeMinVelocity = 500;
	private const float normalRoll = -0.5f;

	private Controller controller;
	private int prevGestureId;
	private bool isMovingLeft, isMovingRight;
	private Dir prevDirection;

	private Frame frame;
	private Hand hand;

	public void Awake ()
	{
		instance = this;
	}

	// Use this for initialization
	void Start ()
	{
		controller = new Controller ();
		controller.EnableGesture (Gesture.GestureType.TYPE_SWIPE);
		controller.Config.SetFloat ("Gesture.Swipe.MinLength", swipeMinLength);
		controller.Config.SetFloat ("Gesture.Swipe.MinVelocity", swipeMinVelocity);
		controller.Config.Save ();
	}

	public void StartGame ()
	{
		enabled = true;
	}
	
	public void GameOver ()
	{
		enabled = false;
	}

	/*
	void Update ()
	{
		Frame frame = controller.Frame ();
		HandList hands = frame.Hands;
		
		if (!hands.IsEmpty) {
			hand = hands [0];

			// y-axis movement
			float pitch = hand.Direction.Pitch;
			if (pitch > minPitch) {
				PlayerController.instance.Fly (Mathf.InverseLerp (minPitch, maxPitch, pitch));
			}
		
			Dir currDirection;

			Debug.Log (hands [0].PalmNormal.Roll.ToString ());


			// detect if there is any gesture
			if (!frame.Gestures ().IsEmpty) {
				isMovingLeft = false;
				isMovingRight = false;
				foreach (Gesture gesture in frame.Gestures()) {
					if (gesture.Type == Gesture.GestureType.TYPESWIPE) {
						SwipeGesture swipe = new SwipeGesture (gesture);
						if (isLeftSwipe (swipe)) {
							isMovingLeft = true;
						} else if (isRightSwipe (swipe)) {
							isMovingRight = true;
						}
					}
				}

				if (isMovingLeft) {
					if (isMovingRight) {
						currDirection = Dir.none;
					} else {
						currDirection = Dir.left;
					}
				} else {
					if (isMovingRight) {
						currDirection = Dir.right;
					} else {
						currDirection = Dir.none;
					}
				}
			} else {
				currDirection = Dir.none;
			}
	
			// compare the direction of the gesture of the current frame
			// with that of the previous frame
			switch (prevDirection) {
			
			case Dir.left:
				if (currDirection == Dir.none) {
					prevDirection = Dir.none;
				} else if (currDirection == Dir.right) {
					prevDirection = Dir.right;
					goRight ();
				}
				break;
			
			case Dir.none:
				if (currDirection == Dir.left) {
					prevDirection = Dir.left;
					goLeft ();
				} else if (currDirection == Dir.right) {
					prevDirection = Dir.right;
					goRight ();
				}
				break;
			
			case Dir.right:
				if (currDirection == Dir.none) {
					prevDirection = Dir.none;
				} else if (currDirection == Dir.left) {
					prevDirection = Dir.left;
					goLeft ();
				}
				break;
			}
		}
	}
	*/

	void Update (){
		frame = controller.Frame ();
		HandList hands = frame.Hands;

		if (!hands.IsEmpty) {
			hand = hands [0];
			
			// y-axis movement
			float pitch = hand.Direction.Pitch;
			if (pitch > minPitch) {
				PlayerController.instance.Fly (Mathf.InverseLerp (minPitch, maxPitch, pitch));
			}

			// changing lane
			SlotPosition charPosition = PlayerController.instance.GetCurrentSlotPosition();
			SlotPosition handPosition = getHandSlotPosition();
			changeLane(charPosition, handPosition);

			// turning
			turn();

		}
	}

	SlotPosition getHandSlotPosition(){
		float x = hand.PalmPosition.x;
		if (x < -60) {
			return SlotPosition.Left;
		} else if (x < 60) {
			return SlotPosition.Center;
		} else {
			return SlotPosition.Right;
		}
	}

	void changeLane(SlotPosition charPosition, SlotPosition handPosition){
		if (handPosition < charPosition) {
			PlayerController.instance.ChangeSlots (false);
		} else if (handPosition > charPosition) {
			PlayerController.instance.ChangeSlots (true);
		}
	}

	void turn(){
		float handVelocity = hand.PalmVelocity.x;
		float fingerVelocity = 0;

		foreach (Finger finger in frame.Fingers) {
			if (Mathf.Abs(finger.TipVelocity.x) > Mathf.Abs(fingerVelocity)){
				fingerVelocity = finger.TipVelocity.x;
			}
		}

		if (handVelocity < -400 || fingerVelocity < -600) {
			PlayerController.instance.Turn (false);
			Debug.Log("Turn left");
		} else if (handVelocity > 400 || fingerVelocity > 600) {
			PlayerController.instance.Turn (true);
			Debug.Log("Turn right");
		}
	}

	void goRight ()
	{
		if (!PlayerController.instance.Turn (true)) {
			PlayerController.instance.ChangeSlots (true);
		}
	}

	void goLeft ()
	{
		if (!PlayerController.instance.Turn (false)) {
			PlayerController.instance.ChangeSlots (false);
		}
	}

	bool isLeftSwipe (SwipeGesture swipe)
	{
		Vector swipeDirection = swipe.Direction;
		return swipeDirection.x < -Mathf.Cos(swipeMaxAngle * Mathf.Deg2Rad)
			   && hand.PalmNormal.Roll < normalRoll;
	}
	
	bool isRightSwipe (SwipeGesture swipe)
	{
		Vector swipeDirection = swipe.Direction;
		return swipeDirection.x > Mathf.Cos(swipeMaxAngle * Mathf.Deg2Rad)
			   && hand.PalmNormal.Roll > normalRoll;
	}
}
