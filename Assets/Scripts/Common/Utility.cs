using UnityEngine;
using System.Collections;

/*
 * This class provide utility functions which will be used across the project
 */
public class Utility
{
	// Activate an object hierachy recuresively
	public static void SetActive(Transform obj, bool active)
	{
		foreach (Transform child in obj) {
			Utility.SetActive(child, active);
		}
		obj.gameObject.SetActive(active);
	}
}