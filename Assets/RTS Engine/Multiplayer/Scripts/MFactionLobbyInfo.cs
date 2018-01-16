using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;

/* Multiplayer Faction Lobby Info: script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

public class MFactionLobbyInfo : NetworkLobbyPlayer {

	[HideInInspector]
	[SyncVar]
	public int FactionID = 0; //holds the faction ID associated to the player.
	[HideInInspector]
	[SyncVar]
	public string FactionName; //holds the player's faction name.
	[HideInInspector]
	[SyncVar]
	public string FactionCode; //holds the faction type code.
	[HideInInspector]
	[SyncVar]
	public Color FactionColor = Color.blue; //player faction's color
	[HideInInspector]
	[SyncVar]
	public int FactionColorID = 0;
	[HideInInspector]
	[SyncVar]
	public bool IsReady = false; //is the player ready or not to play
	[HideInInspector]
	[SyncVar] 
	public int MapID; //each player must know the map's ID in order to load the same map later.

	//UI Info:
	public Image ColorImg; //showing the faction's color
	public InputField FactionNameInput; //input/show the faction's name
	public GameObject ReadyToBeginButton; //to announce that the player is to ready or not
	public Image ReadyImage; //the image to show when the player is ready
	public Dropdown FactionTypeMenu; //UI Dropdown used to display the list of possible faction types that can be used in the currently selected maps.
	public GameObject KickButton; //the button that the host can use to kick this player.

	[HideInInspector]
	public NetworkMapManager LobbyMgr;

	public void OnPlayerJoined ()
	{
		if (isServer) { //if this is the server:
			if (LobbyMgr.FactionLobbyInfos.Count > 0) { //if there are other clients other than the server here:
				//the server will share all the lobby setting sfor the new player
				foreach (MFactionLobbyInfo LobbyPlayer in LobbyMgr.FactionLobbyInfos) {
					if (LobbyPlayer != null) {
						LobbyPlayer.CmdServerUpdateMap ();
						LobbyPlayer.CmdUpdateFactionColor (LobbyPlayer.FactionColorID, LobbyMgr.AllowedColors [LobbyPlayer.FactionColorID]);
						LobbyPlayer.CmdUpdateFactionName (LobbyPlayer.FactionNameInput.text);
						LobbyPlayer.CmdUpdateFactionType (LobbyPlayer.FactionTypeMenu.value);
						LobbyPlayer.CmdUpdateReadyStatus ();
					}
				}
			}
		}
	}

	void Start ()
	{
		//get the lobby manager script
		LobbyMgr = FindObjectOfType (typeof(NetworkMapManager)) as NetworkMapManager;

		//if it's a local player:
		if (isLocalPlayer)
		{
			//set the local faction
			SetupLocalFaction();
			CmdAddPlayerToList ();
		}
		else
		{
			//if it's not the local player:
			SetupOtherFaction();
		}

		SetObjParent (); //set the lobby info object's parent.

		if (FactionID == 0) { //checking if this is the host's faction
			//make sure that the kick button is not activated and that the host can't announce he's ready or not, only him can launch the game when all other players are ready.
			ReadyToBeginButton.SetActive (false);
			KickButton.SetActive (false);
		}

		//Show the kick buttons for the server only:
		if (!isServer) {
			KickButton.SetActive (false);
		}
	}

	[Command]
	public void CmdAddPlayerToList ()
	{
		//clear the list:
		if (LobbyMgr.FactionLobbyInfos.Count > 0) { //if there are other clients other than the server here:
			//the server will share all the lobby setting sfor the new player
			int i = 0;
			while (i < LobbyMgr.FactionLobbyInfos.Count) {
				if (LobbyMgr.FactionLobbyInfos [i] == null) {
					LobbyMgr.FactionLobbyInfos.RemoveAt (i);
				} else {
					i++;
				}
			}
		}

		//ask the server:
		if (LobbyMgr.FactionLobbyInfos.Count > 0) {
			LobbyMgr.FactionLobbyInfos [0].OnPlayerJoined ();
		}
		LobbyMgr.FactionLobbyInfos.Add (this);
	}

	//setting up the local faction:
	public void SetupLocalFaction ()
	{
		FactionNameInput.interactable = true; //only the player can change his faction's name
		FactionTypeMenu.interactable = true; //only the player can pick his faction type.
		//Hide the multiplayer main menu:
		LobbyMgr.LoadingMenu.gameObject.SetActive(false);
		LobbyMgr.MainMPMenu.gameObject.SetActive (false);
		LobbyMgr.MatchMakingMenu.gameObject.SetActive (false);
		LobbyMgr.HostMapMenu.gameObject.SetActive (true);
		//Show the lobby menu:
		LobbyMgr.LobbyMenu.gameObject.SetActive (true);

		LobbyMgr.LobbyPlayerInfo = this; //link the lobby manager with the local players

		LobbyMgr.CurrentMapID = MapID; //get the map's ID.
		LobbyMgr.MapDropDownMenu.value = LobbyMgr.CurrentMapID; //set the map on the map drop down menu
		LobbyMgr.UpdateMapUIInfo (); //and update the map's UI.

		FactionNameInput.text = FactionName;
		ColorImg.color = LobbyMgr.AllowedColors[FactionColorID]; //set its color.

		SetMapInfo ();
		ResetFactionTypes ();
	}


	//when the faction does not belong to the local player:
	public void SetupOtherFaction ()
	{
		FactionNameInput.interactable = false; //can't change its name
		FactionTypeMenu.interactable = false; //can't change the faction type.
		FactionNameInput.text = FactionName;
		ColorImg.color = LobbyMgr.AllowedColors[FactionColorID]; //set its color.
		ReadyImage.gameObject.SetActive (IsReady); //check if it's ready or not
		ResetFactionTypes ();
	}

	//setting the map info:
	public void SetMapInfo ()
	{
		if (isServer) { //if this is the server:
			LobbyMgr.MapDropDownMenu.interactable = true; //then make the player able to pick the map
			LobbyMgr.StartGameButton.SetActive (true); //activate the start game button because only the host can launch a game

			//initial map settings:
			LobbyMgr.CurrentMapID = 0;
			LobbyMgr.MapDropDownMenu.value = LobbyMgr.CurrentMapID;

		} else {
			//if this is not the server, then the player can't start the game or change the map:
			LobbyMgr.MapDropDownMenu.interactable = false;
			LobbyMgr.StartGameButton.SetActive (false);
		}
	}

	//this method sets the lobby info object parent to one chosen in the lobby manager:
	public void SetObjParent ()
	{
		if (LobbyMgr.LobbyPlayerParent != null) {
			transform.SetParent (LobbyMgr.LobbyPlayerParent);
			gameObject.GetComponent<RectTransform> ().localScale = new Vector3 (1.0f, 1.0f, 1.0f);
		}
	}

	//checking if the player is ready or not and updating it:
	[HideInInspector]
	public bool ReadyOrNot;
	void Update ()
	{
		ReadyOrNot = readyToBegin;
	}

	//updating the map info:
	public void UpdateMapInfo()
	{
		CmdServerUpdateMap (); //ask the server
	}

	[Command]
	public void CmdServerUpdateMap ()
	{
		RpcUpdateMapInfo (); //update the map info for all players
	}

	[ClientRpc]
	public void RpcUpdateMapInfo ()
	{
		LobbyMgr.UpdateMapUIInfo (); //update the map info from lobby manager

		LobbyMgr.LobbyPlayerInfo.ResetFactionTypes (); 
	}

	public void ResetFactionTypes ()
	{
		List<string> FactionTypes = new List<string> ();
		if (LobbyMgr.Maps [MapID].FactionTypes.Length > 0) { //if there are actually faction types to choose from:
			for(int i = 0; i < LobbyMgr.Maps [MapID].FactionTypes.Length ; i++) //create a list with the names with all possible faction types:
			{
				FactionTypes.Add (LobbyMgr.Maps [MapID].FactionTypes [i].Name);
			}
		}

		FactionTypeMenu.ClearOptions (); //clear all the faction type options.
		if (FactionTypes.Count > 0) {
			//Add the faction types' names as options:
			FactionTypeMenu.AddOptions(FactionTypes);
			FactionTypeMenu.value = 0;
			FactionCode = LobbyMgr.Maps [MapID].FactionTypes [0].Code;
		}
	}

	//Toggle the ready status of the player:
	public void ToggleReadyStatus()
	{
		if (isLocalPlayer && !isServer) { //if this is not the server and this belongs to a local player:
			if (ReadyImage.gameObject.activeInHierarchy == false) { //if the ready image is not active then make the player ready
				SendReadyToBeginMessage ();
			} else {
				SendNotReadyToBeginMessage (); //and vice versa
			}
			CmdUpdateReadyStatus (); //ask the server to update the ready status of this faction to all players:
		}
	}

	[Command]
	public void CmdUpdateReadyStatus ()
	{
		//update the ready status for all players
		RpcUpdateReadyStatus ();
		IsReady = readyToBegin;
	}
		
	[ClientRpc]
	public void RpcUpdateReadyStatus ()
	{
		ReadyImage.gameObject.SetActive (readyToBegin); //Show the ready image  or hide it depending on the ready status of the player.
	}

	//Updating the faction name:
	public void OnFactionNameChange ()
	{
		if (isLocalPlayer) { //only if it's the local player:
			if (FactionNameInput.text != "") { //and the new name is valid
				CmdUpdateFactionName (FactionNameInput.text); //ask the server to update it
			}
		}
	}
	//update the faction's name to all players:
	[ClientRpc]
	public void RpcUpdateFactionName (string Value)
	{
		FactionNameInput.text = Value;

	}

	[Command]
	public void CmdUpdateFactionName(string Value)
	{
		//update the name to all players:
		FactionName = Value;

		RpcUpdateFactionName (Value);
	}

	//Updating the faction name:
	public void OnFactionTypeChange ()
	{
		if (isLocalPlayer) { //only if it's the local player:
			if (FactionTypeMenu.value < LobbyMgr.Maps[LobbyMgr.CurrentMapID].FactionTypes.Length) { //if the new type is valid
				//update it:
				CmdUpdateFactionType(FactionTypeMenu.value);
			}
		}
	}

	//update the faction's type to all players:
	[ClientRpc]
	public void RpcUpdateFactionType (int ID)
	{
		FactionTypeMenu.value = ID;

	}

	[Command]
	public void CmdUpdateFactionType(int ID)
	{
		//update the type to all players:
		FactionCode = LobbyMgr.Maps[LobbyMgr.CurrentMapID].FactionTypes[ID].Code;

		RpcUpdateFactionType (ID);
	}

	//Updating the faction color:
	public void OnFactionColorChange ()
	{
		if (isLocalPlayer) { //only if it's the local player

			//change the faction's color ID
			if (LobbyMgr.AllowedColors.Length - 1 > FactionColorID) {
				FactionColorID++;
			} else {
				FactionColorID = 0;
			}

			//ask the server to update the color for all players:
			CmdUpdateFactionColor (FactionColorID, LobbyMgr.AllowedColors [FactionColorID]);
		}
	}

	[ClientRpc]
	public void RpcUpdateFactionColor (Color Value)
	{
		ColorImg.color = Value; //update the color
	}
 

	[Command]
	public void CmdUpdateFactionColor(int ColorID, Color Value)
	{
		//send a message to all players to update this faction's color:
		FactionColor = Value;
		FactionColorID = ColorID;
		RpcUpdateFactionColor (Value);
	}


	//Map settings:
	[Command]
	public void CmdKickPlayer () //this method allows the host to kick other players:
	{
		RpcKickPlayer (FactionID);
	}

	[ClientRpc]
	public void RpcKickPlayer (int ID) //kicking the player locally.
	{
		if (ID == FactionID && isLocalPlayer) {
			LobbyMgr.LeaveLobby (false); //simply make him leave the game.
			LobbyMgr.ShowInfoMsg("You have been kicked from the room.", 2.0f);
		}
	}
}
