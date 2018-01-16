using UnityEngine;
using System.Collections;

public class universalDemoGUI : MonoBehaviour {
	public GameObject[] targets;
	Vector3[] targetStartPositions;
	public Transform target;
	public GUIStyle style;
	public demoButton[] buttons;
	int i = 0;

	void Start(){
		//lets fill in the start positions for all the possible targets
		targetStartPositions = new Vector3[targets.Length];
		for(i=0;i<targets.Length;i++){
			targetStartPositions[i] = targets[i].transform.position;
		}
		if(target == null){
			target = targets[0].transform;
		}
	}


	void OnGUI() {
		for(i=0;i<buttons.Length;i++){
			if (buttons[i].isLabel){
				GUI.Label(buttons[i].btnRect,buttons[i].btnName,style);
			}else{
				if (GUI.Button(buttons[i].btnRect,buttons[i].btnName)){
					if(buttons[i].callFunction){
						gameObject.SendMessage(buttons[i].btnName);
					}else{
						PlayAnim (buttons[i].btnName);
					}
				}
			}
		}
	}

	void PlayAnim(string animName){
		for(i=0;i<targets.Length;i++){
			targets[i].GetComponent<Animator>().Play(animName,0);
		}
	}
}
