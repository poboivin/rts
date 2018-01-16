using UnityEngine;
using System.Collections;

/* Attack Object script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

public class AttackObject : MonoBehaviour {

	[HideInInspector]
	public string Code; //Give each type of attack object a unique code used to identify it.

	[HideInInspector]
	public GameObject Source; //From the attack object was launched.
	[HideInInspector]
	public Vector3 MvtVector; //The attack object
	[HideInInspector]
	public float Speed = 10.0f; //attack object's speed:

	//attack object settings (that it gets from attack comp):
	[HideInInspector]
	public float BuildingDamage; //damage points to cause buildings
	[HideInInspector]
	public float UnitDamage; //damage points to cause a unit
	[HideInInspector]
	public bool DamageOnce = true; //do damage once? 
	[HideInInspector]
	public bool DoDamage = true; //do damage at all? 
	[HideInInspector]
	public bool DestroyOnDamage; //destroy on first given damage?

	[HideInInspector]
	public int TargetFactionID; //target faction to attack
	[HideInInspector]
	public int SourceFactionID; //the unit's faction that this object came from:

	[HideInInspector]
	public bool DidDamage = false;
	[HideInInspector]
	public bool AreaDamage = false;

	[HideInInspector]
	public Attack.AttackRangesVars[] AttackRanges;

	public GameObject SpawnEffect; //the spawn effect that is instantied when this object is spawned

	//Attack object life timer:
	[HideInInspector]
	public float DestroyTime;
	[HideInInspector]
	public float DestroyTimer;

	//Scripts:
	EffectObjPooling ObjPooling;

	// Use this for initialization
	void Start () {
		DidDamage = false;

		//Settings in order to make OnCollisionEnter work:
		GetComponent<Collider> ().isTrigger = false;
		GetComponent<Rigidbody> ().isKinematic = false;
		GetComponent<Rigidbody> ().useGravity = false;

		ObjPooling = FindObjectOfType (typeof(EffectObjPooling)) as EffectObjPooling;
	}
		
	// Update is called once per frame
	void Update () 
	{
		//move the attack object towards its target:
		Vector3 moveDir = MvtVector.normalized;
		transform.position += moveDir * Speed * Time.deltaTime;

		//if we already done damage and this object gets destroyed after damage:
		if (DestroyOnDamage == true && DidDamage == true) {
			//actually hide the object and don't destroy it, hiding it will add it automatically to the pool allowing us to re-use it.
			gameObject.SetActive (false);
		}

		//destroy timer:
		if (DestroyTimer > 0) {
			DestroyTimer -= Time.deltaTime;
		}
		if (DestroyTimer < 0) {
			gameObject.SetActive (false);
		}
	}


	//Show the attack object's spawn effect:
	public void ShowAttackObjEffect ()
	{
		if (SpawnEffect != null) {
			//Check if the spawn effect has the effect obj script which we will need for pooling:
			if (SpawnEffect.GetComponent<EffectObj> ()) {
				GameObject SpawnEffectObj = null;

				if (ObjPooling != null) {
					//Search for an inactive spawn effect obj to re-use:
					SpawnEffectObj = ObjPooling.GetFreeEffectObj (EffectObjPooling.EffectObjTypes.AttackObjEffect, SpawnEffect.GetComponent<EffectObj> ().Code);
				}

				if (SpawnEffectObj == null) { //If no inactive spawn effect is found, then..
					//create a new damage effect on the attack object's position:
					SpawnEffectObj = (GameObject)Instantiate (SpawnEffect, transform.position, SpawnEffect.transform.rotation);
					//and add it to the list:
					if (ObjPooling != null) {
						ObjPooling.AttackObjEffects.Add (SpawnEffectObj);
					}
				} else {
					//there's a spawn effect object, activate it and show it:
					SpawnEffectObj.SetActive (true);
					SpawnEffectObj.transform.position = transform.position;
					SpawnEffectObj.transform.rotation = SpawnEffect.transform.rotation;
				}
				SpawnEffectObj.GetComponent<EffectObj> ().Timer = SpawnEffectObj.GetComponent<EffectObj> ().LifeTime;
			} else {
				Debug.LogWarning ("Add the 'EffectObj.cs' to the attack spawn effect object. It is required for the object pooling to work!");
			}
		}
	}

	//Attack object collision effect:
	void OnTriggerEnter (Collider other)
	{
		if ((DidDamage == false || DamageOnce == false) && DoDamage == true) { //Make sure that the attack obj either didn't do damage when the attack object is allowed to do damage once or if it can do damage multiple times.
			SelectionObj HitObj = other.gameObject.GetComponent<SelectionObj> ();
			if (HitObj != null) {
				Unit HitUnit = HitObj.MainObj.GetComponent<Unit> ();
				Building HitBuilding = HitObj.MainObj.GetComponent<Building> ();

				//If the damaged object is a unit:
				if (HitUnit) {
					//Check if the unit belongs to the faction that this attack obj is targeted to and if the unit is actually not dead yet:
					if (HitUnit.FactionID == TargetFactionID && HitUnit.Dead == false) {
						if (other != null) {
							DidDamage = true; //Inform the script that the damage has been done
							if (AreaDamage) {
								LaunchAreaDamage (transform.position);
							}
							else
							{
								//Remove health points from the unit:
								HitUnit.AddHealth (-UnitDamage, Source);
							}

							//Spawning the damage effect object:
							//First check if the unit has a damage effect object:
							if (HitUnit.DamageEffect != null) {
								//Check if the damage effect has the effect obj script which we will need for pooling:
								if (HitUnit.DamageEffect.GetComponent<EffectObj> ()) {
									GameObject DamageObj = null;

									if (ObjPooling != null) {
										//Search for an inactive damage effect obj to re-use:
										DamageObj = ObjPooling.GetFreeEffectObj (EffectObjPooling.EffectObjTypes.UnitDamageEffect, HitUnit.DamageEffect.GetComponent<EffectObj> ().Code);
									}

									if (DamageObj == null) { //If no inactive damage effect is found, then..
										//create a new damage effect on the contact point between the attack obj and the unit:
										DamageObj = (GameObject)Instantiate (HitUnit.DamageEffect, other.transform.position, HitUnit.DamageEffect.transform.rotation);
										//and add it to the list:
										if (ObjPooling != null) {
											ObjPooling.UnitDamageEffects.Add (DamageObj);
										}
									} else {
										DamageObj.SetActive (true);
										DamageObj.transform.position = other.transform.position;
										DamageObj.transform.rotation = HitUnit.DamageEffect.transform.rotation;
									}
									DamageObj.GetComponent<EffectObj> ().Timer = DamageObj.GetComponent<EffectObj> ().LifeTime;
								} else {
									Debug.LogWarning ("Add the 'EffectObj.cs' to the damage effect object. It is required for the object pooling to work!");
								}
							}
						}
					}
				}
				//If the attack obj hit a building:
				if (HitBuilding) {
					//Check if the building belongs to the faction that this attack obj is targeted to and if the building still has health:
					if (HitBuilding.FactionID == TargetFactionID && HitBuilding.Health >= 0) {
						if (other != null) {
							DidDamage = true; //Inform the script that the damage has been done

							if (AreaDamage) {
								LaunchAreaDamage (transform.position);
							}
							else
							{
								//Remove health points from the unit:
								HitBuilding.AddHealth (-BuildingDamage, Source);
							}

							//Spawning the damage effect object:
							//First check if the building has a damage effect object:
							if (HitBuilding.DamageEffect != null) {
								//Check if the damage effect has the effect obj script which we will need for pooling:
								if (HitBuilding.DamageEffect.GetComponent<EffectObj> () != null) {
									GameObject DamageObj = null;

									if (ObjPooling != null) {
										//Search for an inactive damage effect obj to re-use:
										DamageObj = ObjPooling.GetFreeEffectObj (EffectObjPooling.EffectObjTypes.BuildingDamageEffect, HitBuilding.DamageEffect.GetComponent<EffectObj> ().Code);
									}

									if (DamageObj == null) { //If no inactive damage effect is found, then..
										//create a new damage effect on the contact point between the attack obj and the unit:
										DamageObj = (GameObject) Instantiate (HitBuilding.DamageEffect, other.transform.position, HitBuilding.DamageEffect.transform.rotation);
										//and add it to the list:
										if (ObjPooling != null) {
											ObjPooling.BuildingDamageEffects.Add (DamageObj);
										}

									} else {
										DamageObj.SetActive (true);

										DamageObj.transform.position = other.transform.position;
										DamageObj.transform.rotation = HitBuilding.DamageEffect.transform.rotation;
									}
									DamageObj.GetComponent<EffectObj> ().Timer = DamageObj.GetComponent<EffectObj> ().LifeTime;
								} else {
									Debug.LogWarning ("Add the 'EffectObj.cs' to the damage effect object. It is required for the object pooling to work!");
								}
							}
						}
					}
				}
			}
		}
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
						if (Unit.FactionID == SourceFactionID)
							Unit = null;
					}
					if (Building) {
						if (Building.FactionID == SourceFactionID)
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
									Unit.AddHealth (-AttackRanges [j].UnitDamage, this.gameObject);
								else if (Building)
									Building.AddHealth (-AttackRanges [j].BuildingDamage, this.gameObject);

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
