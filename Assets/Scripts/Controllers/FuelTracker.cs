using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class FuelTracker : MonoBehaviour 
{
	static public FuelTracker instance;

	public Slider fuelBar;

	private float fuel;
	private float maxFuel;
	private float minFuel;

	public void Awake() 
	{
		instance = this;		
	}

	// Use this for initialization
	public void Start() 
	{
		maxFuel = 100.0f;
		fuel = maxFuel;
		minFuel = 0.0f;
		UpdateFuelBar();
	}

	public float getFuel() {
		return fuel;
	}

	public void IncreaseFuel(float fuelGain) 
	{
		fuel += fuelGain;

		if (fuel > maxFuel) {
			fuel = maxFuel;
		}
		UpdateFuelBar();
	}

	public void DecreaseFuel(float fuelDrain) 
	{
		fuel -= fuelDrain;

		if (fuel < minFuel) {
			fuel = minFuel;
		}
		UpdateFuelBar();
	}

	private void UpdateFuelBar() 
	{	
		if (fuelBar != null) {
			fuelBar.value = (fuel / 100.0f);
		}
	}
}
