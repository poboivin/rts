using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/* Invisibility script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

public class Invisibility : MonoBehaviour {

	public float InvisibilityDuration = 5.0f; //maximum time a unit is allowed to stay invisible.
	float InvisibilityTimer;

	//effected materials:
	public SkinnedMeshRenderer[] AffectedMaterials;

	//alpha material main color when invisible:
	public float AlphaColorWhenInvisible = 0.4f;

	//sprties:
	public Sprite GoInvisibleSprite;
	public Sprite GoVisibleSprite;
	public int InvisibilityTasksCategory = 0;

	//audio clips:
	public AudioClip GoInvisibleAudio;
	public AudioClip GoVisibleAudio;

	Unit UnitMvt;
	[Header("While invisible:")]
	//list of things that the unit can or can not do while being invisible:
	public bool CanAttack = false;
	public bool CanCollect = false;
	public bool CanBuild = false;
	public bool CanConvert = false;
	public bool CanHeal = false;

	void Start () {
		UnitMvt = GetComponent<Unit> ();
	}

	void Update ()
	{
		if (InvisibilityTimer > 0) {
			InvisibilityTimer -= Time.deltaTime;
		}
		if (InvisibilityTimer < 0) {
			InvisibilityTimer = 0.0f;

			ToggleInvisibility ();
		}
	}

	public void ToggleInvisibility ()
	{
		if (GameManager.MultiplayerGame == true) { //if this is a MP game and it's the local player:
			if (GameManager.PlayerFactionID == UnitMvt.FactionID) {
				//send the custom action input:
				MFactionManager.InputActionsVars NewInputAction = new MFactionManager.InputActionsVars ();

				NewInputAction.Source = UnitMvt.netId;
				NewInputAction.CustomAction = true;
				NewInputAction.StoppingDistance = 12.0f; //12 stands for the invisibility.

				UnitMvt.MFactionMgr.InputActions.Add (NewInputAction);
			}
		} else {
			//offline game? update the attack type directly:
			ToggleInvisibilityLocal();
		}
	}

	public void ToggleInvisibilityLocal ()
	{
		UnitMvt.IsInvisible = !UnitMvt.IsInvisible;

		if (UnitMvt.IsInvisible == true) {
			InvisibilityTimer = InvisibilityDuration;
			AudioManager.PlayAudio (UnitMvt.GameMgr.GeneralAudioSource.gameObject, GoVisibleAudio, false);

			//check what the unit is doing and take action:
			if (CanAttack == false) {
				if (UnitMvt.AttackMgr) {
					if (UnitMvt.AttackMgr.AttackTarget != null) {
						UnitMvt.CancelAttack ();
					}
				}
			}
			if (CanBuild == false) {
				if (UnitMvt.BuilderMgr) {
					if (UnitMvt.BuilderMgr.TargetBuilding != null) {
						UnitMvt.CancelBuilding ();
					}
				}
			}
			if (CanConvert == false) {
				if (UnitMvt.ConvertMgr) {
					if (UnitMvt.ConvertMgr.TargetUnit != null) {
						UnitMvt.CancelConverting ();
					}
				}
			}
			if (CanCollect == false) {
				if (UnitMvt.ResourceMgr) {
					if (UnitMvt.ResourceMgr.TargetResource != null) {
						UnitMvt.CancelCollecting ();
					}
				}
			}
			if (CanHeal == false) {
				if (UnitMvt.HealMgr) {
					if (UnitMvt.HealMgr.TargetUnit != null) {
						UnitMvt.CancelHealing ();
					}
				}
			}

			float AlphaColor = (GameManager.PlayerFactionID == UnitMvt.FactionID) ? AlphaColorWhenInvisible : 0.0f;

			//set the aplha color on affected materials:
			for (int i = 0; i < AffectedMaterials.Length; i++) {
				if (AffectedMaterials [i]) {
					AffectedMaterials [i].material.color = new Color (AffectedMaterials [i].material.color.r, AffectedMaterials [i].material.color.g, AffectedMaterials [i].material.color.b, AlphaColor);
				}
			}

			//disable the Selection obj if this is not the local player in a LAN game:
			if (GameManager.PlayerFactionID != UnitMvt.FactionID) {
				if (GameManager.Instance.SelectionMgr.SelectedUnits.Contains (UnitMvt)) {
					GameManager.Instance.SelectionMgr.DeselectUnit (UnitMvt);
				}
				UnitMvt.PlayerSelection.gameObject.GetComponent<Collider> ().enabled = false;
			}

			//custom event:
			if (UnitMvt.GameMgr.Events) {
				UnitMvt.GameMgr.Events.OnUnitGoInvisible (UnitMvt);
			}
		} else {
			InvisibilityTimer = 0.0f;
			AudioManager.PlayAudio (UnitMvt.GameMgr.GeneralAudioSource.gameObject, GoInvisibleAudio, false);

			//set the aplha color on affected materials:
			for (int i = 0; i < AffectedMaterials.Length; i++) {
				if (AffectedMaterials [i]) {
					AffectedMaterials [i].material.color = new Color (AffectedMaterials [i].material.color.r, AffectedMaterials [i].material.color.g, AffectedMaterials [i].material.color.b, 1.0f);
				}
			}

			if (GameManager.PlayerFactionID != UnitMvt.FactionID) {
				UnitMvt.PlayerSelection.gameObject.GetComponent<Collider> ().enabled = enabled;
			}

			//custom event:
			if (UnitMvt.GameMgr.Events) {
				UnitMvt.GameMgr.Events.OnUnitGoVisible (UnitMvt);
			}
		}

		UnitMvt.UIMgr.UpdateUnitTasks ();
	}
}