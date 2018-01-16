using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackBuilding : MonoBehaviour {

	public bool DirectAttack = false; //If set to true, the building will affect damage when in range with the target, if not the damage will be affected within an object released by the unit (like a particle effect).

	public float AttackReload = 1.0f; //How long one single attack lasts.
	float AttackTimer;

	public bool AttackAllTypes = true;
	public string AttackCategories; //enter here the names the categories that the faction can attack, categories must be seperated with a comma.
	[HideInInspector]
	public List<string> AttackCategoriesList = new List<string>();

	//Distance interval between the enemy and its target unit to launch an attack.
	public float MinAttackDistance = 2.5f; //Make sure it's not 0.0f because it will lead to weird behaviour!
	public float MaxAttackDistance = 4.0f;

	public float SearchReload = 1.0f; //Search for enemy units every ..
	float SearchTimer;

	//Target:
	[HideInInspector]
	public GameObject Target; //the target (unit or building) that the building is attacking.

	//Attack type:
	public float Damage = 10.0f; //damage points when this building attacks another unit.

	public bool UseAttackObj = false; //If set to true, the building will launch an object (that can be something like a particle effect) to attack the enemy unit.
	public GameObject AttackObj; //attack object prefab.
	public float AttackObjDestroyTime = 3.0f; //life duration of the attack object
	public Transform AttackObjSource; //Where will the attack object be sent from?
	public GameObject WeaponObj; //When assigned, this object will be rotated depending on the target's position.
	[HideInInspector]
	public Vector3 MvtVector; //The attack object movement direction
	public float AttackObjSpeed = 10.0f; //how fast is the attack object moving
	public bool DamageOnce = true; //should the attack object do damage once it hits a building/unit and then do no more damage.
	public bool DestroyAttackObjOnDamage = true; //should the attack object get destroyed after it has caused damage.

	//Movement:
	Building BuildingMgr;
	GameManager GameMgr;

	//Other scripts:
	AttackObjsPooling AttackObjsPool;

	//Audio:
	public AudioClip AttackSound; //played each time the building attacks.

	void Start () {
		//get the ame manager script
		BuildingMgr = GetComponent<Building>();
		GameMgr = GameManager.Instance;

		//default values for the timers:
		AttackTimer = 0.0f;

		AttackObjsPool = GameMgr.AttackObjsPool;

		if (AttackAllTypes == false) { //if it can not attack all the units
			UnitManager.Instance.AssignAttackCategories (AttackCategories, ref AttackCategoriesList);
		}

	}

	void Update () {
		if (BuildingMgr.IsBuilt == true) //If the building still have health points:
		{
			if(GameMgr.PeaceTime <= 0) //if we're not in the peace time
			{
				if (Target == null) { //If the unit's target is still not defined.

					//if it's a single player game or a multiplayer game and the building belongs to the local player:
					if (GameManager.MultiplayerGame == false || (GameManager.MultiplayerGame == true && GameManager.PlayerFactionID == BuildingMgr.FactionID)) {
						if (SearchTimer > 0) {
							//search timer
							SearchTimer -= Time.deltaTime;
						} else {

							//Search if there are enemy units in range:
							bool Found = false;
							float Distance = 0.0f;
							GameObject TempTarget = null;
						
							Collider[] ObjsInRange = Physics.OverlapSphere (transform.position, MaxAttackDistance);
							for (int i = 0; i < ObjsInRange.Length; i++) {

								Unit UnitInRange = ObjsInRange [i].gameObject.GetComponent<Unit> ();
								if (UnitInRange) { //If it's a unit object 
									//If this unit and the target have different teams and make sure it's not dead.
									if (UnitInRange.FactionID != BuildingMgr.FactionID && UnitInRange.Dead == false) {
										if (UnitInRange.IsInvisible == false) {
											if (Target == null || Distance > Vector3.Distance (ObjsInRange [i].transform.position, transform.position)) {
												//check if the unit can actually attack the target:
												if (AttackAllTypes == true || UnitManager.Instance.CanAttackTarget (ObjsInRange [i].gameObject, AttackCategoriesList)) { //if the unit can attack the target.
													//Set this unit as the target 
													TempTarget = ObjsInRange [i].gameObject;
													Found = true;

													Distance = Vector3.Distance (ObjsInRange [i].transform.position, transform.position);
												}
											
											}
										}
									}
								}

							}

							if (Found == true) { //if enemies are found
								if (TempTarget != null) {
									LaunchAttack (TempTarget);
								}
							}

							SearchTimer = SearchReload; //No enemy units found? search again.
						}
					}

				} else {
					//if it's a MP game:
					if (GameManager.MultiplayerGame == true) {
						//only launch attacks if every client is on track: 
						if (GameMgr.Factions [GameManager.PlayerFactionID].MFactionMgr.CanSendInput == false) {
							return;
						}
					}

					//Checking whether the target is dead or not (if it's a unit or a building.
					bool Dead = false;
					float MaxDistance = 0.0f; //The distance at which the unit must stand to launch the attack

					if (Target.GetComponent<Unit> ()) {
						Dead = Target.GetComponent<Unit> ().Dead;
						MaxDistance = MaxAttackDistance + Target.GetComponent<Unit>().NavAgent.radius;
					}

					if (Dead == true) {
						//if the target unit is dead, cancel everything
						Target = null;
					} else {
						//Face the target:
						//transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(Target.transform.position), Time.deltaTime);
						//If the current building has a target,
						if (Vector3.Distance (this.transform.position, Target.transform.position) > MaxDistance) { //This means that the target has left the follow range of the building.
								Target = null;
							//This building doesn't have a target anymore.
						} else {

							if (Vector3.Distance (this.transform.position, Target.transform.position) <= MaxDistance) {
								//Attack the target unit:
									if (AttackTimer <= 0) {
										//Play the attack audio:
										AudioManager.PlayAudio (gameObject, AttackSound, false);

										//Launch the attack:
										if (DirectAttack == true) {
											if (Target.GetComponent<Unit> ()) {
												Target.GetComponent<Unit> ().AddHealth (-Damage, this.gameObject);
											}
										}

										if (UseAttackObj == true && AttackObj != null) { //If the unit can launch attack objs towards the target unit
											GameObject NextAttackObj = null;

											//If there's an attack objects pool:
											if (AttackObjsPool != null) {
												//Then search if there's an attack object of the same type available to be re-used:
												if (AttackObjsPool.GetFreeAttackObject (AttackObj.gameObject.GetComponent<AttackObject> ().Code) != null) {
													NextAttackObj = AttackObjsPool.GetFreeAttackObject (AttackObj.gameObject.GetComponent<AttackObject> ().Code).gameObject;
												}
											}

											//If we found a free attack object we can use:
											if (NextAttackObj != null) {
												NextAttackObj.transform.position = AttackObjSource.transform.position; //Set the attack object's position:
												NextAttackObj.gameObject.SetActive (true); //Activate the attack object
											}

											//If no free attack object was found in the pool, then...
											else {
												//Create a new one:
												NextAttackObj = (GameObject)Instantiate (AttackObj, AttackObjSource.transform.position, AttackObj.transform.localRotation); //Spawn the attack object:
												//Add it to the pool:
												if (NextAttackObj != null)
													AttackObjsPool.AttackObjects.Add (NextAttackObj.gameObject.GetComponent<AttackObject> ());
											}

											NextAttackObj.GetComponent<AttackObject> ().DidDamage = false;
											NextAttackObj.GetComponent<AttackObject> ().Source = gameObject;
											NextAttackObj.GetComponent<AttackObject> ().DoDamage = !DirectAttack;

											if (DirectAttack == false) { //If it's not a direct attack, we'll add a component to the attack object that will handle doing the damage.
												//Attack object settings:

											NextAttackObj.GetComponent<AttackObject> ().UnitDamage = Damage;
												NextAttackObj.GetComponent<AttackObject> ().BuildingDamage = 0.0f;

												if (Target.GetComponent<Unit> ()) {
													NextAttackObj.GetComponent<AttackObject> ().TargetFactionID = Target.GetComponent<Unit> ().FactionID;
												} else if (Target.GetComponent<Building> ()) {
													NextAttackObj.GetComponent<AttackObject> ().TargetFactionID = Target.GetComponent<Building> ().FactionID;
												}

												NextAttackObj.GetComponent<AttackObject> ().DamageOnce = DamageOnce;
												NextAttackObj.GetComponent<AttackObject> ().DestroyOnDamage = DestroyAttackObjOnDamage;
											}

											//Attack object movement:
										NextAttackObj.GetComponent<AttackObject> ().MvtVector = (Target.transform.position - AttackObjSource.transform.position) / Vector3.Distance (Target.transform.position, AttackObjSource.transform.position);
											NextAttackObj.GetComponent<AttackObject> ().Speed = AttackObjSpeed;

											//Set the attack obj's rotation so that it looks at the target:
											NextAttackObj.transform.rotation = Quaternion.LookRotation (Target.transform.position - NextAttackObj.transform.position);
										if (WeaponObj != null) {
											WeaponObj.transform.rotation = Quaternion.LookRotation (Target.transform.position - WeaponObj.transform.position);
										}
											//Make the attack object look at the target:

											//Hide the attack object after some time:
											NextAttackObj.GetComponent<AttackObject> ().DestroyTimer = AttackObjDestroyTime;
											NextAttackObj.GetComponent<AttackObject> ().ShowAttackObjEffect ();

										} 
										//Reload the attack timer:
										AttackTimer = AttackReload;
									}

									//Attack timer:
									if (AttackTimer > 0) {
										AttackTimer -= Time.deltaTime;
									}
								}
						}
					}
				}
			}
		}
	}

	//attack building functions:
	public void LaunchAttack (GameObject TargetUnit)
	{
		if (GameManager.MultiplayerGame == false) { //If this a single player game
			LaunchAttackLocal (TargetUnit); //instantly launch the attack.
		} else { //if it is a multiplayer game:
			//first check if this building belongs to the local player:
			if (BuildingMgr.FactionID == GameManager.PlayerFactionID) {

				//send input action to the MP faction manager if it's a MP game:
				MFactionManager.InputActionsVars NewInputAction = new MFactionManager.InputActionsVars ();
				NewInputAction.Source = gameObject.GetComponent<Building> ().netId;
				NewInputAction.InitialPos = transform.position;
				NewInputAction.Target = TargetUnit.GetComponent<Unit>().netId;
				GameMgr.Factions [BuildingMgr.FactionID].MFactionMgr.InputActions.Add (NewInputAction);

			}
		}
	}

	//attacking the target:
	public void LaunchAttackLocal (GameObject TargetUnit)
	{
		Target = TargetUnit.gameObject;
	}
}

