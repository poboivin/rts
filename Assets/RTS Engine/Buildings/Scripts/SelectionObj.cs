using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/* Selection Obj script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

public class SelectionObj : MonoBehaviour {

	//This script gets added to an empty object that only have a collider and a rigidbody, the collider represents the boundaries of the object (building or resource) that can be selected by the player.
	//The main collider of the object will only be used for placement.

	[HideInInspector]
	public GameObject MainObj; //The object that we're actually selecting.

	SelectionManager SelectionMgr;

	void Start () {
		SelectionMgr = GameManager.Instance.SelectionMgr;

		gameObject.layer = 0; //Setting it to the default layer because raycasting ignores building and resource layers.

		//In order for collision detection to work, we must assign these settings to the collider and rigidbody.
		GetComponent<Collider> ().isTrigger = true;
		if (GetComponent<Rigidbody> () == null) {
			gameObject.AddComponent<Rigidbody> ();
		}
		GetComponent<Rigidbody> ().isKinematic = true;
		GetComponent<Rigidbody> ().useGravity = false;
	}
	
	// Update is called once per frame
	public void SelectObj () {
		if (MainObj != null) { //Making sure we have linked an object or a resource object to this script:
			if (!EventSystem.current.IsPointerOverGameObject () && BuildingPlacement.IsBuilding == false) { //Make sure that the mouse is not over any UI element
				if (MainObj.GetComponent<Building> ()) { //If the object to select is a building:
					if (MainObj.GetComponent<Building> ().Placed == true) {
						//Only select the building when it's already placed and when we are not attempting to place any building on the map:
						MainObj.GetComponent<Building> ().FlashTime = 0.0f;
						MainObj.GetComponent<Building> ().CancelInvoke ("SelectionFlash");

						SelectionMgr.SelectBuilding (MainObj.GetComponent<Building> ());
					}
				} else if (MainObj.GetComponent<Resource> ()) { //If the object to select is a resource:
					MainObj.GetComponent<Resource> ().FlashTime = 0.0f;
					MainObj.GetComponent<Resource> ().CancelInvoke ("SelectionFlash");

					SelectionMgr.SelectResource (MainObj.GetComponent<Resource> ());
				} else if (MainObj.GetComponent<Unit> ()) {
					MainObj.GetComponent<Unit> ().SelectUnit ();
				}
			}
		}
	}

}
