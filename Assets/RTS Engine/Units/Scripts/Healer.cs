using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Healer script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

public class Healer : MonoBehaviour {

	[HideInInspector]
	public bool IsHealing = false; //is the unit healing another unit?
	[HideInInspector]
	public Unit TargetUnit; //does the player have a target to heal.
	public float MaxHealingDistance = 3.0f; //the maximum distance between the healer and the target unit to heal.

	public float HealthPerSecond = 5f; //Amount of health to give the target unit per second.
	//Timer:
	float Timer; //timers that, when it ends, adds health points to the target unit

	//Automatic behavior:
	public bool AutoHeal = true; //searches for units to heal and does it on its own.
	public float SearchReload = 5.0f; //timer before healer looks for wounded units.
	float SearchTimer;
	public float SearchRange = 20.0f; //the range at where the healer will search for wounded units.

	//main unit script:
	[HideInInspector]
	public Unit UnitMvt;

	//Audio clips:
	public AudioClip HealOrderAudio; //audio clip played when the unit is ordered to heal a unit.

	void Start () {
		//get he unit mvt script:
		UnitMvt = gameObject.GetComponent<Unit> ();

		//if the game is offline and this is a NPC character.
		if (GameManager.MultiplayerGame == false && GameManager.PlayerFactionID != UnitMvt.FactionID) {
			AutoHeal = true; //must be able to auto heal.
		}
	}
	
	void Update () { 
		if (TargetUnit == null) { //if there is no target yet.
			if (IsHealing == true) { //unit is still healing but the target unit is invalid
				UnitMvt.StopMvt (); //stop healing
				UnitMvt.CancelHealing ();
			}

			if (AutoHeal == true && UnitMvt.IsIdle() == true) { //if the unit can heal automatically and the unit is not doing any other task
				if (GameManager.MultiplayerGame == false || (GameManager.MultiplayerGame == true && GameManager.PlayerFactionID == UnitMvt.FactionID)) { //if this is the local player in a MP game or if this is simply an offline game
					if (SearchTimer > 0) {
						SearchTimer -= Time.deltaTime;
					} else {
						//search for units 
						int i = 0;
						while (TargetUnit == null && i < UnitMvt.FactionMgr.Units.Count) {//loop through the faction's units
							if (UnitMvt.FactionMgr.Units [i] != UnitMvt) { //if this is not the same unit
								//if the unit is in the defined range:
								if (Vector3.Distance (UnitMvt.FactionMgr.Units [i].transform.position, transform.position) < SearchRange) {
									//if the unit have less health
									if (UnitMvt.FactionMgr.Units [i].Health < UnitMvt.FactionMgr.Units [i].MaxHealth) {
										SetTargetUnit(UnitMvt.FactionMgr.Units [i]);
									}
								}
							}
							i++;
						}
						SearchTimer = SearchReload; //reload the search timer.
					}
				}
			}
		}
		//If the player has a target unit, then send him there:
		else
		{
			if (TargetUnit.Health >= TargetUnit.MaxHealth) { //If the target unit reached the maximum health:
				//stop healing
				UnitMvt.StopMvt ();
				UnitMvt.CancelHealing ();
			} else {	
				//If the unit is in range of the unit to heal
				if (Vector3.Distance (transform.position, TargetUnit.transform.position) <= (TargetUnit.NavAgent.radius+MaxHealingDistance)) {
					if (IsHealing == false) {
						//Stop moving:
						UnitMvt.StopMvt ();

						//Start healing:
						IsHealing = true;

						//Inform the animator that we started healing:
						UnitMvt.SetAnimState (Unit.UnitAnimState.Healing);

						//custom event:
						if (UnitMvt.GameMgr.Events)
							UnitMvt.GameMgr.Events.OnUnitStartHealing (UnitMvt, TargetUnit);
						
						Timer = 1.0f;
					}
				}
				else if(UnitMvt.Moving == false) 
				{
					//Bring the unit back:
					UnitMvt.CheckUnitPathLocal (Vector3.zero, TargetUnit.gameObject, UnitMvt.GameMgr.MvtStoppingDistance, -1);
				}

				//Adding health to the unit:
				if (IsHealing == true) {
					//if it's a MP game:
					if (GameManager.MultiplayerGame == true) {
						//only heal if every client is on track: 
						if (UnitMvt.GameMgr.Factions [GameManager.PlayerFactionID].MFactionMgr.CanSendInput == false) {
							return;
						}
					}

					//healing timer:
					if (UnitMvt.Moving == false) {
						if (Timer > 0) {
							Timer -= Time.deltaTime;
						}
						if (Timer <= 0) {
							Timer = 1.0f;
							TargetUnit.AddHealth (HealthPerSecond, null); //adding health points each second
						}
					} else {
						//if the unit has moved while healing.
						IsHealing = false;
						//move the unit back to its target unit
						if (TargetUnit)
							UnitMvt.CheckUnitPathLocal (Vector3.zero, TargetUnit.gameObject, UnitMvt.GameMgr.MvtStoppingDistance, -1);
					}
				}
			}
		}
	}

	//Set the target unit to heal.
	public void SetTargetUnit (Unit Target)
	{
		//if it's as single player game.
		if (GameManager.MultiplayerGame == false) {
			//directly send the unit to build
			SetTargetUnitLocal (Target);
		} else {
			//in a case of a MP game
			//and it's the unit belongs to the local player:
			if (GameManager.PlayerFactionID == UnitMvt.FactionID) {
				//ask the server to tell all clients at once that this unit is going to heal a unit

				MFactionManager.InputActionsVars NewInputAction = new MFactionManager.InputActionsVars ();
				NewInputAction.Source = UnitMvt.netId;

				NewInputAction.Target = Target.netId;

				NewInputAction.InitialPos = transform.position;
				NewInputAction.TargetPos = Target.transform.position;

				UnitMvt.GameMgr.Factions [UnitMvt.FactionID].MFactionMgr.InputActions.Add (NewInputAction);
			}
		}
	}

	//Set the unit to heal
	public void SetTargetUnitLocal (Unit Target)
	{
		if (Target == null || TargetUnit == Target)
			return;

		//Check first if the unit actually needs health by cheking if its current health is below the max health:
		if (Target.Health < Target.MaxHealth) {
			UnitMvt.CancelHealing (); //stop healing the current unit

			IsHealing = false;

			TargetUnit = Target;

			//Move the unit:
			UnitMvt.CheckUnitPathLocal (Vector3.zero, TargetUnit.gameObject, UnitMvt.GameMgr.MvtStoppingDistance, -1);

			/*UnitMvt.NavAgent.avoidancePriority = GameManager.Instance.UnitMgr.MinArmyPriority + Target.CurrentBuilders.Count - 1;
			if (UnitMvt.NavAgent.avoidancePriority > 99) {
				UnitMvt.NavAgent.avoidancePriority = 99;
			}*/
		} else {
			UnitMvt.UIMgr.ShowPlayerMessage ("Target unit has maximum health.", UIManager.MessageTypes.Error);
		}
	}
}
