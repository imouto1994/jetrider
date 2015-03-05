using UnityEngine;

[RequireComponent(typeof(SpawnProbs))]
[RequireComponent(typeof(SceneSpawnRules))]
public class SceneObject : BasicObject
{
	// True if this piece is used for section transitions
	public bool isForSectionTransition;
	
	// Set this offset if the scene object's center doesn't match up with the true center 
	public Vector3 centerOffset;
	
	public override void Init()
	{
		base.Init();
		objectType = ObjectType.Scene;
	}
}
