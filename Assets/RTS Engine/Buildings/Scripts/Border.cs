using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

/* Border script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

public class Border : MonoBehaviour {

	[HideInInspector]
	public bool IsActive = false; //is the border active or not?
	public GameObject BorderObj; //Use an object that is only visible on the terrain to avoid drawing borders outside the terrain.
	public float BorderObjSizeMultiplier = 2.0f; //To control the relation of the border obj's actual size and the border's map. Using different textures for the border objects will require using 
	//different multipliers to fit the actual border size.
	public bool SpawnBorderObj = true;

	public float Size = 10.0f; //The size of the border around this building:
	//public float CenterAddDistance = 5.0f; //You can make building centers be built outside the faction's borders by setting this var's value:

	//The list of resources belonging inside this border:
	[HideInInspector]
	public List<Resource> ResourcesInRange = new List<Resource> ();

	GameManager GameMgr;
	ResourceManager ResourceMgr;
	[HideInInspector]
	public FactionManager FactionMgr = null;

	//If the border belongs to an NPC player, it will require him to build all the objects in the array below.

	//If the border belongs to the player, then this array will only represent the maximum amounts for each building
	//and if a building is not in this array, then the player is free to build as many as he wishes to build.
	[System.Serializable]
	public class BuildingsInsideBorderVars
	{
		public Building Prefab;
		public string FactionCode; //Leave empty if you want this building to be considered by all factions
		[HideInInspector]
		public int CurrentAmount = 0;
		public int MaxAmount = 1;
		[HideInInspector]
		public int ProgressAmount;

		public bool Required = true; //Only for NPC factions, if set to true, the faction will have to place this building but if it's false, the building will only be placed when needed.

		[HideInInspector]
		public bool AskedForResources = false; //Did we ask to collect the resources needed for this building:
		//[HideInInspector]
		public bool MaxAmountReached = false; //Is the building already built? 

	}
	public List<BuildingsInsideBorderVars> BuildingsInsideBorder;
	[HideInInspector]
	public List<Building> BuildingsInRange = new List<Building>();

	//For more diversity to the NPC player, the timer that checks for the buildings inside a border can take a value between a min and a max one.
	public float BuildingCheckMinTimer = 3.0f;
	public float BuildingCheckMaxTimer = 5.0f;
	float Timer;

	bool AllBuilt = false; //Are all required buildings inside the border built or not?


	public void ActivateBorder () {
		GameMgr = GameManager.Instance;
		ResourceMgr = GameMgr.ResourceMgr;

		if (IsActive == false) {

			if (SpawnBorderObj == true) {
				BorderObj = (GameObject)Instantiate (BorderObj, gameObject.transform.position, Quaternion.identity);
				BorderObj.transform.localScale = new Vector3 (Size * BorderObjSizeMultiplier, BorderObj.transform.localScale.y, Size * BorderObjSizeMultiplier);
				BorderObj.transform.SetParent (transform, true);

				//Set the border's color to the faction it belongs to:
				Color FactionColor = GameMgr.Factions [gameObject.GetComponent<Building> ().FactionID].FactionColor;
				BorderObj.GetComponent<MeshRenderer	> ().material.color = FactionColor;
				//Set the border's sorting order:
				BorderObj.gameObject.GetComponent<MeshRenderer> ().sortingOrder = GameMgr.LastBorderSortingOrder;
				GameMgr.LastBorderSortingOrder--;
			}


			//Add the border to all borders list:
			GameMgr.AllBorders.Add(this);

			CheckBorderResources ();

			IsActive = true;

		
		}

		FactionMgr = GameMgr.Factions[gameObject.GetComponent<Building> ().FactionID].FactionMgr;

		if (FactionMgr != null) {
			if (FactionMgr.FactionID != GameManager.PlayerFactionID && GameManager.MultiplayerGame == false) {
				Timer = Random.Range (BuildingCheckMinTimer, BuildingCheckMaxTimer);
			}
		}

			//Inform the faction's resource manager that we need to check resource inside the new border:
			if (FactionMgr.ResourceMgr != null) {
				FactionMgr.ResourceMgr.CheckResources = true;
			}

		CheckBuildingsInBorder ();
	}

	public void CheckBorderResources ()
	{
		//We'll check the resources inside this border:
		if (ResourceMgr.AllResources.Count > 0) {
			for (int j = 0; j < ResourceMgr.AllResources.Count; j++) {
				if (ResourcesInRange.Contains (ResourceMgr.AllResources [j]) == false && ResourceMgr.AllResources [j].FactionID == -1) {
					//Making sure that it doesn't already exist before adding it.
					if (Vector3.Distance (ResourceMgr.AllResources [j].transform.position, transform.position) < Size) {
						ResourcesInRange.Add (ResourceMgr.AllResources [j]);
						ResourceMgr.AllResources [j].FactionID = gameObject.GetComponent<Building>().FactionID;
					}
				}
			}
		}
	}

	void Update ()
	{
		if (FactionMgr != null) {
			if (AllBuilt == false && BuildingsInsideBorder.Count > 0 && FactionMgr.BuildingCenters.IndexOf (gameObject.GetComponent<Building> ()) >= 0) {
				//If we still haven't built everything needed:
				if (Timer > 0) {
					Timer -= Time.deltaTime;
				}
				if (Timer < 0) {
					Timer = Random.Range (BuildingCheckMinTimer, BuildingCheckMaxTimer);
					bool AllBuildingsBuilt = true;

					//Loop through all the buildings inside the array
					for (int i = 0; i < BuildingsInsideBorder.Count; i++) {
						if (BuildingsInsideBorder [i].Required == true) {
							//If the building is not built yet:
							if (BuildingsInsideBorder [i].MaxAmountReached == false) {
								//If the currrent amount + the in progress amount (amount of buildings that are being placed) is less than the max allowed amount, only then proceed:
								if (BuildingsInsideBorder [i].ProgressAmount + BuildingsInsideBorder [i].CurrentAmount < BuildingsInsideBorder [i].MaxAmount) {
									AllBuildingsBuilt = false;

									FactionMgr.BuildingMgr.AttemptToAddBuilding (BuildingsInsideBorder [i].Prefab, true, null);
								}
							}
						}
					}

					AllBuilt = AllBuildingsBuilt;
				}
			}
		}
	}

	public int GetBuildingIDInBorder (string Code)
	{
		if (BuildingsInsideBorder.Count > 0) {
			//Loop through all the buildings inside the array:
			for (int i = 0; i < BuildingsInsideBorder.Count; i++) {
				//When we find the building in the border's list, return it.
				if (BuildingsInsideBorder [i].Prefab.gameObject.GetComponent<Building> ().Code == Code) {
					return i;
				}
			}
		}

		return -1;
	}

	public void RegisterBuildingInBorder (Building Building)
	{
		//add the building to the list:
		BuildingsInRange.Add(Building);
		//First check if the building exists inside the border:
		int i = GetBuildingIDInBorder (Building.Code);
		if (i != -1) {

			//If we reach the maximum allowed amount for this item then add it:
			BuildingsInsideBorder [i].CurrentAmount++;
			if (BuildingsInsideBorder [i].CurrentAmount == BuildingsInsideBorder [i].MaxAmount) {
				BuildingsInsideBorder [i].MaxAmountReached = true;
			}
		}
	}

	public void UnegisterBuildingInBorder (Building Building)
	{
	    //remove the building from the list:
		BuildingsInRange.Remove(Building);
		//First check if the building exists inside the border:
		int i = GetBuildingIDInBorder (Building.Code);
		if (i != -1) {

			//If we reach the maximum allowed amount for this item then add it:
			BuildingsInsideBorder [i].CurrentAmount--;
			if (BuildingsInsideBorder [i].CurrentAmount < BuildingsInsideBorder [i].MaxAmount) {
				BuildingsInsideBorder [i].MaxAmountReached = false;
			}
		}
	}

	//Progress amount for buildings inside the border, only for NPC factions so that building orders won't be asked many times:
	public void RegisterInProgressBuilding (string Code)
	{
		//First check if the building exists inside the border:
		if (GetBuildingIDInBorder (Code) != -1) {
			int i = GetBuildingIDInBorder (Code);

			//Update the progress amount:
			BuildingsInsideBorder [i].ProgressAmount++;
		}
	}

	public void UnregisterInProgressBuilding (string Code)
	{
		//First check if the building exists inside the border:
		if (GetBuildingIDInBorder (Code) != -1) {
			int i = GetBuildingIDInBorder (Code);

			//Update the progress amount:
			BuildingsInsideBorder [i].ProgressAmount--;
		}
	}

	public bool AllowBuildingInBorder (string Code)
	{
		//This determines if we're still able to construct a building inside the borders:
		//Loop through all the buildings inside the array:
		if (BuildingsInsideBorder.Count > 0) {
			for (int i = 0; i < BuildingsInsideBorder.Count; i++) {
				//When we find the building in the border's list, return it.
				if (BuildingsInsideBorder [i].Prefab.gameObject.GetComponent<Building> ().Code == Code) {
					if (BuildingsInsideBorder [i].MaxAmountReached == false) {
						return true;
					} else {
						return false;
					}
				}
			}
		}

		//If the building doesn't belong the buildings allowed in border, then we're free to place it without any limitations:
		return true;
	}

	public void CheckBuildingsInBorder ()
	{
		if (GameManager.MultiplayerGame == false || (GameManager.MultiplayerGame == true && GameManager.PlayerFactionID == gameObject.GetComponent<Building> ().FactionID)) {

			int FactionTypeID = GameMgr.GetFactionTypeID (GameMgr.Factions [gameObject.GetComponent<Building> ().FactionID].Code); //Get the faction type ID depending on the code presented with this faction.
			//This checks if there are buildings that are faction type specific and remove/keep them based on the faction code:
			//Loop through all the buildings inside the array:
			if (BuildingsInsideBorder.Count > 0) {
				int i = 0;
				while (i < BuildingsInsideBorder.Count) {
					//When a building has 
					if (BuildingsInsideBorder [i].FactionCode != "") {
						if (FactionTypeID != GameMgr.GetFactionTypeID (BuildingsInsideBorder [i].FactionCode)) { //if the faction code is different
							BuildingsInsideBorder.RemoveAt (i);
						} else {
							i++;
						}
					} else {
						i++;
					}
				}
			}
		}
	}


	/*
	//The method below allows us to determine whether a resource type is inside a building center's borders or not.
	//It is used when placing when the building center when it's preferable to pick a place that includes a resource type that we're looking for.
	Resource LastSearchedResource;
	public bool ResourceExistsInsideBorder(string ResourceName)
	{
		//Since the method is called multiple times, then we will try to save some memory, everytime we find the border include the wanted resource type, we will store it in "LastSearchedResource".
		//When the mehtod is called again, we will check if the last found resource is still inside the center's borders or not.
		if (LastSearchedResource != null) {
			//If the last searched resource leaves the border:
			if (Vector3.Distance (LastSearchedResource.transform.position, this.transform.position) > Size) {
				LastSearchedResource = null; //Setting this to null will allow us to search for another resource of the same type inside the border, below;
			} else {
				return true; //If the same last searched resource is still inside the building's borders then we will simply return true letting the building manager know that the requested resource type is inside the building.		}
			}
		}

		//If we didn't already return true above, then we're going to search for the requested resource type:
		if (LastSearchedResource == null) {
			//We will first loop through all available resources inside the map:
			if (GameManager.GameMgr.AllResources.Length > 0) {
				for (int i = 0; i < GameManager.GameMgr.AllResources.Length; i++) {
					//Make sure we're looking for a resource with the same name:
					if (GameManager.GameMgr.AllResources [i].Name == ResourceName) {
						//Once we find a resource inside the upcoming borders...
						if (Vector3.Distance (GameManager.GameMgr.AllResources [i].transform.position, this.transform.position) < Size) {
							LastSearchedResource = GameManager.GameMgr.AllResources [i];
							return true;
							//Return true and set the last searched resource to this resource:

						}
					}
				}
			}
		}

		//If all fails, let the building manager know that we couldn't find the requested resource around the building's border:
		return false;
	}*/

}
