using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Portal script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

public class Portal : MonoBehaviour {

	public string Name; //give a name for each portal.

	public Transform SpawnPos; //when units come out of this portal
	public Transform GotoPos; //if there's a goto pos, then the unit will move to this position when they spawn.

	public Portal TargetPortal; 

	public bool AllowAllUnits = true;
	public List<string> AllowedUnitCategories = new List<string>(); //a list of the allowed unit categories to teleport using this portal.

	//audio clips:
	public AudioClip TeleportAudio;

	//double click:
	float DoubleClickTimer;
	bool ClickedOnce = false;

	void Start () {
		if (SpawnPos == null) {
			Debug.LogError ("You must assign a spawn position (transform) for the portal to spawn units at");
		}

		ClickedOnce = false;
		DoubleClickTimer = 0.0f;
	}

	void Update ()
	{
		//double click timer:
		if (ClickedOnce == true) {
			if (DoubleClickTimer > 0) {
				DoubleClickTimer -= Time.deltaTime;
			}
			if (DoubleClickTimer <= 0) {
				ClickedOnce = false;
			}
		}
	}
	public void Teleport (Unit Unit)
	{
		if (TargetPortal != null) { //make sure there's a portal to spawn at.
			if (TargetPortal.SpawnPos != null) { //likewise, the target portal must have a spawn pos for units to spawn at.
				//teleport unit:
				Unit.gameObject.SetActive(false);
				Unit.transform.position = TargetPortal.SpawnPos.position;
				Unit.gameObject.SetActive(true);

				if (GameManager.Instance.Events) {
					GameManager.Instance.Events.OnUnitTeleport (this, TargetPortal, Unit);
				}

				//play the audio clip:
				AudioManager.PlayAudio(this.gameObject, TeleportAudio, false);

				//if there's a goto position:
				if (TargetPortal.GotoPos) {
					//go there:
					if (GameManager.MultiplayerGame == false || (GameManager.MultiplayerGame == true && GameManager.PlayerFactionID == Unit.FactionID)) { //if this is an offline game or a MP game and this is the local player
						Unit.CheckUnitPath (TargetPortal.GotoPos.position,null, GameManager.Instance.MvtStoppingDistance, -1,true); //Move to the goto position:
					}
				}
			}
		}
	}

	public void TriggerMouseClick ()
	{
		if (ClickedOnce == false) {
			DoubleClickTimer = 0.5f;
			ClickedOnce = true;
		} else if(TargetPortal != null) {
			//move the view to the target portal:
			GameManager.Instance.CamMov.LookAt(TargetPortal.transform.position);

			//custom event:
			if (GameManager.Instance.Events) {
				GameManager.Instance.Events.OnPortalDoubleClick (this, TargetPortal, null);
			}
		}

	}

	//What units are allowed through this portal?

	public bool IsAllowed (Unit Unit)
	{
		if (AllowAllUnits == true)
			return true;
		if (AllowedUnitCategories.Contains (Unit.Category))
			return true;

		return false;
	}
}
