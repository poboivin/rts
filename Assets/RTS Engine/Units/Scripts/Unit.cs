using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.AI;

/* Unit script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

public class Unit : NetworkBehaviour {

	//Unit's info:
	public string Name; //the name of the unit that will be displayd when it is selected.
	public string Code; //unique code for each unit that is used to identify it in the system.
	public string Category; //the category that this unit belongs to.
	public string Description; //the description of the unit that will be displayed when it is selected.
	public Sprite Icon; //the icon that will be displayed when the unit is selected.

	public bool FreeUnit = false; //does this unit belong to no faction?
	public bool CanBeMoved = true; //does this unit move on orders from the player?

	[HideInInspector]
	public bool IsInvisible = false; //is the unit invisible?

	//health:
	public float MaxHealth = 100.0f; //maximum health points of the unit
	[HideInInspector]
	public float Health; //current health of the unit
	[HideInInspector]
	public float HealthBarYPos; //the height of the health bar that shows when the mouse is over the building
	[HideInInspector]
	public bool Dead = false; //is the unit dead?
	public float DestroyObjTime = 2.0f;

	//Damage effect:
	public GameObject DamageEffect; //Created when a damage is received in the contact point between the attack object and this one:

	public GameObject UnitPlane; //The plane where the selection texture appears.
	[HideInInspector]
	//this the timer during which the unit selection texture flahes when it is interacted with.
	public float FlashTime = 0.0f;
	//[HideInInspector]
	[SyncVar]
	public int FactionID = 0; //Unit's faction ID.

	[HideInInspector]
	[SyncVar]
	public NetworkInstanceId CreatedByID; //The building ID that created this building.
	public Building CreatedBy = null; //The building that produced the unit.

	[HideInInspector]
	public NavMeshAgent NavAgent; //Nav agent component attached to the unit's object.
	public NavMeshPath NavPath; //Nav path that the unit is currently following.
	public GameObject MvtTargetObj; //when the target destination is actually an object, it is saved here.
	public bool DestinationReached = false; //when the target his destination, this is set to true.
	public float Speed = 10.0f; //The unit's movement speed.
	public float RotationDamping = 2.0f; //How fast does the rotation updates?
	public bool CanRotate = false; //can the unit rotate? 
	[HideInInspector]
	public bool Moving = false; //Is the player currently moving?
	public float UnitHeight = 1.0f; // This will always be the position on the y axis for this unit.
	public bool FlyingUnit = false; //is the unit flying or walking on the normal terrain? 
	float MvtCheck; //timer to check whether the unit is moving towards target or not.
	Vector3 LastRegisteredPos; //saves the last player's position to compare it later and see if the unit has actually moved.

	public SelectionObj PlayerSelection; //Must be an object that only include this script, a trigger collider and a kinematic rigidbody.
	//the collider represents the boundaries of the object (building, resource or unit) that can be selected by the player.
	public SkinnedMeshRenderer[] FactionColorObjs; //The child objects of the unit prefab that will get the color of the faction (skinned mesh renderers)
	public MeshRenderer[] FactionColorObjs2; //The child objects of the unit prefab that will get the color of the faction (simple mesh renderers)

	//AI Faction manager
	[HideInInspector]
	public FactionManager FactionMgr;

	//Animations:
	[HideInInspector]
	public Animator AnimMgr; //the animator comp attached to the unit's object
	//public AnimatorOverrideController AnimOverrideController;
	public enum UnitAnimState {Idle, Building, Collecting, Moving, Attacking, Healing, Converting, TakeDamage, Dead} //the possible animations states:
	UnitAnimState CurrentAnimState = UnitAnimState.Idle & UnitAnimState.Building & UnitAnimState.Moving & UnitAnimState.Collecting & UnitAnimState.Attacking & UnitAnimState.Healing & UnitAnimState.TakeDamage & UnitAnimState.Dead;
	public AnimatorOverrideController AnimController; //the unit's main controller

	//APC:
	public APC TargetAPC;

	//Portal:
	public Portal TargetPortal;

	public AudioClip SelectionAudio; //Audio played when the unit has been selected.
	public AudioClip MvtOrderAudio; //Audio played when the unit is ordered to move.
	public AudioClip InvalidMvtPathAudio; //When the movement path is invalid, this audio is played.

	//NPC Unit Spawner:
	public int NPCUnitSpawnerID = -1;

	//components:
	[HideInInspector]
	public Builder BuilderMgr;
	[HideInInspector]
	public Attack AttackMgr;
	[HideInInspector]
	public MultipleAttacks MultipleAttacksMgr;
	[HideInInspector]
	public Healer HealMgr;
	[HideInInspector]
	public GatherResource ResourceMgr;
	[HideInInspector]
	public Converter ConvertMgr;
	[HideInInspector]
	public APC APCMgr;
	[HideInInspector]
	public Invisibility InvisibilityMgr;

	//Scripts:
	[HideInInspector]
	SelectionManager SelectionMgr;
	[HideInInspector]
	public UIManager UIMgr;
	[HideInInspector]
	public MFactionManager MFactionMgr;
	int InputMvtID = -1; //registers the last mvt input action ID made by this unit.
	[HideInInspector]
	public GameManager GameMgr;

	//Double Click:
	bool FirstClick = false;
	float DoubleClickTimer = 0;

	//mini map warning images:
	[HideInInspector]
	public bool HasWarningImage = false;

	void Awake () {

		GameMgr = GameManager.Instance;
		SelectionMgr = GameMgr.SelectionMgr;
		UIMgr = GameMgr.UIMgr;

		BuilderMgr = GetComponent <Builder> ();
		HealMgr = GetComponent<Healer> ();
		ConvertMgr = GetComponent<Converter> ();
		APCMgr = GetComponent<APC> ();
		ResourceMgr = GetComponent<GatherResource> ();
		AttackMgr = GetComponent<Attack> ();
		MultipleAttacksMgr = GetComponent<MultipleAttacks> ();
		InvisibilityMgr = GetComponent<Invisibility> ();

		//get the comps that the unit script needs:
		NavAgent = GetComponent<UnityEngine.AI.NavMeshAgent> ();
		if (NavAgent != null) {
			//Set the unit's speed:
			NavAgent.speed = Speed;
			//Set the unit's height:
			NavAgent.baseOffset = UnitHeight;

			if (FlyingUnit == true && SelectionMgr.AirTerrain != null) {
				NavAgent.baseOffset = SelectionMgr.AirTerrain.transform.position.y * 2;
			}
		}

		//Animations:
		if(AnimMgr == null)
		{
			AnimMgr = GetComponent<Animator> (); //Look if there's an animator component attached in the unit main object:
		}
		if (AnimMgr != null) {//If there is
			if(AnimController != null) AnimMgr.runtimeAnimatorController = AnimController;
			//Set the current animation state to idle because the unit just spawned!
			SetAnimState (UnitAnimState.Idle);
		}

		Moving = false; //mark as not moving when the unit spawns
		Dead = false; //obviously not dead when the unit just spawend.


		//if there's no unit selection texture, we'll let you know
		if (UnitPlane == null) {
			Debug.LogError ("You must attach a plane object at the bottom of the building and set it to 'UnitPlane' in the inspector.");
		} else {
			UnitPlane.SetActive (false); //hide the selection texture object when the unit just spawned.
		}

		Health = MaxHealth; //initial health:


		//In order for collision detection to work, we must assign these settings to the unit's collider and rigidbody
		GetComponent<Collider> ().isTrigger = false;
		if (GetComponent<Rigidbody> () == null) {
			gameObject.AddComponent<Rigidbody> ();
		}
		//unit's rigidbody settings:
		GetComponent<Rigidbody> ().isKinematic = true;
		GetComponent<Rigidbody> ().useGravity = false;

		TargetAPC = null;
		TargetPortal = null;

		if (FactionID == GameManager.PlayerFactionID) { //if this is the local player
			NPCUnitSpawnerID = -1; //then it does not have a NPC Unit Spawner component:
		}

		HasWarningImage = false;
	}

	void Start ()
	{
		if (LayerMask.NameToLayer ("SelectionPlane") > 0) { //if there's a layer for the selection plane
			UnitPlane.layer = LayerMask.NameToLayer ("SelectionPlane"); //assign this layer because we don't want the main camera showing it
		}

		//set the unit height:
		if (NavAgent != null) {
			//NavAgent.baseOffset = UnitHeight;

			NavAgent.angularSpeed = 999.0f; //we handle rotation in the code so no need to set it from the nav agent
			NavAgent.acceleration = 200.0f; //to avoid units sliding when reaching the destination, make sure this is set to a high value
			NavAgent.autoBraking = false;
		}

		//Set the selection object if we're using a different collider for player selection:
		if (PlayerSelection != null) {
			//set the player selection object for this building/resource:
			PlayerSelection.MainObj = this.gameObject;
		} else {
			Debug.LogError("Player selection collider is missing!");
		}

		if (FreeUnit == false) {
			SetUnitColors ();

			FactionMgr = GameMgr.Factions [FactionID].FactionMgr; //get the faction manager that this unit belongs to.

			//Add the newly created unit to the team manager list:
			FactionMgr.AddUnitToLists (this);

		} else {
			FactionID = -1;
		}

		//if it's a MP game:
		if (GameManager.MultiplayerGame == true) {
			int ID = FactionID;
			if (FreeUnit == true) { //if the unit is free (belongs to no faction), then the host will be responsible for moving it and controlling it.
				ID = 0;
			}
			MFactionMgr = GameMgr.Factions [ID].MFactionMgr; //Set the multiplayer faction manager
		}
			
		if (GameManager.PlayerFactionID == FactionID && CreatedBy != null && isServer == false) { //TBC ("isServer == false")
			//if the new unit does not have a task when spawned, send them to the goto position.
			CreatedBy.SendUnitToRallyPoint (this);
		}

		//call the custom event below:
		if (GameMgr.Events)
			GameMgr.Events.OnUnitCreated (this);
	}

	public void SetUnitColors ()
	{
		//Set the faction color objects:
		//If there's actually objects to color in this prefab:

		//for skinned mesh renderers
		if (FactionColorObjs.Length > 0) {
			//Loop through the faction color objects (the array is actually a MeshRenderer array because we want to allow only objects that include mesh renderers in this prefab):
			for (int i = 0; i < FactionColorObjs.Length; i++) {
				//Always checking if the object/material is not invalid:
				if (FactionColorObjs [i] != null) {
					//Color the object to the faction's color:
					FactionColorObjs [i].material.color = GameMgr.Factions [FactionID].FactionColor;
				}
			}
		}

		//for simple mesh renderers
		if (FactionColorObjs2.Length > 0) {
			//Loop through the faction color objects (the array is actually a MeshRenderer array because we want to allow only objects that include mesh renderers in this prefab):
			for (int i = 0; i < FactionColorObjs2.Length; i++) {
				//Always checking if the object/material is not invalid:
				if (FactionColorObjs2 [i] != null) {
					//Color the object to the faction's color:
					FactionColorObjs2 [i].material.color = GameMgr.Factions [FactionID].FactionColor;
				}
			}
		}
	}

	void Update () 
	{
		//Double click timer:
		if (DoubleClickTimer > 0) {
			DoubleClickTimer -= Time.deltaTime;
		} else {
			DoubleClickTimer = 0.0f;
			FirstClick = false;
		}
		//Selection flash timer:
		if (FlashTime > 0) {
			FlashTime -= Time.deltaTime;
		}
		if (FlashTime < 0) {
			FlashTime = 0.0f;
			CancelInvoke ("SelectionFlash");
			UnitPlane.gameObject.SetActive (false);
		}

		if (CanRotate == true && Moving == false) {
			//Unit rotation:
			Vector3 LookAt = NavAgent.destination - transform.position;

			//if the unit has an attack target
			if (AttackMgr != null) {
				if (AttackMgr.AttackTarget != null) {
					//make the unit look at its target
					LookAt = AttackMgr.AttackTarget.transform.position - transform.position;
				}
			}
			if (BuilderMgr != null) {
				//if the unit has a target building to construct
				if (BuilderMgr.TargetBuilding != null) {
					//make it look at that building
					LookAt = BuilderMgr.TargetBuilding.transform.position - transform.position;
				}
			}
			if (HealMgr != null) {
				//if the unit has a target unit to heal
				if (HealMgr.TargetUnit != null) {
					//make it look at that unit
					LookAt = HealMgr.TargetUnit.transform.position - transform.position;
				}
			}
			if (ConvertMgr != null) {
				//if the unit has a target unit to convert
				if (ConvertMgr.TargetUnit != null) {
					//make it look at that unit
					LookAt = ConvertMgr.TargetUnit.transform.position - transform.position;
				}
			}
			if (ResourceMgr != null) {
				//depending on the resource collector goal:
				if (ResourceMgr.TargetResource != null) {
					//make it look at the dropping off building or the resource:
					if (ResourceMgr.DroppingOff == false || (ResourceMgr.DroppingOff == true && ResourceMgr.DropOffBuilding == null)) {
						LookAt = ResourceMgr.TargetResource.transform.position - transform.position;
					} else {
						if (ResourceMgr.DropOffBuilding != null) {
							LookAt = ResourceMgr.DropOffBuilding.transform.position - transform.position;
						}
					}
				}
			}

			LookAt.y = 0;
			if (LookAt != Vector3.zero) {
				Quaternion NewRot = Quaternion.LookRotation (LookAt);
				transform.rotation = Quaternion.Slerp (transform.rotation, NewRot, Time.deltaTime * RotationDamping);
			} else {
				CanRotate = false;
			}
		}


		if (Moving == true && Dead == false) { //Is the unit currently moving?


			if (NavPath == null) {
				StopMvt ();
			} else {
				//movement check timer:
				if (MvtCheck > 0) {
					MvtCheck -= Time.deltaTime;
				}
				if (MvtCheck < 0) {
					if (transform.position == LastRegisteredPos) { //if the time passed and we still in the same position then stop the mvt.
						StopMvt ();
					} else {
						MvtCheck = 2.0f; //launch the timer again
						LastRegisteredPos = transform.position;
					}
				}
				/*if (AttackMgr != null && AttackMgr.AttackTarget != null) {
					DestinationReached = Vector3.Distance (transform.position, AttackMgr.AttackTarget.transform.position) <= NavAgent.stoppingDistance+NavAgent.radius;
				} else if (TargetAPC != null) {
					DestinationReached = Vector3.Distance (transform.position, TargetAPC.transform.position) <= TargetAPC.MaxDistance+NavAgent.radius;
				}  else if (TargetPortal != null) {
					DestinationReached = Vector3.Distance (transform.position, TargetPortal.transform.position) <= TargetPortal.MaxDistance+NavAgent.radius;
				} else {
					DestinationReached = Vector3.Distance (transform.position, NavAgent.destination) <= NavAgent.stoppingDistance+NavAgent.radius;
				}*/

				if (DestinationReached == false) {
					DestinationReached = Vector3.Distance (transform.position, NavAgent.destination) <= NavAgent.stoppingDistance + NavAgent.radius;
				}
			}
			//Cancel movement when the builder gets in the required range of the building to construct:

			if (DestinationReached == true) {
				if (TargetAPC != null) {
					if (TargetAPC.CurrentUnits.Count < TargetAPC.MaxAmount) {
						TargetAPC.AddUnit (this);
					}
				} else if (TargetPortal != null) {
					TargetPortal.Teleport (this);
				}
				StopMvt ();
			}

		}
	}

	//Flashing building selection (when the player sends units to contruct a building, its texture flashes for some time):
	public void SelectionFlash ()
	{
		UnitPlane.gameObject.SetActive (!UnitPlane.activeInHierarchy);
	}

	//method to select the unit:
	public void SelectUnit ()
	{
		//If the selection key is down, then we will add this unit to the current selection, if not we will deselect the selected units then select this one.
		//Make sure we are not clicking on a UI object:
		if (!EventSystem.current.IsPointerOverGameObject ()) {
			if (BuildingPlacement.IsBuilding == false) {

				if (FirstClick == true && FactionID == GameManager.PlayerFactionID) {
					SelectionMgr.SelectUnitsInRange (this);
				} else {
					FlashTime = 0.0f;
					CancelInvoke ("SelectionFlash");

					SelectionMgr.SelectUnit (this, SelectionMgr.SelectionKeyDown);

					if (SelectionMgr.SelectionKeyDown == false) {
						FirstClick = true;
						DoubleClickTimer = 0.5f;
					}
				}
			}
		}
	}

	//Showing the unit's health when the mouse hovers over it.
	void OnMouseOver ()
	{
		if (UIMgr != null) {
			UIMgr.UpdateHealthBar (this.gameObject);
		}
	}

	//Hide the health bar when the mouse leaves.
	void OnMouseExit ()
	{
		if (UIMgr != null) {
			UIMgr.HideHealthBar ();
		}
	}

	//a method that stops the player movement.
	public void StopMvt ()
	{
		Moving = false; //If the movement path is somehow unvalid, stop moving.

		if (gameObject.activeInHierarchy == true) {
			if(NavAgent != null) NavAgent.Stop (); //stop the nav agent comp

			//Inform the animator that the unit stopped moving:
			SetAnimState (UnitAnimState.Idle);
		}
		StopAllCoroutines ();

		TargetPortal = null;
		TargetAPC = null;
		MvtTargetObj = null;

		GetComponent<Collider> ().isTrigger = false;
	}

	public void CheckUnitPath (Vector3 TargetPos, GameObject TargetObj, float StoppingDistance, int Order, bool Send)
	{
		if (GameManager.MultiplayerGame == false || Send == false) {
			CheckUnitPathLocal (TargetPos, TargetObj, StoppingDistance, Order); //directly move the unit if it's a single player game
		} else {
			if(FactionID == GameManager.PlayerFactionID && Send == true)
			{
				//send input action to the MP faction manager if it's a MP game:
				MFactionManager.InputActionsVars NewInputAction = new MFactionManager.InputActionsVars ();
				NewInputAction.Source = netId;
				NewInputAction.InitialPos = transform.position;
				NewInputAction.TargetPos = TargetPos;
				NewInputAction.StoppingDistance = StoppingDistance;

				if (TargetObj != null) {
					NetworkInstanceId TargetNetID;
					TargetNetID = TargetObj.GetComponent<NetworkIdentity> ().netId;

					NewInputAction.Target = TargetNetID;
					NewInputAction.TargetPos = TargetObj.transform.position;
				}
				if (MFactionMgr.InputMvtUnits.Contains (this)) { //if the unit already registered a movement request in this same cycle
					//then remove the old one and register the new one:
					MFactionMgr.InputActions.RemoveAt(InputMvtID);
				} else {
					MFactionMgr.InputMvtUnits.Add (this);
				}
				InputMvtID = MFactionMgr.InputActions.Count;
				MFactionMgr.InputActions.Add (NewInputAction);
			}
		}
	}

	public void CheckUnitPathLocal (Vector3 TargetPos, GameObject TargetObj, float StoppingDistance, int Order)
	{
		UnityEngine.AI.NavMeshHit Hit;
		//Calculate the path that will get us to the target destination:
		NavPath = new UnityEngine.AI.NavMeshPath ();

		if (TargetObj != null) { //if there is a building:
			if (TargetObj.GetComponent<Building> ()) {
				//Then we check if the path is valid or invalid then decide to move the player or keep him at his position.
				if (UnityEngine.AI.NavMesh.SamplePosition (TargetObj.transform.position, out Hit, TargetObj.GetComponent<Building> ().MaxBuildingDistance, NavAgent.areaMask)) {
					TargetPos = Hit.position;
					if (TargetObj.GetComponent<Building> ().FactionID == FactionID) {
						StoppingDistance = GameMgr.MvtStoppingDistance;
					} else {
						if (AttackMgr) {
							AttackMgr.LastBuildingDistance = Vector3.Distance (Hit.position, TargetObj.transform.position);
						}
					}
					StoppingDistance += NavAgent.radius;
				}
			} else if (TargetObj.GetComponent<Resource> ()) { //if there's a resource to go to:
				if (FactionID == GameManager.PlayerFactionID)
					print (TargetObj.GetComponent<Resource> ().MaxDistance);
				if (UnityEngine.AI.NavMesh.SamplePosition (TargetObj.transform.position, out Hit, 10.0f, NavAgent.areaMask)) {
					TargetPos = Hit.position;
					if (FactionID == GameManager.PlayerFactionID)
						print (TargetPos);
					StoppingDistance = NavAgent.radius+GameMgr.MvtStoppingDistance;
				}
			} else if (TargetObj.GetComponent<Unit> ()) { //if the target object is a unit
				TargetPos = TargetObj.transform.position;
				if (TargetObj.GetComponent<Unit> ().FactionID == FactionID) {// if the unit belongs to the same faction
					if (TargetObj.GetComponent<APC> ()) {
						//APC
					} else {
						//healer
					}
				} else { //if we're  attacking an enemy unit:
					
				}
			}
		} 

		if (FactionID == GameManager.PlayerFactionID)
			print (TargetPos);
		NavAgent.CalculatePath (TargetPos, NavPath);

		OnUnitPathComplete (TargetObj, StoppingDistance, Order);

	}

	//This callback will inform us if there's a possible path to the target position:
	void OnUnitPathComplete (GameObject TargetObj, float StoppingDistance, int i)
	{
		bool ValidMvt = false; //check if the movement is valid:
		if (NavPath != null) { //if it's a nav mesh mvt:
			if (NavPath.status != UnityEngine.AI.NavMeshPathStatus.PathInvalid) {
				ValidMvt = true;
			}
		}
		if (ValidMvt == true) {

			MvtCheck = 2.0f;
			LastRegisteredPos = transform.position;

			NavAgent.Stop ();
			//TargetAPC = null;

			NavAgent.stoppingDistance = StoppingDistance; //default movement stopping distance.

			NavAgent.Resume ();
			NavAgent.SetPath (NavPath);

			GetComponent<Collider> ().isTrigger = true;

			if (TargetObj != null) {
				if (TargetObj.GetComponent<Building> ()) { //if the target object is a building
					if (TargetObj.GetComponent<Building> ().FactionID == FactionID) { //if it belongs to the same faction and the unit can build
						//then we're constructing it or dropping resources at it
						bool IsDroppingOff = false;
						if (ResourceMgr) {
							if (ResourceMgr.DroppingOff == true) {
								IsDroppingOff = true;
								TargetAPC = null; TargetPortal = null;
								ResourceMgr.GoingToDropOffBuilding = true; //marking the player as going to the drop off building
							}
						}
						if (IsDroppingOff == false)
							CancelCollecting ();

						CancelAttack ();
						CancelHealing ();
						CancelConverting ();

						bool Constructing = false;

						if (BuilderMgr) {
							if (TargetObj.GetComponent<Building> () == BuilderMgr.TargetBuilding) { //if the target building is the one the unit's going to construct not just go to
								Constructing = true;
								TargetAPC = null; TargetPortal = null;
								//If it's the local player:
								if (GameManager.PlayerFactionID == FactionID && i == 0) {
									//Play the building order audio:
									AudioManager.PlayAudio (GameMgr.GeneralAudioSource.gameObject, BuilderMgr.BuildingOrderAudio, false);
								}
							}
						}

						if (Constructing == false) {
							CancelBuilding ();
						}
					}
					else
					{
						if (TargetObj.GetComponent<Portal> ()) { //if the target building is a portal:
							CancelCollecting ();
							CancelBuilding ();
							CancelHealing ();
							CancelConverting ();
							TargetAPC = null;

							TargetPortal = TargetObj.GetComponent<Portal> ();
						}
						else if (AttackMgr != null) { //if it does not belong to the same faction and the unit can attack, then we're attacking it
							if (AttackMgr.AttackTarget == TargetObj) {
								CancelCollecting ();
								CancelBuilding ();
								CancelHealing ();
								CancelConverting ();
								TargetAPC = null;
								TargetPortal = null;

								//If it's the local player:
								if (GameManager.PlayerFactionID == FactionID && AttackMgr.TargetAssigned == true && i == 0) {
									//Play the attack order sound:
									AudioManager.PlayAudio (GameMgr.GeneralAudioSource.gameObject, AttackMgr.AttackOrderSound, false);
								}
							}
						}
					}
				} else if (ResourceMgr && TargetObj.GetComponent<Resource> ()) { //if the target object is a resource
					if (TargetObj.GetComponent<Resource> () == ResourceMgr.TargetResource) { //if the unit's collecting a resource
						CancelBuilding ();
						CancelHealing ();
						CancelAttack ();
						CancelConverting ();
						TargetAPC = null; TargetPortal = null;
					}
				} else if (TargetObj.GetComponent<Unit> ()) { //if the target object is a unit
					if (TargetObj.GetComponent<Unit> ().FactionID == FactionID) { //if the unit belongs to the same faction:
						if (TargetObj.GetComponent<APC> ()) { //APC
							//then move to the APC:
							TargetAPC = TargetObj.GetComponent<APC> ();
						} else if (HealMgr != null) { //else if the unit has a healer component
							if (HealMgr.TargetUnit.gameObject == TargetObj) {
								CancelCollecting ();
								CancelBuilding ();
								CancelAttack ();
								CancelConverting ();
								TargetAPC = null; TargetPortal = null;

								//If it's the local player:
								if (GameManager.PlayerFactionID == FactionID && i == 0) {
									//Play the healing order sound:
									AudioManager.PlayAudio (GameMgr.GeneralAudioSource.gameObject, HealMgr.HealOrderAudio, false);
								}
							}
						}
					} else {
						if (AttackMgr) { //if the unit does not belong to the same faction and the unit has an attack manager:
							if (AttackMgr.AttackTarget == TargetObj) {
								CancelCollecting ();
								CancelBuilding ();
								CancelHealing ();
								CancelConverting ();
								TargetAPC = null; TargetPortal = null;

								//If it's the local player:
								if (GameManager.PlayerFactionID == FactionID && AttackMgr.TargetAssigned == true && i == 0) {
									//Play the attack order sound:
									AudioManager.PlayAudio (GameMgr.GeneralAudioSource.gameObject, AttackMgr.AttackOrderSound, false);
								}
							}
						} else if(ConvertMgr) {
							if (ConvertMgr.TargetUnit != null && TargetObj != null) {
								if (ConvertMgr.TargetUnit.gameObject == TargetObj) {
									CancelCollecting ();
									CancelBuilding ();
									CancelHealing ();
									CancelAttack ();
									TargetAPC = null;
									TargetPortal = null;

									//If it's the local player:
									if (GameManager.PlayerFactionID == FactionID && i == 0) {
										//Play the converting order sound:
										AudioManager.PlayAudio (GameMgr.GeneralAudioSource.gameObject, ConvertMgr.ConvertOrderAudio, false);
									}

									//custom event:
									if (GameMgr.Events)
										GameMgr.Events.OnUnitStartConverting (this, ConvertMgr.TargetUnit);
								}
							}
						}
					}
				}
			}
			else {

				TargetAPC = null; TargetPortal = null;
				CancelBuilding();
				CancelHealing ();
				CancelAttack ();
				CancelCollecting ();
				CancelConverting ();

				//If it's the local player:
				if (GameManager.PlayerFactionID == FactionID && i == 0) {
					//Play the movement sound:
					AudioManager.PlayAudio (GameMgr.GeneralAudioSource.gameObject, MvtOrderAudio, false);
				}
			}

			//If there's no problem with the current path (meaning there's no obstacles in the way):
			//Then we'll start moving the player there:
			Moving = true;

			//Inform the animator that the unit is currently moving:
			SetAnimState(UnitAnimState.Moving);

			//enable rotation when moving:
			CanRotate = true;

			MvtTargetObj = TargetObj;
			DestinationReached = false;
		} 
		else {
			StopMvt ();

			if (TargetObj != null) {
				if (BuilderMgr && TargetObj.GetComponent<Building> ()) {
					if (TargetObj.GetComponent<Building> () == BuilderMgr.TargetBuilding) { //if the target building is the one the unit's going to construct not just go to
						CancelBuilding ();
					}
				} else if (ResourceMgr && TargetObj.GetComponent<Resource> ()) {
					if (TargetObj.GetComponent<Resource> () == ResourceMgr.TargetResource) { //if the unit's collecting a resource
						CancelCollecting ();
					}
				} else if (AttackMgr) {
					if (AttackMgr.AttackTarget.gameObject == TargetObj) {
						CancelAttack ();
					}
				}
				else if (HealMgr) {
					if (HealMgr.TargetUnit.gameObject == TargetObj) {
						CancelHealing ();
					}
				}
				else if (ConvertMgr) {
					if (ConvertMgr.TargetUnit.gameObject == TargetObj) {
						CancelConverting ();
					}
				}
			}

			//If it's the local player:
			if (GameManager.PlayerFactionID == FactionID && i == 0) {
				//Play the invalid movement path sound:
				AudioManager.PlayAudio (GameMgr.GeneralAudioSource.gameObject, InvalidMvtPathAudio, false);
			}
		}
	}

	//a method that stops the unit from building
	public void CancelBuilding ()
	{
		//If the player was supposed to be constructing a building:
		if (BuilderMgr) {
			if (BuilderMgr.TargetBuilding != null) {

				//custom event:
				if(GameMgr.Events) GameMgr.Events.OnUnitStopBuilding(this,BuilderMgr.TargetBuilding);

				//Stop building:
				BuilderMgr.TargetBuilding.CurrentBuilders.Remove(BuilderMgr);

				//Hide the builder object:
				if (BuilderMgr.BuilderObj != null) {
					BuilderMgr.BuilderObj.SetActive (false);
				}

				if (SelectionMgr.SelectedBuilding == BuilderMgr.TargetBuilding) {
					SelectionMgr.UIMgr.UpdateBuildingUI (SelectionMgr.SelectedBuilding);
				}

				BuilderMgr.TargetBuilding = null;
				BuilderMgr.IsBuilding = false;

				//Inform the animator that we're returning to the idle state as we stopped this action:
				SetAnimState(UnitAnimState.Idle);

				AudioManager.StopAudio (gameObject);
			}
		}
	}

	//a method that stops the unit from healing
	public void CancelHealing ()
	{
		//If the player was supposed to be healing:
		if (HealMgr) {
			if (HealMgr.TargetUnit != null) {

				//custom event:
				if(GameMgr.Events) GameMgr.Events.OnUnitStopHealing(this,HealMgr.TargetUnit);

				HealMgr.TargetUnit = null;
				HealMgr.IsHealing = false;

				//Inform the animator that we're returning to the idle state as we stopped this action:
				SetAnimState(UnitAnimState.Idle);

				AudioManager.StopAudio (gameObject);
			}
		}
	}

	//a method that stops the unit from converting
	public void CancelConverting ()
	{
		//If the player was supposed to be healing:
		if (ConvertMgr) {
			if (ConvertMgr.TargetUnit != null) {

				//custom event:
				if(GameMgr.Events) GameMgr.Events.OnUnitStopHealing(this,ConvertMgr.TargetUnit);

				ConvertMgr.TargetUnit = null;
				ConvertMgr.IsConverting = false;

				//Inform the animator that we're returning to the idle state as we stopped this action:
				SetAnimState(UnitAnimState.Idle);

				AudioManager.StopAudio (gameObject);
			}
		}
	}

	//stop the unit from collectng a resource
	public void CancelCollecting ()
	{
		//Cancel collectin resources when the unit moves away:
		if (ResourceMgr) {
			if (ResourceMgr.TargetResource != null) {
				if(AnimController != null && AnimMgr != null) AnimMgr.runtimeAnimatorController = AnimController; //set the main animation controller.

				//custom event:
				if(GameMgr.Events) GameMgr.Events.OnUnitStopCollecting(this,ResourceMgr.TargetResource);

				//hide the collection object and the drop off object:
				if (ResourceMgr.CurrentCollectionObj != null) {
					ResourceMgr.CurrentCollectionObj.SetActive (false);
				}
				ResourceMgr.CurrentCollectionObj = null;
				if (ResourceMgr.DropOffObj != null) {
					ResourceMgr.DropOffObj.SetActive (false);
				}

				//if it's a single player game:
				if (GameManager.MultiplayerGame == false) {
					//if this unit belongs to a NPC faction
					if (FactionID != GameManager.PlayerFactionID && FreeUnit == false) {
						//if the targer resource is already inside the exploited resource lists of the NPC Resource mgr
						if (ResourceMgr.TargetResource.CurrentCollectors.Count == 0) { //if this unit is the last collector for the target resource.
							if (FactionMgr.ResourceMgr.ExploitedResources [GameMgr.ResourceMgr.GetResourceID (ResourceMgr.TargetResource.Name)].Contains (ResourceMgr.TargetResource) == false) {
								//add it then:
								FactionMgr.ResourceMgr.ExploitedResources [GameMgr.ResourceMgr.GetResourceID (ResourceMgr.TargetResource.Name)].Remove (ResourceMgr.TargetResource);
							}
						}
					}
				}

				//Stop collecting:
				ResourceMgr.TargetResource.CurrentCollectors.Remove(this);

				if (SelectionMgr.SelectedResource == ResourceMgr.TargetResource) {
					SelectionMgr.UIMgr.UpdateResourceUI (SelectionMgr.SelectedResource);
				}

				ResourceMgr.TargetResource = null;
				ResourceMgr.IsCollecting = false;
				ResourceMgr.DroppingOff = false;

				//Inform the animator that we're returning to the idle state as we stopped this action:
				SetAnimState(UnitAnimState.Idle);

				AudioManager.StopAudio (gameObject);
			}
		}
	}

	//stop the unit from attacking.
	public void CancelAttack ()
	{
		if (AttackMgr != null) {
			AttackMgr.AttackTarget = null;
			AttackMgr.AttackStep = 0;
			AttackMgr.AttackStepTimer = 0.0f;

			AttackMgr.Attacking = false;

			//Inform the animator that we're returning to the idle state as we stopped this action:
			SetAnimState(UnitAnimState.Idle);
		}
	}

	[ClientRpc]
	public void RpcUpdateHealth (float Value)
	{
		AddHealthLocal (Value, null);
	}

	public void AddHealth (float Value, GameObject Source)
	{
		//AddHealthLocal (Value, Source);

		if (GameManager.MultiplayerGame == false) {
			AddHealthLocal (Value, Source);
		} else {
			if (hasAuthority) {
				//Sync the building's health with everyone in the network
				//MFactionMgr.TryToSyncUnitHealth (netId, Value);

				//send input action to the MP faction manager if it's a MP game:
				MFactionManager.InputActionsVars NewInputAction = new MFactionManager.InputActionsVars ();
				NewInputAction.Source = netId;
				NewInputAction.Target = netId;
				NewInputAction.InitialPos = transform.position;
				NewInputAction.StoppingDistance = Value;

				GameMgr.Factions[FactionID].MFactionMgr.InputActions.Add (NewInputAction);
			}
		}
	}

	//Health:
	public void AddHealthLocal (float HealthPoints, GameObject Source)
	{
		Health += HealthPoints;
		if (Health > MaxHealth) {
			Health = MaxHealth;
		}

		if (Health <= 0.0f) {

			Health = 0.0f;
			if (Dead == false) { 
				//destroy the building
				DestroyUnit ();
				Dead = true;
			}
		}

		if (Health > 0) {
			//Apply the damage animation:
			if (HealthPoints < 0) {
				SetAnimState (UnitAnimState.TakeDamage);

			}
			//Update health UI:
			if (SelectionMgr.SelectedUnits.Count > 0) {
				//Checking if the unit that has just received damage is currently selected:
				if (SelectionMgr.SelectedUnits.Contains(this) == this) {
					//Update the health UI:
					UIMgr.UpdateUnitHealthUI(SelectionMgr.SelectedUnits [0]);
				}
			}

			if (GameManager.MultiplayerGame == false) {
				//If the unit lost health:
				if (HealthPoints < 0 && Source != null) {
					//can the unit actually defend himself? 
					if (AttackMgr != null) {
						if (AttackMgr.AttackWhenAttacked == true) {
							//attack back if the unit does not have a target already:
							if (AttackMgr.AttackTarget == null) {
								AttackMgr.SetAttackTarget (Source);
							}
						}
					}

					//Check if the unit belongs to an AI player:  
					if (FactionID != GameManager.PlayerFactionID && FreeUnit == false) {
						//We'll search for the nearest building center from the attacking unit:
						Building Center = FactionMgr.BuildingMgr.GetNearestBuilding (transform.position, FactionMgr.BuildingMgr.CapitalBuilding.Code);
						//If the attacked unit is outside the faction's border.
						if (Vector3.Distance (Center.transform.position, transform.position) > Center.GetComponent<Border> ().Size) {
							//Ask for support:
							Collider[] ObjectsInRange = Physics.OverlapSphere (transform.position, FactionMgr.ArmyMgr.AttackingSupportRange);

							//Search for the army units in range of the damaged unit:
							if (ObjectsInRange.Length > 0) {
								foreach (Collider Coll in ObjectsInRange) {
									//If the object is a unit that belongs to the same faction
									if (Coll.gameObject.GetComponent<Unit> ()) {
										if (Coll.gameObject.GetComponent<Attack> () && Coll.gameObject.GetComponent<Unit> ().FactionID == FactionID) {
											//If the object is a unit that can attack
											//Make sure the unit is not currently attacking another unit. Even if it's attacking a building, it will stop and assist the other unit
											bool Attack = false;
											if (Coll.gameObject.GetComponent<Attack> ().AttackTarget == null) {
												Attack = true;
											} else if (Coll.gameObject.GetComponent<Attack> ().AttackTarget.GetComponent<Building> ()) {
												Attack = true;
											} else if (Coll.gameObject.GetComponent<Attack> ().AttackTarget.GetComponent<Unit> ()) {
												//If the target enemy is a unit but this unit hasn't reached it yet or the target can't attack back:
												if (Coll.gameObject.GetComponent<Unit> ().Moving == true || Coll.gameObject.GetComponent<Attack> ().AttackTarget.GetComponent<Attack> () == null) {
													Attack = true;
												}
											}

											if (Attack == true) {
												//Attack the source of the damage:
												Coll.gameObject.GetComponent<Attack> ().TargetAssigned = false;
												Coll.gameObject.GetComponent<Attack> ().SetAttackTarget(Source);

												float MaxDistance = 0.0f; //The distance at which the unit must stand to launch the attack

												if (Source.GetComponent<Unit> ()) {
													Dead = Source.GetComponent<Unit> ().Dead;
													MaxDistance = Coll.gameObject.GetComponent<Attack> ().MaxUnitStoppingDistance + Source.GetComponent<Unit> ().NavAgent.radius;
												} else if (Source.GetComponent<Building> ()) {
													MaxDistance = Coll.gameObject.GetComponent<Attack> ().MaxBuildingStoppingDistance;
												}


												Coll.GetComponent<Unit> ().CheckUnitPath (Vector3.zero, Source.gameObject, MaxDistance, 0, true);

											}
										}
									}
								}
							}

						} else {
							//If the unit is inside the faction's borders:

							int SourceFactionID = -1;
							if (Source.GetComponent<Attack> ()) {
								//If the source of the attack is a unit.
								SourceFactionID = Source.GetComponent<Unit> ().FactionID;
							}
							if (Center != null && SourceFactionID != -1) {
								FactionMgr.ArmyMgr.UnderAttack = true;
								FactionMgr.ArmyMgr.CheckArmyTimer = -1.0f;
								FactionMgr.ArmyMgr.SetDefenseCenter (Center, SourceFactionID);
							}
						}
					}
				}
			}
		}
	}

	[ClientRpc]
	public void RpcDestroyUnit ()
	{
		DestroyUnitLocal ();
	}

	public void DestroyUnit ()
	{
		if (GameManager.MultiplayerGame == false) {
			DestroyUnitLocal ();
		} else {
			if (hasAuthority) {
				//Sync the building's health with everyone in the network
				MFactionMgr.TryToDestroyUnit (netId);
			}
		}
	}

	public void DestroyUnitLocal ()
	{
		if ((GameManager.PlayerFactionID == FactionID && GameManager.MultiplayerGame == true) || GameManager.MultiplayerGame == false) {
			RemoveFromFaction ();

			//If this unit was selected, hide the selection menu:
			if (SelectionMgr.SelectedUnits.Contains (this)) {
				SelectionMgr.DeselectUnit (this);
				if (SelectionMgr.SelectedUnits.Count == 0) {
					UIMgr.HideTaskButtons ();
					UIMgr.HideSelectionInfoPanel ();
					UIMgr.TaskInfoMenu.gameObject.SetActive (false);
				}
			}
		}

		//Inform the animator that the unit died:
		SetAnimState (UnitAnimState.Dead);

		//unit death:
		Dead = true;
		Health = 0.0f;

		CancelAttack ();
		CancelBuilding ();
		CancelCollecting ();
		CancelHealing ();
		CancelConverting ();

		if(GameMgr.Events) GameMgr.Events.OnUnitDead (this);

		if (GameManager.MultiplayerGame == false) {
			//remove components to avoid interacting with the unit:
			if(BuilderMgr) 
				BuilderMgr.enabled = false;
			if (AttackMgr)
				AttackMgr.enabled = false;
			if (ResourceMgr)
				ResourceMgr.enabled = false;
			GetComponent<Collider> ().enabled = false;
			//Destroy the building's object:
			Destroy (gameObject, DestroyObjTime);
		}
	
	}

	public void RemoveFromFaction ()
	{
		//Remove this unit from the lists in the team manager:
		if (FactionMgr != null && FreeUnit == false) {
			FactionMgr.RemoveUnitFromLists (this);

			if (AttackMgr) {
				if (GameManager.MultiplayerGame == false) {

					//If the unit belongs to a AI faction, then make sure to remove it from the army units list if it belongs to it:
					if (FactionID != GameManager.PlayerFactionID) {
						if (AttackMgr.ArmyUnitID >= 0) {
							if (FactionMgr.ArmyMgr.ArmyUnits [AttackMgr.ArmyUnitID].CurrentUnits.Contains (this)) {
								FactionMgr.ArmyMgr.ArmyUnits [AttackMgr.ArmyUnitID].CurrentUnits.Remove (this);
							}
						}
						if (FactionMgr.ArmyMgr.Army.Contains (this)) {
							FactionMgr.ArmyMgr.Army.Remove (this);
							FactionMgr.ArmyMgr.AttackingArmyPower -= this.gameObject.GetComponent<Attack> ().UnitDamage;
						}

						//If the unit belongs to an attacking army of a NPC player
						if (FactionMgr.ArmyMgr.Army.Contains (this)) {
							FactionMgr.ArmyMgr.AttackingArmyPower -= this.gameObject.GetComponent<Attack> ().UnitDamage;

						}
					}
				}
			}
			if (gameObject.GetComponent<APC> ()) {
				//if the unit is an APC vehicle:
				int ContainedUnits = gameObject.GetComponent<APC> ().CurrentUnits.Count;
				if (ContainedUnits > 0) { //if ther eare units inside the APC
					for (int i = 0; i < ContainedUnits; i++) { //loop through them
						//release all units:
						if (gameObject.GetComponent<APC> ().ReleaseOnDestroy == true) { //release on destroy:
							gameObject.GetComponent<APC> ().RemoveUnit (gameObject.GetComponent<APC> ().CurrentUnits [0]);
						} else {
							//destroy contained units:
							gameObject.GetComponent<APC> ().CurrentUnits [0].DestroyUnit();
						}
					}
				}
			}

			GameMgr.Factions [FactionID].CurrentPopulation--;
			UIMgr.UpdatePopulationUI ();
		}
	}

	//unit conversion:
	public void ConvertUnit (Unit Converter)
	{
		//if same faction, then do nothing
		if (Converter.FactionID == FactionID)
			return;

		if (GameManager.MultiplayerGame == false) {
			//single player game, directly convert unit:
			ConvertUnitLocal (Converter);
		}
		else //online game:
		{
			if (FactionID == GameManager.PlayerFactionID) { //if this is the local player:
				//send the custom action input:
				MFactionManager.InputActionsVars NewInputAction = new MFactionManager.InputActionsVars ();

				NewInputAction.Source = netId;
				NewInputAction.Target = Converter.netId;
				NewInputAction.CustomAction = true;
				NewInputAction.StoppingDistance = 11.0f; //11 means a converting action

				MFactionMgr.InputActions.Add (NewInputAction);
			}
		}
	}
		
	public void ConvertUnitLocal (Unit Converter)
	{
		//stop movement of unit:
		StopMvt();
		//make the unit idle:
		CancelAttack();
		CancelBuilding ();
		CancelCollecting ();
		CancelHealing ();
		CancelConverting();

		//remove unit from the previous faction:
		if (FreeUnit == false) {
			RemoveFromFaction ();
		}

		FreeUnit = false;
		//Add unit to new faction:
		FactionMgr = GameMgr.Factions[Converter.FactionID].FactionMgr;
		FactionID = Converter.FactionID;
		CreatedBy = GameMgr.Factions[Converter.FactionID].CapitalBuilding;
		//Add the newly created unit to the team manager list:
		FactionMgr.AddUnitToLists (this);	    
		//reset the unit's colors:
		SetUnitColors();

		if (GameManager.MultiplayerGame == true) {
			MFactionMgr = Converter.MFactionMgr;
			if (GameManager.PlayerFactionID == FactionID) {
				MFactionMgr.TryToAssignAuthority (netId);
			}
		}

		//spawn the convertion effect:
		if (Converter.ConvertMgr.ConvertEffect) {
			Instantiate (Converter.ConvertMgr.ConvertEffect, transform.position, Converter.ConvertMgr.ConvertEffect.transform.rotation);
		}

		if (GameMgr.Events) {
			GameMgr.Events.OnUnitConverted (Converter, this);
		}
	}

	//See if the unit is in idle state or not:
	public bool IsIdle()
	{
		if (Moving == true) {
			return false;
		}
		if (BuilderMgr) {
			if (BuilderMgr.TargetBuilding == true)
				return false;
		}
		if (ResourceMgr) {
			if (ResourceMgr.TargetResource == true)
				return false;
		}
		if (AttackMgr) {
			if (AttackMgr.AttackTarget != null)
				return false;
		}
		if (HealMgr) {
			if (HealMgr.TargetUnit != null)
				return false;
		}
		if (ConvertMgr) {
			if (ConvertMgr.TargetUnit != null)
				return false;
		}

		return true;
	}

	//Handling animations:
	public void SetAnimState (UnitAnimState NewState)
	{
		if (AnimMgr != null && gameObject.activeInHierarchy == true) { //if there's an animation manager
			if (AnimMgr.gameObject.activeInHierarchy == true) { //making sure the object that has the animator manager is active
				CurrentAnimState = NewState;
				switch (CurrentAnimState) {
				case UnitAnimState.Idle:
					AnimMgr.SetBool ("IsIdle", true);

				//Stop any current action we're making because we just moved to the idle state:
					if(AttackMgr)
						AnimMgr.SetBool ("IsAttacking", false);
					AnimMgr.SetBool ("IsMoving", false);
					if(ResourceMgr)
						AnimMgr.SetBool ("IsCollecting", false);
					if(BuilderMgr)
						AnimMgr.SetBool ("IsBuilding", false);
					if(ConvertMgr)
						AnimMgr.SetBool ("IsConverting", false);
					if(HealMgr)
						AnimMgr.SetBool ("IsHealing", false);
					break;
				case UnitAnimState.Building:
					AnimMgr.SetBool ("IsIdle", false);
					AnimMgr.SetBool ("IsBuilding", true);
					break;
				case UnitAnimState.Healing:
					AnimMgr.SetBool ("IsIdle", false);
					AnimMgr.SetBool ("IsHealing", true);
					break;
				case UnitAnimState.Converting:
					AnimMgr.SetBool ("IsIdle", false);
					AnimMgr.SetBool ("IsConverting", true);
					break;
				case UnitAnimState.Collecting:
					AnimMgr.SetBool ("IsIdle", false);
					AnimMgr.SetBool ("IsCollecting", true);
					break;
				case UnitAnimState.Moving:
					AnimMgr.SetBool ("IsIdle", false);
					AnimMgr.SetBool ("IsCollecting", false);
					AnimMgr.SetBool ("IsBuilding", false);
					AnimMgr.SetBool ("IsMoving", true);
					break;
				case UnitAnimState.Attacking:
					AnimMgr.SetBool ("IsIdle", false);
					AnimMgr.SetBool ("IsAttacking", true);
					break;
				case UnitAnimState.TakeDamage:
					//AnimMgr.SetBool ("TookDamage", true);
					break;
				case UnitAnimState.Dead:
					AnimMgr.SetBool ("IsIdle", true);
					AnimMgr.SetBool ("IsDead", true);
					break;
				default:
					return;
				}
			}
		}
	}

	void OnTriggerEnter(Collider other) {
		//check if the unit enters in collision with the treasure object:
		/*if (other.gameObject.GetComponent<Treasure> ()) {
			Treasure Treasure = other.gameObject.GetComponent<Treasure> ();
			if (Treasure.Resources.Length > 0) {
				for (int i = 0; i < Treasure.Resources.Length; i++) { //go through all the resources to reward
					GameMgr.ResourceMgr.AddResource(FactionID,Treasure.Resources[i].Name, Treasure.Resources[i].Amount);
				}

				//play the claimed reward sound:
				if (GameManager.PlayerFactionID == FactionID && Treasure.ClaimedAudio != null) { //only if the unit is the local player's faction:
					AudioManager.PlayAudio (GameMgr.GeneralAudioSource.gameObject, Treasure.ClaimedAudio, false);
				}
				//create the effect
				if (Treasure.ClaimedEffect != null) {
					Instantiate (Treasure.ClaimedEffect, Treasure.transform.position, Treasure.ClaimedEffect.transform.rotation);
				}
			}
		}

		//must be modified.
		/f (other.gameObject.GetComponent<Unit> ()) {
			if (Moving == true && other.gameObject.GetComponent<Unit> ().Moving == false) {
				NavAgent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.NoObstacleAvoidance;
				other.gameObject.GetComponent<UnityEngine.AI.NavMeshAgent> ().obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.NoObstacleAvoidance;
			}
		}*/
	}

	void OnTriggerExit (Collider other)
	{
		/*if (other.gameObject.GetComponent<Unit> ()) {
			other.gameObject.GetComponent<UnityEngine.AI.NavMeshAgent> ().obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.LowQualityObstacleAvoidance;
		}*/
	}
		

	void OnTriggerStay (Collider other)
	{
		if (Moving == true && MvtTargetObj != null) {
			if (MvtTargetObj == other.gameObject && !MvtTargetObj.gameObject.GetComponent<Unit> ()) {
				DestinationReached = true;
			}
		}
	}
}
