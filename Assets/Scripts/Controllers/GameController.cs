using UnityEngine;
using System.Collections;

// SINGLETON CLASS FOR GAME CONTROLLER
public class GameController : MonoBehaviour {

	static public GameController instance;
	
	public delegate void PauseHandler(bool paused);
	public event PauseHandler OnPauseGame;

}
