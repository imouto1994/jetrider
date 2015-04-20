using UnityEngine;
using System.Collections;

public class WelcomeScreenController : MonoBehaviour {

	public GameObject instructionsScreen;
	public GameObject highScoreScreen;

	public void DisplayInstructions() {
		instructionsScreen.SetActive(true);
	}
	
	public void CloseInstructions() {
		instructionsScreen.SetActive(false);
	}

	public void DisplayHighScore() {
		highScoreScreen.SetActive(true);
	}
	
	public void CloseHighScore() {
		highScoreScreen.SetActive(false);
	}

	public void Update() {
		if (Input.GetKeyDown(KeyCode.Escape)) {
			if (instructionsScreen.activeSelf) {
				CloseInstructions();
			}
			if (highScoreScreen.activeSelf) {
				CloseHighScore();
			}
		}
	}
}
