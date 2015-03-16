using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpawnProbs))]
[RequireComponent(typeof(CollidableSpawnRules))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class DonutObject : CollidableObject 
{
	//modify in Unity editor
	public int pointsPerDonut = 100;
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
			CollectCoin();
		}
	}
	
	public void CollectCoin()
	{
		PointTracker.instance.IncreasePoints(pointsPerDonut);
		Deactivate();
	}
}
