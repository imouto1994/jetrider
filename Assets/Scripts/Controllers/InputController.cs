using UnityEngine;
using System.Collections;

public class ActionController : MonoBehaviour {

	static public ActionController instance;

	public void Awake()
	{
		instance = this;
	}
}
