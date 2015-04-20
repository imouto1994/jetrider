using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

public class HighScoreController : MonoBehaviour {
	public Text highScoreText;
	
	void OnEnable() {
		UpdateHighScore();
	}

	void UpdateHighScore() {
		highScoreText.text = PlayerPrefs.GetInt ("High Score").ToString();
	}
	public void ResetHighScore() {
		PlayerPrefs.SetInt("High Score", 0);
		UpdateHighScore();
	}
}
