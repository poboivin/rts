using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomEvents : MonoBehaviour {

	public bool DebugEnabled = false;

	public delegate void UnitEventHandler(Unit Unit);
	public static event UnitEventHandler UnitCreated = delegate {};
	public static event UnitEventHandler UnitDead = delegate {};
	public static event UnitEventHandler UnitSelected = delegate {};
	public static event UnitEventHandler UnitDeselcted = delegate {};

	public delegate void UnitResourceEventHandler(Unit UnitComp, Resource Resource);
	public static event UnitResourceEventHandler UnitStartCollecting = delegate {};
	public static event UnitResourceEventHandler UnitStopCollecting = delegate {};

	public delegate void UnitBuildingEventHandler (Unit UnitComp, Building Building);
	public static event UnitBuildingEventHandler UnitStartBuilding = delegate {};
	public static event UnitBuildingEventHandler UnitStopBuilding = delegate {};

	public delegate void UnitHealingEventHandler (Unit UnitComp, Unit TargetUnit);
	public static event UnitHealingEventHandler UnitStartHealing = delegate {};
	public static event UnitHealingEventHandler UnitStopHealing = delegate {};

	public delegate void UnitConvertingEventHandler (Unit UnitComp, Unit TargetUnit);
	public static event UnitConvertingEventHandler UnitStartConverting = delegate {};
	public static event UnitConvertingEventHandler UnitStopConverting = delegate {};
	public static event UnitConvertingEventHandler UnitConverted = delegate {};

	public delegate void UnitSwitchingAttackEventHandler (Unit Unit, Attack From, Attack To);
	public static event UnitSwitchingAttackEventHandler UnitSwitchAttack = delegate {};

	public delegate void BuildingEventHandler (Building Building);
	public static event BuildingEventHandler BuildingPlaced = delegate {};
	public static event BuildingEventHandler BuildingBuilt = delegate {};
	public static event BuildingEventHandler BuildingDestroyed = delegate {};
	public static event BuildingEventHandler BuildingSelected = delegate {};
	public static event BuildingEventHandler BuildingDeselected = delegate {};

	public delegate void BuildingUpgradeEventHandler (Building Building, bool Direct);
	public static event BuildingUpgradeEventHandler BuildingStartUpgrade = delegate {};
	public static event BuildingUpgradeEventHandler BuildingCompleteUpgrade = delegate {};

	public delegate void TaskEventHandler (Building Building, Building.BuildingTasksVars Task);
	public static event TaskEventHandler TaskLaunched = delegate {};
	public static event TaskEventHandler TaskCanceled = delegate {};
	public static event TaskEventHandler TaskCompleted = delegate {};

	public delegate void ResourceEventHandler (Resource Resource);
	public static event ResourceEventHandler ResourceEmpty = delegate {};
	public static event ResourceEventHandler ResourceSelected = delegate {};
	public static event ResourceEventHandler ResourceDeselected = delegate {};

	public delegate void APCEventHandler (APC APC, Unit Unit);
	public static event APCEventHandler APCAddUnit = delegate {};
	public static event APCEventHandler APCRemoveUnit = delegate {}; 
	public static event APCEventHandler APCCallUnits = delegate {};

	public delegate void PortalEventHandler (Portal From, Portal To, Unit Unit);
	public static event PortalEventHandler UnitTeleport = delegate {};
	public static event PortalEventHandler PortalDoubleClick = delegate {}; 

	public delegate void GameEventHandler (GameManager.FactionInfo FactionInfo);
	public static event GameEventHandler FactionEliminated = delegate {};
	public static event GameEventHandler FactionWin = delegate {}; 

	public delegate void InvisibilityEventHandler (Unit Unit);
	public static event InvisibilityEventHandler UnitGoInvisible = delegate {};
	public static event InvisibilityEventHandler UnitGoVisible = delegate {};

	public delegate void CustomActionEventHandler (GameObject Source, GameObject Target, int ID);
	public static event CustomActionEventHandler CustomAction = delegate {};


	//Unit custom events:
	public void OnUnitCreated (Unit Unit) //called when a unit is created.
	{
		if (DebugEnabled == true) {
			Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") created");
		}
		UnitCreated (Unit);
	}
	public void OnUnitDead (Unit Unit) //called when a unit is dead
	{
		if (DebugEnabled == true) {
			Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") dead");
		}
		UnitDead (Unit);
	}
	public void OnUnitSelected (Unit Unit) //called when a unit is selected
	{
		if (DebugEnabled == true) {
			Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") selected");
		}
		UnitSelected (Unit);
	}
	public void OnUnitDeselected (Unit Unit) //called when a unit is deselected
	{
		if (DebugEnabled == true) {
			Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") deselected");
		}
		UnitDeselcted (Unit);
	}

	//Unit-Resource events:
	public void OnUnitStartCollecting (Unit Unit, Resource Resource) //called when a unit starts collecting a resource 
	{
		if (DebugEnabled == true) {
			Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") start collecting resource: '"+Resource.Name+"' (ID: "+Resource.ID+")");
		}
		UnitStartCollecting (Unit, Resource);
	}
	public void OnUnitStopCollecting (Unit Unit, Resource Resource) //called when a unit stops collecting a resource
	{
		if (DebugEnabled == true) {
			Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") stopped collecting resource: '"+Resource.Name+"' (ID: "+Resource.ID+")");
		}
		UnitStopCollecting (Unit, Resource);
	}

	//Unit-Building events:
	public void OnUnitStartBuilding (Unit Unit, Building Building) //called when a unit starts constructing a building
	{
		if (DebugEnabled == true) {
			Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") started constructing building: '"+Building.Name+"'");
		}
		UnitStartBuilding (Unit, Building);
	}
	public void OnUnitStopBuilding (Unit Unit, Building Building) //called when a unit stops constructing a building
	{
		if (DebugEnabled == true) {
			Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") stopped constructing building: '"+Building.Name+"'");
		}
		UnitStopBuilding (Unit, Building);
	}

	//Portal:
	public void OnUnitTeleport (Portal From, Portal To, Unit Unit) //called when a unit teleports in a portal
	{
		if (DebugEnabled == true) {
			Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") teleported from: '"+From.Name+"' to '"+From.Name+"'");
		}
		UnitTeleport (From,To,Unit);
	}
	public void OnPortalDoubleClick (Portal From, Portal To, Unit Unit) //called when a unit teleports in a portal
	{
		if (DebugEnabled == true) {
			Debug.Log ("Moved camera view from '"+From.Name+"' to '"+From.Name+"'");
		}
		PortalDoubleClick (From,To,Unit);
	}

	//Attack:
	public void OnUnitSwitchAttack (Unit Unit, Attack From, Attack To) //called when a unit switchs attack type:
	{
		if (DebugEnabled == true) {
			Debug.Log ("Unit '"+ Unit.Name +"' (Faction ID: "+Unit.FactionID+") has changed its attack type");
		}
		UnitSwitchAttack (Unit,From,To);
	}

	//Unit-Healing events:
	public void OnUnitStartHealing (Unit Unit, Unit TargetUnit) //called when a unit starts healing another unit
	{
		if (DebugEnabled == true) {
			Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") started healing unit: '"+TargetUnit.Name+"'");
		}
		UnitStartHealing (Unit, TargetUnit);
	}
	public void OnUnitStopHealing (Unit Unit, Unit TargetUnit) //called when a unit stops healing another unit
	{
		if (DebugEnabled == true) {
			Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") stopped healing unit: '"+TargetUnit.Name+"'");
		}
		UnitStopHealing (Unit, TargetUnit);
	}

	//Unit-Converting events:
	public void OnUnitStartConverting (Unit Unit, Unit TargetUnit) //called when a unit starts converting another unit
	{
		if (DebugEnabled == true) {
			Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") started converting unit: '"+TargetUnit.Name+"'");
		}
		UnitStartConverting (Unit, TargetUnit);
	}
	public void OnUnitStopConverting (Unit Unit, Unit TargetUnit) //called when a unit stops converting another unit
	{
		if (DebugEnabled == true) {
			Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") stopped converting unit: '"+TargetUnit.Name+"'");
		}
		UnitStopConverting (Unit, TargetUnit);
	}
	public void OnUnitConverted (Unit Unit, Unit TargetUnit) //called when a unit is converted
	{
		if (DebugEnabled == true) {
			Debug.Log (TargetUnit.Name+" has been converted.");
		}
		UnitConverted (Unit, TargetUnit);
	}

	//Building custom events:
	public void OnBuildingPlaced (Building Building) //called when a building is placed:
	{
		if (DebugEnabled == true) {
			Debug.Log ("Building '"+Building.Name+"' (Faction ID "+Building.FactionID+") placed");
		}
		BuildingPlaced (Building);
	}
	public void OnBuildingBuilt (Building Building) //called when a building is built:
	{
		if (DebugEnabled == true) {
			Debug.Log ("Building '"+Building.Name+"' (Faction ID "+Building.FactionID+") built");
		}
		BuildingBuilt (Building);
	}
	public void OnBuildingDestroyed (Building Building) //called when a building is placed:
	{
		if (DebugEnabled == true) {
			Debug.Log ("Building '"+Building.Name+"' (Faction ID "+Building.FactionID+") destroyed");
		}
		BuildingDestroyed (Building);
	}
	public void OnBuildingSelected (Building Building) //called when a building is placed:
	{
		if (DebugEnabled == true) {
			Debug.Log ("Building '"+Building.Name+"' (Faction ID "+Building.FactionID+") selected");
		}
		BuildingSelected (Building);
	}
	public void OnBuildingDeselected (Building Building) //called when a building is placed:
	{
		if (DebugEnabled == true) {
			Debug.Log ("Building '"+Building.Name+"' (Faction ID "+Building.FactionID+") deselected");
		}
		BuildingDeselected (Building);
	}
	public void OnBuildingStartUpgrade (Building Building, bool Direct) //called when a building starts the process of an upgrade:
	{
		if (DebugEnabled == true) {
			Debug.Log("Building '"+Building.Name+"' (Faction ID "+Building.FactionID+") started an upgrade.");
		}
		BuildingStartUpgrade(Building, Direct);
	}
	public void OnBuildingCompleteUpgrade (Building Building, bool Direct) //called when a building starts the process of an upgrade:
	{
		if (DebugEnabled == true) {
			Debug.Log("Building '"+Building.Name+"' (Faction ID "+Building.FactionID+") is the result of a building upgrade.");
		}
		BuildingCompleteUpgrade(Building, Direct);
	}

	//APC:
	public void OnAPCAddUnit (APC APC, Unit Unit) //called when an APC adds a unit.
	{
		if (DebugEnabled == true) {
			string APCName = "";
			if (APC.gameObject.GetComponent<Unit> ()) {
				APCName = APC.gameObject.GetComponent<Unit> ().Name;
			} else if (APC.gameObject.GetComponent<Building> ()) {
				APCName = APC.gameObject.GetComponent<Building> ().Name;
			}
			Debug.Log("APC '"+APCName+"' added unit: "+Unit.Name);
		}
		APCAddUnit(APC, Unit);
	}
		
	public void OnAPCRemoveUnit (APC APC, Unit Unit) //called when an APC removes a unit.
	{
		if (DebugEnabled == true) {
			string APCName = "";
			if (APC.gameObject.GetComponent<Unit> ()) {
				APCName = APC.gameObject.GetComponent<Unit> ().Name;
			} else if (APC.gameObject.GetComponent<Building> ()) {
				APCName = APC.gameObject.GetComponent<Building> ().Name;
			}
			Debug.Log("APC '"+APCName+"' removed unit: "+Unit.Name);
		}
		APCRemoveUnit(APC, Unit);
	}

	public void OnAPCCallUnits (APC APC, Unit Unit) //called when an APC removes a unit (Unit here is irrelevant)
	{
		if (DebugEnabled == true) {
			string APCName = "";
			if (APC.gameObject.GetComponent<Unit> ()) {
				APCName = APC.gameObject.GetComponent<Unit> ().Name;
			} else if (APC.gameObject.GetComponent<Building> ()) {
				APCName = APC.gameObject.GetComponent<Building> ().Name;
			}
			Debug.Log("APC '"+APCName+"' is calling for units.");
		}
		APCCallUnits(APC, Unit);
	}

	//Task Events:
	public void OnTaskLaunched (Building Building, Building.BuildingTasksVars Task) //called when a building launches a task
	{
		if (DebugEnabled == true) {
			Debug.Log ("Building '"+Building.Name+"' launched a task.");
		}
		TaskLaunched (Building, Task);
	}
	public void OnTaskCanceled (Building Building, Building.BuildingTasksVars Task) //called when a building cancels a task
	{
		if (DebugEnabled == true) {
			Debug.Log ("Building '"+Building.Name+"' canceled a pending task.");
		}
		TaskCanceled (Building, Task);
	}
	public void OnTaskCompleted (Building Building, Building.BuildingTasksVars Task) //called when a building completes a task
	{
		if (DebugEnabled == true) {
			Debug.Log ("Building '"+Building.Name+"' completed a task.");
		}
		TaskCompleted (Building, Task);
	}

	//Resource events:
	public void OnResourceEmpty (Resource Resource) //called when a resource is empty
	{
		if (DebugEnabled == true) {
			Debug.Log ("Resource '"+Resource.Name+"' is now empty");
		}
		ResourceEmpty (Resource);
	}
	public void OnResourceSelected (Resource Resource) //called when a resource is selected
	{
		if (DebugEnabled == true) {
			Debug.Log ("Resource '"+Resource.Name+"' is selected");
		}
		ResourceSelected (Resource);
	}
	public void OnResourceDeselected (Resource Resource) //called when a resource is desselected
	{
		if (DebugEnabled == true) {
			Debug.Log ("Resource '"+Resource.Name+"' is deselected");
		}
		ResourceDeselected (Resource);
	}

	//Game events:
	public void OnFactionEliminated (GameManager.FactionInfo FactionInfo)
	{
		if (DebugEnabled == true) {
			Debug.Log ("Faction: " + FactionInfo.Name + " has been eliminated from the game.");
		}
		FactionEliminated (FactionInfo);
	}
	public void OnFactionWin (GameManager.FactionInfo FactionInfo)
	{
		if (DebugEnabled == true) {
			Debug.Log ("Faction: " + FactionInfo.Name + " won the game.");
		}
		FactionWin (FactionInfo);
	}

	//Invisibility events:
	public void OnUnitGoInvisible (Unit Unit)
	{
		if (DebugEnabled == true) {
			Debug.Log ("Unit: " + Unit.Name + " just went invisible.");
		}
		UnitGoInvisible (Unit);
	}
	public void OnUnitGoVisible (Unit Unit)
	{
		if (DebugEnabled == true) {
			Debug.Log ("Unit: " + Unit.Name + " just went visible.");
		}
		UnitGoVisible (Unit);
	}

	//custom action events:
	public void OnCustomAction (GameObject Source, GameObject Target, int ID)
	{
		/*
		 * Reserved custom action IDs:
		 * 0-10: reserved for the "MultipleAttacks.cs" component.
		 * 11: converting units
		 * 12: toggling invisibility
		 * 13-30: remove APC units
		 * 31-50: launch research task:
		*/

		//Multiple Attacks actions:
		if (Source != null) {
			Unit SourceUnit = Source.gameObject.GetComponent<Unit>();
			if (ID >= 0 && ID <= 10) {
				if (SourceUnit.MultipleAttacksMgr) {
					SourceUnit.MultipleAttacksMgr.EnableAttackTypeLocal (ID);
				}
			}
			else if (ID == 11) {
				Unit Converter = Target.gameObject.GetComponent<Unit>();
				if (Converter != null) {
					SourceUnit.ConvertUnitLocal (Converter);
				}
			}
			else if (ID == 12) {
				if (SourceUnit.InvisibilityMgr) {
					SourceUnit.InvisibilityMgr.ToggleInvisibilityLocal ();
				}
			}

			//APC:
			if (Source.GetComponent<APC> ()) { 
				if (ID >= 13 && ID <= 30) { //remove units from APC: 17 slots
					Source.GetComponent<APC> ().RemoveUnitFromAPCLocal (ID - 13);
				}
			}

			//Building research task:
			if (Source.GetComponent<Building> ()) { 
				if (ID >= 31 && ID <= 50) { //remove units from APC: 17 slots
					Source.GetComponent<Building> ().LaunchResearchTaskLocal (ID - 31);
				}
			}
		}
			

		CustomAction (Source, Target, ID);
	}
}
