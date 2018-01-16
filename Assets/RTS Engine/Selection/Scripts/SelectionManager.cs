using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/* Selection Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

public class SelectionManager : MonoBehaviour {

	public LayerMask RaycastLayerMask; //Make sure the raycast doesn't include the "Building" and "Resource" layers.
	public GameObject TerrainObj;
	public GameObject AirTerrain; //Terrain object for flying units (it does not have to be a terrain object, it can be a simple plane but make sure to bake it all as walkable in the navmesh).
	//the air terrain helps to seperate the movement of flying units and normal units.

	//Selection textures for buildings, units and resources:
	public Texture2D UnitSelectionTexture;
	public Texture2D BuildingSelectionTexture;
	public Texture2D ResourceSelectionTexture;
	
	[HideInInspector]
	public Building SelectedBuilding;
	[HideInInspector]
	public List<Unit> SelectedUnits;
	public class UnitTypes
	{
		public string Code;
		public float MinUnitStoppingDistance;
		public float MinBuildingStoppingDistance;
		public List<Unit> UnitsList;
	}
	[HideInInspector]
	public List<UnitTypes> SelectedUnitTypes;
	[HideInInspector]
	public Resource SelectedResource;

	//Selection Box:
	public Image SelectionBox;
	public RectTransform Canvas;
	//Hold the first and last mouse position when creating the selection box:
	Vector3 FirstMousePos;
	Vector3 LastMousePos;
	bool CreatedSelectionBox = false;
	[HideInInspector]
	public bool SelectionBoxEnabled = false;
	public float MinBoxSize = 1.0f; //Holds the minimal selection box to draw it.
	public KeyCode SelectionKey = KeyCode.LeftShift; //Key used to select multiple units.
	[HideInInspector]
	public bool SelectionKeyDown = false;

	//Mvt target effect:
	public MvtTargetEffect MvtTargetEffectObj;

	//double selection range:
	public float DoubleClickSelectSize = 10.0f;

	//Raycast: We'll need those two many times.
	RaycastHit Hit;
	Ray RayCheck;

	GameManager GameMgr;
	[HideInInspector]
	public UIManager UIMgr;
	ResourceManager ResourceMgr;
	BuildingPlacement BuildingMgr;
	UnitManager UnitMgr;

	void Awake () 
	{
		GameMgr = GameManager.Instance;
		UIMgr = GameMgr.UIMgr;
		ResourceMgr = GameMgr.ResourceMgr;
		BuildingMgr = GameMgr.BuildingMgr;
		UnitMgr = GameMgr.UnitMgr;

		SelectionKeyDown = false;
	}


	void Update () 
	{
		//Checking if the selection key is held down or not!
		SelectionKeyDown = Input.GetKey (SelectionKey);

		if (BuildingPlacement.IsBuilding == false && GameMgr.GameEnded == false) { //If we are not placing a building.
			if (!EventSystem.current.IsPointerOverGameObject ()) {
			//We check if the player hasn't clicked a building or a unit by drawing a ray from the mouse position:
			if (Input.GetMouseButtonDown (1) || Input.GetMouseButtonDown (0)) {
				RayCheck = Camera.main.ScreenPointToRay (Input.mousePosition);
				if (Physics.Raycast (RayCheck, out Hit, 80.0f, RaycastLayerMask.value)) {  
					//If the ray doesn't hit a building, a unit object and any UI object:
						SelectionObj HitObj = Hit.transform.gameObject.GetComponent<SelectionObj> ();
						Unit HitUnit = null;
						Building HitBuilding = null;
						Resource HitResource = null;
						if (HitObj != null) {
							HitUnit = HitObj.MainObj.GetComponent<Unit> ();
							HitBuilding = HitObj.MainObj.GetComponent<Building> ();
							HitResource = HitObj.MainObj.GetComponent<Resource> ();
						}

						if (Input.GetMouseButtonDown (1)) {
							if (UnitMgr.AwaitingTaskType != TaskManager.TaskTypes.Null) { //if we click with the right mouse button while having an awaiting component task..
								UnitMgr.ResetAwaitingTaskType (); //reset it.
							} else {
								if (HitObj != null) {
									if (HitBuilding != null) {
										ActionOnBuilding (HitBuilding, TaskManager.TaskTypes.Null);
									} else if (HitResource != null) {
										ActionOnResource (HitResource, TaskManager.TaskTypes.Null);
									} else if (HitUnit != null) {
										ActionOnUnit (HitUnit, TaskManager.TaskTypes.Null);
									}
								} else {
									//Moving selected units:
									//The position which the unit will move to will be determined by a ray coming out from the mouse to the terrain object. 
									if (Hit.transform.gameObject == TerrainObj) { //make sure that the terrain is hit
										MoveSelectedUnits (Hit.point);
									}
									//If we're currently selecting a building:
									if (SelectedBuilding != null) {
										//If the building has been already placed.
										if (SelectedBuilding.IsBuilt == true) {
											//If it has a go to position:
											if (SelectedBuilding.Rallypoint != null && SelectedBuilding.FactionID == GameManager.PlayerFactionID) {
												if (HitObj == null) { //then the player clicked on an empty point of the map
													//Move the goto position:
													SelectedBuilding.GotoPosition.gameObject.SetActive (true);
													SelectedBuilding.GotoPosition.position = Hit.point;
													SelectedBuilding.Rallypoint = SelectedBuilding.GotoPosition;
												} else { //the player has clicked on either a building or a resource:
													if (HitBuilding) { //if it's a building
														//check if the building belongs to this faciton
														if (HitBuilding.FactionID == GameManager.PlayerFactionID) {
															SelectedBuilding.GotoPosition.gameObject.SetActive (false);
															SelectedBuilding.Rallypoint = HitBuilding.transform;

															//Make the building plane flash to indicate that it has been selected to be contruscted:
															HitBuilding.BuildingPlane.GetComponent<Renderer> ().material.color = BuildingMgr.SelectionFlashColor;
															HitBuilding.FlashTime = BuildingMgr.SelectionFlashTime;
															HitBuilding.InvokeRepeating ("SelectionFlash", 0.0f, BuildingMgr.SelectionFlashRepeat);
														}
													} else if (HitResource) {
														if (HitResource.FactionID == GameManager.PlayerFactionID) {
															SelectedBuilding.GotoPosition.gameObject.SetActive (false);
															SelectedBuilding.Rallypoint = HitResource.transform;

															HitResource.ResourcePlane.GetComponent<Renderer> ().material.color = ResourceMgr.SelectionFlashColor;
															HitResource.FlashTime = ResourceMgr.SelectionFlashTime;
															HitResource.InvokeRepeating ("SelectionFlash", 0.0f, ResourceMgr.SelectionFlashRepeat);
														}
													}
												}
											}
										}
									}
								}
							}
						} else if (Input.GetMouseButtonDown (0)) {
							
							if ((SelectionKeyDown == false || HitUnit == null) && UnitMgr.AwaitingTaskType == TaskManager.TaskTypes.Null) {
								DeselectUnits ();
							}
							DeselectBuilding ();
							DeselectResource ();

							if (HitObj != null) {
								//If we selected a building or a resource, update the selection info:
								if (HitBuilding) {
									if (UnitMgr.AwaitingTaskType != TaskManager.TaskTypes.Null) { //if the player assigned selected unit(s) to do a component task:
										ActionOnBuilding (HitBuilding, UnitMgr.AwaitingTaskType);
									} else {
										HitObj.SelectObj ();
										AudioManager.PlayAudio (GameMgr.GeneralAudioSource.gameObject, HitBuilding.SelectionAudio, false);
										if (HitBuilding.PortalMgr) { //if this is a portal building:
											//trigger a mouse click:
											HitBuilding.PortalMgr.TriggerMouseClick ();
										}
									}
								} else if (HitResource) {
									if (UnitMgr.AwaitingTaskType != TaskManager.TaskTypes.Null) { //if the player assigned selected unit(s) to do a component task:
										ActionOnResource (HitResource, UnitMgr.AwaitingTaskType);
									} else {
										HitObj.SelectObj ();
										AudioManager.PlayAudio (GameMgr.GeneralAudioSource.gameObject, ResourceMgr.ResourcesInfo [HitResource.ResourceID].SelectionAudio, false);
									}
								} else if (HitUnit) {
									if (UnitMgr.AwaitingTaskType != TaskManager.TaskTypes.Null) { //if the player assigned selected unit(s) to do a component task:
										ActionOnUnit (HitUnit, UnitMgr.AwaitingTaskType);
									} else {
										HitObj.SelectObj ();
										if (HitUnit.FactionID == GameManager.PlayerFactionID) {
											AudioManager.PlayAudio (GameMgr.GeneralAudioSource.gameObject, HitUnit.SelectionAudio, false);
										}
									}
								}
							} else if (UnitMgr.AwaitingTaskType == TaskManager.TaskTypes.Mvt)  { //if the pending comp task is a mvt one
								MoveSelectedUnits (Hit.point); //move the selected units.
							}

							//resets the awaiting task type
							if (UnitMgr.AwaitingTaskType != TaskManager.TaskTypes.Null) {
								UnitMgr.ResetAwaitingTaskType ();
							}
						}
					}
				}
			}

		}


		/***********************************************************************************************************************************************/
		//Selection Box:

		if (SelectionBox != null) {

			if (Input.GetMouseButton (0)) { //If the player is holding the left mouse button.
				//If we haven't created the selection box yet
				if (CreatedSelectionBox == false) {
					//Create the selection and save the initial mouse position on the screen:
					FirstMousePos = Input.mousePosition;

					CreatedSelectionBox = true;
				}
				//Check if the box size is above the minimal size.
				if (Vector3.Distance (FirstMousePos, Input.mousePosition) > MinBoxSize) {
					SelectionBoxEnabled = true;
					//Activate the selection box object if it's not activated.
					if (SelectionBox.gameObject.activeSelf == false) {
						SelectionBox.gameObject.SetActive (true);
					}

					LastMousePos = Input.mousePosition; //Always save the last mouse position.

					Vector3 Center = (FirstMousePos + Input.mousePosition) / 2 - Canvas.localPosition; //Calculate the center of the selection box.
					SelectionBox.GetComponent<RectTransform> ().localPosition = Center; //Set the selection position.

					//Calculate the box's size in the canvas:
					Vector3 CurrentMousePosUI = Input.mousePosition - Canvas.localPosition;
					Vector3 FirstMousePosUI = FirstMousePos - Canvas.localPosition;

					//Set the selection box size in the canvas.
					SelectionBox.GetComponent<RectTransform> ().sizeDelta = new Vector2 (Mathf.Abs (CurrentMousePosUI.x - FirstMousePosUI.x), Mathf.Abs (CurrentMousePosUI.y - FirstMousePosUI.y));

				}
			}

			//If the player releases the mouse button:
			if (Input.GetMouseButtonUp (0) && CreatedSelectionBox == true) {
				CreatedSelectionBox = false;
				SelectionBoxEnabled = false;

				//We'll check if he had selected units:
				if (Vector3.Distance (FirstMousePos, Input.mousePosition) > MinBoxSize) {
					bool SelectionBoxReady = false; //True when we are able to detect objects inside the selection box.

					Vector3 Center = Vector3.zero;
					Vector3 Corner = Vector3.zero;

					//We'll use a raycast which will detect the terrain objects and then allow us to look for the objects inside the selection box.
					RaycastHit[] Hits;
					RayCheck = Camera.main.ScreenPointToRay ((FirstMousePos + LastMousePos) / 2);
					Hits = Physics.RaycastAll (RayCheck, 100.0f);

					//First we'll send a raycast from the center of the selection box.
					if (Hits.Length > 0) {
						for (int i = 0; i < Hits.Length; i++) {
							int TerrainLayer = LayerMask.NameToLayer ("FlatTerrain");
							if (Hits [i].transform.gameObject.layer == TerrainLayer) {
								Center = Hits [i].point;
								Center.y = 0.0f; 

								SelectionBoxReady = true;

								//The raycast hit the terrain object so we save the hit point position and move to the next step.
							}
						}
					}


					//If the last step was successfully completed.
					if (SelectionBoxReady == true) {

						DeselectBuilding ();

						SelectionBoxReady = false;

						//We'll send another raycast from one of the corners of the selectin box.
						RayCheck = Camera.main.ScreenPointToRay (FirstMousePos);
						Hits = Physics.RaycastAll (RayCheck, 100.0f);

						if (Hits.Length > 0) {
							for (int i = 0; i < Hits.Length; i++) {
								int TerrainLayer = LayerMask.NameToLayer ("FlatTerrain");
								if (Hits [i].transform.gameObject.layer == TerrainLayer) {
									Corner = Hits [i].point;
									Corner.y = 0.0f; 

									SelectionBoxReady = true;

									//Step successful when the raycast hit the terrain object, so we save the hit point position.
								}
							}
						}

						//If the player is holding the multiple selection key, we'll keep the previosuly selected units, if not we won't
						if (SelectionKeyDown == false) {
							DeselectUnits ();
						}

						//If both of the above steps have been successful: 
						if (SelectionBoxReady == true) {
							//All set.

							//Search for all objects in range of the selection box.
							Collider[] ObjsInSelection = Physics.OverlapSphere (Center, Vector3.Distance (Center, Corner));
							if (ObjsInSelection.Length > 0) {
								//Create a new list to select units in range: 
								List<Unit> UnitsInRange = new List<Unit> ();
								for (int i = 0; i < ObjsInSelection.Length; i++) {
									//Search for the units objects only and add them to the previous list:
									Unit SelectedUnit = ObjsInSelection [i].gameObject.GetComponent<Unit> ();
									if (SelectedUnit) {
										//Only select units belonging to the player's team.
										if (SelectedUnit.FactionID == GameManager.PlayerFactionID) {
											UnitsInRange.Add (SelectedUnit);
										}
									}
								}



								//Check if there are units in the selection box:
								if (UnitsInRange.Count > 0) {
									Vector3 BoxCenter = (LastMousePos + FirstMousePos) / 2 - Canvas.localPosition;

									if (FirstMousePos.x >= LastMousePos.x) {
										if (FirstMousePos.y >= LastMousePos.y) {
											FirstMousePos = LastMousePos;
										} else {
											FirstMousePos.x = LastMousePos.x;
										}
									}

									if (FirstMousePos.x <= LastMousePos.x) {
										if (FirstMousePos.y >= LastMousePos.y) {
											FirstMousePos.y = LastMousePos.y;
										}
									}

									Vector3 BoxCorner = FirstMousePos - Canvas.localPosition;



									for (int i = 0; i < UnitsInRange.Count; i++) {									
										//Convert the unit's world position to screen position:
										Vector3 ScreenPos = Camera.main.WorldToScreenPoint (UnitsInRange [i].transform.position);
										Vector3 CanvasPos = ScreenPos - Canvas.localPosition;
										if (CanvasPos.x > BoxCorner.x && CanvasPos.x <= (BoxCorner.x + Mathf.Abs (BoxCorner.x - BoxCenter.x) * 2) && CanvasPos.y >= BoxCorner.y && CanvasPos.y <= (BoxCorner.y + Mathf.Abs (BoxCorner.y - BoxCenter.y) * 2)) {
											//If unit is in selection box, then add it to the selected units:
											SelectUnit (UnitsInRange [i], true);
										}
									}


									//Only play the selection audio if the unit belongs to the player's faction:
									if (SelectedUnits.Count > 0) {
										AudioManager.PlayAudio (GameMgr.GeneralAudioSource.gameObject,SelectedUnits[0].SelectionAudio, false);
									}
								}

								UnitsInRange.Clear ();
							}


						} else {
							// Error when we couldnt hit the terrain object with the ray.
						}

					} else {
						// Error when we couldnt hit the terrain object with the ray.
					}

				}
				//Desactivate the selection box:
				SelectionBox.gameObject.SetActive (false);
			}
		}
	}

	//-----------------------------------------------------------------------------------------------------------------------------------------------------------
	void ActionOnBuilding (Building HitBuilding, TaskManager.TaskTypes TaskType)
	{
		//when TaskType is null that means that all tasks are allowed to be launched.
		bool ShowFriendlySelection = false;
		if (SelectedUnits.Count > 0) { //Also make sure, at least a unit has been selected
			if (GameManager.PlayerFactionID == SelectedUnits [0].FactionID) { //Units from the player team can be moved by the player, others can't.
				if (SelectedUnits [0].FactionID == HitBuilding.FactionID) { //Make sure that the units and the building are from the same team.
					//If the selected units can construct and the building needs construction & the task type is the build one

					if (HitBuilding.Health < HitBuilding.MaxHealth && (TaskType == TaskManager.TaskTypes.Null || TaskType == TaskManager.TaskTypes.Build)) {
						if (HitBuilding.CurrentBuilders.Count < HitBuilding.MaxBuilders) {
							int i = 0; //counter
							bool MaxBuildersReached = false; //true when the maximum amount of builders for the hit building has been reached.
							bool SentBuilder = false;
							int TempBuilderCounter = HitBuilding.CurrentBuilders.Count;
							while (i < SelectedUnits.Count && MaxBuildersReached == false) { //loop through the selected as long as the max builders amount has not been reached.
								if (SelectedUnits [i].BuilderMgr && SelectedUnits[i].CanBeMoved == true) { //check if this unit has a builder comp (can actually build).
									//invisibility check:
									if (SelectedUnits [i].IsInvisible == false || (SelectedUnits [i].IsInvisible == true && SelectedUnits [i].InvisibilityMgr.CanBuild)) {
										//make sure that the maximum amount of builders has not been reached:
										if (TempBuilderCounter < HitBuilding.MaxBuilders) {
											//Make the units fix/build the building:
											SelectedUnits [i].BuilderMgr.SetTargetBuilding (HitBuilding);
											SentBuilder = true;
											TempBuilderCounter++;
										} else {
											MaxBuildersReached = true;
											//if the max builders amount has been reached.
											//Show this message: 
											UIMgr.ShowPlayerMessage ("Max building amount for building has been reached!", UIManager.MessageTypes.Error);

										}
									}
								}

								i++;
							}

							if (SentBuilder == true) {
								ShowFriendlySelection = true;
							}

							AudioManager.PlayAudio (GameMgr.GeneralAudioSource.gameObject, BuildingMgr.SendToBuildAudio, false);
						} else {
							UIMgr.ShowPlayerMessage ("Max building amount for building has been reached!", UIManager.MessageTypes.Error);
						}
					} else if (HitBuilding.GetComponent<APC> () && (TaskType == TaskManager.TaskTypes.Null || TaskType == TaskManager.TaskTypes.Mvt)) {
						//APC:
						int i = 0;
						APC CurrentAPC = HitBuilding.GetComponent<APC> ();
						while (i < SelectedUnits.Count && CurrentAPC.MaxAmount > CurrentAPC.CurrentUnits.Count) { //loop through the selected units as long as the APC still have space
							if (!SelectedUnits [i].gameObject.GetComponent<APC> () && CurrentAPC.AllowedUnitsCategories.Contains (SelectedUnits [i].Category) && SelectedUnits[i].CanBeMoved == true) { //if the selected unit is no APC and its category matches the allowed categories in the targer APC.
								//send the unit to the APC vehicule:
								SelectedUnits [i].TargetAPC = CurrentAPC;
								SelectedUnits [i].CheckUnitPath (CurrentAPC.transform.position, CurrentAPC.gameObject, GameManager.Instance.MvtStoppingDistance, i, true);
							}

							i++;
						}

						ShowFriendlySelection = true;
					} 
				} else {
					if (HitBuilding.GetComponent<Portal> () && (TaskType == TaskManager.TaskTypes.Null || TaskType == TaskManager.TaskTypes.Mvt)) {
						//Portal:
						Portal CurrentPortal = HitBuilding.GetComponent<Portal> ();
						for (int i = 0; i < SelectedUnits.Count; i++) { //loop through the selected units
							if (CurrentPortal.IsAllowed (SelectedUnits [i]) && SelectedUnits[i].CanBeMoved == true) { //if the selected unit's category matches the allowed categories in the target portal.
								SelectedUnits [i].CheckUnitPath (CurrentPortal.transform.position, CurrentPortal.gameObject, GameManager.Instance.MvtStoppingDistance, i, true);
							}
						}

						ShowFriendlySelection = true;
					} else if(TaskType == TaskManager.TaskTypes.Null || TaskType == TaskManager.TaskTypes.Attack) {
						//If the building is from a different team, we'll see if the unit can attack or not
						if (HitBuilding.Health > 0 && HitBuilding.CanBeAttacked == true) { //If the selected units can build and building has health	
							//Make sure it's not peace time:
							if (GameMgr.PeaceTime == 0) {
								LaunchAttack (SelectedUnits, HitBuilding.gameObject, true);
							} else {
								UIMgr.ShowPlayerMessage ("Can't attack in peace time!", UIManager.MessageTypes.Error);
							}
						}
					}
				}
			} 
		}

		if (ShowFriendlySelection == true) {
			//Make the building plane flash to indicate that it has been selected to be contruscted:
			HitBuilding.BuildingPlane.GetComponent<Renderer> ().material.color = GameMgr.Factions[SelectedUnits[0].FactionID].FactionColor;
			HitBuilding.FlashTime = BuildingMgr.SelectionFlashTime;
			HitBuilding.InvokeRepeating ("SelectionFlash", 0.0f, BuildingMgr.SelectionFlashRepeat);
		}
	}

	public void ActionOnResource (Resource HitResource, TaskManager.TaskTypes TaskType)
	{
		if (SelectedUnits.Count > 0) { //Also make sure, at least a unit has been selected
			if (TaskType == TaskManager.TaskTypes.Null || TaskType == TaskManager.TaskTypes.Collect) { //if the player comp task is a collect resource one or null
				if (GameManager.PlayerFactionID == SelectedUnits [0].FactionID) { //Units from the player team can be moved by the player, others can't.
					if(HitResource.FactionID == GameManager.PlayerFactionID || HitResource.CollectOutsideBorder == true)
					{
						if (HitResource.Amount > 0) { //If the selected units can gather resources, and make sure that the player can actually pick this up								
							if (HitResource.CurrentCollectors.Count < HitResource.MaxCollectors) { //Make sure that there's still room for another collectors:

								int i = 0; //counter
								bool MaxCollectorsReached = false; //true when the maximum amount of collectors for the hit resources has been reached.
								bool SentCollector = false;
								int TempBuilderCounter = HitResource.CurrentCollectors.Count;
								while (i < SelectedUnits.Count && MaxCollectorsReached == false) { //loop through the selected as long as the max collectors amount has not been reached.
									if (SelectedUnits [i].ResourceMgr && SelectedUnits [i].CanBeMoved == true) { //check if this unit has a gather resource comp (can actually build).
										//invisibility check:
										if (SelectedUnits [i].IsInvisible == false || (SelectedUnits [i].IsInvisible == true && SelectedUnits [i].InvisibilityMgr.CanCollect)) {
											//make sure that the maximum amount of collectors has not been reached:
											if (TempBuilderCounter < HitResource.MaxCollectors) {
												//Collect the resource:
												SelectedUnits [i].ResourceMgr.SetTargetResource (HitResource);
												SentCollector = true;
												TempBuilderCounter++;

											} else {
												MaxCollectorsReached = true;
												//if the max collectors amount has been reached.
												//Show this message: 
												UIMgr.ShowPlayerMessage ("Max amount of collectors has been reached!", UIManager.MessageTypes.Error);

											}
										}
									}

									i++;
								}

								if (SentCollector == true) {
									HitResource.ResourcePlane.GetComponent<Renderer> ().material.color = ResourceMgr.SelectionFlashColor;
									HitResource.FlashTime = ResourceMgr.SelectionFlashTime;
									HitResource.InvokeRepeating ("SelectionFlash", 0.0f, ResourceMgr.SelectionFlashRepeat);
								}

								AudioManager.PlayAudio (GameMgr.GeneralAudioSource.gameObject, ResourceMgr.ResourcesInfo [HitResource.ResourceID].SendToCollectAudio, false);
							} else {
								//Inform the player that there's no room for another collector in this resource:
								UIMgr.ShowPlayerMessage ("Max amount of collectors has been reached!", UIManager.MessageTypes.Error);
							}
						}
						else {
							UIMgr.ShowPlayerMessage ("The targer resource is empty!", UIManager.MessageTypes.Error);
						}
					} else {
						UIMgr.ShowPlayerMessage ("The target resource is outside your faction's borders", UIManager.MessageTypes.Error);
					}
				}
			}
		}
	}

	public void ActionOnUnit (Unit HitUnit, TaskManager.TaskTypes TaskType)
	{
		if (SelectedUnits.Count > 0) { //Also make sure, at least a unit has been selected
			bool ShowFriendlySelection = false;
			if (GameManager.PlayerFactionID == SelectedUnits [0].FactionID) { //Units from the player team can be moved by the player, others can't.
				if (HitUnit.FactionID != SelectedUnits [0].FactionID) { //If the target unit is not dead and has different team I
					if (HitUnit.Dead == false) {
						if ((TaskType == TaskManager.TaskTypes.Null || TaskType == TaskManager.TaskTypes.Convert) && SelectedUnits [0].ConvertMgr != null) { //if the pending comp task is a convert one
							if (SelectedUnits.Count == 1 && SelectedUnits [0].CanBeMoved == true) { //if one unit is selected and it has the convert component
								//invisibility check:
								if (SelectedUnits [0].IsInvisible == false || (SelectedUnits [0].IsInvisible == true && SelectedUnits [0].InvisibilityMgr.CanConvert)) {
									SelectedUnits [0].ConvertMgr.SetTargetUnit (HitUnit); //set the target unit to convert.
									ShowFriendlySelection = true;
								}
							}
						} else if(TaskType == TaskManager.TaskTypes.Null || TaskType == TaskManager.TaskTypes.Attack) { //else if the pending comp task is an attack one.
							//launch an attack if the peace time is over.
							if (GameMgr.PeaceTime == 0.0f) {
								//make sure the target unit can be attacked:
								if (HitUnit.IsInvisible == false) {
									LaunchAttack (SelectedUnits, HitUnit.gameObject, true);
									ShowFriendlySelection = true;
								}
							} else {
								UIMgr.ShowPlayerMessage ("Can't attack in peace time!", UIManager.MessageTypes.Error);
							}
						}
					}
				} else { //if the hit unit belongs to the player's faction
					if (HitUnit.APCMgr != null && (TaskType == TaskManager.TaskTypes.Null || TaskType == TaskManager.TaskTypes.Mvt)) { //if the selected unit has a APC comp and the pending task is a mvt one
						//APC:
						int i = 0;
						APC CurrentAPC = HitUnit.GetComponent<APC> ();
						while (i < SelectedUnits.Count && CurrentAPC.MaxAmount > CurrentAPC.CurrentUnits.Count) { //loop through the selected units as long as the APC still have space
							if (!SelectedUnits [i].gameObject.GetComponent<APC> () && CurrentAPC.AllowedUnitsCategories.Contains (SelectedUnits [i].Category) && SelectedUnits [i].CanBeMoved == true) { //if the selected unit is no APC and its category matches the allowed categories in the targer APC.
								//send the unit to the APC vehicule:
								SelectedUnits [i].TargetAPC = CurrentAPC;
								SelectedUnits [i].CheckUnitPath (CurrentAPC.transform.position, CurrentAPC.gameObject, GameManager.Instance.MvtStoppingDistance, i, true);
							}

							i++;
						}

						ShowFriendlySelection = true;
					} else if(TaskType == TaskManager.TaskTypes.Null || TaskType == TaskManager.TaskTypes.Heal) { //if the selected unit(s) have a healer component
						for (int i = 0; i < SelectedUnits.Count; i++) {
							if (SelectedUnits [i].CanBeMoved == true && SelectedUnits [i].HealMgr != null) {
								//invisibility check:
								if (SelectedUnits [i].IsInvisible == false || (SelectedUnits [i].IsInvisible == true && SelectedUnits [i].InvisibilityMgr.CanHeal)) {
									SelectedUnits [i].HealMgr.SetTargetUnit (HitUnit); //heal the target unit
									ShowFriendlySelection = true;
								}
							}
						}
					}
				}

			}
			if (ShowFriendlySelection == true) {
				//Make the building plane flash to indicate that it has been selected to be contruscted:
				HitUnit.UnitPlane.GetComponent<Renderer> ().material.color = GameMgr.Factions [SelectedUnits [0].FactionID].FactionColor;
				HitUnit.FlashTime = BuildingMgr.SelectionFlashTime;
				HitUnit.InvokeRepeating ("SelectionFlash", 0.0f, BuildingMgr.SelectionFlashRepeat);
			}
		}
	}

	//a method that allows selected units to move to a target destination:
	public void MoveSelectedUnits (Vector3 Destination)
	{
		if (SelectedUnits.Count > 0) { //make sure, at least a unit has been selected
			if (GameManager.PlayerFactionID == SelectedUnits [0].FactionID) { //Units from the player team can be moved by the player, others can't.
				SortUnitsByDistance (ref SelectedUnits, Destination);

				float StoppingDistance = GameMgr.MvtStoppingDistance;
				float Perimeter = SelectedUnits [0].NavAgent.radius * Mathf.PI;
				int Amount = Mathf.RoundToInt (Perimeter / (SelectedUnits [0].NavAgent.radius * 2));
				int Cercle = 1;

				for (int i = 0; i < SelectedUnits.Count; i++) {
					if (SelectedUnits [i].CanBeMoved == true) {
						//SelectedUnits [i].NavObstacle.enabled = false;
						//Inform the units about the target position to go to and they'll see if there's a valid path to go there:
						if (StoppingDistance < SelectedUnits [i].NavAgent.radius * 2) {
							StoppingDistance = SelectedUnits [i].NavAgent.radius * 2;
						}

						SelectedUnits [i].CheckUnitPath (Destination, null, StoppingDistance, i, true);
						Amount--;

						//SelectedUnits [i].NavAgent.avoidancePriority = 50;


						if (Amount <= 0) {
							Cercle += 2;
							Perimeter = SelectedUnits [0].NavAgent.radius * Cercle * Mathf.PI;
							Amount = Mathf.RoundToInt (Perimeter / (SelectedUnits [0].NavAgent.radius * 2.5f));
							StoppingDistance += SelectedUnits [0].NavAgent.radius * 2;
						}

						//Show the mvt target effect:
						if (MvtTargetEffectObj != null) {
							MvtTargetEffectObj.transform.position = Hit.point;
							MvtTargetEffectObj.Activate ();
						}
					}
				}
			}

		}
	}

	//Resource selection:
	public void SelectResource (Resource Resource)
	{
		if(SelectedResource != null) DeselectResource(); //Deselect the currently selected resource.
		DeselectUnits(); //Deselect currently selected units.
		DeselectBuilding(); //Deselect buildings.

		if (Resource.ResourcePlane) {
			//Activate the resource's plane object where we will show the selection texture.
			Resource.ResourcePlane.SetActive (true);

			//Show the selection texture and set its color.
			Resource.ResourcePlane.GetComponent<Renderer> ().material.mainTexture = ResourceSelectionTexture;
			//Set the selection color to the resource color:
			Color SelectionColor = ResourceMgr.ResourceColor;
			Resource.ResourcePlane.GetComponent<Renderer> ().material.color = new Color (SelectionColor.r, SelectionColor.g, SelectionColor.b, 0.5f);
		}

		SelectedResource = Resource;
		//Selected UI:
		UIMgr.UpdateResourceUI (Resource);

		//custom event:
		GameMgr.Events.OnResourceSelected(Resource);
	}

	public void DeselectResource ()
	{
		if (SelectedResource != null) {
			UIMgr.HideTaskButtons ();
			UIMgr.HideSelectionInfoPanel ();
		}

		//Deselect the resource by hiding the resource plane:
		if (SelectedResource != null) {
			GameMgr.Events.OnResourceDeselected (SelectedResource);
			if (SelectedResource.ResourcePlane)
				SelectedResource.ResourcePlane.SetActive (false);
		}
		SelectedResource = null;
	}

	//building selection:
	public void SelectBuilding (Building Building)
	{
		DeselectUnits ();

		//If the building has been already placed.
		if (Building.Placed == true) {

			if(SelectedBuilding != null) DeselectBuilding(); //Deselect the currently selected building.
			DeselectUnits(); //Deselect currently selected units.
			DeselectResource(); //Deselect the selected resource if there is any

			if (Building.BuildingPlane) {
				//Activate the building's plane object where we will show the selection texture.
				Building.BuildingPlane.SetActive (true);

				//Show the selection texture and set its color.
				Building.BuildingPlane.GetComponent<Renderer> ().material.mainTexture = BuildingSelectionTexture;
				//Set the selection color to the building's team color:
				Color SelectionColor = new Color();
				if (Building.FreeBuilding == false) {
					SelectionColor = GameMgr.Factions [Building.FactionID].FactionColor;
				} else {
					SelectionColor = GameMgr.BuildingMgr.FreeBuildingSelectionColor;
				}
				Building.BuildingPlane.GetComponent<Renderer> ().material.color = new Color (SelectionColor.r, SelectionColor.g, SelectionColor.b, 0.5f);
			}

			SelectedBuilding = Building;
			//Building UI:
			UIMgr.UpdateBuildingUI (Building);
			//If it has a go to position and if the building is already built:
			if (SelectedBuilding.GotoPosition != null && SelectedBuilding.GotoPosition == SelectedBuilding.Rallypoint && SelectedBuilding.IsBuilt == true && Building.FactionID == GameManager.PlayerFactionID) {
				//Show the goto position:
				SelectedBuilding.GotoPosition.gameObject.SetActive(true);
			}

			//custom event:
			if(GameMgr.Events) GameMgr.Events.OnBuildingSelected(Building);

		}
	}

	public void DeselectBuilding ()
	{
		UIMgr.HideTaskButtons ();
		UIMgr.HideSelectionInfoPanel ();
		UIMgr.TaskInfoMenu.gameObject.SetActive (false);

		//Deselect the building by hiding the building plane:
		if (SelectedBuilding != null) {
			
			if (SelectedBuilding.BuildingPlane)
				SelectedBuilding.BuildingPlane.SetActive (false);
			//If it has a go to position:
			if (SelectedBuilding.GotoPosition != null) {
				//Hide the goto position:
				SelectedBuilding.GotoPosition.gameObject.SetActive(false);
			}

			if(GameMgr.Events) GameMgr.Events.OnBuildingDeselected(SelectedBuilding);
		}

		//custom event:
		SelectedBuilding = null;
	
	}

	//select units having the same code in a certain range:
	public void SelectUnitsInRange (Unit Unit)
	{
		if (GameMgr.Factions[GameManager.PlayerFactionID].FactionMgr.Units.Count > 0) {
			for (int x = 0; x < GameMgr.Factions[GameManager.PlayerFactionID].FactionMgr.Units.Count; x++) { //go through the present units in the scene
				Unit ThisUnit = GameMgr.Factions[GameManager.PlayerFactionID].FactionMgr.Units[x];
				if (Vector3.Distance (ThisUnit.transform.position, Unit.transform.position) <= DoubleClickSelectSize) {
					if (ThisUnit.Code == Unit.Code) {
						SelectUnit (ThisUnit, true);

					}
				}
			}
		}
	}

	//unit selection:
	public void SelectUnit(Unit Unit, bool Add)
	{
		//stop the cam from following the last selected unit it was already doing that.
		GameMgr.CamMov.UnitToFollow = null;

		DeselectBuilding ();
		//If the unit is already selected
		if (IsUnitSelected (Unit) == true && SelectionKeyDown == true) {
			//Deselect it:
			DeselectUnit(Unit);
			return;
		}
		//If we're adding units to the current selection.
		if (Add == true && SelectedUnits.Count > 0) {
			//Make sure they belong to the same team:
			if(Unit.FactionID != SelectedUnits[0].FactionID)
			{
				return; //Don't select this unit.
			}
		}

		if(Add == false || SelectedUnits.Count == 0) //If we choose to select this unit only or simply if a building was selected
		{
		    DeselectBuilding(); //Deselect the currently selected building.
			DeselectResource();
		    DeselectUnits(); //Deselect currently selected units.
		}


		if (Unit.UnitPlane) {
			//Activate the unit's plane object where we will show the selection texture.
			Unit.UnitPlane.SetActive (true);

			//Show the selection texture and set its color.
			Unit.UnitPlane.GetComponent<Renderer> ().material.mainTexture = UnitSelectionTexture;
			//Set the selection color to the building's team color:
			Color SelectionColor = new Color();
			if (Unit.FreeUnit == false) {
				SelectionColor = GameMgr.Factions [Unit.FactionID].FactionColor;
			} else {
				SelectionColor = GameMgr.UnitMgr.FreeUnitSelectionColor;
			}
			Unit.UnitPlane.GetComponent<Renderer> ().material.color = new Color (SelectionColor.r, SelectionColor.g, SelectionColor.b, 0.5f);
		}

		SelectedUnits.Add (Unit);

		//Unit UI:
		UIMgr.UpdateUnitUI (SelectedUnits[0]);

		//custom event to alert that we selected a unit:
		if(GameMgr.Events) GameMgr.Events.OnUnitSelected(Unit);
	}

	//Called to deselect all units:
	public void DeselectUnits ()
	{
		//stop the cam from following the last selected unit it was already doing that.
		GameMgr.CamMov.UnitToFollow = null;

		UIMgr.HideTaskButtons ();
		UIMgr.HideSelectionInfoPanel ();

		//Deselect all the units:
		if (SelectedUnits.Count > 0) //Loop through all selected units then 
		{
			for (int i = 0; i < SelectedUnits.Count; i++) {
				
				if (SelectedUnits [i].UnitPlane) {
					SelectedUnits [i].UnitPlane.SetActive (false);
				}

				if(GameMgr.Events) GameMgr.Events.OnUnitDeselected (SelectedUnits [i]);
			}

		}

		SelectedUnits.Clear ();
	}

	//called to deselect one unit
	public void DeselectUnit (Unit Unit)
	{
		//stop the cam from following the last selected unit it was already doing that.
		GameMgr.CamMov.UnitToFollow = null;

		if (IsUnitSelected(Unit) == true) //Make sure that the unit is selected
		{
			if (SelectedUnits.Count == 1) {
				//If it's the only unit selected, deselect all units:
				DeselectUnits ();
			} else {
				SelectedUnits.Remove (Unit);
				Unit.UnitPlane.SetActive (false);

				//Unit UI:
				UIMgr.UpdateUnitUI (SelectedUnits[0]);

				if(GameMgr.Events) GameMgr.Events.OnUnitDeselected (Unit);
			}
		}
	}

	//check if a unit is selected:
	public bool IsUnitSelected (Unit Unit)
	{
		//See if a unit is selected or not.
		if (SelectedUnits.Count > 0) {
			bool Found = false;
			int i = 0;
			//loop through all the units
			while (i < SelectedUnits.Count && Found == false) {
				//look for the unit
				if (SelectedUnits [i] == Unit) {
					Found = true;
				}
				i++;
			}
			return Found;
		} else {
			return false;
		}
	}

	//get the selected unit ID.
	public int GetSelectedUnitID (Unit Unit)
	{
		//Get the ID of a selected unit.
		if (SelectedUnits.Count > 0) {
			int i = 0;
			while (i < SelectedUnits.Count) {
				if (SelectedUnits [i] == Unit) {
					return i;
				}
				i++;
			}
			return -1;
		} else {
			return -1;
		}
	}

	//Sort a list of units depending on the distance between each unit and a target destination:
	public void SortUnitsByDistance(ref List<Unit> Units, Vector3 Destination)
	{
		if (Units.Count > 1) {
			int i = 0;
			while(i < Units.Count-1) {
				if (Units [i] != null) {
					float Distance = Vector3.Distance (Units [i].transform.position, Destination);
					int current = i;
					for (int j = i + 1; j < Units.Count; j++) {

						if (Units [j] != null) {
							if (Distance > Vector3.Distance (Units [j].transform.position, Destination)) {
								current = j;
								Distance = Vector3.Distance (Units [j].transform.position, Destination);
							}
						}
					}

					if (current != i) {
						Unit SwapUnit = Units [current];
						Units [current] = Units [i];
						Units [i] = SwapUnit;
					}


				}
				i++;
			}
		}
	}

	//Set selected units types:
	public List<UnitTypes> SetSelectedUnitsTypes (List<Unit> SelectedUnits)
	{
		List<UnitTypes> SelectedUnitTypes = new List<UnitTypes> ();

		//First, make lists of the selected units based on their type (code):
		if (SelectedUnits.Count > 0) {
			for (int i = 0; i < SelectedUnits.Count; i++) {
				if (SelectedUnits [i] != null) {

					bool Continue = true;
					//if the unit can't attack assigned targets then don't add it to the list.
					if (SelectedUnits [i].AttackMgr) {
						if (SelectedUnits [i].AttackMgr.AttackOnAssign == false) {
							Continue = false;
						}
					}

					if (Continue == true) {
						int j = 0;
						bool Found = false;
						//add units that has the requested code to the list:
						while (j < SelectedUnitTypes.Count && Found == false) {
							if (SelectedUnitTypes [j].Code == SelectedUnits [i].Code) {
								//Add it to this list type:
								SelectedUnitTypes [j].UnitsList.Add (SelectedUnits [i]);
								Found = true;
							}
							j++;
						}

						if (Found == false) {
							UnitTypes NewUnitType = new UnitTypes ();
							NewUnitType.Code = SelectedUnits [i].Code;
							if (SelectedUnits [i].AttackMgr) {
								NewUnitType.MinUnitStoppingDistance = SelectedUnits [i].AttackMgr.MinUnitStoppingDistance;
								NewUnitType.MinBuildingStoppingDistance = SelectedUnits [i].AttackMgr.MinBuildingStoppingDistance;
							}
							NewUnitType.UnitsList = new List<Unit> ();
							NewUnitType.UnitsList.Add (SelectedUnits [i]);

							SelectedUnitTypes.Add (NewUnitType);
						}
					}
				}
			}
		}

		return SelectedUnitTypes;
	}
		
	public void SortSelectedUnitTypes (int SortBy, ref List<UnitTypes> SelectedUnitTypes) //SortBy = 1 means sort by min unit stopping distance, SortBy = 2, means sory by min building stopping distance
	{
		if (SortBy != 1 && SortBy != 2) {
			return;
		}

		if (SelectedUnitTypes.Count > 3) {
			//divide the selected units into lists depending on their codes.
			for (int i = 0; i < SelectedUnitTypes.Count-1; i++) {
				int current = i;
				for (int j = i+1; j < SelectedUnitTypes.Count; j++) {

					if (SortBy == 1) {
						if (SelectedUnitTypes [current].MinUnitStoppingDistance > SelectedUnitTypes [j].MinUnitStoppingDistance) {
							current = j;
						}
					} else if (SortBy == 2) {
						if (SelectedUnitTypes [current].MinBuildingStoppingDistance > SelectedUnitTypes [j].MinBuildingStoppingDistance) {
							current = j;
						}
					}
				}

				if (current != i) {
					UnitTypes SwapUnitType = SelectedUnitTypes [current];
					SelectedUnitTypes [current] = SelectedUnitTypes [i];
					SelectedUnitTypes [i] = SwapUnitType;
				}
			}
		}

	}
		
	//make a list of units launch an attack on a target object:
	public void LaunchAttack (List<Unit> SelectedUnits, GameObject TargetObj, bool ChangeTarget)
	{
		List<UnitTypes> SelectedUnitTypes = new List<UnitTypes>();
		SelectedUnitTypes = SetSelectedUnitsTypes (SelectedUnits);

		if (SelectedUnitTypes.Count > 0) {
			SortSelectedUnitTypes (2, ref SelectedUnitTypes); //sort the selected units by type


			//go through each unit type list:
			for (int j = 0; j < SelectedUnitTypes.Count; j++) {
				if (SelectedUnitTypes [j].UnitsList [0].GetComponent<Attack> ()) {
					SortUnitsByDistance (ref SelectedUnitTypes [j].UnitsList, TargetObj.transform.position); //sort the selected units by distance:

					List<Unit> CurrentUnitList = SelectedUnitTypes [j].UnitsList;

					//set the stopping distance depending on the unit's distance from the target.
					float StoppingDistance = CurrentUnitList[0].NavAgent.radius;
					if (TargetObj.GetComponent<Unit> ()) {
						StoppingDistance += TargetObj.GetComponent<Unit> ().NavAgent.radius + SelectedUnitTypes [j].MinUnitStoppingDistance;
					} else if (TargetObj.GetComponent<Building> ()) {
						StoppingDistance += SelectedUnitTypes [j].MinBuildingStoppingDistance;
					}

					float Perimeter = CurrentUnitList[0].NavAgent.radius* Mathf.PI;
					int Amount = Mathf.RoundToInt (Perimeter / (CurrentUnitList[0].NavAgent.radius * 2));
					int Cercle = 1;

					for (int i = 0; i < CurrentUnitList.Count; i++) {

						if (CurrentUnitList [i] != null) {
							//make sure that this unit can attack when invisible:
							if (CurrentUnitList [i].IsInvisible == false || (CurrentUnitList [i].IsInvisible == true && CurrentUnitList [i].InvisibilityMgr.CanAttack == true)) {
								if ((ChangeTarget == false && CurrentUnitList [i].AttackMgr.AttackTarget == null) || ChangeTarget == true) {
									//check if the unit can actually attack the target:
									if (UnitManager.Instance.CanAttackTarget (TargetObj, CurrentUnitList [i].gameObject.GetComponent<Attack> ().AttackCategoriesList) == true || CurrentUnitList [i].gameObject.GetComponent<Attack> ().AttackAllTypes == true) {
										//Make the units attack the building/unit

										CurrentUnitList [i].AttackMgr.TargetAssigned = true;
										CurrentUnitList [i].AttackMgr.SetAttackTarget (TargetObj);

										/*CurrentUnitList [i].NavAgent.avoidancePriority = GameManager.Instance.UnitMgr.MinArmyPriority + i;
									if (CurrentUnitList [i].NavAgent.avoidancePriority > 99) {
										CurrentUnitList [i].NavAgent.avoidancePriority = 99;
									}*/

										if (StoppingDistance < CurrentUnitList [i].NavAgent.radius * 2) {
											StoppingDistance = CurrentUnitList [i].NavAgent.radius * 2;
										}
										CurrentUnitList [i].CheckUnitPath (Vector3.zero, TargetObj, StoppingDistance, i, true);
									
										Amount--;

										if (Amount == 0) {
											Cercle += 2;
											Perimeter = CurrentUnitList [0].NavAgent.radius * Cercle * Mathf.PI;
											Amount = Mathf.RoundToInt (Perimeter / (CurrentUnitList [0].NavAgent.radius * 2.5f));
											StoppingDistance += CurrentUnitList [0].NavAgent.radius * 2;
										}
									} else {
										if (CurrentUnitList [i].FactionID == GameManager.PlayerFactionID) {
											UIMgr.ShowPlayerMessage ("Unit can't attack that target!", UIManager.MessageTypes.Error);
										}
									}
								}
							}
						}
					}
				}
			}

			if (SelectedUnits [0]) {
				if (SelectedUnits [0].FactionID == GameManager.PlayerFactionID) {
					if (TargetObj.GetComponent<Building> ()) {
						TargetObj.GetComponent<Building> ().BuildingPlane.GetComponent<Renderer> ().material.color = GameMgr.AttackSelectionColor;
						TargetObj.GetComponent<Building> ().FlashTime = BuildingMgr.SelectionFlashTime;
						TargetObj.GetComponent<Building> ().InvokeRepeating ("SelectionFlash", 0.0f, BuildingMgr.SelectionFlashRepeat);
					} else if (TargetObj.GetComponent<Unit> ()) {
						TargetObj.GetComponent<Unit> ().UnitPlane.GetComponent<Renderer> ().material.color = GameMgr.AttackSelectionColor;
						TargetObj.GetComponent<Unit> ().FlashTime = BuildingMgr.SelectionFlashTime;
						TargetObj.GetComponent<Unit> ().InvokeRepeating ("SelectionFlash", 0.0f, BuildingMgr.SelectionFlashRepeat);
					}
				}
			}

		}
	}
}
