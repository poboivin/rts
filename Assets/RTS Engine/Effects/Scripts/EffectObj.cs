using UnityEngine;
using System.Collections;

public class EffectObj : MonoBehaviour {

	public string Code; //Give each type of attack object a unique code used to identify it.

	public float LifeTime = 3.0f; //Determine how long will the effect object will be shown for
	[HideInInspector]
	public float Timer; 

	void Update ()
	{
		if (Timer > 0.0f) {
			Timer -= Time.deltaTime;
		}
		if (Timer < 0.0f) {
			Timer = 0.0f;
			gameObject.SetActive (false);
		}
	}
}
