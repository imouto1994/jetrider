using UnityEngine;
using System.Collections;

public class TutorialController : MonoBehaviour {


	public GameObject tutorialRight;
	public GameObject tutorialHover;

	private GameObject activeAnim;
	// Use this for initialization
	void Start () {
		activeAnim = null;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnTriggerEnter(Collider collider) {
		if (collider.tag == "Player") {
			Debug.Log ("player hit tutorial trigger");
			switch (gameObject.name) {
			case "TutorialTriggerRight": 
				tutorialRight.SetActive(true);
				activeAnim = tutorialRight;
				Invoke("StopAnimation", 3.0f);
				
				Debug.Log ("play right animation");
				break;
			case "TutorialTriggerHover":
				tutorialHover.SetActive(true);
				activeAnim = tutorialHover;
				Invoke("StopAnimation", 3.0f);
				
				Debug.Log ("play hover animation");
				break;
			default : Debug.Log ("wts default");
				break;
			}
		}
	}

	void StopAnimation() {
		if (activeAnim != null) {
			activeAnim.SetActive(false);
			activeAnim = null;
		}
	}
}
