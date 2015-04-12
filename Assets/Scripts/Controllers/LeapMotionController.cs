using UnityEngine;
using System.Collections;
using Leap;

public class LeapMotionController : MonoBehaviour
{

	enum Dir {left, right, none};

	static public LeapMotionController instance;
	[SerializeField]
	private float
		swipeMinLength;
	[SerializeField]
	private float
		swipeMinVelocity;
	private Controller controller;
	private int prevGestureId;
	private bool isMovingLeft, isMovingRight;
	private Dir prevDirection;

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

	void Update ()
	{
		Frame frame = controller.Frame ();
		HandList hands = frame.Hands;
		Hand hand;
		if (!hands.IsEmpty) {
			hand = hands [0];
			float pitch = hand.Direction.Pitch;
			if (pitch > 0.35) {
				PlayerController.instance.Fly ();
			}
		}
		Dir currDirection;

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
		return swipeDirection.x < -0.3f;
	}
	
	bool isRightSwipe (SwipeGesture swipe)
	{
		Vector swipeDirection = swipe.Direction;
		return swipeDirection.x > 0.3f;
	}
}
