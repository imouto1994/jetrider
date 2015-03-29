using UnityEngine;
using System.Collections;

// SINGLETON CLASS FOR GAME CONTROLLER
public class GameController : MonoBehaviour
{
	static public GameController instance;
	
	public delegate void GenericHandler();
	public event GenericHandler OnStartGame;
	public delegate void PauseHandler(bool paused);
	public event PauseHandler OnPauseGame;

	public bool runInBackground;

	public GameObject character;
	
	private bool gamePaused;
	private bool gameActive;
	
	public void Awake()
	{
		instance = this;
	}

	// Initialize function before first frame
	public void Start()
	{
		Application.runInBackground = runInBackground;
		SpawnCharacter();
		StartGame(false);
	}

	// Spawn character
	private void SpawnCharacter()
	{
		character = GameObject.Instantiate(character) as GameObject;
		PlayerController.instance.Init();
	}
	
	public void StartGame(bool fromRestart)
	{
		gameActive = true;
		InputController.instance.StartGame();
		CameraController.instance.StartGame(fromRestart);
		PlayerController.instance.StartGame();
		if (OnStartGame != null) {
			OnStartGame();
		}
	}

	public void ObstacleCollision(ObstacleObject obstacle)
	{
		obstacle.Deactivate();
		GameOver();
	}
	
	public bool IsGameActive()
	{
		return gameActive;
	}

	public void GameOver() {
		CameraController.instance.GameOver();
		PointTracker.instance.GameOver();
		Destroy (character);
	}
}
