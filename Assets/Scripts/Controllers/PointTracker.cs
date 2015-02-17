using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PointTracker : MonoBehaviour {

	public Text pointsText;
	public int ticksPerSecond  = 1;
	public float pointsPerTick = 2;

	private float points;
	private const string POINTS_TEXT = "Points: ";

	// Use this for initialization
	void Start () {
		points = 0.0f;
		float tickInterval = 1.0f / ticksPerSecond;
		InvokeRepeating("AutoIncreasePointsPerTick", 0.001f, tickInterval);
	}
	
	// Update is called once per frame
	void Update () {
	}

	void UpdateText() {
		pointsText.GetComponent<Text>().text =  points.ToString();
	}

	void AutoIncreasePointsPerTick() {
		points += pointsPerTick;
		UpdateText();
	}

	public void IncreasePoints(float pointsAwarded) {
		Debug.Log ("Player picked up donut");
		points += pointsAwarded;
		UpdateText();
	}

	public void IncreaseStep(float step = 1.0f) {
		pointsPerTick += step;
		Debug.Log ("Increase Step, points per tick = " + pointsPerTick);
	}
}
