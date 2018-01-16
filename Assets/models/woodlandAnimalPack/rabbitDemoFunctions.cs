using UnityEngine;
using System.Collections;

public class rabbitDemoFunctions : MonoBehaviour {
	public GameObject rabbit;
	public Material brown;
	public Material white;


	// Use this for initialization
	void BrownMaterial () {
		rabbit.GetComponent<Renderer>().material = brown;
	}
	
	// Update is called once per frame
	void WhiteMaterial() {
		rabbit.GetComponent<Renderer>().material = white;
	}
}
