using UnityEngine;
using System.Collections;

public class SceneTransitionController : MonoBehaviour {

	public void QuitGame() {
		Debug.Log ("Quit game");
		Application.Quit ();
	}

	public void StartGame() {
		Debug.Log ("Play game!");
		Application.LoadLevel("LevelTest");
	}

	public void RestartGame() {
		Debug.Log ("Restart game");
		Application.LoadLevel("LevelTest");
	}

	public void ReturnToMainMenu() {
		Debug.Log ("Return to main menu");
		Application.LoadLevel("Welcome");
	}
}
