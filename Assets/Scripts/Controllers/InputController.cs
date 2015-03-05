using UnityEngine;
using System.Collections;

public class InputController : MonoBehaviour {

	private const bool IS_RIGHT = true;
	private const bool IS_LEFT = false;

	static public InputController instance;

	public void Awake()
	{
		instance = this;
	}

	public void StartGame() {
		enabled = true;
	}

	public void GameOver() {
		enabled = false;
	}

	public void Update() {
		bool hasTurned = false;
		if (Input.GetButtonDown("TurnLeft")) {
			hasTurned = PlayerController.instance.Turn(IS_LEFT);
		} else if (Input.GetButtonDown("TurnRight")) {
			hasTurned = PlayerController.instance.Turn(IS_RIGHT);
		}
		
		// can move horizontally if the player hasn't turned
		if (!hasTurned) {
			if (Input.GetButtonDown("GoLeft")) {
				PlayerController.instance.ChangeSlots(IS_LEFT);
			} else if (Input.GetButtonDown("GoRight")) {
				PlayerController.instance.ChangeSlots(IS_RIGHT);
			}
		}
	}
}
