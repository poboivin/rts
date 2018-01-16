using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

/* Attack script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

public class Attack : MonoBehaviour {

	public Sprite AttackIcon; //an icon to represent this attack type in the task panel.

	public bool DirectAttack = false; //If set to true, the unit will affect damage when in range with the target, if not the damage will be affected within an object released by the unit (like a particle effect).
	public bool MoveOnAttack = false; //Is the unit allowed to move while attacking?

	//Attack type:
	public bool AttackAllTypes = true;
	public string AttackCategories; //enter here the names the categories that the faction can attack, categories must be seperated with a comma.
	//[HideInInspector]
	public List<string> AttackCategoriesList = new List<string>();

	public float AttackReload = 2.0f; //Time between two successive attacks
	float AttackTimer;

	//Distance interval between the unit and its target unit to launch an attack.
	public float MinUnitStoppingDistance = 2.5f; //Make sure it's not 0.0f because it will lead to weird behaviour!
	public float MaxUnitStoppingDistance = 4.0f;

	//How far the unit must stand from the building to launch an attack?
	public float MinBuildingStoppingDistance = 0.5f; //Make sure it's not 0.0f because it will lead to weird behaviour!
	public float MaxBuildingStoppingDistance = 2.0f;
	[HideInInspector]
	public float LastBuildingDistance;
	//Attack range:
	public bool AttackOnAssign = true; //can attack when the player assigns a target?
	public bool AttackWhenAttacked = false; //is the unit allowed to defend itself when attacked? 
	public bool AttackInRange = false; //when an enemy unit enter in range of this unit, can the unit attack it automatically?
	public float AttackRange = 10.0f;

	//AI related settings;
	[HideInInspector]
	public Building AttackRangeCenter; //which building center the units should protect?
	[HideInInspector]
	public bool AttackRangeFromCenter = false; //should the unit protect the building center:

	public float SearchReload = 1.0f; //Search for enemy units every ..
	float SearchTimer;
	//Target:
	public bool RequireAttackTarget = true; //if set to false then the player can attack anywhere in the map (suits areal attacks).
	[HideInInspector]
	public Vector3 AttackPosition;

	[HideInInspector]
	public GameObject AttackTarget; //the target (unit or building) that the unit is attacking.
	[HideInInspector]
	public bool Attacking = false;
	[HideInInspector]
	public bool TargetAssigned = false; //false when target has been chosen automatically by the unit and true if the player chosen the target.
	public float FollowRange = 15.0f; //If the target leaves this range then the unit will stop following/attacking it.
	bool WasInTargetRange = false;

	//Attack type:
	public float UnitDamage = 10.0f; //damage points when this unit attacks another unit.

	public float BuildingDamage = 10.0f; //damage points when this unit attacks a building.

	//Area attack:
	public bool AreaDamage = false;
	[System.Serializable]
	public class AttackRangesVars
	{
		public float Range = 10.0f;
		public float UnitDamage = 5.0f;
		public float BuildingDamage = 4.0f;
	}
	public AttackRangesVars[] AttackRanges;

	public enum AttackTypes {Random, InOrder}; //in the case of having a lot of attack sources, there are two mods, the first is to choose  and the second is attacking from all sources in order
	public AttackTypes AttackType = AttackTypes.Random & AttackTypes.InOrder;
	[System.Serializable]
	public class AttackSourceVars
	{
		public float AttackDelay = 0.2f;
		public GameObject AttackObj; //attack object prefab.
		public float AttackObjDestroyTime = 3.0f; //life duration of the attack object
		public Transform AttackObjSource; //Where will the attack object be sent from?
		public GameObject WeaponObj; //When assigned, this object will be rotated depending on the target's position.
		public bool FreezeRotX = false;
		public bool FreezeRotY = false;
		public bool FreezeRotZ = false;
		public float AttackObjSpeed = 10.0f; //how fast is the attack object moving
		public bool DamageOnce = true; //should the attack object do damage once it hits a building/unit and then do no more damage.
		public bool DestroyAttackObjOnDamage = true; //should the attack object get destroyed after it has caused damage.
	}
	public AttackSourceVars[] AttackSources;
	[HideInInspector]
	public Vector3 MvtVector; //The attack object movement direction
	public float AttackStepTimer;
	public int AttackStep;

	//Attacking anim timer:
	public float AttackAnimTime = 0.2f; //Must be lower than the duration of the attacking animation.
	[HideInInspector]
	public float AttackAnimTimer;

	//Movement:
	[HideInInspector]
	public Unit UnitMvt;

	Building BuildingMgr;
	GameManager GameMgr;

	//Other scripts:
	AttackObjsPooling AttackObjsPool;

	//Army Unit ID:
	[HideInInspector]
	public int ArmyUnitID = -1;
     
	//Audio:
	public AudioClip AttackOrderSound; //played when the unit is ordered to attack
	public AudioClip AttackSound; //played each time the unit attacks.

	void Awake ()
	{
		UnitMvt = gameObject.GetComponent<Unit> ();
	}

	void Start () {
		//get the ame manager script
		GameMgr = FindObjectOfType (typeof(GameManager)) as GameManager;

		//default values for the timers:
		AttackTimer = 0.0f;
		SearchTimer = 0.0f;
	
		AttackObjsPool = FindObjectOfType (typeof(AttackObjsPooling)) as AttackObjsPooling; //attack object pooling

		if (UnitMvt.NavAgent != null) {
			//If the min unit/building attacking distance is smaller than the nav mesh agent diameter then it would be impossible for the unit to reach its target, so we set it to the minimal possible value:
			if (MinUnitStoppingDistance < UnitMvt.NavAgent.radius * 2) {
				MinUnitStoppingDistance = UnitMvt.NavAgent.radius * 2;
			}
			if (MinBuildingStoppingDistance < UnitMvt.NavAgent.radius * 2) {
				MinBuildingStoppingDistance = UnitMvt.NavAgent.radius * 2;
			}
		}

		if (AttackAllTypes == false) { //if it can not attack all the units
			UnitManager.Instance.AssignAttackCategories (AttackCategories, ref AttackCategoriesList);
		}	
	}
	
	void Update () {
		//the attack animation timer:
		if (AttackAnimTimer > 0) {
			AttackAnimTimer -= Time.deltaTime;
		}
		if (AttackAnimTimer < 0) {
			AttackAnimTimer = 0;
			//stop showing the attacking animation when the timer is done.
			if (UnitMvt.AnimMgr) {
				if (UnitMvt.AnimMgr.GetBool ("IsAttacking") == true) {
					UnitMvt.AnimMgr.SetBool ("IsAttacking", false);
				}
			}
		}

		if (UnitMvt.Dead == false) //If the unit is still not dead
		{
			if(GameMgr.PeaceTime <= 0) //if we're not in the peace time
			{
				if (GameManager.MultiplayerGame == false || (GameManager.MultiplayerGame == true && GameManager.PlayerFactionID == UnitMvt.FactionID)) { //if this is an offline game or online but this is the local player
					if(AttackInRange == true && UnitMvt.Moving == false)
					{
						//if the faction is a NPC or a local player and having a target is required.
						if(GameManager.PlayerFactionID != UnitMvt.FactionID || (GameManager.PlayerFactionID == UnitMvt.FactionID && RequireAttackTarget == true))
						{
							if (AttackTarget == null) { //If the unit's target is still not defined. 
								if (SearchTimer > 0) {
									//search timer
									SearchTimer -= Time.deltaTime;
								} else {

									//Search if there are enemy units in range:
									bool Found = false;
									float Distance = 0.0f;

									float Size = FollowRange;
									Vector3 SearchFrom = transform.position;

									//only for NPC factions:

									//if there's no city center to protect:
									if (AttackRangeCenter == null) {
										AttackRangeFromCenter = false; //we're not defending any city center then:
									}
									//if there's a city center to protect
									if (AttackRangeFromCenter == true && AttackRangeCenter != null) {
										SearchFrom = AttackRangeCenter.transform.position; //the search pos is the city center
										Size = AttackRangeCenter.GetComponent<Border> ().Size; //and the search size is the whole city border size:
									}

									Collider[] ObjsInRange = Physics.OverlapSphere (SearchFrom, Size);
									for (int i = 0; i < ObjsInRange.Length; i++) {

										Unit UnitInRange = ObjsInRange [i].gameObject.GetComponent<Unit> ();
										if (UnitInRange) { //If it's a unit object 
											//If this unit and the target have different teams and make sure it's not dead.
											if (UnitInRange.FactionID != UnitMvt.FactionID && UnitInRange.Dead == false) {
												//if the unit is visible:
												if (UnitInRange.IsInvisible == false) {
													if (AttackTarget == null || Distance > Vector3.Distance (ObjsInRange [i].transform.position, SearchFrom)) {
														if (AttackAllTypes == true || UnitManager.Instance.CanAttackTarget (ObjsInRange [i].gameObject, AttackCategoriesList)) { //if the unit can attack the target.
															//Set this unit as the target 
															SetAttackTarget (ObjsInRange [i].gameObject);
															Found = true;

															Distance = Vector3.Distance (ObjsInRange [i].transform.position, SearchFrom);
														}
													}
												}
											}
										}

									}

									if (Found == false) {
										SearchTimer = SearchReload; //No enemy units found? search again.
									} else {
										TargetAssigned = false;

										//Follow the taraget:
										UnitMvt.CheckUnitPath (Vector3.zero, AttackTarget, MaxUnitStoppingDistance + AttackTarget.GetComponent<Unit> ().NavAgent.radius + UnitMvt.NavAgent.radius, 0, true);
									}

								}
							}
						}

					}
				}
				if (AttackTarget != null) {
					//if the target went invisible:
					//Checking whether the target is dead or not (if it's a unit or a building.
					bool Dead = false;
					float MaxDistance = UnitMvt.NavAgent.radius; //The distance at which the unit must stand to launch the attack

					if (AttackTarget.GetComponent<Unit> ()) { 
						//if the target went invisible:
						if (AttackTarget.GetComponent<Unit> ().IsInvisible == true) {
							//stop attacking it:
							UnitMvt.CancelAttack ();
							return;
						} else if (AttackTarget.GetComponent<Unit> ().FactionID == UnitMvt.FactionID) {
							UnitMvt.CancelAttack ();
							return;
						}
						Dead = AttackTarget.GetComponent<Unit> ().Dead;
						MaxDistance += MaxUnitStoppingDistance + AttackTarget.GetComponent<Unit> ().NavAgent.radius;
					} else if (AttackTarget.GetComponent<Building> ()) {
						if (AttackTarget.GetComponent<Building> ().FactionID == UnitMvt.FactionID) {
							UnitMvt.CancelAttack ();
							return;
						}
						//Dead = !(Target.GetComponent<Building> ().Health > 0);
						MaxDistance += MaxBuildingStoppingDistance + LastBuildingDistance;
					}
										
					if (Dead == true) {
						//if the target unit is dead, cancel everything
						AttackTarget = null;
						UnitMvt.CancelAttack ();
					} else {
						//Face the target:
						//transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(Target.transform.position), Time.deltaTime);
						//If the current unit has a target,
						if (Vector3.Distance (this.transform.position, AttackTarget.transform.position) > FollowRange && AttackRangeFromCenter == false && AttackTarget.GetComponent<Unit> () && WasInTargetRange == true) { //This means that the target has left the follow range of the unit.
							AttackTarget = null;
							if (UnitMvt.Moving == true)
								UnitMvt.StopMvt ();
							//This unit doesn't have a target anymore.
						} else {
							if (UnitMvt.DestinationReached == false && UnitMvt.Moving == false) { //If the unit didn't reach its target and it looks like it's not moving:
								//Follow the target:
								UnitMvt.CheckUnitPath (Vector3.zero, AttackTarget, MaxDistance, 0, true);
							}
								
							//if the attacker is in the correct range of his target
							if (UnitMvt.DestinationReached == true) {

								if (UnitMvt.Moving == true) {
									UnitMvt.StopMvt ();
								}
								WasInTargetRange = true;
								
								//Attack the target unit:
								if (MoveOnAttack == true || (MoveOnAttack == false && UnitMvt.Moving == false)) { //if we can move while attacking or we're not moving:
									if (AttackTimer <= 0) { //if the attack timer is ready:

										if (UnitMvt.AnimMgr) {
											if (UnitMvt.AnimMgr.GetBool ("IsAttacking") == false) {
												UnitMvt.AnimMgr.SetBool ("IsAttacking", true);
											}
										}

										//Launch the attack:
										if (DirectAttack == true) {
											if (AreaDamage == false) { //if this is no areal damage.
												if (AttackTarget.GetComponent<Unit> ()) {
													AttackTarget.GetComponent<Unit> ().AddHealth (-UnitDamage, this.gameObject);
												} else if (AttackTarget.GetComponent<Building> ()) {
													AttackTarget.GetComponent<Building> ().AddHealth (-BuildingDamage, this.gameObject);
												}
											} else {
												LaunchAreaDamage (AttackTarget.transform.position);			
											}

											//Play the attack audio:
											AudioManager.PlayAudio (gameObject, AttackSound, false);
											UnitMvt.SetAnimState (Unit.UnitAnimState.Attacking);

											AttackTimer = AttackReload;
											AttackAnimTimer = AttackAnimTime;
										} else { //If the unit can launch attack objs towards the target unit
											if (AttackStep < AttackSources.Length && AttackSources.Length > 0) { //if we haven't already launched attacks from all sources

												if (AttackStepTimer > 0) {
													AttackStepTimer -= Time.deltaTime;
												}
												if (AttackStepTimer <= 0) {
													AttackTimer = AttackReload;
													AttackAnimTimer = AttackAnimTime;

													GameObject NextAttackObj = null;

													//If there's an attack objects pool:
													if (AttackObjsPool != null) {
														//Then search if there's an attack object of the same type available to be re-used:
														if (AttackObjsPool.GetFreeAttackObject (AttackSources [AttackStep].AttackObj.gameObject.GetComponent<AttackObject> ().Code) != null) {
															NextAttackObj = AttackObjsPool.GetFreeAttackObject (AttackSources [AttackStep].AttackObj.gameObject.GetComponent<AttackObject> ().Code).gameObject;
														}
													}

													//If we found a free attack object we can use:
													if (NextAttackObj != null) {
														NextAttackObj.transform.position = AttackSources [AttackStep].AttackObjSource.transform.position; //Set the attack object's position:
														NextAttackObj.gameObject.SetActive (true); //Activate the attack object
													}

													//If no free attack object was found in the pool, then...
													else {
														//Create a new one:
														NextAttackObj = (GameObject)Instantiate (AttackSources [AttackStep].AttackObj, AttackSources [AttackStep].AttackObjSource.transform.position, AttackSources [AttackStep].AttackObj.transform.localRotation); //Spawn the attack object:
														//Add it to the pool:
														if (NextAttackObj != null)
															AttackObjsPool.AttackObjects.Add (NextAttackObj.gameObject.GetComponent<AttackObject> ());
													}
														

													//Attack object settings:

													NextAttackObj.GetComponent<AttackObject> ().UnitDamage = UnitDamage;
													NextAttackObj.GetComponent<AttackObject> ().BuildingDamage = BuildingDamage;

													Vector3 TargetPos = AttackTarget.transform.position;

													if (AttackTarget.GetComponent<Unit> ()) {
														NextAttackObj.GetComponent<AttackObject> ().TargetFactionID = AttackTarget.GetComponent<Unit> ().FactionID;
														TargetPos = AttackTarget.GetComponent<Unit> ().PlayerSelection.transform.position;
													} else if (AttackTarget.GetComponent<Building> ()) {
														NextAttackObj.GetComponent<AttackObject> ().TargetFactionID = AttackTarget.GetComponent<Building> ().FactionID;
														TargetPos = AttackTarget.GetComponent<Building> ().PlayerSelection.transform.position;
													}

													NextAttackObj.GetComponent<AttackObject> ().DamageOnce = AttackSources [AttackStep].DamageOnce;
													NextAttackObj.GetComponent<AttackObject> ().DestroyOnDamage = AttackSources [AttackStep].DestroyAttackObjOnDamage;

													NextAttackObj.GetComponent<AttackObject> ().Source = gameObject;
													NextAttackObj.GetComponent<AttackObject> ().SourceFactionID = UnitMvt.FactionID;

													if (AreaDamage == false) {
														NextAttackObj.GetComponent<AttackObject> ().DidDamage = false;
														NextAttackObj.GetComponent<AttackObject> ().DoDamage = !DirectAttack;
														NextAttackObj.GetComponent<AttackObject> ().AreaDamage = false;
													} else {
														NextAttackObj.GetComponent<AttackObject> ().DamageOnce = true;
														NextAttackObj.GetComponent<AttackObject> ().DidDamage = false;
														NextAttackObj.GetComponent<AttackObject> ().AreaDamage = true;
														NextAttackObj.GetComponent<AttackObject> ().AttackRanges = AttackRanges;

													}

													//Attack object movement:
													NextAttackObj.GetComponent<AttackObject> ().MvtVector = (TargetPos - AttackSources [AttackStep].AttackObjSource.transform.position) / Vector3.Distance (AttackTarget.transform.position, AttackSources [AttackStep].AttackObjSource.transform.position);
													NextAttackObj.GetComponent<AttackObject> ().Speed = AttackSources [AttackStep].AttackObjSpeed;

													//Set the attack obj's rotation so that it looks at the target:
													NextAttackObj.transform.rotation = Quaternion.LookRotation (TargetPos - NextAttackObj.transform.position);

													//Make the attack object look at the target:
													if (AttackSources [AttackStep].WeaponObj != null) {
														Vector3 LookAt = AttackTarget.transform.position - AttackSources [AttackStep].WeaponObj.transform.position;
														if (AttackSources [AttackStep].FreezeRotX == true)
															LookAt.y = 0.0f;
														if (AttackSources [AttackStep].FreezeRotY == true)
															LookAt.y = 0.0f;
														if (AttackSources [AttackStep].FreezeRotZ == true)
															LookAt.z = 0.0f;
														AttackSources [AttackStep].WeaponObj.transform.rotation = Quaternion.LookRotation (LookAt);

													}

													//Hide the attack object after some time:
													NextAttackObj.GetComponent<AttackObject> ().DestroyTimer = AttackSources [AttackStep].AttackObjDestroyTime;
													NextAttackObj.GetComponent<AttackObject> ().ShowAttackObjEffect ();

													//Play the attack audio:
													AudioManager.PlayAudio (gameObject, AttackSound, false);

													//-----------------------------------------------------------------------------------------------

													//search for the next attack object:
													if (AttackType == AttackTypes.InOrder) { //if the attack types is in order
														AttackStep++;
													}

													if (AttackStep >= AttackSources.Length || AttackType == AttackTypes.Random) { //end of attack round:
														//Reload the attack timer:
														AttackStep = 0;
														AttackStepTimer = AttackSources [AttackStep].AttackDelay;
													} else {
														AttackStepTimer = AttackSources [AttackStep].AttackDelay;
													}



												} 
											}
										}
									}
									//Attack timer:
									if (AttackTimer > 0) {
										AttackTimer -= Time.deltaTime;
									}
								}
							}
						}
					}
				} else if (Attacking == true) {
					UnitMvt.CancelAttack ();
				}
			}
		}
	}

	//Set attack target:
	public void SetAttackTarget (GameObject Obj)
	{
		UnitMvt.DestinationReached = false;
		AttackTarget = Obj;

		if (DirectAttack == false) {
			//other settings here:
			if (AttackType == AttackTypes.Random) { //if the attack type is random
				AttackStep = Random.Range (0, AttackSources.Length); //pick a random source
			} else if (AttackType == AttackTypes.InOrder) { //if it's in order
				AttackStep = 0; //start with the first attack source:
			}
			
			AttackStepTimer = AttackSources [AttackStep].AttackDelay;
		}

		WasInTargetRange = false;

		/*if (AttackTarget.GetComponent<Unit> ()) { 
			UnitMvt.CheckUnitPath (Vector3.zero, AttackTarget, MaxUnitStoppingDistance, 0, true);
		} else if (AttackTarget.GetComponent<Building> ()) {
			UnitMvt.CheckUnitPath (Vector3.zero, AttackTarget, MaxBuildingStoppingDistance, 0, true);
		}*/
	}

	public void ResetAttack () //reset the values of the attack:
	{
		AttackStep = 0;
		AttackStepTimer = 0.0f;
		AttackTimer = 0.0f;
		AttackTarget = null;
		WasInTargetRange = false;
	}

	public void LaunchAreaDamage (Vector3 AreaCenter)
	{
		if (AttackRanges.Length > 0) {
			Collider[] ObjsInRange = Physics.OverlapSphere(AreaCenter, AttackRanges[AttackRanges.Length-1].Range);
			int i = 0;
			while (i < ObjsInRange.Length)
			{
				if (ObjsInRange [i].gameObject.GetComponent<SelectionObj> ()) {
					Unit Unit = ObjsInRange [i].gameObject.GetComponent<SelectionObj> ().MainObj.GetComponent<Unit> ();
					Building Building = ObjsInRange [i].gameObject.GetComponent<SelectionObj> ().MainObj.GetComponent<Building> ();

					if (Unit) {
						if (Unit.FactionID == UnitMvt.FactionID)
							Unit = null;
					}
					if (Building) {
						if (Building.FactionID == UnitMvt.FactionID)
							Building = null;
					}

					if (Unit != null || Building != null) {
						float Distance = Vector3.Distance (ObjsInRange [i].transform.position, AreaCenter);
						int j = 0;
						bool Found = false;
						while (j < AttackRanges.Length && Found == false) {
							if (Distance < AttackRanges [j].Range) {
								Found = true;
								if (Unit)
									Unit.AddHealth (AttackRanges [j].UnitDamage, this.gameObject);
								else if (Building)
									Building.AddHealth (AttackRanges [j].BuildingDamage, this.gameObject);
								
							}
							j++;
						}
					}
				}
				i++;
			}
		}
	}
}
