using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

public class HighScoreController : MonoBehaviour {
	public Text highScoreText;
	
	void OnEnable() {
		highScoreText.text = PlayerPrefs.GetInt ("High Score").ToString();
	}
}
