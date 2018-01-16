using UnityEngine;
using System.Collections;

/* Builder script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

public class Builder : MonoBehaviour {
	

	[HideInInspector]
	public bool IsBuilding = false; //is the player constructing a building?
	[HideInInspector]
	public Building TargetBuilding; //does the player have a target building to construct?

	public float HealthPerSecond = 5f; //Amount of health to give the building per second
	//Timer:
	float Timer; //timers that, when it ends, adds health points to the target building.

	//main unit script:
	[HideInInspector]
	public Unit UnitMvt;

	//auto-build:
	public bool AutoBuild = true; //searches for buildings to construct them
	public float SearchReload = 5.0f; //timer before the builder looks for buildings to construct
	float SearchTimer;
	public float SearchRange = 20.0f; //the range at where the builder will search for buildings

	//Audio clips:
	public AudioClip[] BuildingAudio; //audio played when the unit is building.
	public AudioClip BuildingOrderAudio; //audio clip played when the unit is ordered to construct a builid.

	public GameObject BuilderObj; //The object that will be activated when the player starts building.

	void Awake () {
		//get he unit mvt script:
		UnitMvt = gameObject.GetComponent<Unit> ();
	}
	
	void Update () {

		//If the player has a target building, then send him there:
		if (TargetBuilding != null) {
			if (TargetBuilding.Health >= TargetBuilding.MaxHealth) { //If the target building reached the maximum health:
				//stop building
				UnitMvt.StopMvt ();
				UnitMvt.CancelBuilding ();
			} else {	
				//If the unit is in range of the buidling to construct:
				if (UnitMvt.DestinationReached == true) {
					if (IsBuilding == false) {
						//Stop moving:
						UnitMvt.StopMvt ();

						//Start building:
						IsBuilding = true;

						//Inform the animator that we started building:
						UnitMvt.SetAnimState (Unit.UnitAnimState.Building);

						//Play the construction audio clips:
						if (BuildingAudio.Length > 0) {
							int AudioID = Random.Range (0, BuildingAudio.Length - 1);
							AudioManager.PlayAudio (gameObject, BuildingAudio [AudioID], true);
						}

						//activate the builder object: 
						if (BuilderObj != null) {
							BuilderObj.SetActive (true);
						}

						//custom event:
						if (UnitMvt.GameMgr.Events)
							UnitMvt.GameMgr.Events.OnUnitStartBuilding (UnitMvt, TargetBuilding);

						Timer = 1.0f;
					}
				}
				else if(UnitMvt.Moving == false) 
				{
					//Bring the unit back:
					UnitMvt.CheckUnitPathLocal (Vector3.zero, TargetBuilding.gameObject, UnitMvt.GameMgr.MvtStoppingDistance, -1);
					print (UnitMvt.FactionID);
				}

				//Adding health to the building:
				if (IsBuilding == true) {
					//if it's a MP game:
					if (GameManager.MultiplayerGame == true) {
						/*//only build if every client is on track: 
						if (UnitMvt.GameMgr.Factions [GameManager.PlayerFactionID].MFactionMgr.CanSendInput == false) {
							print ("can't");
						}*/
					}

					//building timer:
					if (UnitMvt.Moving == false) {
						if (Timer > 0) {
							Timer -= Time.deltaTime;
						}
						if (Timer <= 0) {
							Timer = 1.0f;
							TargetBuilding.AddHealth (HealthPerSecond, null); //adding health points each second
						}
					} else {
						//if the unit has moved while building.
						IsBuilding = false;
						//move the unit back to its target building
						if (TargetBuilding)
							UnitMvt.CheckUnitPathLocal (Vector3.zero, TargetBuilding.gameObject, UnitMvt.GameMgr.MvtStoppingDistance, -1);
					}
				}
			}
		} else {
			if (IsBuilding == true) {
				UnitMvt.StopMvt ();
				UnitMvt.CancelBuilding ();
			}

			if (AutoBuild == true && UnitMvt.IsIdle() == true) { //if the unit can build automatically and is not doing any other task
				if (GameManager.PlayerFactionID == UnitMvt.FactionID) { //if this is the local player in a MP game or if this is simply an offline game
					if (SearchTimer > 0) {
						SearchTimer -= Time.deltaTime;
					} else {
						//search for units 
						int i = 0;
						while (TargetBuilding == null && i < UnitMvt.FactionMgr.Buildings.Count) {//loop through the faction's buildings
							if (Vector3.Distance (UnitMvt.FactionMgr.Buildings [i].transform.position, transform.position) < SearchRange) {
								//if the building does not have full health
								if (UnitMvt.FactionMgr.Buildings [i].Health < UnitMvt.FactionMgr.Buildings [i].MaxHealth) {
									SetTargetBuilding(UnitMvt.FactionMgr.Buildings [i]); //target found.
								}
							}
							i++;
						}
						SearchTimer = SearchReload; //reload the search timer.
					}
				}
			}
		}
	
	}

	//Set the building's that the unit will construct:
	public void SetTargetBuilding (Building Target)
	{
		//if it's as single player game.
		if (GameManager.MultiplayerGame == false) {
			//directly send the unit to build
			SetTargetBuildingLocal (Target);
		} else {
			//in a case of a MP game
			//and it's the unit belongs to the local player:
			if (GameManager.PlayerFactionID == UnitMvt.FactionID) {
				//ask the server to tell all clients at once that this unit is going to construct a building.

				MFactionManager.InputActionsVars NewInputAction = new MFactionManager.InputActionsVars ();
				NewInputAction.Source = UnitMvt.netId;

				NewInputAction.Target = Target.netId;

				NewInputAction.InitialPos = transform.position;
				NewInputAction.TargetPos = Target.transform.position;

				UnitMvt.GameMgr.Factions [UnitMvt.FactionID].MFactionMgr.InputActions.Add (NewInputAction);
			}
		}
	}

	//Set the building's that the unit will construct:
	public void SetTargetBuildingLocal (Building Target)
	{
		if (Target == null || TargetBuilding == Target)
			return;

		//Check first if the building needs construction by cheking if its current health is below the max health:
		if (Target.Health < Target.MaxHealth && Target.CurrentBuilders.Count < Target.MaxBuilders) {
			UnitMvt.CancelBuilding (); //stop constructing the current building

			IsBuilding = false;

			Target.CurrentBuilders.Add (this);

			TargetBuilding = Target;

			//Move the unit to the building:
			UnitMvt.CheckUnitPathLocal(Vector3.zero, TargetBuilding.gameObject,UnitMvt.GameMgr.MvtStoppingDistance, -1);

			/*UnitMvt.NavAgent.avoidancePriority = GameManager.Instance.UnitMgr.MinArmyPriority + Target.CurrentBuilders.Count - 1;
			if (UnitMvt.NavAgent.avoidancePriority > 99) {
				UnitMvt.NavAgent.avoidancePriority = 99;
			}*/
		}
	}
}