using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Multiple Attacks script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

public class MultipleAttacks : MonoBehaviour {

	[HideInInspector]
	public Attack[] AttackTypes; //holds all the attack types that the unit has.
	int ActiveAttackID; //saves the current active attack type id in the above list.
	public int AttackTypesTaskCategory = 0;

	Unit UnitMvt;
	void Start () {

		//get the unit comp:
		UnitMvt = GetComponent<Unit>();

		//get all the attack types in the unit:
		AttackTypes = GetComponents<Attack>();

		if (AttackTypes.Length > 1) { //if we have more than one attack type
			//the first one is enabled:
			AttackTypes [0].enabled = true;
			ActiveAttackID = 0;
			//the rest are disabled
			for (int i = 1; i < AttackTypes.Length; i++) {
				AttackTypes [i].enabled = false;
			}
		} else {
			//if we have 1 or less attack types, we won't be needing this script then:
			Destroy(this); //remove it then.
		}
	}

	public void EnableAttackType (int ID)
	{
		if (GameManager.MultiplayerGame == true) { //if this is a MP game and it's the local player:
			if (GameManager.PlayerFactionID == UnitMvt.FactionID) {
				//send the custom action input:
				MFactionManager.InputActionsVars NewInputAction = new MFactionManager.InputActionsVars ();
	
				NewInputAction.Source = UnitMvt.netId;
				NewInputAction.CustomAction = true;
				NewInputAction.StoppingDistance = ID;

				UnitMvt.MFactionMgr.InputActions.Add (NewInputAction);
			}
		} else {
			//offline game? update the attack type directly:
			EnableAttackTypeLocal(ID);
		}
	}
	
	public void EnableAttackTypeLocal (int ID) //called locally to change the attack type
	{
		if (AttackTypes [ActiveAttackID].AttackTarget != null) { //if the unit has an attack target in the last attack type
			AttackTypes[ID].SetAttackTarget(AttackTypes [ActiveAttackID].AttackTarget); //set the same target for the new attack tpye
		}

		AttackTypes [ActiveAttackID].ResetAttack (); //reset the old attack type

		//disable the last attack type:
		AttackTypes[ActiveAttackID].enabled = false;
		//enable the new one
		AttackTypes [ID].enabled = true; //enable the new attack:

		//custom event
		if (GameManager.Instance.Events) {
			GameManager.Instance.Events.OnUnitSwitchAttack (UnitMvt, AttackTypes [ActiveAttackID], AttackTypes [ID]);
		}

		//save the new attack ID.
		ActiveAttackID = ID;

		//update the tasks UI if the unit is selected:
		if (GameManager.Instance.SelectionMgr.SelectedUnits.Count == 1) {
			if (GameManager.Instance.SelectionMgr.SelectedUnits [0] == UnitMvt) {
				GameManager.Instance.UIMgr.UpdateUnitTasks ();
			}
		}
	}
}
