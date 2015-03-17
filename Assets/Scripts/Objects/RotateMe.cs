using UnityEngine;
using System.Collections;

public class RotateMe : MonoBehaviour 
{
	public float rotateSpeed = 300;

	// Update is called once per frame
	void Update () 
	{
		Rotate ();
	}

	void Rotate ()
	{
		Vector3 rotateVector = new Vector3 (0.0f, rotateSpeed, 0.0f);
		transform.Rotate (rotateVector * Time.deltaTime, Space.World);
	}
}
