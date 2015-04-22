using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

// SINGLETON CLASS FOR GAME CONTROLLER
public class GameController : MonoBehaviour
{
	static public GameController instance;
	public GameObject gameOverScreen;
	public GameObject menuScreen;
	public GameObject instructionsScreen;
	public GameObject highScoreScreen;

	public delegate void GenericHandler();
	public event GenericHandler OnStartGame;
	public delegate void PauseHandler(bool paused);
	public event PauseHandler OnPauseGame;

	public bool runInBackground;

	public GameObject character;
	
	private bool gamePaused = false;
	private bool gameActive;

	private int highScore = 0;

	private string currentScreen = "";
	
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

	public void ObstacleCollision(ObstacleObject obstacle, Vector3 position)
	{
		PlayerController.instance.ObstacleCollision(obstacle.GetTransform(), position);
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
		Text scoreText = gameOverScreen.transform.Find ("Score").GetComponentInChildren<Text>();
		int score = Int32.Parse(PointTracker.instance.GetScore());
		scoreText.text = "Your score: " + score;

		highScore = PlayerPrefs.GetInt ("High Score");
		if (score > highScore) {
			GameObject highScoreNotification = gameOverScreen.transform.Find("HighScore").gameObject;
			PlayerPrefs.SetInt("High Score", score);
			highScoreNotification.SetActive(true);
		}
	}

	public void Update() {
		if (Input.GetKeyDown(KeyCode.Escape)) {
			if (instructionsScreen.activeSelf) {
				CloseInstructions();
			} else if (highScoreScreen.activeSelf) {
				CloseHighScore();
			} else {
				TogglePause();
			}
		}

		if ( (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return)) && gameOverScreen.activeSelf) {
			SceneTransitionController transitionScript = GetComponent<SceneTransitionController>();
			transitionScript.RestartGame();
		}

	}

	public void TogglePause() {
		gamePaused = !gamePaused;
		OnPauseGame(gamePaused);
		Time.timeScale = gamePaused ? 0 : 1; 
		menuScreen.SetActive (gamePaused);
		InputController.instance.ToggleActive();
	}

	public void ForceResume() {
		OnPauseGame(true);
		Time.timeScale = 1;
		menuScreen.SetActive(false);
		gamePaused = false;
		InputController.instance.ForceActive();
	}

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
}
