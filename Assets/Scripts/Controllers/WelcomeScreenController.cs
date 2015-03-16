using UnityEngine;
using System.Collections;

public class WelcomeScreenController : MonoBehaviour {

	public void QuitGame() {
		Debug.Log ("Quit game");
		Application.Quit ();
	}

	public void StartGame() {
		Debug.Log ("Play game!");
		Application.LoadLevel("LevelTest");
	}
}
