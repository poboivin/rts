using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

/* Building Placement script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

public class BuildingPlacement : MonoBehaviour {

	public List<Building> AllBuildings;

	//The height at which the player is allowed to place buildings on the map.
	public float MinHeight = 0.01f; 
	public float MaxHeight = 6.0f;
	public float BuildingYOffset = 0.01f; //The value added to the building position on the Y axis so that the building object is not stuck inside the terrain.

	[HideInInspector]
	public Building CurrentBuilding; //Holds the current building to place info.
	public LayerMask TerrainMask; //Terrain layer mask.

	public static bool IsBuilding = false;

	public float SelectionFlashTime = 2.0f; //This is the total time at which the building/resource selection texture will be chosen after the player sent units to construct/collect it.
	public Color SelectionFlashColor = Color.green; //The color of the building/resource selection texture when it starts flashing.
	public float SelectionFlashRepeat = 0.2f; //The selection texture flash repeat time.

	//Key to keep spawning buildings:
	public bool HoldAndSpawn = false;
	public KeyCode HoldAndSpawnKey = KeyCode.LeftShift;
	[HideInInspector]
	public int LastBuildingID;

	public Building[] FreeBuildings; //buildings that don't belong to any faction here.
	public Color FreeBuildingSelectionColor = Color.black;

	public bool BuildingsInsideBorders = true; //if false, the player will be able to place buildings t

	public AudioClip SendToBuildAudio; //This audio is played when the player sends a group of units to fix/construct a building.
	public AudioClip PlaceBuildingAudio; //Audio played when a building is placed.

	//Scripts:
	ResourceManager ResourceMgr;
	SelectionManager SelectionMgr;
	GameManager GameMgr;

	
	void Awake () 
	{
		IsBuilding = false; 

		//find the scripts that we will need:
		GameMgr = GameManager.Instance;
		ResourceMgr = GameMgr.ResourceMgr;
		SelectionMgr = GameMgr.SelectionMgr;
	}

	void Start ()
	{
		//activate the free units:
		if (FreeBuildings.Length > 0 && GameManager.MultiplayerGame == false) {
			for (int i = 0; i < FreeBuildings.Length; i++) {
				FreeBuildings [i].gameObject.SetActive (true);
			}
		}
	}


	void Update () 
	{
	    if(CurrentBuilding != null) //If we are currently attempting to place a building on the map
		{
			IsBuilding = true; //it means we are informing other scripts that we are placing a building.

			//using a raycheck, we will make the building to place, follow the mouse position and stay on top of the terrain.
			Ray RayCheck = Camera.main.ScreenPointToRay (Input.mousePosition);

			RaycastHit[] Hits;
			Hits = Physics.RaycastAll (RayCheck, 100.0f);

			if (Hits.Length > 0) {
				for (int i = 0; i < Hits.Length; i++) {
					int TerrainLayer = LayerMask.NameToLayer ("Terrain");
					if(Hits[i].transform.gameObject.layer == TerrainLayer)
					{
						//depending on the height of the terrain, we will place the building on it.
						Vector3 BuildingPos = Hits[i].point;
						//make sure that the building position on the y axis stays inside the min and max height interval:
						if (BuildingPos.y < MinHeight) {
							BuildingPos.y = MinHeight + BuildingYOffset; 
						} else if (BuildingPos.y > MaxHeight) {
							BuildingPos.y = MaxHeight + BuildingYOffset; 
						} else {
							BuildingPos.y += BuildingYOffset; 
						}
						if (CurrentBuilding.transform.position != BuildingPos) {
							CurrentBuilding.NewPos = true; //inform the building's comp that we have moved it so that it checks whether the new position is suitable or not.
						}
						CurrentBuilding.transform.position = BuildingPos; //set the new building's pos.
					}
				}
			}
			if(Input.GetMouseButtonDown(1)) //If the player preses the right mouse button.
			{
				//Abort the building process
				Destroy(CurrentBuilding.gameObject);
				CurrentBuilding = null;

				IsBuilding = false;

				//Show the tasks again for the builders again:
				if (SelectionMgr.SelectedUnits.Count > 0) {
					SelectionMgr.UIMgr.UpdateUnitTasks ();
				}
			}
			else if(CurrentBuilding.CanPlace == true) //If the player can place the building at its current position:
			{
				if(Input.GetMouseButtonUp(0)) //If the player preses the left mouse button
				{
					if(CheckBuildingResources(CurrentBuilding) == true) //Does the player's team have all the required resources to build this building
					{
						PlaceBuilding (); //place the building.

						//if holding and spawning is enabled and the player is holding the right key to do that:
						if(HoldAndSpawn == true && Input.GetKey (HoldAndSpawnKey))
						{
							//start placing the same building again
							StartPlacingBuilding (LastBuildingID);
						}
						//Show the tasks again for the builders again:

						if (SelectionMgr.SelectedUnits.Count > 0 && IsBuilding == false) {
							SelectionMgr.UIMgr.UpdateUnitTasks ();
						}
					}
					else
					{
						//Inform the player that he doesn't have enough resources.
						//SEND MSG.
					}
				}
			}
		}
		else
		{
			if(IsBuilding == true) IsBuilding = false;
		}
	}

	//the method that allows us to place the building
	void PlaceBuilding ()
	{
		if (GameManager.MultiplayerGame == false) { //if it's a single player game.
			//enable the nav mesh obstacle comp
			if (CurrentBuilding.NavObs) {
				CurrentBuilding.NavObs.enabled = true;
			}

			TakeBuildingResources (CurrentBuilding); //Remove the resources needed to create the building.

			//if the building includes a border comp, then enable it as well
			if (CurrentBuilding.gameObject.GetComponent<Border> ())
				CurrentBuilding.gameObject.GetComponent<Border> ().enabled = true;
			
			GameManager.PlayerFactionMgr.AddBuildingToList (CurrentBuilding); //add the building to the faction manager list

			//Activate the player selection collider:
			CurrentBuilding.PlayerSelection.gameObject.SetActive (true);

			//Set the building's health to 0 so that builders can start adding health to it:
			CurrentBuilding.Health = 0.0f;

			CurrentBuilding.BuildingPlane.SetActive (false); //hide the building's plane

			//Make builders move to the building to construct it.
			if (SelectionMgr.SelectedUnits.Count > 0) {

				int i = 0; //counter
				bool MaxBuildersReached = false; //true when the maximum amount of builders for the hit building has been reached.
				while (i < SelectionMgr.SelectedUnits.Count && MaxBuildersReached == false) { //loop through the selected as long as the max builders amount has not been reached.
					if (SelectionMgr.SelectedUnits [i].BuilderMgr) { //check if this unit has a builder comp (can actually build).
						//make sure that the maximum amount of builders has not been reached:
						if(CurrentBuilding.CurrentBuilders.Count < CurrentBuilding.MaxBuilders)
						{
							//Make the units fix/build the building:
							SelectionMgr.SelectedUnits [i].BuilderMgr.SetTargetBuilding (CurrentBuilding);
						}
						else
						{
							MaxBuildersReached = true;
							//if the max builders amount has been reached.
							//Show this message: 
							GameMgr.UIMgr.ShowPlayerMessage ("Max building amount for building has been reached!", UIManager.MessageTypes.Error);
						}
					}

					i++;
				}
			}

			//Building is now placed:
			CurrentBuilding.Placed = true;
			//custom event:
			if(GameMgr.Events) GameMgr.Events.OnBuildingPlaced(CurrentBuilding);
			CurrentBuilding.ToggleConstructionObj (true); //Show the construction object when the building is placed.

			if(BuildingsInsideBorders == true)
			{
				CurrentBuilding.CurrentCenter.RegisterBuildingInBorder (CurrentBuilding); //register building in the territory that it belongs to.
			}

			CurrentBuilding = null;
		} else { //in case it's a multiplayer game:

			TakeBuildingResources (CurrentBuilding); //Remove the resources needed to create the building.
			//ask the server to spawn the building for all clients:
			GameMgr.Factions[GameManager.PlayerFactionID].MFactionMgr.TryToSpawnBuilding(CurrentBuilding.Code, false, CurrentBuilding.transform.position, false);
			Destroy (CurrentBuilding.gameObject);

			CurrentBuilding = null;
		}

		IsBuilding = false;
		//Show the tasks panel after placing the building:
		AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, PlaceBuildingAudio, false);
	}

	//This checks if we have enough resources to build this building or not.
	public bool CheckBuildingResources (Building CheckBuilding)
	{
		if(CheckBuilding.BuildingResources.Length > 0)
		{
			for(int i = 0; i < CheckBuilding.BuildingResources.Length; i++) //Loop through all the requried resources:
			{
				//Check if the team resources are lower than one of the demanded amounts:
				if(ResourceMgr.GetResourceAmount(GameManager.PlayerFactionID,CheckBuilding.BuildingResources[i].Name) < CheckBuilding.BuildingResources[i].Amount)
				{
					return false; //If yes, return false.
				}
			}
			return true; //If not, return true.
		}
		else //This means that no resource are required to build this building.
		{
			return true;
		}
	}

	//a method that takes the buildings resources.
	public void TakeBuildingResources (Building CheckBuilding)
	{
		if(CheckBuilding.BuildingResources.Length > 0) //If the building requires resources:
		{
			for(int i = 0; i < CheckBuilding.BuildingResources.Length; i++) //Loop through all the requried resources:
			{
				//Remove the demanded resources amounts:
				ResourceMgr.AddResource(GameManager.PlayerFactionID, CheckBuilding.BuildingResources[i].Name, -CheckBuilding.BuildingResources[i].Amount);
			}
		}
	}

	public void StartPlacingBuilding (int BuildingID)
	{
		//make sure we have enough resources
		if (CheckBuildingResources(AllBuildings [BuildingID].GetComponent<Building>()) == true) {
			//Spawn the building for the player to place on the map:
			GameObject BuildingClone = (GameObject)Instantiate (AllBuildings [BuildingID].gameObject, new Vector3 (0, 0, 0), Quaternion.identity);
			LastBuildingID = BuildingID;
			BuildingClone.gameObject.GetComponent<Building> ().FactionID = GameManager.PlayerFactionID;

			//Set the position of the new building:
			RaycastHit Hit;
			Ray RayCheck = Camera.main.ScreenPointToRay (Input.mousePosition);

			//Disable the building's collider when placing it:
			//if(BuildingClone.GetComponent<Collider>()) 

			if (Physics.Raycast (RayCheck, out Hit)) {  
				Vector3 BuildingPos = Hit.point;
				BuildingPos.y = MinHeight + BuildingYOffset; 
				BuildingClone.transform.position = BuildingPos;
			}

			IsBuilding = true;
			CurrentBuilding = BuildingClone.GetComponent<Building> ();
			if (BuildingClone.gameObject.GetComponent<UnityEngine.AI.NavMeshObstacle> ()) {
				BuildingClone.gameObject.GetComponent<UnityEngine.AI.NavMeshObstacle> ().enabled = false;
			}

			GameMgr.UIMgr.HideTaskButtons ();
		}
		else {
			//Inform the player that he can't place this building because he's lacking resources.
			GameMgr.UIMgr.ShowPlayerMessage ("Not enough resources to launch task!", UIManager.MessageTypes.Error);
		}
	}

	//replace an existing building in the all building list that the faction can spawn with another building (usually after having an age upgrade).
	public void ReplaceBuilding(string Code, Building NewBuilding)
	{
		if (Code == NewBuilding.Code) //if both buildings have the same codes.
			return;
		
		if (AllBuildings.Count > 0) { //go through all the buildings in the list
			int i = 0;
			bool Found = false;
			while (i < AllBuildings.Count && Found == false) {
				if (AllBuildings [i].gameObject.GetComponent<Building> ().Code == Code) { //when the building is found
					AllBuildings.RemoveAt (i);//remove it
					if (!AllBuildings.Contains (NewBuilding)) { //make sure we don't have the same building already in the list
						AllBuildings.Insert (i,NewBuilding); //place the new building in the same position
					}
					Found = true;
				}
				i++;
			}
		}
	}

}
