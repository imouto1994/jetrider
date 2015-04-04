using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class FadeInFadeOut : MonoBehaviour {

	Image img;
	float delta = -0.01f;
	void Start () {
		img = gameObject.GetComponent<Image>();
	}

	void Update() {
		if (img.color.a <= 0f || img.color.a >= 1f) {
			delta = -delta;
		}
		img.color = new Vector4(1.0f, 1.0f, 1.0f, delta);
	}
}
