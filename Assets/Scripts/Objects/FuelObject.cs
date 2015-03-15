using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpawnProbs))]
[RequireComponent(typeof(CollidableSpawnRules))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class FuelObject : CollidableObject {

	public int fuelGain = 100;
	private int playerLayer;
	
	public override void Init()
	{
		base.Init();
		objectType = ObjectType.Donut;
	}
	
	public override void Awake()
	{
		base.Awake();
		playerLayer = LayerMask.NameToLayer("Player");
	}
	
	public void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.layer == playerLayer) {
			CollectFuel();
		}
	}
		
	public void CollectFuel()
	{
		FuelTracker.instance.IncreaseFuel(fuelGain);
		Deactivate();
	}
}
