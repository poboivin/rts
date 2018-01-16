using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;


/* Resource Generator script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

public class ResourceGenerator : MonoBehaviour {

	[System.Serializable]
	public class ResourceGenInfo
	{
		public string Name; //name of the resource to collect
		public float CollectOneUnitTime = 1.0f; //How much time is needed to collect this resource:
		[HideInInspector]
		public float Timer;
		public int MaxAmount; //The maximum amount that this generator can store.
		[HideInInspector]
	    public int Amount; //Current amount of the collected resource.
		[HideInInspector]
		public bool MaxAmountReached = false;
		public AudioClip CollectionAudioClip; //played when the resource is collected.
		public Sprite TaskIcon; //When the maximum amount is reached, then a task appears on the task panel to collect the gathered resource when the generator is selected. This is the task's icon.
	}
	public ResourceGenInfo[] Resources;
	[HideInInspector]
	public List<int> ReadyToCollect = new List<int> ();

	public bool AutoCollect = false; //if true, then resources will automatically added to the faction. if false then the resource collection will be limited and player would have to manually gather them when they reach the max amount.

	//other scripts:
	ResourceManager ResourceMgr;
	Building Building;

	void Start ()
	{

		Building = gameObject.GetComponent<Building> ();
		if (Building != null) {
			ResourceMgr = Building.ResourceMgr;
		} else {
			Debug.LogError ("The Resource Generator script must be placed at the same object that has the Building script!");
			enabled = false;
		}

		if (Resources.Length > 0) {
			for (int i = 0; i < Resources.Length; i++) { //loop through all the resources to generate
				if (ResourceMgr.GetResourceID (Resources [i].Name) >= 0) { //if it's a valid resource
					if (Resources [i].CollectOneUnitTime > 0) {
						//start the collection timer:
						Resources [i].Timer = Resources [i].CollectOneUnitTime;
						Resources [i].MaxAmountReached = false;
					}
				} else {
					Debug.LogError ("Resource ID: " + i.ToString () + "in the Resource Generator is not a valid resource.");
					enabled = false;
				}
			}
		}

		//if it's a multiplayer game and this does not belong to the local player's faction:
		if (GameManager.MultiplayerGame == true && GameManager.PlayerFactionID != Building.FactionID) {
			enabled = false;
		}
	}

	void Update ()
	{
		if (Building == null) {
			Debug.LogError ("The Resource Generator script must be placed at the same object that has the Building script!");
			enabled = false;
			return;
		}
		//if the building can produce resources:
		if (Building.IsBuilt == true && Building.Health >= Building.MinTaskHealth) {
			if (Resources.Length > 0) {
				for (int i = 0; i < Resources.Length; i++) { //loop through all the resources to generate
					if (Resources [i].MaxAmountReached == false) {
						if (Resources [i].Timer > 0) {
							Resources [i].Timer -= Time.deltaTime;
						} else {
							if (AutoCollect == true) {
								//if resources are auto added to the faction:
								ResourceMgr.AddResource (Building.FactionID, Resources [i].Name, 1);
							} else {
								//if resources are not auto added:
								Resources [i].Amount += 1;
								if (Resources [i].Amount >= Resources [i].MaxAmount) {
									if (GameManager.PlayerFactionID == Building.FactionID) {
										//max amount reached, stop collecting.
										Resources [i].MaxAmountReached = true;
										ReadyToCollect.Add (i);

										//update the task panel if this building is selected:
										if (Building.SelectionMgr.SelectedBuilding == Building) {
											Building.UIMgr.UpdateBuildingUI (Building);
										}
									} else {
										ResourceMgr.AddResource (Building.FactionID, Resources [i].Name, Resources[i].Amount);
										Resources [i].Amount = 0;
									}
								}
							}

							Resources [i].Timer = Resources [i].CollectOneUnitTime; //relaunch the timer.
						}
					}
				}
			}
		}
	}
}