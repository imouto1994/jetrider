using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class FuelTracker : MonoBehaviour {

	public Slider fuelBar;

	private float fuel;
	private float maxFuel;
	private float minFuel;

	// Use this for initialization
	void Start () {
		fuel = 0.0f;
		maxFuel = 100.0f;
		minFuel = 0.0f;
		UpdateFuelBar();
	}

	public void IncreaseFuel(float fuelGain) {
		Debug.Log ("Player picked up fuel");
		fuel += fuelGain;

		if (fuel > maxFuel) {
			fuel = maxFuel;
		}
		UpdateFuelBar();
	}

	public void DecreaseFuel(float fuelDrain) {
		Debug.Log ("Draining fuel");
		fuel -= fuelDrain;

		if (fuel < minFuel) {
			fuel = minFuel;
		}
		UpdateFuelBar();
	}

	private void UpdateFuelBar() {
		fuelBar.value = (fuel / 100.0f);
	}
}
