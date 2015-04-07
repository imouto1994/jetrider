using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PointTracker : MonoBehaviour 
{
	static public PointTracker instance;

	public Text pointsText;
	public int ticksPerSecond  = 1;
	public float pointsPerTick = 2;

	bool isActive = true;

	private float points;
	//private const string POINTS_TEXT = "Points: ";

	public void Awake() 
	{
		instance = this;
	}

	// Use this for initialization
	public void Start () {
		points = 0.0f;
		float tickInterval = 1.0f / ticksPerSecond;
		InvokeRepeating("AutoIncreasePointsPerTick", 0.001f, tickInterval);
	}

	public void UpdateText() {
		if(pointsText != null) {
			pointsText.GetComponent<Text>().text =  points.ToString();
		}
	}

	public void AutoIncreasePointsPerTick() {
		if (isActive) {
			points += pointsPerTick;
			UpdateText();
		}
	}

	public void IncreasePoints(float pointsAwarded) {
		if (isActive) {
			points += pointsAwarded;
			UpdateText();
		}
	}

	public void IncreaseStep(float step = 1.0f) {
		if (isActive) {
			pointsPerTick += step;
		}
	}

	public void GameOver() {
		isActive = false;
	}

	public string GetScore() {
		return pointsText.text;
	}
}
