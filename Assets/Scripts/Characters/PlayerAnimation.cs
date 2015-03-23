using UnityEngine;
using System.Collections;

public class PlayerAnimation : MonoBehaviour {

	// Animation Name
	public string runAnimationName = "Run";
	public string hoverAnimationName = "Hover";
	public string dieAnimationName = "Die";

	// The speed of the run animation when the player is running
	public float slowRunSpeed = 1.0f;
	public float fastRunSpeed = 1.0f;

	private Animator thisAnimator;
	private int hoverHash;
	private int dieHash;

	public void Init()
	{
		thisAnimator = GetComponentsInChildren<Animator>()[0];
		
		hoverHash = Animator.StringToHash("Base Layer.Hover");
		dieHash = Animator.StringToHash("Base Layer.Die");
	}
	
	public void OnEnable()
	{
		GameController.instance.OnPauseGame += OnPauseGame;
	}
	
	public void OnDisable()
	{
		GameController.instance.OnPauseGame -= OnPauseGame;
	}
	
	public void Update()
	{

	}
	
	public void SetRunSpeed(float speed, float t)
	{
		thisAnimator.SetFloat("Speed", speed);
	}

	public void Run()
	{
		thisAnimator.SetBool(runAnimationName, true);
	}

	public void Hover()
	{
		thisAnimator.SetBool(hoverAnimationName, true);
	}

	
	public void GameOver()
	{
		thisAnimator.SetBool(dieAnimationName, true);
	}
	
	public void ResetValues()
	{
		thisAnimator.SetFloat("Speed", 0);
		thisAnimator.SetBool(hoverAnimationName, false);
		thisAnimator.SetBool(dieAnimationName, false);
	}
	
	public void OnPauseGame(bool paused)
	{
		float speed = (paused ? 0 : 1);
		thisAnimator.speed = speed;
	}
}
