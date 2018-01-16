using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/* APC script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

public class APC : MonoBehaviour {

	public List<string> AllowedUnitsCategories = new List<string>(); //a list of the allowed unit categories to enter the APC.

	public int MaxAmount = 4; //max amount to transport at the same time.
	[HideInInspector]
	public List<Unit> CurrentUnits = new List<Unit>(); //the units contained in the APC unit.
	public int EjectOneUnitTaskCategory = 0;
	public AudioClip EjectAudio;
	public float MaxDistance = 4; //The maximum distance at which the unit can enter the APC.

	//UI:
	public bool EjectAllOnly = false;
	public int EjectAllOnlyTaskCategory = 0;
	public Sprite EjectAllIcon; //The task's icon that will eject all the contained units when launched.

	//calling units:
	public bool CanCallUnits = true; //can the APC call units to get them inside?
	public int CallUnitsTaskCategory = 0;
	public float CallingRange = 20.0f; //the range at which units will be called to get into the APC
	public Sprite CallUnitsSprite; //The task's icon that will eject all the contained units when launched.
	public bool StopUnitsFromAttackingOnCall = false; //stop units from attacking when they are called? 

	//audio clips:
	public AudioClip AddUnitAudio;
	public AudioClip RemoveUnitAudio;
	public AudioClip CallUnitsAudio;

	public bool ReleaseOnDestroy = true; //if true, all units will be released on destroy, if false, all contained units will be destroyed.

	//other scripts:
	GameManager GameMgr;
	FactionManager FactionMgr;
	MFactionManager MFactionMgr;

	void Start ()
	{
		GameMgr = GameManager.Instance;
		//get the faction manager from the unit APC
		if (gameObject.GetComponent<Unit> ()) {
			FactionMgr = GameMgr.Factions[gameObject.GetComponent<Unit> ().FactionID].FactionMgr;
			MFactionMgr = GameMgr.Factions[gameObject.GetComponent<Unit> ().FactionID].MFactionMgr;
		}
		//get the faction manager from the building APC.
		else if (gameObject.GetComponent<Building> ()) {
			FactionMgr = GameMgr.Factions[gameObject.GetComponent<Building> ().FactionID].FactionMgr;
			MFactionMgr = GameMgr.Factions[gameObject.GetComponent<Building> ().FactionID].MFactionMgr;
		}
	}

	//method to add a unit to the APC:
	public void AddUnit (Unit Unit)
	{
		//check if there is space left:
		if (CurrentUnits.Count < MaxAmount) {
			//add the unit:
			Unit.gameObject.SetActive(false);
			CurrentUnits.Add(Unit);
			Unit.transform.SetParent (transform, true);

			//deselect unit:
			if (GameMgr.SelectionMgr.SelectedUnits.Contains (Unit)) {
				GameMgr.SelectionMgr.DeselectUnit (Unit);
			}

			//play the audio clip:
			AudioManager.PlayAudio(this.gameObject, AddUnitAudio, false);

			//if this unit APC is selected:
			if (GameMgr.SelectionMgr.SelectedUnits.Contains (gameObject.GetComponent<Unit> ())) {
				GameMgr.UIMgr.UpdateUnitTasks (); //update the task panel
			}
			//if this building APC is selected
			if (GameMgr.SelectionMgr.SelectedBuilding != null) {
				//update the task panel
				GameMgr.UIMgr.UpdateBuildingTasks (GameMgr.SelectionMgr.SelectedBuilding);
			}

			//custom event:
			if (GameMgr.Events != null) {
				GameMgr.Events.OnAPCAddUnit (this, Unit);
			}
		}
	}

	public void RemoveUnitFromAPC (int ID)
	{
		if (GameManager.MultiplayerGame == true) { //if this is a MP game and it's the local player:
			if (GameManager.PlayerFactionID == FactionMgr.FactionID) {
				//send the custom action input:
				MFactionManager.InputActionsVars NewInputAction = new MFactionManager.InputActionsVars ();

				//get the APC netID from the unit APC
				if (gameObject.GetComponent<Unit> ()) {
					NewInputAction.Source = gameObject.GetComponent<Unit> ().netId;
				}
				//get the APC netID from the unit APC
				else if (gameObject.GetComponent<Building> ()) {
					NewInputAction.Source = gameObject.GetComponent<Building> ().netId;
				}
				NewInputAction.CustomAction = true;
				NewInputAction.StoppingDistance = ID+13; //12 stands for the invisibility.

				MFactionMgr.InputActions.Add (NewInputAction);
			}
		} else {
			//offline game? update the attack type directly:
			RemoveUnitFromAPCLocal(ID);
		}
	}

	public void RemoveUnitFromAPCLocal (int ID)
	{
		//when ID is 0, then all units inside the APC will be removed.
		if (ID == 0) { //if the ID is 0
			int Count = CurrentUnits.Count;
			for (int i = 0; i < Count; i++) { //all units inside the APC are removed
				RemoveUnit (CurrentUnits [0]);
			}
		} else {
			RemoveUnit (CurrentUnits [ID - 1]); //remove only one unit from the APC
		}
	}

	//method to remove a unit from the APC:
	public void RemoveUnit (Unit Unit)
	{
		//check if the unit is actually in the APC
		if (CurrentUnits.Contains(Unit)) {
			//remove the unit:
			Unit.transform.SetParent (null, true);
			CurrentUnits.Remove(Unit);
			Unit.gameObject.SetActive(true); //set it active again1

			//play the audio clip:
			AudioManager.PlayAudio(this.gameObject, RemoveUnitAudio, false);

			//if this unit APC is selected:
			if (GameMgr.SelectionMgr.SelectedUnits.Contains (gameObject.GetComponent<Unit> ())) {
				GameMgr.UIMgr.UpdateUnitTasks (); //update the task panel
			}
			//if this building APC is selected
			if (GameMgr.SelectionMgr.SelectedBuilding != null) {
				//update the task panel
				GameMgr.UIMgr.UpdateBuildingTasks (GameMgr.SelectionMgr.SelectedBuilding);
			}

			//custom event:
			if (GameMgr.Events != null) {
				GameMgr.Events.OnAPCRemoveUnit (this, Unit);
			}
		}
	}

	//called when the APC requests nearby units to enter.
	public void CallForUnits ()
	{
		int i = 0; //counter
		AudioManager.PlayAudio(this.gameObject, CallUnitsAudio, false); //play the call for units audio
		while (i < FactionMgr.Units.Count && CurrentUnits.Count < MaxAmount) { //go through the faction's units while still making sure that there is more space for units to get in
			//the target unit can't be another APC and it must be active and its category matches the allowed categories in this APC.
			if (!FactionMgr.Units [i].gameObject.GetComponent<APC>() && AllowedUnitsCategories.Contains (FactionMgr.Units [i].Category) && FactionMgr.Units [i].gameObject.activeInHierarchy == true) {
				//if the unit is at a distance that is less or equal to the calling distance
				if (Vector3.Distance (this.transform.position, FactionMgr.Units [i].transform.position) <= CallingRange) {
					if (StopUnitsFromAttackingOnCall == true) { //if the APC can stop units from attacking
						if (FactionMgr.Units [i].GetComponent<Attack> ()) { //then check if they have an attack component
							FactionMgr.Units [i].CancelAttack (); //cancel the attack
							FactionMgr.Units [i].GetComponent<Attack> ().AttackTarget = null;
						}
					}
					//set the target APC for the nearby unit
					FactionMgr.Units [i].TargetAPC = this;
					//make the unit move to the APC.
					FactionMgr.Units [i].CheckUnitPath (this.transform.position, this.gameObject, GameManager.Instance.MvtStoppingDistance, i, true);
				}
			}
			i++;
		}
	}
}
