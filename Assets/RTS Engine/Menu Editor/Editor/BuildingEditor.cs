using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

/* Building Editor script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

[CustomEditor(typeof(Building))]
public class BuildingEditor : Editor {

	public SerializedProperty BuildingStates;
	public SerializedProperty BuildingResources;
	public SerializedProperty BuildingTasks;
	public SerializedProperty BonusResources;
	public SerializedProperty FactionColors;
	public SerializedProperty ConstructionStates;
	public SerializedProperty UpgradeBuildingResources;
	public SerializedProperty UpgradeRequiredBuildings;

	int TaskID = 0;

	private ReorderableList TestList;

	public override void OnInspectorGUI ()
	{
		Building Target = (Building)target;

		GUIStyle TitleGUIStyle = new GUIStyle ();
		TitleGUIStyle.fontSize = 20;
		TitleGUIStyle.alignment = TextAnchor.MiddleCenter;
		TitleGUIStyle.fontStyle = FontStyle.Bold;

		EditorGUILayout.LabelField ("Building:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		TitleGUIStyle.fontSize = 15;
		EditorGUILayout.LabelField ("General Building Settings:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		Target.Name = EditorGUILayout.TextField ("Building Name: ", Target.Name);
		Target.Code = EditorGUILayout.TextField ("Building Code: ", Target.Code);
		Target.Category = EditorGUILayout.TextField ("Building Category: ", Target.Category);
		Target.Description = EditorGUILayout.TextField ("Building Description: ", Target.Description);
		EditorGUILayout.LabelField ("Building Icon:");
		Target.Icon = EditorGUILayout.ObjectField (Target.Icon, typeof(Sprite), true) as Sprite;
		Target.FreeBuilding = EditorGUILayout.Toggle ("Free Building? (Belongs to no faction)", Target.FreeBuilding);
		Target.CanBeAttacked = EditorGUILayout.Toggle ("Can Be Attacked? ", Target.CanBeAttacked);
		Target.TaskPanelCategory = EditorGUILayout.IntField ("Task Panel Category: ", Target.TaskPanelCategory);
		Target.MaxBuilders = EditorGUILayout.IntField ("Max Builders Amount: ", Target.MaxBuilders);
		Target.MinCenterDistance = EditorGUILayout.FloatField ("Minimum Center Distance: ", Target.MinCenterDistance);
		//Target.MaxBuildingDistance = EditorGUILayout.FloatField ("Maximum Building Distance: ", Target.MaxBuildingDistance);
		//Target.InteractionAreaSize = EditorGUILayout.FloatField ("Interaction Area Size: ", Target.InteractionAreaSize);
		Target.FactionID = EditorGUILayout.IntField ("Faction ID: ", Target.FactionID);
		Target.PlacedByDefault = EditorGUILayout.Toggle ("Placed by default?", Target.PlacedByDefault);
		Target.AddPopulation = EditorGUILayout.IntField ("Population Slots To Add: ", Target.AddPopulation);
		Target.ResourceDropOff = EditorGUILayout.Toggle ("Is Resource Drop Off?", Target.ResourceDropOff);

		BuildingResources = serializedObject.FindProperty("BuildingResources");
		EditorGUILayout.PropertyField (BuildingResources, true);
		serializedObject.ApplyModifiedProperties();

		BonusResources = serializedObject.FindProperty("BonusResources");
		EditorGUILayout.PropertyField (BonusResources, true);
		serializedObject.ApplyModifiedProperties();

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.LabelField ("Building Health Settings:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		Target.MaxHealth = EditorGUILayout.FloatField ("Maximum Building Health: ", Target.MaxHealth);
		Target.MinTaskHealth = EditorGUILayout.FloatField ("Minimum Health To Launch Task: ", Target.MinTaskHealth);
		Target.HealthBarYPos = EditorGUILayout.FloatField ("Health Bar Height (Position): ", Target.HealthBarYPos);

		BuildingStates = serializedObject.FindProperty("BuildingStates");
		EditorGUILayout.LabelField ("Building States Parent Obj:");
		Target.BuildingStatesParent = EditorGUILayout.ObjectField (Target.BuildingStatesParent, typeof(GameObject), true) as GameObject;
		EditorGUILayout.PropertyField (BuildingStates, true);
		serializedObject.ApplyModifiedProperties();

		EditorGUILayout.LabelField ("Building Destruction:");
		EditorGUILayout.LabelField ("Destruction Sound Effect:");
		Target.DestructionAudio = EditorGUILayout.ObjectField (Target.DestructionAudio, typeof(AudioClip), true) as AudioClip;
		EditorGUILayout.LabelField ("Destruction Object:");
		Target.DestructionObj = EditorGUILayout.ObjectField (Target.DestructionObj, typeof(GameObject), true) as GameObject;

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.LabelField ("Building Upgrade:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		Target.DirectUpgrade = EditorGUILayout.Toggle ("Upgrade Building Directly?", Target.DirectUpgrade);
		EditorGUILayout.LabelField ("Upgrade Building:");
		Target.UpgradeBuilding = EditorGUILayout.ObjectField (Target.UpgradeBuilding, typeof(Building), true) as Building;
		UpgradeBuildingResources = serializedObject.FindProperty("BuildingUpgradeResources");
		EditorGUILayout.PropertyField (UpgradeBuildingResources, true);
		serializedObject.ApplyModifiedProperties();
		UpgradeRequiredBuildings = serializedObject.FindProperty("UpgradeRequiredBuildings");
		EditorGUILayout.PropertyField (UpgradeRequiredBuildings, true);
		serializedObject.ApplyModifiedProperties();
		Target.BuildingUpgradeReload = EditorGUILayout.FloatField ("Building Upgrade Duration: ", Target.BuildingUpgradeReload);
		Target.UpgradeAllBuildings = EditorGUILayout.Toggle ("Upgrade All Buildings?", Target.UpgradeAllBuildings);

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.LabelField ("Building Tasks:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		Target.MaxTasks = EditorGUILayout.IntField ("Max Simultaneous Tasks: ", Target.MaxTasks);

		BuildingTasks = serializedObject.FindProperty("BuildingTasksList");
		EditorGUILayout.PropertyField (BuildingTasks, true);
		serializedObject.ApplyModifiedProperties();

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.LabelField ("Building Components:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		EditorGUILayout.LabelField ("Building Plane:");
		Target.BuildingPlane = EditorGUILayout.ObjectField (Target.BuildingPlane, typeof(GameObject), true) as GameObject;
		EditorGUILayout.LabelField ("Building Selection Component:");
		Target.PlayerSelection = EditorGUILayout.ObjectField (Target.PlayerSelection, typeof(SelectionObj), true) as SelectionObj;
		EditorGUILayout.LabelField ("Construction Object:");
		Target.ConstructionObj = EditorGUILayout.ObjectField (Target.ConstructionObj, typeof(GameObject), true) as GameObject;
		ConstructionStates = serializedObject.FindProperty("ConstructionStates");
		EditorGUILayout.PropertyField (ConstructionStates, true);
		serializedObject.ApplyModifiedProperties();
		EditorGUILayout.LabelField ("Building Damage Effect:");
		Target.DamageEffect = EditorGUILayout.ObjectField (Target.DamageEffect, typeof(GameObject), true) as GameObject;
		EditorGUILayout.LabelField ("Units Spawn Position:");
		Target.SpawnPosition = EditorGUILayout.ObjectField (Target.SpawnPosition, typeof(Transform), true) as Transform;
		EditorGUILayout.LabelField ("Units Goto Position (right after spawning):");
		Target.GotoPosition = EditorGUILayout.ObjectField (Target.GotoPosition, typeof(Transform), true) as Transform;
		FactionColors = serializedObject.FindProperty("FactionColorObjs");
		EditorGUILayout.PropertyField (FactionColors, true);
		serializedObject.ApplyModifiedProperties();

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.LabelField ("Audio Clips:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		EditorGUILayout.LabelField ("Selection Sound Effect:");
		Target.SelectionAudio = EditorGUILayout.ObjectField (Target.SelectionAudio, typeof(AudioClip), true) as AudioClip;
		EditorGUILayout.LabelField ("Launch Task Sound Effect:");
		Target.LaunchTaskAudio = EditorGUILayout.ObjectField (Target.LaunchTaskAudio, typeof(AudioClip), true) as AudioClip;
		EditorGUILayout.LabelField ("Declined Task Sound Effect:");
		Target.DeclinedTaskAudio = EditorGUILayout.ObjectField (Target.DeclinedTaskAudio, typeof(AudioClip), true) as AudioClip;

		//Gizmos:
		Target.AllowBuildingDistanceGizmos = EditorGUILayout.Toggle("Enable Gizomos for Max Building Distance?", Target.AllowBuildingDistanceGizmos);
		if (Target.AllowBuildingDistanceGizmos == true) {
			Target.BuildingDistanceGizmosColor = EditorGUILayout.ColorField ("Gizmos Color: ", Target.BuildingDistanceGizmosColor);
		}

		EditorUtility.SetDirty (Target);
	}

	public void ChangeTaskID (int Value, int Max)
	{
		int ProjectedID = TaskID + Value;
		if (ProjectedID < Max && ProjectedID >= 0) {
			TaskID = ProjectedID;
		}
	}
}
