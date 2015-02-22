using UnityEngine;
using System.Collections;

public class Fuel : MonoBehaviour {

	private FuelTracker fuelTracker;
	public int fuelGain = 100;
	// Use this for initialization
	void Start () {
		GameObject fTracker = GameObject.Find ("FuelTracker");
		if (fTracker == null) {
			Debug.Log ("Cannot find FuelTracker!");
		} else {
			fuelTracker = fTracker.GetComponent<FuelTracker>();
		}
	}
	
	void OnTriggerEnter(Collider collider) {
		if (collider.tag == "Player") {
			//increase fuel
			fuelTracker.IncreaseFuel(fuelGain);
			Destroy(this.gameObject);
		}
	}
}
