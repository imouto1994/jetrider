using UnityEngine;
using System.Collections;

public class Donut : MonoBehaviour {

	private PointTracker pointTracker;
	public int pointsPerDonut = 100;

	// Use this for initialization
	void Start () {
		GameObject pTracker = GameObject.Find ("PointTracker");
		if (pTracker == null) {
			Debug.Log ("Cannot find PointTracker!");
		} else {
			pointTracker = pTracker.GetComponent<PointTracker>();
		}
	}

	void OnTriggerEnter(Collider collider) {
		if (collider.tag == "Player") {
			pointTracker.IncreasePoints(pointsPerDonut);
			Destroy(this.gameObject);
		}
	}
}
