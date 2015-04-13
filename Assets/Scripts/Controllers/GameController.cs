using UnityEngine;
using System.Collections;
using UnityEngine.UI;

// SINGLETON CLASS FOR GAME CONTROLLER
public class GameController : MonoBehaviour
{
	static public GameController instance;
	public GameObject gameOverScreen;

	public delegate void GenericHandler();
	public event GenericHandler OnStartGame;
	public delegate void PauseHandler(bool paused);
	public event PauseHandler OnPauseGame;

	public bool runInBackground;

	public GameObject character;
	
	private bool gamePaused = false;
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
		LeapMotionController.instance.StartGame();
		CameraController.instance.StartGame(fromRestart);
		AudioController.instance.PlayBackgroundMusic(true);
		PlayerController.instance.StartGame();
		if (OnStartGame != null) {
			OnStartGame();
		}
	}

	public void ObstacleCollision(ObstacleObject obstacle)
	{
		GameOver();
	}
	
	public bool IsGameActive()
	{
		return gameActive;
	}

	public void GameOver() {
		gameActive = false;
		if(PlayerController.instance.enabled) {
			PlayerController.instance.GameOver();
		}
		AudioController.instance.PlayBackgroundMusic(false);
		InputController.instance.GameOver();
		LeapMotionController.instance.GameOver();
		CameraController.instance.GameOver();
		PointTracker.instance.GameOver();
		DisplayGameOverScreen();
	}

	public void DisplayGameOverScreen() {
		gameOverScreen.SetActive(true);
		Text score = gameOverScreen.transform.Find ("Score").GetComponentInChildren<Text>();
		score.text = "Your score: " + PointTracker.instance.GetScore();
	}

	public void Update() {
		if (Input.GetKeyDown(KeyCode.Escape)) {
			TogglePause();
		}
	}

	public void TogglePause() {
		OnPauseGame(gamePaused);
		Time.timeScale = gamePaused ? 0 : 1; 
		gamePaused = !gamePaused;
	}
}
