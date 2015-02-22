using UnityEngine;
using System.Collections;

// SINGLETON CLASS FOR GAME CONTROLLER
public class GameController : MonoBehaviour {

	static public GameController instance;

	public delegate void GenericHandler();
	public event GenericHandler OnPlayerSpawn;
	public event GenericHandler OnStartGame;
	public delegate void PauseHandler(bool paused);
	public event PauseHandler OnPauseGame;

}
