using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;

/* Resource Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

public class ResourceManager : MonoBehaviour {

	public GameObject ResourcesParent; //All resources must be placed as children of the same object.
	[System.Serializable]
	//This array appears in the inspector, it's where you can create the resources types:
	public class ResourcesVars
	{
		public string Name; //Resrouce name

		public int StartingAmount; //The amount that each team will start with.
		[HideInInspector]
		public int Amount; //The current amount of this resource.
		public Sprite Icon; //Resource Icon.

		//UI:
		//show UI:
		public bool ShowUI = true; //show this resource in the dash board? 

		public Image UIImage; //Resource UI image
		public Text UIText; //Resource UI text to display the resource amount.

		//NPC Resource Tasks:
		[HideInInspector]
		public int TargetAmount = 0; //the target amount that the faction wants to reach.
		[HideInInspector]
		public int LastCenterID = 0; //Whenever a resource is missing, we start searching for it from a city center. This variable holds the last ID of the city center that we started the search from.
	
		//Audio clips:
		public AudioClip SelectionAudio; //Audio played when the player selects this resource.
		public AudioClip SendToCollectAudio; //Audio played when the player sends a group of units to collect from this resource.
		public AudioClip[] CollectionAudio; //Audio played each time the unit collects some of this resource.
	}
	public ResourcesVars[] ResourcesInfo;

	public bool AutoCollect = true; //Collect resources automatically when true. if false, the unit must drop off the collected resources each time at a building that allow that.

	//Selection color the resources:
	public Color ResourceColor;

	//This array doesn't appear in the inspector, its values are set by the game manager depending on the number of teams:
	[System.Serializable]
	public class FactionResourcesVars
	{
		public ResourcesVars[] ResourcesTypes; //For each team, we'll associate all the resources types.
		public float NeedRatio = 1.0f;
	}
	[HideInInspector]
	public FactionResourcesVars[] FactionResourcesInfo;

	public float SelectionFlashTime = 1.0f; //This is the total time at which the building/resource selection texture will be chosen after the player sent units to construct/collect it.
	public Color SelectionFlashColor = Color.green; //The color of the building/resource selection texture when it starts flashing.
	public float SelectionFlashRepeat = 0.2f; //The selection texture flash repeat time.

	//Resources class:
	[System.Serializable]
	public class Resources
	{
		public string Name;
		public int Amount;
	}

	//Spawn resource list:
	[System.Serializable]
	public class SpawnResourcesVars
	{
		public GameObject Prefab;
		public Vector3 Pos;
		public Quaternion Rot;
		public bool Hidden = false;
	}
	[HideInInspector]
	public List<SpawnResourcesVars> SpawnResources = new List<SpawnResourcesVars>();
	//All resources:
	[HideInInspector]
	public List<Resource> AllResources = new List<Resource>();
	//scene resources:
	[HideInInspector]
	public Resource[] SceneResources;

	//in order to set the faction IDs of resources after they spawn, we need to register the amount of resources we have in the scene in ..
	[HideInInspector]
	public int ResourcesAmount;
	//then when the spawned resources amount reaches the value above, all centers will look to set the faction IDs for resources.

	[HideInInspector]
	public GameManager GameMgr;

	//a method that adds amount to a faction's resources.
	public void AddResource(int FactionID, string Name, int Amount)
	{
		int ResourceID = GetResourceID (Name);
		if(ResourceID >= 0) //Checking if the resource ID is valid.
		{
			//Add the resource amount.
			FactionResourcesInfo[FactionID].ResourcesTypes[ResourceID].Amount += Amount;
			if(FactionID == GameManager.PlayerFactionID) UpdateResourcesUI(); //Update the resource UI if the team ID corressponds to the team controlled by the player.
		}
		else
		{
			return;
		}
	}

	public int GetResourceAmount (int FactionID, string Name)
	{
		int ResourceID = GetResourceID (Name);
		if(ResourceID >= 0) //Checking if the resource ID is valid.
		{
			//Get the resource amount
			return FactionResourcesInfo[FactionID].ResourcesTypes[ResourceID].Amount;
		}
		else
		{
			return -1;
		}
	}

	public int GetResourceID(string Name)
	{
		//Search for the resource ID using its name.
		for(int i = 0; i < ResourcesInfo.Length; i++)
		{
			if(ResourcesInfo[i].Name == Name)
			{
				return i;
			}
		}
		return -1;
	}

	public void UpdateResourcesUI()
	{
		//Update the resources UI:
		for(int i = 0; i < ResourcesInfo.Length; i++)
		{
			if (ResourcesInfo [i].ShowUI == true) {
				if (ResourcesInfo [i].UIImage)
					ResourcesInfo [i].UIImage.sprite = ResourcesInfo [i].Icon;
				if (ResourcesInfo [i].UIText && GameManager.PlayerFactionID >= 0) {
					ResourcesInfo [i].UIText.text = FactionResourcesInfo [GameManager.PlayerFactionID].ResourcesTypes [i].Amount.ToString ();
				}
			}
		}
	}

	//Check resources:
	//This checks if the faction have enough resources to launch a task:
	public bool CheckResources (ResourceManager.Resources[] RequiredResources, int FactionID, float Ratio)
	{
		if(RequiredResources.Length > 0)
		{
			for(int i = 0; i < RequiredResources.Length; i++) //Loop through all the requried resources:
			{
				//Check if the team resources are lower than one of the demanded amounts:
				if(GetResourceAmount(FactionID,RequiredResources[i].Name) < RequiredResources[i].Amount*Ratio)
				{
					return false; //If yes, return false.
				}
			}
			return true; //If not, return true.
		}
		else //This means that no resource are required.
		{
			return true;
		}
	}

	//this method gives back the resource of a task to the faction:
	public void GiveBackResources (ResourceManager.Resources[] RequiredResources, int FactionID)
	{
		if(RequiredResources.Length > 0)
		{
			for(int i = 0; i < RequiredResources.Length; i++) //Loop through all the requried resources:
			{
				//Give back the resources:
				AddResource(FactionID, RequiredResources[i].Name, RequiredResources[i].Amount);
			}
		}
	}

	//this method takes the resources of a task from a faction
	public void TakeResources (ResourceManager.Resources[] RequiredResources, int FactionID)
	{
		if(RequiredResources.Length > 0) //If the building requires resources:
		{
			for(int i = 0; i < RequiredResources.Length; i++) //Loop through all the requried resources:
			{
				//Remove the demanded resources amounts:
				AddResource(FactionID, RequiredResources[i].Name, -RequiredResources[i].Amount);
			}
		}
	}

	//loading resources in multiplayer and singleplayer games:

	void Start ()
	{
		ResourcesAmount = SceneResources.Length; //set the total resources amount in this map.

		DestroySceneResources (); //first destroy all resources in scene because we will spawn them cleaner again
		if (GameManager.MultiplayerGame == false) { //if it's a single player game
			//Spawn all resources in list:
			SpawnResourcesInList ();
		}
	}

	public void DestroySceneResources ()
	{
		//destroy all scene resources:
		if (SceneResources.Length > 0) {
			//loop through all resources
			for (short i = 0; i < SceneResources.Length; i++) {
				DestroyImmediate (SceneResources [i].gameObject);
			}
		}
	}

	public void SpawnResourcesInList ()
	{
		if (SpawnResources.Count > 0) {
			foreach (SpawnResourcesVars ResourcePrefab in SpawnResources) {
				GameObject SpawnedResource = Instantiate (ResourcePrefab.Prefab);
				SpawnedResource.transform.position = ResourcePrefab.Pos;
				SpawnedResource.transform.rotation = ResourcePrefab.Rot;

				SpawnedResource.GetComponent<Resource> ().FactionID = -1;

				if (GameManager.MultiplayerGame == true) {
					NetworkServer.Spawn (SpawnedResource);
				}
			}
		}
	}

	//register the resource in this map:
	public void RegisterResource (Resource NewResource)
	{
		//set the resource ID and add it to the list:
		NewResource.ID =AllResources.Count;
		AllResources.Add (NewResource);

		//if all resources are spawned:
		if (AllResources.Count == ResourcesAmount) {
			//ask spawned building centers (borders) to check for resources in order to set their faction ID:

			//go through all factions:
			if (GameMgr.Factions.Count > 0) {
				for (int i = 0; i < GameMgr.Factions.Count; i++) {
					if (GameMgr.Factions [i].FactionMgr.BuildingCenters.Count > 0) { //if the current faction actually has building centers
						foreach (Building Center in GameMgr.Factions [i].FactionMgr.BuildingCenters) {
							Center.CurrentCenter.CheckBorderResources (); //reload the border resources here.
						}
					}
				}
			}
		}
	}
}
