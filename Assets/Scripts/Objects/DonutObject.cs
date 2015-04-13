using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(SpawnProbs))]
[RequireComponent(typeof(CollidableSpawnRules))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class DonutObject : CollidableObject 
{
	//modify in Unity editor
	public int pointsPerDonut = 100;
	private int playerLayer;
	public Text donutText;

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
		DisplayPointsAwardedInGUI ();
		Deactivate();
	}

	void DisplayPointsAwardedInGUI ()
	{	
		if (donutText != null) {
			GameObject dCanvas = GameObject.FindGameObjectWithTag ("DonutCanvas");
			Text displayText = Instantiate (donutText, Vector3.zero, Quaternion.identity) as Text;
			displayText.transform.SetParent (dCanvas.transform, false);
			displayText.text = "+" + pointsPerDonut;
		}
	}
}
