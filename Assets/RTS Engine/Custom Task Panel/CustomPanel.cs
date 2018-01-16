using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomPanel : MonoBehaviour {

	List<Building> BuiltBuildings = new List<Building>();
	List<string> BuiltBuildingsCodes = new List<string>();

	class TaskPanelVars 
	{
		public Button TaskButton;
		public int TaskID;
		public List<Building> Building = new List<Building>();
	}
	List<TaskPanelVars> TaskPanel = new List<TaskPanelVars>();

	public Button TaskButton;
	public Transform TaskButtonsParent;
	public GameObject PanelObj;

	TaskManager TaskMgr;
	GameManager GameMgr;

	// Use this for initialization
	void Start () {
		GameMgr = GameManager.Instance;
		TaskMgr = GameMgr.TaskMgr;

		CustomEvents.BuildingBuilt += AddBuildingTasks;
		CustomEvents.BuildingDestroyed += RemoveBuildingTasks;

		TaskPanel.Clear ();

		TaskButton.gameObject.SetActive (false);
	}

	public void ToggleCustomPanel ()
	{
		PanelObj.gameObject.SetActive (!PanelObj.gameObject.activeInHierarchy);

	}

	void AddBuildingTasks (Building Building)
	{
		if (Building.FactionID == GameManager.PlayerFactionID) {
			if (Building.BuildingTasksList.Count > 0) {
				BuiltBuildings.Add (Building);
				if (BuiltBuildingsCodes.Contains (Building.Code) == false) {

					for (int i = 0; i < Building.BuildingTasksList.Count; i++) {
						if (Building.BuildingTasksList [i].TaskType == Building.BuildingTasks.CreateUnit) {
							TaskPanelVars Item = new TaskPanelVars ();
							if (TaskPanel.Count == 0) {
								Item.TaskButton = TaskButton;
								TaskButton.gameObject.SetActive (false);
							} else {
								GameObject NewTaskButton = Instantiate (TaskButton.gameObject);
								Item.TaskButton = NewTaskButton.GetComponent<Button> ();
								Item.TaskButton.transform.SetParent (TaskButtonsParent, true);
								Item.TaskButton.transform.localScale = new Vector3 (1.0f, 1.0f, 1.0f);
							}
							Item.TaskButton.gameObject.GetComponent<Image> ().sprite = Building.BuildingTasksList [i].TaskIcon;
							Item.TaskButton.gameObject.GetComponent<CustomTaskButton> ().ID = TaskPanel.Count;
							Item.TaskButton.gameObject.GetComponent<CustomTaskButton> ().Panel = this;
							Item.TaskButton.gameObject.SetActive (true);
							Item.TaskID = i;
							Item.Building.Add (Building);
							TaskPanel.Add (Item);
						}
					}
				}
				BuiltBuildingsCodes.Add (Building.Code);
			}
		}

	}

	void RemoveBuildingTasks (Building Building)
	{
		if (Building.FactionID == GameManager.PlayerFactionID) {
			
			if (Building.BuildingTasksList.Count > 0) {
				BuiltBuildings.Remove (Building);
				BuiltBuildingsCodes.Remove (Building.Code);
				if (BuiltBuildingsCodes.Contains (Building.Code) == false) {

					int i = 0;
					while (i < TaskPanel.Count) {
						if (TaskPanel [i].Building[0].Code == Building.Code) {
							TaskPanel [i].TaskButton.gameObject.SetActive (false);
							TaskPanel.RemoveAt (i);
						} else {
							i++;
						}
					}
				}
			}
		}

	}

	//check for resources.
	//then check for max task places
	//if building one in list is full then move to building two.
	//if all full print error.
	public void LaunchTask (int ID)
	{
		if (ID < TaskPanel.Count) {
			if (GameMgr.ResourceMgr.CheckResources (TaskPanel [ID].Building [0].BuildingTasksList [TaskPanel [ID].TaskID].RequiredResources, GameManager.PlayerFactionID, 1) == true) {
				if (GameMgr.Factions [GameManager.PlayerFactionID].CurrentPopulation < GameMgr.Factions [GameManager.PlayerFactionID].MaxPopulation) {
					int i = 0;
					bool Found = false;
					while (i < TaskPanel [ID].Building.Count && Found == false) {
						if (TaskPanel [ID].Building != null) {
							if (TaskPanel [ID].Building [i].Health >= TaskPanel [ID].Building [i].MinTaskHealth) {
								if (TaskPanel [ID].Building [i].MaxTasks > TaskPanel [ID].Building [i].TasksQueue.Count) {
									Found = true;

									TaskMgr.LaunchTask (TaskPanel [ID].Building [i], TaskPanel [ID].TaskID, -1, TaskManager.TaskTypes.CreateUnit);
								}
							}
						}
						i++;
					}

					if (Found == false) {
						GameMgr.UIMgr.ShowPlayerMessage ("Buildings that launch this task might have reached the max tasks amount or have not enough health!", UIManager.MessageTypes.Error);
						AudioManager.PlayAudio (GameMgr.GeneralAudioSource.gameObject, TaskPanel [ID].Building [0].DeclinedTaskAudio, false); //Declined task audio.
					}
				} else {
					//max population reached error
					GameMgr.UIMgr.ShowPlayerMessage ("Maximum population has been reached!", UIManager.MessageTypes.Error);
					AudioManager.PlayAudio (GameMgr.GeneralAudioSource.gameObject, TaskPanel [ID].Building [0].DeclinedTaskAudio, false); //Declined task audio.
				}
			} else {
				//not enough resources:
				GameMgr.UIMgr.ShowPlayerMessage ("Not enough resources to launch task!", UIManager.MessageTypes.Error);
				AudioManager.PlayAudio (GameMgr.GeneralAudioSource.gameObject, TaskPanel [ID].Building [0].DeclinedTaskAudio, false); //Declined task audio.
			}
		}
	}
}