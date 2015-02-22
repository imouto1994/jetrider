using UnityEngine;
using System.Collections;

public class Utility
{
	public static void SetActive(Transform obj, bool active)
	{
		foreach (Transform child in obj) {
			Utility.SetActive(child, active);
		}
		obj.gameObject.SetActive(active);
	}
}