using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WallObject : MonoBehaviour
{

	private int playerLayer;
	
	public void Awake()
	{
		playerLayer = LayerMask.NameToLayer("Player");
	}

	public void OnTriggerEnter(Collider other)
	{	
		if (other.gameObject.layer == playerLayer) {
			GameController.instance.GameOver();
		}
	}
}