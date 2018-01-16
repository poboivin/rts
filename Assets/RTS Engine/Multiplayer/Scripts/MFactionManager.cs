using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.AI;

/* Multiplayer Faction Manager: script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

public class MFactionManager : NetworkBehaviour {

	[HideInInspector]
	[SyncVar]
	public int FactionID = 0; //The faction ID that this script manages.

	public GameObject CapitalBuilding; //Drag and drop the capital's building prefab here.

	public GameObject[] Buildings; //list of all the buildings that the player can place in a multiplayer game.
	public GameObject[] Units; //list of all the units that the player can create in a MP game.

	//Attack objects:
	//public GameObject[] AttackObjs; //list of all the attack objects that can be issued in a multiplayer game.

	//other scripts.
	GameManager GameMgr;
	NetworkMapManager NetworkMgr;

	//Input actions: All player actions are stored in this list in order to be exectuted later.
	[System.Serializable]
	public class InputActionsVars : MessageBase
	{
		public NetworkInstanceId Source;
		public NetworkInstanceId Target;

		public Vector3 InitialPos;
		public Vector3 TargetPos;
		public float StoppingDistance;

		public bool CustomAction = false; //if true then the player triggered custom action for the source/target object (it will be called in a custom event).
	}
	[HideInInspector]
	public List<InputActionsVars> InputActions = new List<InputActionsVars>();
	[HideInInspector]
	public List<Unit> InputMvtUnits = new List<Unit>(); //holds the list of units that have registered an input action in order to move.
	[HideInInspector]
	public float InputActionTimer; //a player can send input in a limited period of time before being able to send them again.
	[HideInInspector]
	public bool CanSendInput = true; //determines if the player can send input or waiting for an instruction from the server.
	float LockstepCycle = 0.2f; //the cycle's length, at which the player can send inputs.
	public Vector2 LockstepCycleRange = new Vector2(0.2f,0.4f);
	int CurrentTurn = 0;

	//Destroying objects:
	public float DestroyObjDelay = 1.0f;
	float DestroyObjTimer = 0.0f;
	List<GameObject> ObjectsToDestroy = new List<GameObject> ();

	//Pending actions: These are the actions received from the server and that the player will execute when he gets the order from the server.
	[System.Serializable]
	public class PendingActionsVars
	{
		public Vector3 InitialPos;
		public Vector3 TargetPos;
		public float StoppingDistance;
		public GameObject Source;
		public GameObject Target;

		public bool CustomAction = false;
	}
	List<PendingActionsVars> PendingActions = new List<PendingActionsVars>();
	[HideInInspector]
	public bool ReceivedActions = false; //true when the player receives orders from the server
	[HideInInspector]
	public bool CanPerformActions = false; //true when the player is able to perform the pending actions
	public float SnapDistance = 5f; //when an action is a unit movement, the snap distance represents the maximal distance between the unit's position in the server and its position in the client view.
	//if this distance is surpassed, then the unit will be snapped to the correct distance before starting the movement.

	List<NextCommandsVars> PendingCommands = new List<NextCommandsVars>();
	List<NextCommandsVars> ReadyCommands = new List<NextCommandsVars>();
	//Server related: A list of commands/orders sent by the clients and stored by the server.
	[System.Serializable]
	public class NextCommandsVars : MessageBase
	{
		public NetworkInstanceId Source;
		public NetworkInstanceId Target;

		public Vector3 TargetPos;
		public Vector3 InitialPos;
		public float StoppingDistance;

		public int Amount;

		public bool CustomAction = false;
	}
	List<NextCommandsVars> NextCommands = new List<NextCommandsVars>();
	[HideInInspector]
	public bool AllClientsReady = false; //true when all clients received the pending actions
	[HideInInspector]
	public int ClientsReady = 0; //the amount of clients who reported of receiving the pending actions
	[HideInInspector]
	public bool CommandsSent = false; //true when the server had already sent the commands to the players.
	[HideInInspector]
	public float ClientResponseLimit = 0.4f; //Time before announcing that a player is lagging (did not receive the info from the server):
	public Vector2 ClientResponseLimitRange = new Vector2(0.6f,0.8f);
	[HideInInspector]
	public float ClientResponseTimer;

	//Network check: allows to determine the best client response limit and the lockstep cycle:
	bool NetTesting = false; //are we testing the network? 
	public int NoResponseTimesToTest = 5; //how many times of "having no response from clients in the correct time" in order to start testing the networking automatically to improve it? 
	int NoResponseTimesCount; //count for above attribute.
	public int NetTestingRepeatTimes = 10; //how many times do we need to test the lockstep cycle and client reponse in order to build new values of reponse limit and lockstep cycle to suit the current clients.
	int NetTestingRepeatCount; //count for above attribute.
	float NewResponseLimit; //register the new client response limit here.
	float TestResponseTimer; //timer for above attribute.

	//we use messages to communicate between the players and the server
	public class MyMsgType {
		public static short CollectInput = MsgType.Highest + 1;
		public static short GameAction = MsgType.Highest + 2;
		public static short TellServerReady = MsgType.Highest + 3;
		public static short GotPermission = MsgType.Highest + 4;
		public static short ReceiveInput = MsgType.Highest + 5;
		public static short WaitForPlayers = MsgType.Highest + 6;
	};

	int ReadyPlayers = 0;

	//Letting the server that a player is ready:
	[Command]
	public void CmdPlayerIsReady ()
	{
		GameMgr.Factions [0].MFactionMgr.SpawnObjects ();
	}

	public void SpawnObjects ()
	{
		ReadyPlayers++;
		if(ReadyPlayers == GameMgr.Factions.Count)
		{

			GameMgr.ResourceMgr.SpawnResourcesInList ();
		}
	}

	bool IsClientReady = false; //is the client's connection ready or not?
		
	void Update ()
	{
		if (isServer == true) { //if this is the server.
			//Destroy timer:
			if (DestroyObjTimer > 0) {
				DestroyObjTimer -= Time.deltaTime;
			}
			if (DestroyObjTimer < 0) {
				foreach (GameObject Obj in ObjectsToDestroy) {
					NetworkServer.Destroy (Obj);
				}
				ObjectsToDestroy.Clear ();
				DestroyObjTimer = 0.0f;
			}
		}

		//Lockstep multiplayer:
		if (isLocalPlayer) { //if this is the local player:
			//faction init:
			if (IsClientReady == false) { //as long as the connection is not ready
				if (ClientScene.ready == true) { //as soon as the connection is ready
					IsClientReady = true;

					CmdFactionSpawn (); //spawn the faction in the game.

					//register client related messages:
					NetworkManager.singleton.client.connection.RegisterHandler(MyMsgType.GameAction, OnGameAction); //when a command from the server is received. 
					NetworkManager.singleton.client.connection.RegisterHandler(MyMsgType.GotPermission, OnHasPermission); //when the client gets the permission from the server to play the commands.
					NetworkManager.singleton.client.connection.RegisterHandler (MyMsgType.CollectInput, OnInputRequest); //when the server asks the client to get the input.
					NetworkManager.singleton.client.connection.RegisterHandler (MyMsgType.WaitForPlayers, OnWaitForPlayers); //when the server asks the client to get the input.

					//register server related messages:
					if (isServer) {
						NetworkServer.RegisterHandler(MyMsgType.TellServerReady, OnTellServerReady); //when the client tells the server that he's ready to launch commands
						NetworkServer.RegisterHandler(MyMsgType.ReceiveInput, OnReceiveInput); //when the server receives input commands from clients.
					}

					GameMgr.Factions [FactionID].MFactionMgr.CmdPlayerIsReady ();
				}
			}

			//TO BE CHANGED:
			float AddTime = Time.deltaTime;
			if (Time.timeScale == 0.000001f) {
				AddTime *= 1000000;
			}

			if (isServer == true && ReadyPlayers == GameMgr.Factions.Count) { //if this is the server.

				if (NetTesting == true) { //if we are testing the network.
					TestResponseTimer += AddTime; //count the new client response time.
				}


				//if the client lag timer going:
				if (ClientResponseTimer > 0) {
					ClientResponseTimer -= AddTime;
				}
				if (ClientResponseTimer < 0) { //when the player has not received the actions from the server and the response limit has passed.
					//announce there's a player lagging:
					//RpcWaitForPlayers ();

					var EmptyMsg = new EmptyMessage ();
					NetworkServer.SendToAll (MyMsgType.WaitForPlayers, EmptyMsg);

					ClientResponseTimer = 0.0f; //reset the client response timer

					NoResponseTimesCount++; //register this as a non response in time from one of the clients
				}
				//the lockstep cycle:
				InputActionTimer = InputActionTimer + AddTime;

				while (InputActionTimer > LockstepCycle) {
					CurrentTurn++; //increase the turn amount

					if (CurrentTurn > 1) {
						//move processed actions to next commands to send for clients
						ReadyToNextCommands ();
						//move pending actions to ready commands after processing them
						PendingToReadyCommands ();
					}

					//sending collected commands to all clients:
					if (CurrentTurn > 1 && AllClientsReady == false && ClientResponseTimer == 0.0f) { //only if the current turn is superior than 1 (to guarantee that we have collected input at least one time) and make sure that all clients are not ready (awaiting commands).

						if (NextCommands.Count > 0) {
							for (int i = 0; i < NextCommands.Count; i++) {
								//send commands as pending actions for all the players:
								NextCommands[i].Amount = NextCommands.Count;
								NetworkServer.SendToAll (MyMsgType.GameAction, NextCommands [i]);
								//RpcSendPendingAction (NextCommands [i].Source, NextCommands [i].Target, NextCommands [i].InitialPos, NextCommands [i].TargetPos, NextCommands [i].StoppingDistance, NextCommands.Count);
							}

							ClientResponseTimer = ClientResponseLimit; //start the client lag timer:

							if (NetTesting == false) { //if we are not testing the network
								if (NoResponseTimesCount >= NoResponseTimesToTest) { //if we reached the max count amount
									NoResponseTimesCount = 0;
									//start testing the network:
									NewResponseLimit = 0.0f;
									TestResponseTimer = 0.0f;

									NetTesting = true;
									NetTestingRepeatCount = 0;
								}
							}
						
						}

					}

					//if the server allows for input to be sent
					if (CanSendInput == true && PendingCommands.Count == 0) {
						//ask to collect input commands from all clients
						//RpcSendInput ();
						var EmptyMsg = new EmptyMessage ();
						NetworkServer.SendToAll (MyMsgType.CollectInput, EmptyMsg);
						CanSendInput = false; //stop collecting input.
					}

					InputActionTimer = InputActionTimer - LockstepCycle; //reset the input cycle's timer:

				}
			}

			//if the player can perform pending actions
			if (CanPerformActions == true) {
				if (PendingActions.Count > 0) { //and we actually have pending actions:
					for (int i = 0; i < PendingActions.Count; i++) {
						//go through them and apply them depending on their properties:
						if (PendingActions [i].Source != null) {

							if (PendingActions [i].CustomAction == false) { //no custom action, simple movement:
								if (PendingActions [i].Source.GetComponent<Unit> ()) {
									Unit CurrentUnit = PendingActions [i].Source.GetComponent<Unit> (); //get the source's object.
									bool CanSnapDistance = true;
									if (PendingActions [i].Target != null) { //if player is updating health then don't snap distance.
										if (PendingActions [i].Target.GetComponent<Unit> () == CurrentUnit) {
											CanSendInput = false;
										}
									}

									if (CanSnapDistance == true) {
										//see if we need to snap its position or not.
										if (Vector3.Distance (CurrentUnit.transform.position, PendingActions [i].InitialPos) > SnapDistance) {
											CurrentUnit.transform.position = PendingActions [i].InitialPos;
										}
									}

									if (PendingActions [i].Target == null) {
										//move unit.
										CurrentUnit.CheckUnitPathLocal (PendingActions [i].TargetPos, null, PendingActions [i].StoppingDistance, i);
									} else {
										if (PendingActions [i].Target.GetComponent<Unit> ()) { //if the target is a unit:
											if (PendingActions [i].Target.GetComponent<Unit> () == CurrentUnit) { //if it's the same unit then simply update the health:
												CurrentUnit.AddHealthLocal (PendingActions [i].StoppingDistance, null);
											} else if (PendingActions [i].Target.GetComponent<Unit> ().FactionID != CurrentUnit.FactionID) { //if the unit does not belong to the source's faction.
												//if the source unit has a converter component:
												if (CurrentUnit.ConvertMgr) {
													//convert the target unit.
													CurrentUnit.ConvertMgr.SetTargetUnitLocal (PendingActions [i].Target.GetComponent<Unit> ());
												} else if (CurrentUnit.AttackMgr) { //if it has the attack comp
													//Attack unit.
													CurrentUnit.AttackMgr.TargetAssigned = true;
													CurrentUnit.AttackMgr.SetAttackTarget (PendingActions [i].Target.gameObject);
													CurrentUnit.CheckUnitPathLocal (Vector3.zero, PendingActions [i].Target.gameObject, PendingActions [i].StoppingDistance, -1);
												}
											} else {
												//APC
												if (PendingActions [i].Target.GetComponent<APC> ()) {
													CurrentUnit.TargetAPC = PendingActions [i].Target.GetComponent<APC> ();
													CurrentUnit.CheckUnitPathLocal (Vector3.zero, PendingActions [i].Target.gameObject, PendingActions [i].StoppingDistance, -1);
												} else if (CurrentUnit.HealMgr != null) { //healer:
													CurrentUnit.HealMgr.SetTargetUnitLocal (PendingActions [i].Target.GetComponent<Unit> ());
												}
											}
										} else if (PendingActions [i].Target.GetComponent<Building> ()) { //if the target is a building
											if (PendingActions [i].Target.GetComponent<Building> ().FactionID == CurrentUnit.FactionID) { //and it belongs to the source's faction
												if (PendingActions [i].Target.GetComponent<Building> ().Health < PendingActions [i].Target.GetComponent<Building> ().MaxHealth) { //if it doesn't have max health
													//construct building
													Builder BuilderComp = CurrentUnit.gameObject.GetComponent<Builder> ();

													BuilderComp.SetTargetBuildingLocal (PendingActions [i].Target.GetComponent<Building> ());
												} else {
													if (PendingActions [i].Target.GetComponent<APC> ()) {
														CurrentUnit.TargetAPC = PendingActions [i].Target.GetComponent<APC> ();
														CurrentUnit.CheckUnitPathLocal (Vector3.zero, PendingActions [i].Target.gameObject, PendingActions [i].StoppingDistance, -1);
													}
												}
											} else { //if it belongs to an enemy's faction
												if (PendingActions [i].Target.GetComponent<Building> ().CanBeAttacked == true) { //if the building can actually be attacked
													//attack building
													CurrentUnit.gameObject.GetComponent<Attack> ().TargetAssigned = true;
													CurrentUnit.gameObject.GetComponent<Attack> ().SetAttackTarget (PendingActions [i].Target.gameObject);
													CurrentUnit.CheckUnitPathLocal (Vector3.zero, PendingActions [i].Target.gameObject, PendingActions [i].StoppingDistance, -1);
												} else {
													if (PendingActions [i].Target.GetComponent<Portal> ()) {
														if (PendingActions [i].Target.GetComponent<Portal> ().IsAllowed(CurrentUnit)) { //if the selected unit's category matches the allowed categories in the target portal.
															CurrentUnit.TargetPortal = PendingActions [i].Target.GetComponent<Portal> ();
															CurrentUnit.CheckUnitPathLocal (PendingActions [i].Target.GetComponent<Portal> ().transform.position, PendingActions [i].Target.gameObject, GameManager.Instance.MvtStoppingDistance, -1);
														}
													}
												}

											}
										} else if (PendingActions [i].Target.GetComponent<Resource> ()) { //if the target is a resource
											GatherResource ResourceComp = CurrentUnit.gameObject.GetComponent<GatherResource> ();

											// in order to reduce data transferred between clients, when the target pos and target resource position are the same, we count this a unit requesting to collect a resource...
											if (Vector3.Distance (PendingActions [i].Target.GetComponent<Resource> ().transform.position, PendingActions [i].TargetPos) < 0.5f) {
												//collect resources:
												ResourceComp.SetTargetResourceLocal (PendingActions [i].Target.GetComponent<Resource> ());
											}
										//but if they weren't the same, then we count this a unit going to drop off resources:
										else {
												//send unit the resource drop off building.
												AudioManager.StopAudio (CurrentUnit.gameObject);
												CurrentUnit.CheckUnitPathLocal (Vector3.zero, ResourceComp.DropOffBuilding.gameObject, 0.0f, -1);
											}
										}
									}
								} else if (PendingActions [i].Source.GetComponent<Building> ()) { //if the source 
									//if there's a target object:
									if (PendingActions [i].Target != null) {
										if (PendingActions [i].Target.GetComponent<Building> ()) {
											if (PendingActions [i].Source.GetComponent<Building> () == PendingActions [i].Target.GetComponent<Building> ()) {
												PendingActions [i].Source.GetComponent<Building> ().AddHealthLocal (PendingActions [i].StoppingDistance, null);
											}
										}
									//if the building has an attack script:
									else if (PendingActions [i].Source.GetComponent<AttackBuilding> ()) {
											//launch the attack:
											PendingActions [i].Source.GetComponent<AttackBuilding> ().LaunchAttackLocal (PendingActions [i].Target);
										}
									}
								}
								else if (PendingActions [i].Source.GetComponent<Resource> ()) { //if the source 
									//if there's a target object:
									if (PendingActions [i].Target != null) {
										if(PendingActions [i].Target.GetComponent<GatherResource> ())
										{
											PendingActions [i].Source.GetComponent<Resource> ().AddResourceAmountLocal (PendingActions [i].StoppingDistance, PendingActions [i].Target.GetComponent<GatherResource> ());
										}

									}
								}
							}
							else { //custom action, call custom call back:
								if (GameMgr.Events) {
									//the stopping distance plays here the role of the custom action ID (that's why we rounded it because the custom event requires an int)
									GameMgr.Events.OnCustomAction (PendingActions [i].Source, PendingActions [i].Target, Mathf.RoundToInt (PendingActions [i].StoppingDistance));
								}
							}
						}
					}

					PendingActions.Clear (); //clear pending actions after applying them.
				}

				//announce that the player can send input again:
				CanPerformActions = false;
				ReceivedActions = false;
			}
		}
				
	}

	public void PendingToReadyCommands ()
	{
		if (ReadyCommands.Count == 0 && PendingCommands.Count > 0) {

			ReadyCommands.AddRange(PendingCommands);
			PendingCommands.Clear ();
			CanSendInput = true;
		}
	}

	public void ReadyToNextCommands ()
	{
		if(NextCommands.Count == 0 && ReadyCommands.Count > 0)
		{
			NextCommands.AddRange(ReadyCommands);
			ReadyCommands.Clear ();
			GameMgr.Factions [0].MFactionMgr.AllClientsReady = false; //mark clients as non ready.
		}
	}

	//---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	//Lockstep model using network messages only!

	//a method that allows the client to send input actions to the server:
	void OnInputRequest (NetworkMessage NetMsg)
	{
		GameMgr.Factions [GameManager.PlayerFactionID].MFactionMgr.SendInputLocal ();
	}

	void SendInputLocal ()
	{
		if (InputActions.Count == 0) { //if we don't have commands to process, send empty ones just to make sure everything stays in sync
			InputActionsVars EmptyItem = new InputActionsVars ();
			InputActions.Add (EmptyItem);
		}
		if (InputActions.Count > 0) { //loop through all registered input actions:
			for (int i = 0; i < InputActions.Count; i++) {
				//send them to the server
				//CmdAddPendingCommand (InputActions [i].Source, InputActions [i].Target, InputActions [i].InitialPos, InputActions [i].TargetPos, InputActions [i].StoppingDistance);
				NetworkManager.singleton.client.Send (MyMsgType.ReceiveInput, InputActions [i]);
			}
			InputActions.Clear (); //clear the input list:
			InputMvtUnits.Clear ();
		}
	}

	public void OnReceiveInput(NetworkMessage NetMsg)
	{
		InputActionsVars InputAction = NetMsg.ReadMessage<InputActionsVars>();

		NextCommandsVars NewCommand = new NextCommandsVars ();
		NewCommand.Source = InputAction.Source;
		NewCommand.Target = InputAction.Target;
		NewCommand.TargetPos = InputAction.TargetPos;
		NewCommand.InitialPos = InputAction.InitialPos;
		NewCommand.StoppingDistance = InputAction.StoppingDistance;
		NewCommand.CustomAction = InputAction.CustomAction;

		GameMgr.Factions [0].MFactionMgr.PendingCommands.Add (NewCommand);
	}
		
	public void OnGameAction (NetworkMessage NetMsg)
	{
		if (GameMgr.Factions [GameManager.PlayerFactionID].MFactionMgr.ReceivedActions == false) { //making sure that the player did not receive these actions yet.
			NextCommandsVars ActionCommand = NetMsg.ReadMessage<NextCommandsVars>();
			CanPerformActions = false; 

			//register the pending command:
			GameObject Source = null;
			GameObject TargetObj = null;

			Source = ClientScene.FindLocalObject (ActionCommand.Source);
			TargetObj = ClientScene.FindLocalObject (ActionCommand.Target);

			PendingActionsVars NewPendingAction = new PendingActionsVars ();
			NewPendingAction.Source = Source;
			NewPendingAction.Target = TargetObj;
			NewPendingAction.TargetPos = ActionCommand.TargetPos;
			NewPendingAction.InitialPos = ActionCommand.InitialPos;
			NewPendingAction.StoppingDistance = ActionCommand.StoppingDistance;
			NewPendingAction.CustomAction = ActionCommand.CustomAction;

			GameMgr.Factions [GameManager.PlayerFactionID].MFactionMgr.PendingActions.Add (NewPendingAction);

			//when the client receives all the commands,
			if (GameMgr.Factions [GameManager.PlayerFactionID].MFactionMgr.PendingActions.Count == ActionCommand.Amount) {
				//let the server know the client's ready to execute them.
				var EmptyMsg = new EmptyMessage();
				NetworkManager.singleton.client.Send (MyMsgType.TellServerReady, EmptyMsg);

				GameMgr.Factions [GameManager.PlayerFactionID].MFactionMgr.ReceivedActions = true;

				//only for demo? 

				//GameMgr.Factions [GameManager.PlayerFactionID].MFactionMgr.CanPerformActions = true;

			}
		}
	}

	public void OnTellServerReady (NetworkMessage NetMsg)
	{
		MFactionManager ServerGameMgr = GameMgr.Factions [0].MFactionMgr;
		ServerGameMgr.GotPermissionFromClient ();
	}

	//call back when a client announces he received the pending commands:
	public void GotPermissionFromClient ()
	{
		ClientsReady++;

		//if all players reported of receiving the pending commands:
		if (ClientsReady == GameMgr.Factions.Count) {
			AllClientsReady = true;

			ClientResponseTimer = 0.0f;

			NextCommands.Clear ();
			//RpcAllowToPerformActions (); //allow the players to perform actions.
			var EmptyMsg = new EmptyMessage();
			NetworkServer.SendToAll(MyMsgType.GotPermission,EmptyMsg);

			ClientsReady = 0;

			CanSendInput = true;

			//testing:
			if (NetTesting == true) {
				NetTestingRepeatCount++;

				if (NewResponseLimit == 0)
					NewResponseLimit = TestResponseTimer;
				else
					NewResponseLimit = (NewResponseLimit + TestResponseTimer) / 2;

				TestResponseTimer = 0;

				if (NetTestingRepeatCount >= NetTestingRepeatTimes) {
					NetTesting = false;
					ClientResponseLimit = Mathf.Clamp (NewResponseLimit, ClientResponseLimitRange.x, ClientResponseLimitRange.y);
					LockstepCycle = Mathf.Clamp (ClientResponseLimit/3, LockstepCycleRange.x, LockstepCycleRange.y);

					//MAKE LIMITS FOR RESPONSE AND TURN PERIOD:


					NetTestingRepeatCount = 0;
				}
			}
		}
	}

	public void OnHasPermission (NetworkMessage NetMsg)
	{
		if (GameMgr.UIMgr.MPMessage) {
			GameMgr.UIMgr.MPMessage.gameObject.SetActive (false);
		}

		Time.timeScale = 1.0f;
		GameMgr.Factions [GameManager.PlayerFactionID].MFactionMgr.CanPerformActions = true;
	}

	//a method called when a player is lagging to let all the clients that one player is lagging
	public void OnWaitForPlayers (NetworkMessage NetMsg)
	{
		if (GameMgr.UIMgr.MPMessage) {
			//GameMgr.UIMgr.MPMessage.gameObject.SetActive (true);
			GameMgr.UIMgr.MPMessage.text = "Waiting for all players...";
		}

		Time.timeScale = 0.000001f;
	}

	//---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	//Lockstep model using Rpc calls and commands:

	/*[ClientRpc]
	//a method that allows the client to send input actions to the server:
	void RpcSendInput ()
	{
		GameMgr.Factions [GameManager.PlayerFactionID].MFactionMgr.SendInputLocal ();
	}

	//a method that allows the server to receive input actions from clients:
	[Command]
	public void CmdAddPendingCommand (NetworkInstanceId NetID, NetworkInstanceId Target, Vector3 InitialPos, Vector3 TargetPos, float StoppingDistance)
	{
		AddPendingCommand (NetID, Target, InitialPos, TargetPos, StoppingDistance);
	}

	//a method that lets the server store the input actions sent from the clients:
	public void AddPendingCommand (NetworkInstanceId NetID, NetworkInstanceId Target, Vector3 InitialPos, Vector3 TargetPos,  float StoppingDistance)
	{
		NextCommandsVars NewCommand = new NextCommandsVars ();
		NewCommand.Source = NetID;
		NewCommand.Target = Target;
		NewCommand.TargetPos = TargetPos;
		NewCommand.InitialPos = InitialPos;
		NewCommand.StoppingDistance = StoppingDistance;

		GameMgr.Factions [0].MFactionMgr.PendingCommands.Add (NewCommand);
	}
		

	//a method that allows the server to send the registered actions to the clients:
	[ClientRpc]
	public void RpcSendPendingAction (NetworkInstanceId NetID, NetworkInstanceId Target, Vector3 InitialPos, Vector3 TargetPos, float StoppingDistance, int CommandCount)
	{
		if (GameMgr.Factions [GameManager.PlayerFactionID].MFactionMgr.ReceivedActions == false) { //making sure that the player did not receive these actions yet.
			CanPerformActions = false; 

			//register the pending command:
			GameObject Source = null;
			GameObject TargetObj = null;

			Source = ClientScene.FindLocalObject (NetID);
			TargetObj = ClientScene.FindLocalObject (Target);

			PendingActionsVars NewPendingAction = new PendingActionsVars ();
			NewPendingAction.Source = Source;
			NewPendingAction.Target = TargetObj;
			NewPendingAction.TargetPos = TargetPos;
			NewPendingAction.InitialPos = InitialPos;
			NewPendingAction.StoppingDistance = StoppingDistance;

			GameMgr.Factions [GameManager.PlayerFactionID].MFactionMgr.PendingActions.Add (NewPendingAction);

			//when the client receives all the commands,
			if (GameMgr.Factions [GameManager.PlayerFactionID].MFactionMgr.PendingActions.Count == CommandCount) {
				//let the server know the client's ready to execute them.
				GameMgr.Factions [GameManager.PlayerFactionID].MFactionMgr.CmdTellServerReady();

				GameMgr.Factions [GameManager.PlayerFactionID].MFactionMgr.ReceivedActions = true;

			}
		}
	}

	//let the server know that one of the client is ready:
	[Command]
	public void CmdTellServerReady ()
	{
		MFactionManager ServerGameMgr = GameMgr.Factions [0].MFactionMgr;
		ServerGameMgr.GotPermissionFromClient ();
	}

	//allow the client to perform the pending commands:
	[ClientRpc]
	public void RpcAllowToPerformActions ()
	{
		if (GameMgr.UIMgr.MPMessage) {
			GameMgr.UIMgr.MPMessage.gameObject.SetActive (false);
		}

		Time.timeScale = 1.0f;
		GameMgr.Factions [GameManager.PlayerFactionID].MFactionMgr.CanPerformActions = true;
	}

	//a method called when a player is lagging to let all the clients that one player is lagging
	[ClientRpc]
	public void RpcWaitForPlayers ()
	{
		if (GameMgr.UIMgr.MPMessage) {
			GameMgr.UIMgr.MPMessage.gameObject.SetActive (true);
			GameMgr.UIMgr.MPMessage.text = "Waiting for all players...";
		}

		Time.timeScale = 0.000001f;
	}
*/

	//end of lockstep multiplayer.

	void Awake ()
	{
		CanSendInput = true;
		NetTesting = true;

		//get the lobby manager:
		NetworkMgr = FindObjectOfType (typeof(NetworkMapManager)) as NetworkMapManager;
		NetworkMgr.CanvasObj.SetActive (false); //hide the multiplayer menu UI as this object is spawned when the map is loaded.
	}

	void Start ()
	{
		GameMgr = GameManager.Instance;

		if (isLocalPlayer) { //if this is the local player

			if (CapitalBuilding == null) { //make sure we have a capital building.
				Debug.LogError ("Make sure that you set the 'Capital Building' Game Object in the 'MFactionManager' script because it will be spawned as soon as the MP game starts");
			}
		}

		GameMgr.Factions [FactionID].MFactionMgr = this;

	}

	//Handling units:

	//called when a local player attempts to spawn a new unit.
	public void TryToSpawnUnit (string UnitCode, Vector3 Pos, NetworkInstanceId Creator)
	{
		if (isLocalPlayer) {
			CmdSpawnUnit(UnitCode, Pos, Creator); //ask the server to spawn this new unit
		}
	}

	//spawn a unit from the server
	[Command]
	public void CmdSpawnUnit (string UnitCode, Vector3 Pos, NetworkInstanceId Creator)
	{
		int i = 0;
		GameObject UnitPrefab = null;
		//search for the unit's prefab:
		while (UnitPrefab == null && i < NetworkMgr.spawnPrefabs.Count) {
			if (NetworkMgr.spawnPrefabs [i].GetComponent<Unit> ()) {
				if (NetworkMgr.spawnPrefabs [i].GetComponent<Unit> ().Code == UnitCode) {
					UnitPrefab = NetworkMgr.spawnPrefabs [i];
				}
			}
			i++;
		}

		if (UnitPrefab != null) { //if the requested unit is valid:
			//create it in the server
			GameObject UnitClone = Instantiate (UnitPrefab);
			UnitClone.SetActive (false);

			//set its position, faction and the building that created it:
			UnitClone.transform.position = Pos;
			UnitClone.GetComponent<Unit> ().FactionID = FactionID;
			UnitClone.GetComponent<Unit> ().CreatedByID = Creator;

			UnitClone.SetActive (true);

			NetworkServer.SpawnWithClientAuthority (UnitClone, connectionToClient); //spawn the unit to all clients

			//send the spawned unit to the rally point:
			RpcSetUnitCreator (UnitClone, Pos);
		} 

	}

	[ClientRpc]
	public void RpcSetUnitCreator (GameObject Unit, Vector3 Pos)
	{
		if (Unit.GetComponent<Unit> ()) {
			Unit.GetComponent<Unit> ().gameObject.GetComponent<NavMeshAgent> ().enabled = true;
			if (Unit.GetComponent<Unit> ().FactionID == GameManager.PlayerFactionID) { //only if the local player controls this unit
				GameObject Creator = ClientScene.FindLocalObject (Unit.GetComponent<Unit> ().CreatedByID);
				Unit.GetComponent<Unit> ().CreatedBy = Creator.GetComponent<Building> ();
			}
		}

		Unit.transform.position = Pos;

		if (Unit.GetComponent<Unit> ().CreatedBy != null && GameManager.PlayerFactionID == Unit.GetComponent<Unit> ().FactionID && isServer == true) {
			//if the new unit does not have a task when spawned, send them to the goto position.
			Unit.GetComponent<Unit> ().CreatedBy.SendUnitToRallyPoint (Unit.GetComponent<Unit> ());
		}
	}
		

	public void TryToSyncUnitHealth (NetworkInstanceId UnitNetID, float Value)
	{
		if (isLocalPlayer) {
			CmdSyncUnitHealth (UnitNetID, Value);
		}
	}

	[Command]
	public void CmdSyncUnitHealth (NetworkInstanceId UnitNetID, float Value)
	{
		GameObject UnitObj = NetworkServer.FindLocalObject (UnitNetID);
		UnitObj.GetComponent<Unit> ().RpcUpdateHealth (Value);
	}

	//called when attempting to destroy a unit:
	public void TryToDestroyUnit (NetworkInstanceId UnitNetID)
	{
		if (isLocalPlayer) {
			CmdDestroyUnit (UnitNetID); //ask the server
		}
	}

	[Command]
	public void CmdDestroyUnit (NetworkInstanceId UnitNetID)
	{
		//destroy the unit locally in each client's game:
		GameObject UnitObj = NetworkServer.FindLocalObject (UnitNetID);

		ObjectsToDestroy.Add (UnitObj);
		if (DestroyObjTimer == 0)
			DestroyObjTimer = DestroyObjDelay;

		UnitObj.GetComponent<Unit> ().RpcDestroyUnit ();
	}

	//Handling factions:

	//called when spawning factions at the beginning of the game:
	[Command]
	public void CmdFactionSpawn ()
	{
		if(GameMgr == null)
			GameMgr = GameManager.Instance;

		GameMgr.Factions [FactionID].MFactionMgr = this; //set the Multiplayer faction manager

		GameObject Capital = null;

		//At the start of the script, we will spawn the capital building and remove the one that already came in the scene:
		int FactionTypeID = GameMgr.GetFactionTypeID (GameMgr.Factions [FactionID].Code);
		if (FactionTypeID >= 0 && GameMgr.FactionDef [FactionTypeID].CapitalBuilding != null) { //if the faction belongs to a certain type

			//set the new capital building:
			Capital = Instantiate (GameMgr.FactionDef [FactionTypeID].CapitalBuilding.gameObject);
		} else {
			Capital = Instantiate (CapitalBuilding); //spawn the default capital building
		}


		//set the capital's settings:
		Capital.GetComponent<Building> ().FactionID = FactionID;
		Capital.GetComponent<Building> ().FactionCapital = true;
		Capital.GetComponent<Building> ().PlacedByDefault = true;

		Capital.transform.position = GameMgr.Factions [FactionID].CapitalPos; //set the capital's position on the map.


		NetworkServer.SpawnWithClientAuthority (Capital, connectionToClient); //spawn the object for all clients:
	}

	//called when the player attempts to place a building:
	public void TryToSpawnBuilding (string BuildingCode, bool PlacedByDefault, Vector3 Pos, bool Capital)
	{
		if (isLocalPlayer) {
			CmdSpawnBuilding(BuildingCode, PlacedByDefault, Pos.x, Pos.y, Pos.z, Capital); //ask the server
		}
	}

	//called when the server is spawning a new building:
	[Command]
	public void CmdSpawnBuilding (string BuildingCode, bool PlacedByDefault, float PosX, float PosY, float PosZ, bool Capital)
	{
		int i = 0;
		GameObject BuildingPrefab = null;
		while (BuildingPrefab == null && i < NetworkMgr.spawnPrefabs.Count) {
			if (NetworkMgr.spawnPrefabs [i].GetComponent<Building> ()) {
				if (NetworkMgr.spawnPrefabs [i].GetComponent<Building> ().Code == BuildingCode) {
					BuildingPrefab = NetworkMgr.spawnPrefabs [i];
				}
			}
			i++;
		}

		if (BuildingPrefab != null) { //if the building to spawn is valid:

			//create the building's object
			GameObject BuildingClone = Instantiate (BuildingPrefab);

			//set its position and faction:
			BuildingClone.transform.position = new Vector3 (PosX, PosY, PosZ);
			BuildingClone.GetComponent<Building> ().FactionID = FactionID;
			BuildingClone.GetComponent<Building>().PlacedByDefault = PlacedByDefault;
			BuildingClone.GetComponent<Building> ().FactionCapital = Capital;

			//Activate the player selection collider:
			BuildingClone.GetComponent<Building>().PlayerSelection.gameObject.SetActive (true);

			if (PlacedByDefault == false) { //if the building is not placed by default:
				//Set the building's health to 0 so that builders can start adding health to it:
				BuildingClone.GetComponent<Building> ().Health = 0.0f;

				BuildingClone.GetComponent<Building> ().BuildingPlane.SetActive (false);
				BuildingClone.GetComponent<Building> ().ToggleConstructionObj (true); //Show the construction object when the building is placed.
			}

			BuildingClone.GetComponent<Building> ().Placed = true;

			//spawn the building for all clients:
			NetworkServer.SpawnWithClientAuthority (BuildingClone, connectionToClient);

		} 
	}

	/*public void TryToSyncBuildingHealth (NetworkInstanceId BuildingNetID, float Value)
	{
		if (isLocalPlayer) {
			CmdSyncBuildingHealth (BuildingNetID, Value);
		}
	}

	[Command]
	public void CmdSyncBuildingHealth (NetworkInstanceId BuildingNetID, float Value)
	{
		GameObject BuildingObj = NetworkServer.FindLocalObject (BuildingNetID);
		BuildingObj.GetComponent<Building> ().RpcUpdateHealth (Value);
	}*/

	//called when attempting to destroy a building:
	public void TryToDestroyBuilding (NetworkInstanceId BuildingNetID, bool Upgrade)
	{
		if (isLocalPlayer) { //local player
			CmdDestroyBuilding(BuildingNetID, Upgrade);
		}
	}

	//destroying a building:
	[Command]
	public void CmdDestroyBuilding (NetworkInstanceId BuildingNetID, bool Upgrade)
	{
		//find the building
		GameObject BuildingObj = NetworkServer.FindLocalObject (BuildingNetID);

		ObjectsToDestroy.Add (BuildingObj);
		if (DestroyObjTimer == 0)
			DestroyObjTimer = DestroyObjDelay;

		//and destroy it:
		BuildingObj.GetComponent<Building> ().RpcDestroyBuilding (Upgrade);
	}

	//Handling Resources:
	//called when attempting to destroy a resource:
	public void TryToDestroyResource (NetworkInstanceId ResourceNetID)
	{
		if (isLocalPlayer) {
			CmdDestroyResource (ResourceNetID); //ask the server
		}
	}

	[Command]
	public void CmdDestroyResource (NetworkInstanceId ResourceNetID)
	{
		//destroy the resource locally in each client's game:
		GameObject ResourceObj = NetworkServer.FindLocalObject (ResourceNetID);
		//ResourceObj.GetComponent<Resource> ().RpcDestroyResource (); might be added in order to play an effect when a resource is destroyed. 

		ObjectsToDestroy.Add (ResourceObj);
		if (DestroyObjTimer == 0)
			DestroyObjTimer = DestroyObjDelay;
	}
  
	//when a faction is defeated

	[Command]
	public void CmdFactionDefeated (int FactionID)
	{
		RpcFactionDefeated (FactionID);
	}

	[ClientRpc]
	public void RpcFactionDefeated (int FactionID)
	{
		GameMgr.OnFactionDefeated (FactionID); //defeat the faction in each client's game:
		GameMgr.UIMgr.ShowPlayerMessage("Player (Faction ID:"+FactionID.ToString()+") has disconnected", UIManager.MessageTypes.Error);
	}

	//trying to assign authority:
	public void TryToAssignAuthority (NetworkInstanceId NetID)
	{
		if (isLocalPlayer) {
			CmdAssignAuthority (NetID);
		}
	}
	[Command]
	public void CmdAssignAuthority (NetworkInstanceId NetID)
	{
		GameObject Obj = NetworkServer.FindLocalObject (NetID);
		Obj.gameObject.GetComponent<NetworkIdentity> ().RemoveClientAuthority (Obj.gameObject.GetComponent<NetworkIdentity> ().clientAuthorityOwner);
		Obj.gameObject.GetComponent<NetworkIdentity> ().AssignClientAuthority (connectionToClient);
	}

	/*//Handling attack objects here:
	public void TryToSpawnAttackObj (string AttackObjCode, Vector3 Pos, bool DoDamage, int TargetFactionID, int SourceFactionID, float BuildingDamage, float UnitDamage, Vector3 MvtVector, float MvtSpeed, float DestroyTime, bool DestroyOnDamage, bool DamageOnce)
	{
		if (isLocalPlayer) {
			CmdSpawnAttackObj (AttackObjCode, Pos.x, Pos.y, Pos.z, DoDamage, TargetFactionID, SourceFactionID ,BuildingDamage, UnitDamage, MvtVector.x, MvtVector.y, MvtVector.z, MvtSpeed, DestroyTime, DestroyOnDamage, DamageOnce);
		}
	}

	[Command]
	public void CmdSpawnAttackObj (string AttackObjCode, float PosX, float PosY, float PosZ, bool DoDamage, int TargetFactionID, int SourceFactionID, float BuildingDamage, float UnitDamage, float DirX, float DirY, float DirZ, float MvtSpeed, float DestroyTime, bool DestroyOnDamage, bool DamageOnce)
	{
		int i = 0;
		GameObject AttackObjPrefab = null;
		bool CreateNewAttackObj = true;

		AttackObjsPooling AttackObjsPool = FindObjectOfType (typeof(AttackObjsPooling)) as AttackObjsPooling;

		if (AttackObjsPool != null) {
			if(AttackObjsPool.GetFreeAttackObject (AttackObjCode))
			{
				AttackObjPrefab = AttackObjsPool.GetFreeAttackObject (AttackObjCode).gameObject;
				CreateNewAttackObj = false;
			}
		}
		while (AttackObjPrefab == null && i < AttackObjs.Length) {
			if (AttackObjs [i].GetComponent<AttackObject> ()) {
				if (AttackObjs [i].GetComponent<AttackObject> ().Code == AttackObjCode) {
					AttackObjPrefab = Instantiate (AttackObjs [i]);
					GameMgr = FindObjectOfType (typeof(GameManager)) as GameManager;
					if (AttackObjsPool != null) {
						AttackObjsPool.AttackObjects.Add (AttackObjPrefab.gameObject.GetComponent<AttackObject> ());
					}
				}
			}
			i++;
		}
		if(AttackObjPrefab != null)
		{
			AttackObjPrefab.transform.position = new Vector3 (PosX, PosY, PosZ);
			AttackObjPrefab.GetComponent<AttackObject> ().DoDamage = DoDamage;
			AttackObjPrefab.GetComponent<AttackObject> ().TargetFactionID = TargetFactionID;
			AttackObjPrefab.GetComponent<AttackObject> ().SourceFactionID = SourceFactionID;
			AttackObjPrefab.GetComponent<AttackObject> ().BuildingDamage = BuildingDamage;
			AttackObjPrefab.GetComponent<AttackObject> ().UnitDamage = UnitDamage;
			AttackObjPrefab.GetComponent<AttackObject> ().MvtVector = new Vector3 (DirX, DirY, DirZ);
			AttackObjPrefab.GetComponent<AttackObject> ().Speed = MvtSpeed;
			AttackObjPrefab.GetComponent<AttackObject> ().DestroyTime = DestroyTime;
			AttackObjPrefab.GetComponent<AttackObject> ().DestroyOnDamage = DestroyOnDamage;
			AttackObjPrefab.GetComponent<AttackObject> ().DamageOnce = DamageOnce;
			if (CreateNewAttackObj == false) {
				AttackObjPrefab.GetComponent<NetworkIdentity> ().RemoveClientAuthority (AttackObjPrefab.GetComponent<NetworkIdentity> ().clientAuthorityOwner);
				AttackObjPrefab.GetComponent<NetworkIdentity> ().AssignClientAuthority (GetTargetFactionConnection (TargetFactionID, connectionToClient));
			}
			else
			{
				NetworkServer.SpawnWithClientAuthority (AttackObjPrefab, GetTargetFactionConnection(TargetFactionID, connectionToClient));
			}

			AttackObjPrefab.GetComponent<AttackObject>().RpcActivateAttackObj(PosX, PosY,PosZ);

		}
	}

	public NetworkConnection GetTargetFactionConnection (int ID, NetworkConnection SourceNetConn)
	{
		int i = 0;
		MFactionManager[] FactionMgrs = FindObjectsOfType (typeof(MFactionManager)) as MFactionManager[];
		while (i < FactionMgrs.Length) {
			if (FactionMgrs [i].FactionID == ID) {
				return FactionMgrs [i].connectionToClient;
			}

			i++;
		}

		return SourceNetConn;
	}

	public void TryToHideAttackObj (NetworkInstanceId AttackObjNetID)
	{
		if (isLocalPlayer) {
			CmdHideAttackObj (AttackObjNetID);
		}
	}

	[Command]
	public void CmdHideAttackObj (NetworkInstanceId AttackObjNetID)
	{
		GameObject AttackObj = NetworkServer.FindLocalObject (AttackObjNetID);
		AttackObj.GetComponent<AttackObject> ().RpcHideAttackObj ();
	}*/

	//Audio:

	/*[Command]
	public void CmdSyncAudio (string Type, NetworkInstanceId SourceNetID, int AudioID, int ResourceID, bool Loop)
	{
		if (Type == "Builder" || Type == "Collector" || Type == "Attack") {
			RpcSyncAudio (Type, SourceNetID, AudioID, ResourceID, Loop);
		}
	}

	[ClientRpc]
	public void RpcSyncAudio (string Type, NetworkInstanceId SourceNetID, int AudioID, int ResourceID, bool Loop)
	{
		GameObject SourceObj = ClientScene.FindLocalObject (SourceNetID);
		if (SourceObj != null) {
			if (Type == "Builder") {
				if (SourceObj.gameObject.GetComponent<Builder> ()) {
					if (SourceObj.gameObject.GetComponent<Builder> ().BuildingAudio.Length > AudioID) {
						AudioManager.PlayAudio (SourceObj, SourceObj.GetComponent<Builder> ().BuildingAudio [AudioID], Loop);
					}
				}
			} else if (Type == "Collector") {
				if (SourceObj.gameObject.GetComponent<GatherResource> ()) {
					if (ResourceMgr.ResourcesInfo [ResourceID].CollectionAudio.Length > AudioID) {
						AudioManager.PlayAudio (SourceObj, ResourceMgr.ResourcesInfo [ResourceID].CollectionAudio [AudioID], Loop);
					}
				}
			} else if (Type == "Attack") {
				if (SourceObj.GetComponent<Attack> ()) {
					if (SourceObj.GetComponent<Attack> ().AttackSound) {
						AudioManager.PlayAudio (SourceObj,SourceObj.GetComponent<Attack> ().AttackSound, Loop);
						Debug.LogError ("We're playin");
					}
				}
			}
		}
	}

	[Command]
	public void CmdStopAudio (NetworkInstanceId SourceNetID)
	{
		RpcStopAudio (SourceNetID);
	}

	[ClientRpc]
	public void RpcStopAudio (NetworkInstanceId SourceNetID)
	{
		GameObject SourceObj = ClientScene.FindLocalObject (SourceNetID);
		if (SourceObj != null) {
			AudioManager.StopAudio (SourceObj);
		}
	}*/
}


