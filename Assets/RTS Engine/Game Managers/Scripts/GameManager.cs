using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

/* Game Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

public class GameManager : MonoBehaviour {

	public static GameManager Instance = null;
	public string MainMenuScene = "Menu"; //Main menu scene name, this is the scene that will be loaded when the player decides to leave the game.

	[HideInInspector]
	public bool GameEnded = false; //Has the game ended or not?

	[System.Serializable]
	public class FactionDefVars
	{
		public string Code = "faction0"; //A unique code for each faction.
		public string Name = "Faction0";
		public Building[] ExtraBuildings; //A list of extra buildings that only this faction can place. 
		//for NPC factions, if one of the buildings below is unique for the faction, it must be precised (leave empty if the faction don't have special forms of these buildings)
		public Building CapitalBuilding;
		public Building BuildingCenter;
		public Building PopulationBuilding;
		public Building DropOffBuilding;
	}
	public FactionDefVars[] FactionDef; //list of factions that can possibly play in this map.

	[System.Serializable]
	//The array that holds all the current teams information.
	public class FactionInfo
	{
		public string Name; //Faction's name.
		public string Code; //Type of this faction (the type determines which extra buildings/units can this faction use). 
		[HideInInspector]
		public string TypeName;
		[HideInInspector]
		public int TypeID;
		public Color FactionColor; //Faction's color.
		public bool PlayerControl = false; //Is the team controlled by the player, make sure that only one team is controlled by the player.

		public int MaxPopulation; //Maximum number of units that can be present at the same time (which can be increased in the game by constructing certain buildings)
		public int CurrentPopulation; //Current number of spawned units.

		public Building CapitalBuilding; //The capital building that MUST be placed in the map before startng the game.
		public Vector3 CapitalPos; //The capital building's position is stored in this variable because when it's a new multiplayer game, the capital buildings are re-spawned in order to be synced in all players screens.
		public FactionManager FactionMgr; //The faction manager is a component that stores the faction data. Each faction is required to have one.

		public bool Lost = false; //true when the faction is defeated and can no longer have an impact on the game.

		//For multiplayer games purpose:
		public MFactionLobbyInfo MFactionLobby; //This is the component that holds the basic information of the network player's faction (faction name, color)
		public MFactionManager MFactionMgr; //This component is the one that handles communication between the local player and the server.
	}
	public List<FactionInfo> Factions = new List<FactionInfo>();
	public bool RandomizePlayerControl = true;

	private int ActiveFactions = 0; //Amount of spawned factions;

	//The stopping distance when a unit moves to an empty space of the map:
	public float MvtStoppingDistance = 1.5f;

	//Peace time:
	public float PeaceTime = 60.0f; //Time (in seconds) after the game starts, when no faction can attack the other.

	public static int PlayerFactionID; //Faction ID of the team controlled by the player.
	public static FactionManager PlayerFactionMgr; //The faction manager component of the faction controlled by the player.

	//Borders:
	[HideInInspector]
	public int LastBorderSortingOrder = 0; //In order to draw borders and show which order has been set before the other, their objects have different sorting orders.
	[HideInInspector]
	public List<Border> AllBorders; //All the borders in the game are stored in this game.

	//Other scripts:
	[HideInInspector]
	public ResourceManager ResourceMgr;
	[HideInInspector]
	public UIManager UIMgr;
	[HideInInspector]
	public CameraMovement CamMov;
	[HideInInspector]
	public BuildingPlacement BuildingMgr;
	[HideInInspector]
	public SelectionManager SelectionMgr;
	NetworkMapManager NetworkMgr;
	[HideInInspector]
	public MapManager MapMgr;
	[HideInInspector]
	public CustomEvents Events;
	[HideInInspector]
	public TaskManager TaskMgr;
	[HideInInspector]
	public BuildingPlacement PlacementMgr;
	[HideInInspector]
	public UnitManager UnitMgr;
	[HideInInspector]
	public AttackObjsPooling AttackObjsPool;

	//Map size:
	public float MapSize = 150; //The approximate size of the map.

	public static bool MultiplayerGame = false; //If it's a multiplayer game, this will be true.
	public MFactionLobbyInfo[] MFactionsInfo; //This array stores all the faction lobby info scripts in a multiplayer game.

	public AudioSource GeneralAudioSource; //The audio source where audio will be played generally unless the audio is local. In that case, it will be played 

	public Color AttackSelectionColor = Color.red; //The color of the selection texture that will be flashed around the target unit/building that was chosen by the player to attack.


	void Awake () 
	{
		//set the instance:
		if (Instance == null) {
			Instance = this;
		} else if(Instance != this) {
			Destroy (gameObject);
		}

		//Randomize player controlled faction:
		RandomizePlayerFaction ();

		Time.timeScale = 1.0f; //In case it was not 1.0f in a previous scene.

		CamMov = FindObjectOfType (typeof(CameraMovement)) as CameraMovement; //Find the camera movement script.
		ResourceMgr = FindObjectOfType (typeof(ResourceManager)) as ResourceManager; //Find the resource manager script.
		if (ResourceMgr != null)
			ResourceMgr.GameMgr = this;
		UIMgr = FindObjectOfType (typeof(UIManager)) as UIManager; //Find the UI manager script.
		BuildingMgr = FindObjectOfType(typeof(BuildingPlacement)) as BuildingPlacement;
		Events = FindObjectOfType (typeof(CustomEvents)) as CustomEvents;
		TaskMgr = FindObjectOfType (typeof(TaskManager)) as TaskManager;
		if (TaskMgr != null)
			TaskMgr.GameMgr = this;
		UnitMgr = FindObjectOfType (typeof(UnitManager)) as UnitManager;
		SelectionMgr = FindObjectOfType (typeof(SelectionManager)) as SelectionManager;
		PlacementMgr = FindObjectOfType (typeof(BuildingPlacement)) as BuildingPlacement;
		AttackObjsPool = FindObjectOfType (typeof(AttackObjsPooling)) as AttackObjsPooling; //attack object pooling

		MultiplayerGame = false; //We start by assuming it's a simple single player game.

		//First check if there's a network manager component in the scene:
		NetworkMgr = FindObjectOfType(typeof(NetworkMapManager)) as NetworkMapManager;
		if (NetworkMgr != null) { //If there's actually a network map manager, it means that the map was loaded from the multiplayer menu, meaning that this is a MP game.
			ClearNPCManagers (); //clearing all the npc components in the map since it's a MP game.
			MultiplayerGame = true; //we now recongize that this a multiplayer game.

			//First we will search for all the lobby objects that held the info for the factions
			MFactionsInfo = FindObjectsOfType(typeof(MFactionLobbyInfo)) as MFactionLobbyInfo[];

			//This where we will set the settings for all the players:
			//First check if we have enough faction slots available:
			if (MFactionsInfo.Length <= Factions.Count) {
				//Loop through all the current factions and set up each faction slot:
				for (int i = 0; i < MFactionsInfo.Length; i++) {

					MFactionLobbyInfo ThisFaction = NetworkMgr.lobbySlots [i].GetComponent<MFactionLobbyInfo> (); //this is the faction info that we will get from the faction lobby info.


					//Set the info for the factions that we will use:
					Factions [i].Name = ThisFaction.FactionName; //get the faction name
					Factions [i].FactionColor = ThisFaction.FactionColor; //the faction color
					//get the initial max population from the network manager (making it the same for all the players).
					Factions [i].MaxPopulation = NetworkMgr.Maps[ThisFaction.MapID].InitialPopulation; 
					Factions [i].Lost = false;

					Factions [i].MFactionLobby = ThisFaction; //linking the faction with its lobby info script.
					Factions [i].CapitalPos = Factions [i].CapitalBuilding.transform.position; //setting the capital pos to spawn the capital building object at later.

					Factions [i].FactionMgr = Factions [i].FactionMgr; //linking the faction with its faction manager.

					Factions [i].Code = ThisFaction.FactionCode;

					//Setting the local player faction ID:
					if (ThisFaction.isLocalPlayer) { //isLoclPlayer determines which lobby faction info script is owned by the player..
						//therefore the faction linked to that script is the player controlled one.
						PlayerFactionID = i;
						Factions [i].PlayerControl = true;
						PlayerFactionMgr = Factions [i].FactionMgr;
					
					} else {
						//all other factions will be defined as NPC but in the reality, they are controlled by other players through the network.
						Factions [i].PlayerControl = false;
					}


				}

				//loop through all the factions and destroy the default capital buildings because the server will spawn new ones for each faction.
				for (int i = 0; i < Factions.Count; i++) {
					DestroyImmediate (Factions [i].CapitalBuilding.gameObject);
				}

				//if there are more slots than required.
				while (MFactionsInfo.Length < Factions.Count) {
					//remove the extra slots:
					Factions.RemoveAt(Factions.Count-1);
				}

					
			} else {
				Debug.LogError ("Not enough slots available for all the factions!");
			}
		}

		MapMgr = FindObjectOfType (typeof(MapManager)) as MapManager; //search for the map manager script.
		//If there's a map manager script in the scene, it means that we just came from the single player menu, so we need to set the NPC players settings!
		if (MapMgr != null) { 


			//This where we will set the NPC settings using the info from the map manager:
			//First check if we have enough faction slots available:
			if (MapMgr.Factions.Count <= Factions.Count) {
				ClearNPCManagers (); //remove the current npc managers as they will be replaced by other ones.

				//loop through the factions slots of this map:
				for (int i = 0; i < MapMgr.Factions.Count; i++) {

					//Set the info for the factions that we will use:
					Factions [i].Name = MapMgr.Factions[i].FactionName; //name
					Factions [i].FactionColor = MapMgr.Factions [i].FactionColor; //color
					Factions [i].PlayerControl = MapMgr.Factions [i].ControlledByPlayer; //is this faction controlled by the player? 
					Factions [i].MaxPopulation = MapMgr.Factions [i].InitialPopulation; //initial maximum population (which can be increased in the game).
					Factions [i].Code = MapMgr.Factions[i].FactionCode; //the faction's code.
					Factions [i].CapitalPos = Factions [i].CapitalBuilding.transform.position; //setting the capital pos to spawn the capital building object at later.


					Factions [i].Lost = false;

					int FactionTypeID = GetFactionTypeID (Factions [i].Code);
					if (FactionTypeID >= 0 && FactionDef [FactionTypeID].CapitalBuilding != null) { //if the faction to a certain type
						DestroyImmediate (Factions [i].CapitalBuilding.gameObject); //destroy the default capital and spawn another one:

						//we will spawn the capital building and remove the one that already came in the scene:
						GameObject Capital = Instantiate (FactionDef[FactionTypeID].CapitalBuilding.gameObject);

						//set the capital's settings:
						Capital.GetComponent<Building> ().FactionID = i;
						Capital.GetComponent<Building> ().FactionCapital = true;
						Capital.GetComponent<Building> ().PlacedByDefault = true;

						Capital.transform.position = Factions [i].CapitalPos; //set the capital's position on the map.
						Factions [i].CapitalBuilding = Capital.GetComponent<Building> ();
					}

					//if this faction not controlled by the player
					if (Factions [i].PlayerControl == false) {
						//Spawn the NPC managers setinngs for this faction:
						GameObject NPCMgrObj = (GameObject) Instantiate(MapMgr.DifficultyLevels[MapMgr.Factions[i].NPCDifficulty], Vector3.zero, Quaternion.identity);

						//NPC Army manager:
						NPCMgrObj.GetComponent<NPCArmy> ().FactionID = i;
						NPCMgrObj.GetComponent<NPCArmy> ().FactionMgr = Factions [i].FactionMgr;
						Factions [i].FactionMgr.ArmyMgr = NPCMgrObj.GetComponent<NPCArmy> ();

						//NPC Building placement manager:
						NPCMgrObj.GetComponent<NPCBuildingPlacement> ().FactionID = i;
						NPCMgrObj.GetComponent<NPCBuildingPlacement> ().FactionMgr = Factions [i].FactionMgr;
						Factions [i].FactionMgr.BuildingMgr = NPCMgrObj.GetComponent<NPCBuildingPlacement> ();

						//NPC Resource manager:
						NPCMgrObj.GetComponent<NPCResource> ().FactionID = i;
						NPCMgrObj.GetComponent<NPCResource> ().FactionMgr = Factions [i].FactionMgr;
						Factions [i].FactionMgr.ResourceMgr = NPCMgrObj.GetComponent<NPCResource> ();

						//NPC Unit spawner: (optional)
						if (NPCMgrObj.GetComponent<NPCUnitSpawner> ()) {
							NPCMgrObj.GetComponent<NPCUnitSpawner> ().FactionID = i;
							NPCMgrObj.GetComponent<NPCUnitSpawner> ().FactionMgr = Factions [i].FactionMgr;
							Factions [i].FactionMgr.UnitSpawner = NPCMgrObj.GetComponent<NPCUnitSpawner> ();
						}
					}
				}

				//if there are more slots than required.
				while (MapMgr.Factions.Count < Factions.Count) {
					//remove the extra slots:
					DestroyImmediate (Factions [Factions.Count-1].CapitalBuilding.gameObject);
					Factions.RemoveAt(Factions.Count-1);
				}
					
			} else {
				Debug.LogError ("Not enough slots available for all the factions!");
			}

			//Destroy the map manager script because we don't really need it anymore:
			DestroyImmediate (MapMgr.gameObject);
		}
			
		//If it's a multiplayer game, we still need to figure what's the local player faction ID.
		if(MultiplayerGame == false) PlayerFactionID = -1;


	    if(Factions.Count > 0)
		{

			//Create as many resource info slots as the amount of the spawned factions.
			ResourceMgr.FactionResourcesInfo = new ResourceManager.FactionResourcesVars[Factions.Count];

			//Loop through all the factions:
			for(int i = 0; i < Factions.Count; i++)
			{
				//if it's not a multiplayer game:
				if (MultiplayerGame == false) {
					//and this faction is controlled by the player:
					if (Factions [i].PlayerControl == true) {
						//then define this as the player faction:
						PlayerFactionID = i;
						PlayerFactionMgr = Factions [i].FactionMgr;
					}
				}

				//Only if it's a single player game we will be checking if the capital buildings have spawned, because in multiplayer, another script handles spawning these
				if (MultiplayerGame == false) {
					if (Factions [i].CapitalBuilding == null) {
						Debug.LogError ("Faction ID: " + i + " is missing the 'Capital Building'");
					} else {
						Factions [i].CapitalBuilding.FactionID = i;
						Factions [i].CapitalBuilding.FactionCapital = true;
					}
				}
					

				ResourceMgr.FactionResourcesInfo[i] = new ResourceManager.FactionResourcesVars();
				//Associate each team with all available resources:
				ResourceMgr.FactionResourcesInfo[i].ResourcesTypes = new ResourceManager.ResourcesVars[ResourceMgr.ResourcesInfo.Length]; 

				//Loop through all the available resources and define them for each team.
				for(int j = 0; j < ResourceMgr.FactionResourcesInfo[i].ResourcesTypes.Length; j++)
				{
					ResourceMgr.FactionResourcesInfo[i].ResourcesTypes[j] = new ResourceManager.ResourcesVars();
				
					ResourceMgr.FactionResourcesInfo[i].ResourcesTypes[j].Name = ResourceMgr.ResourcesInfo[j].Name; //Name of the resource
					ResourceMgr.FactionResourcesInfo[i].ResourcesTypes[j].Amount = ResourceMgr.ResourcesInfo[j].StartingAmount; //Starting amount of the resource for each team.
				}

			}
		}

		ResourceMgr.UpdateResourcesUI (); //right after setting up the resource settings above, refresh the resource UI.

		//In order to avoid having buildings that are being placed by AI players and units collide, we will ignore physics between their two layers:
		Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Hidden"), LayerMask.NameToLayer("Unit"));
		//Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Unit"), LayerMask.NameToLayer("Unit"));

		if (PeaceTime <= 0.0f) {
			//If there's no peace make factions pick their targets early:
			SetFactionTargets ();
		}

		//Set the amount of the active factions:
		ActiveFactions = Factions.Count;


		GameEnded = false; //false, of course, we just started.
	}

	void Start ()
	{
		/*because the building's main compoment is a network behaviour. The building centers, already present in the scene when it loads, will be disabled in an offline 
		 * game because their NetworkIdentity components can not connect to a server. Therefore, we need to enable under "Start ()".
		 * */
		if(Factions.Count > 0)
		{
			for(int i = 0; i < Factions.Count; i++)
			{
				if (Factions [i].CapitalBuilding != null) {
					Factions [i].CapitalBuilding.gameObject.SetActive (true);
				}

				int FactionTypeID = GetFactionTypeID(Factions[i].Code); //Get the faction type ID depending on the code presented with this faction.

				if (FactionTypeID >= 0) { //if this is a valid faction
					Factions [i].TypeName = FactionDef [FactionTypeID].Name; //get its name
				}

				//Depending on the faction type, add extra units/buildings (if there's actually any) to be created for each faction:
				if (MultiplayerGame == false || (MultiplayerGame == true && PlayerFactionID == i)) {
					if (FactionTypeID >= 0) { //if the code actually refers to a valid faction type

						Factions [i].TypeID = FactionTypeID;
						Factions[i].TypeName = FactionDef [FactionTypeID].Name;
						if (Factions [i].PlayerControl == true) { //if this faction is player controlled:
							if (FactionDef [FactionTypeID].ExtraBuildings.Length > 0) { //if the faction type has extra buildings:
								for (int j = 0; j < FactionDef [FactionTypeID].ExtraBuildings.Length; j++) { //go through them:
									BuildingMgr.AllBuildings.Add(FactionDef[FactionTypeID].ExtraBuildings[j]); //add the extra buildings so that this faction can use them.
								}
							}
						} else { //NPC players.
							if (FactionDef [FactionTypeID].ExtraBuildings.Length > 0) { //if the faction type has extra buildings:
								for (int j = 0; j < FactionDef [FactionTypeID].ExtraBuildings.Length; j++) { //go through them:
									Factions[i].FactionMgr.BuildingMgr.AllBuildings.Add(FactionDef[FactionTypeID].ExtraBuildings[j]); //add the extra buildings so that this faction can use them.
								}
							}

							//special buildings for NPC factions:
							if (FactionDef [FactionTypeID].BuildingCenter != null) {
								Factions [i].FactionMgr.BuildingMgr.BuildingCenter = FactionDef [FactionTypeID].BuildingCenter;
							}
							if (FactionDef [FactionTypeID].DropOffBuilding != null) {
								Factions [i].FactionMgr.BuildingMgr.DropOffBuilding = FactionDef [FactionTypeID].DropOffBuilding;
							}
							if (FactionDef [FactionTypeID].PopulationBuilding != null) {
								Factions [i].FactionMgr.BuildingMgr.PopulationBuilding = FactionDef [FactionTypeID].PopulationBuilding;
							}
						}
					}
				}
			}
		}


		//if it's not a MP game:
		if (MultiplayerGame == false) {

			//Set the player's initial cam position (looking at the faction's capital building):
			CamMov.LookAt (Factions [PlayerFactionID].CapitalBuilding.transform.position);
			CamMov.SetMiniMapCursorPos (Factions [PlayerFactionID].CapitalBuilding.transform.position);
		}
	}
		
	void Update () 
	{
	    //Peace timer:
		if (PeaceTime > 0) {
			PeaceTime -= Time.deltaTime;

			UIMgr.UpdatePeaceTimeUI (PeaceTime); //update the peace timer UI each time.
		}
		if (PeaceTime < 0) {
			//when peace timer is ended:
			PeaceTime = 0.0f;

			UIMgr.UpdatePeaceTimeUI (PeaceTime);

			//Make teams look for enemies after the peace time is over:
			SetFactionTargets ();
		}
	}

	//Randomize the order of the factions inside the faction order:
	private void RandomizePlayerFaction ()
	{
		if (RandomizePlayerControl == true) {
			for (int i = 0; i < Factions.Count; i++) {
				Building Capital = Factions [i].CapitalBuilding;
				int Target = Random.Range (0, Factions.Count);

				Factions [i].CapitalBuilding = Factions [Target].CapitalBuilding;
				Factions [Target].CapitalBuilding = Capital;
			}

		}
	}

	//this function gets the ID of the faction type using its code:
	public int GetFactionTypeID (string Code)
	{
		int i = 0;
		while (i < FactionDef.Length) {
			if (FactionDef [i].Code == Code) {
				return i;
			}
			i++;
		}

		return -1;
	}


	//this method handles picking targets for NPC factions:
	public void SetFactionTargets ()
	{
		//if it's a single player game:
		if (MultiplayerGame == false) {
			for (int i = 0; i < Factions.Count; i++) {
				//see if it's a NPC faction that is still playing:
				if (i != PlayerFactionID && Factions [i].Lost == false) {
					//launch a timer which will trigger picking a target faction when it's finished.
					Factions [i].FactionMgr.ArmyMgr.SetTargetTimer = Random.Range(Factions [i].FactionMgr.ArmyMgr.SetTargetReload.x,Factions [i].FactionMgr.ArmyMgr.SetTargetReload.y);
				}
			}
		}
	}

	//Game state methods:

	//call when a faction is defeated (its capital building has fallen):
	public void OnFactionDefeated (int FactionID)
	{
		Factions [FactionID].Lost = true; //ofc.
		ActiveFactions--; //decrease the amount of active factions:
		if(Events) Events.OnFactionEliminated(Factions[FactionID]); //call the custom event.

		if (FactionID == PlayerFactionID) {
			//If the player is defeated then:
			LooseGame ();
		} else {
			//If one of the other factions was defeated:
			//Check if only the player was left undefeated!
			if (ActiveFactions == 1) {
				WinGame (); //Win the game!
				if(Events) Events.OnFactionWin(Factions[FactionID]); //call the custom event.
			}
		}
	}

	//Win the game:
	public void WinGame ()
	{
		//when all the other factions are defeated, 

		//stop whatever the player is doing:
		UIMgr.SelectionMgr.DeselectBuilding ();
		UIMgr.SelectionMgr.DeselectUnits ();
		UIMgr.SelectionMgr.DeselectResource ();

		Time.timeScale = 0.0f; //Stop the game from running by setting the time scale to 0.
		UIMgr.WinningMenu.SetActive(true); //Show the winning menu

		GameEnded = true;
	}

	//called when the player's faction is defeated:
	public void LooseGame ()
	{
		Time.timeScale = 0.0f; //Stop the game from running by setting the time scale to 0.
		UIMgr.LoosingMenu.SetActive(true); //Show the loosing menu

		GameEnded = true;
	}

	//allows the player to leave the current game:
	public void LeaveGame ()
	{
		if (MultiplayerGame == false) {
			//load the main menu if it's a single player game:
			SceneManager.LoadScene (MainMenuScene);
		} else {
			//if it's a MP game, then back to the network lobby:
			NetworkMgr.LeaveLobby (true);
		}
	}

	//this method allows to clear all the NPC managers in the scene in order to replace them by other NPC managers if it's a single game or just get rid them at all if it's a MP game:
	public void ClearNPCManagers ()
	{
		//Find all NPC Army, Building and Resource managers in the scene and remove them because we will need to spawn new ones as chosen in the map menu:
		NPCArmy[] NPCArmyMgrs = FindObjectsOfType(typeof(NPCArmy)) as NPCArmy[];
		foreach (NPCArmy ArmyMgr in NPCArmyMgrs) {
			DestroyImmediate (ArmyMgr);
		}

		NPCBuildingPlacement[] NPCBuildingMgrs = FindObjectsOfType(typeof(NPCBuildingPlacement)) as NPCBuildingPlacement[];
		foreach (NPCBuildingPlacement ArmyMgr in NPCBuildingMgrs) {
			DestroyImmediate (ArmyMgr);
		}

		NPCResource[] NPCResourceMgrs = FindObjectsOfType(typeof(NPCResource)) as NPCResource[];
		foreach (NPCResource ArmyMgr in NPCResourceMgrs) {

			DestroyImmediate (ArmyMgr);
		}
	}

	//Multiplayer:

	//method allowing to get the faction's target manager:
	public static MFactionManager GetTargetFactionConnection (int ID)
	{
		int i = 0;
		MFactionManager[] FactionMgrs = FindObjectsOfType (typeof(MFactionManager)) as MFactionManager[];
		//loop through all the multiplayer faction managers
		while (i < FactionMgrs.Length) {
			//if the ID of the faction manager matches the target ID, then it's the one.
			if (FactionMgrs [i].FactionID == ID) {
				return FactionMgrs [i];
			}

			i++;
		}

		return null;
	}

}
