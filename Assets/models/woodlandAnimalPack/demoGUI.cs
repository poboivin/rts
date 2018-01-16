using UnityEngine;
using System.Collections;

public class demoGUI : MonoBehaviour {
	public GameObject buck;
	public Animator buckAnim;
	public GameObject doe;
	public Animator doeAnim;
	public GameObject fawn;
	public Animator fawnAnim;

	Vector3 buckStart;
	Vector3 doeStart;
	Vector3 fawnStart;

	public Transform target;

	public GUIStyle style;

	public Rect btn1Rect = new Rect (50,20,10,10);
	public Rect btn2Rect;
	public Rect btn3Rect;
	public Rect btn4Rect;
	public Rect btn4p5Rect;
	public Rect btn5Rect;
	public Rect btn6Rect;
	public Rect btn7Rect;
	public Rect lbl1Rect;
	public Rect lbl2Rect;
	public Rect lbl3Rect;
	public Rect lbl4Rect;

	public Rect btn8Rect;
	public Rect btn9Rect;
	public Rect btn10Rect;
	public Rect btn11Rect;

	void Start(){
		buckStart = buck.transform.position;
		doeStart = doe.transform.position;
		fawnStart = fawn.transform.position;
	}


	void OnGUI() {
		GUI.Label(lbl1Rect, "Animations: Idle1, Idle2, Idle3, Idle4", style);
		if (GUI.Button(btn1Rect, "Idle 1-4")){
			PlayAnim ("idle01");
		}
		if (GUI.Button(btn2Rect, "Attack 1")){
			PlayAnim ("attack01");
		}
		if (GUI.Button(btn3Rect, "Attack 2")){
			PlayAnim ("attack02");
		}
		GUI.Label(lbl2Rect, "Walk / Run Animations Include Turning Animations",style);
		if (GUI.Button(btn4Rect, "Walk")){
			PlayAnim ("walk");
		}
		if (GUI.Button(btn4p5Rect, "Walk/Eat")){
			PlayAnim ("walkEat");
		}
		if (GUI.Button(btn5Rect, "Run")){
			PlayAnim ("run");
		}
		GUI.Label(lbl3Rect, "Animations: LayDown, LayIdle, StandUp",style);
		if (GUI.Button(btn6Rect, "Lay")){
			PlayAnim ("layDown");
		}
		if (GUI.Button(btn7Rect, "Die")){
			PlayAnim ("die");
		}
		GUI.Label(lbl4Rect, "Use Arrow Keys to Move Camera",style);

		if (GUI.Button(btn8Rect, "Follow Buck")){
			target.parent = buck.transform;
			target.position = buck.transform.position;
			target.localPosition += new Vector3(-.02f,.5f,-.06f);
		}
		if (GUI.Button(btn9Rect, "Follow Doe")){
			target.parent = doe.transform;
			target.position = doe.transform.position;
			target.localPosition += new Vector3(-.02f,.5f,-.06f);
		}
		if (GUI.Button(btn10Rect, "Follow Fawn")){
			target.parent = fawn.transform;
			target.position = fawn.transform.position;
			target.localPosition += new Vector3(-.02f,.5f,-.06f);
		}
		if (GUI.Button(btn11Rect, "Reset All")){
			buck.transform.position = buckStart;
			doe.transform.position = doeStart;
			fawn.transform.position = fawnStart;
		}
	}

	void PlayAnim(string animName){
		buckAnim.Play (animName);
		doeAnim.Play (animName);
		fawnAnim.Play (animName);
	}
}
