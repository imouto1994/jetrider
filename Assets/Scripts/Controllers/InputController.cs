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

	public void ToggleActive() {
		enabled = !enabled;
	}

	public void ForceActive() {
		enabled = true;
	}

	public void Update() {
		// Turn
		bool hasTurned = false;
		if (Input.GetButtonDown("TurnLeft")) {
			hasTurned = PlayerController.instance.Turn(IS_LEFT);
		} else if (Input.GetButtonDown("TurnRight")) {
			hasTurned = PlayerController.instance.Turn(IS_RIGHT);
		}

		if (!hasTurned) {
			if (Input.GetButtonDown("GoLeft")) {
				PlayerController.instance.ChangeSlots(IS_LEFT);
			} else if (Input.GetButtonDown("GoRight")) {
				PlayerController.instance.ChangeSlots(IS_RIGHT);
			}
		}

		// Hovering
		if (Input.GetButton("Fly")) {
			PlayerController.instance.Fly();
		}
	}
}
