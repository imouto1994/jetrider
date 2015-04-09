using UnityEngine;
using System.Collections;

public class FloatUp : MonoBehaviour {

	// Use this for initialization
	void Start () {
		Invoke ("Destroy", 3.0f);
	}
	
	// Update is called once per frame
	void Update () {
		gameObject.transform.position += (Vector3.up * 2F);
	}

	void Destroy() {
		Destroy (gameObject);
	}
}
