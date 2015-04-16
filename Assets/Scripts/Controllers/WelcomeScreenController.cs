using UnityEngine;
using System.Collections;

public class WelcomeScreenController : MonoBehaviour {

	public GameObject instructionsScreen;

	public void DisplayInstructions() {
		instructionsScreen.SetActive(true);
	}
	
	public void CloseInstructions() {
		instructionsScreen.SetActive(false);
	}

	public void Update() {
		if (Input.GetKeyDown(KeyCode.Escape)) {
			if (instructionsScreen.activeSelf) {
				CloseInstructions();
			}
		}
	}
}
