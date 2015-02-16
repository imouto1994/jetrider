using UnityEngine;
using System.Collections;

public class TimeTracker : MonoBehaviour {

	private int timeElapsed;
	private int increaseInterval;

	public PointTracker pointTracker;


	// Use this for initialization
	void Start () {
		timeElapsed = 0;
		increaseInterval = 10;

		InvokeRepeating("IncreaseTimeCount", 0.001f, 1.0f);
	}

	void IncreaseTimeCount() {
		timeElapsed++;

		//every 10 seconds
		if ((timeElapsed % increaseInterval) == 0) {
			pointTracker.IncreaseStep();
		}
	}

}
