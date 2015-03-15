using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SpawnProbs))]
[RequireComponent(typeof(CollidableSpawnRules))]
public class ObstacleObject : CollidableObject {

	// True if the player is allowed to run on top of the obstacle. 
	public bool canRunOnTop;

	protected int playerLayer;
	private int objectLayer;
	private int platformLayer;

	public override void Init()
	{
		base.Init();
		objectType = ObjectType.Obstacle;
	}
	
	public override void Awake()
	{
		base.Awake();
		playerLayer = LayerMask.NameToLayer("Player");
		platformLayer = LayerMask.NameToLayer("Platform");
	}
	
	public virtual void Start()
	{
		objectLayer = gameObject.layer;
	}
	
	public void OnTriggerEnter(Collider other)
	{	
		print("CC");
		if (other.gameObject.layer == playerLayer) {
			bool collide = true;
			if (canRunOnTop) {
				if (Vector3.Dot(Vector3.up, (other.transform.position - thisTransform.position)) > 0) {
					collide = false;
					thisGameObject.layer = platformLayer;
				}
			}
			
			if (collide) {
				GameController.instance.ObstacleCollision();
			}
		}
	}
	
	public override void CollidableDeactivation()
	{
		base.CollidableDeactivation();
		
		// Reset data
		if (canRunOnTop) {
			thisGameObject.layer = objectLayer;
		}
	}
}
