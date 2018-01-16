using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/* Attack Editor script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

[CustomEditor(typeof(Attack))]
public class AttackEditor : Editor {

	public SerializedProperty AttackCategories;
	public SerializedProperty AttackSources;
	public SerializedProperty AreaDamage;

	public override void OnInspectorGUI ()
	{
		Attack Target = (Attack)target;

		GUIStyle TitleGUIStyle = new GUIStyle ();
		TitleGUIStyle.fontSize = 20;
		TitleGUIStyle.alignment = TextAnchor.MiddleCenter;
		TitleGUIStyle.fontStyle = FontStyle.Bold;

		EditorGUILayout.LabelField ("Attack Component:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		EditorGUILayout.LabelField ("Attack Icon:");
		Target.AttackIcon = EditorGUILayout.ObjectField (Target.AttackIcon, typeof(Sprite), true) as Sprite;

		Target.AttackAllTypes = EditorGUILayout.Toggle ("Attack All Types?", Target.AttackAllTypes);
		if (Target.AttackAllTypes == false) {
			AttackCategories = serializedObject.FindProperty("AttackCategoriesList");
			EditorGUILayout.PropertyField (AttackCategories, true);
			serializedObject.ApplyModifiedProperties();
		}

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		Target.MinUnitStoppingDistance = EditorGUILayout.FloatField ("Min Units Stopping Distance: ", Target.MinUnitStoppingDistance);
		Target.MaxUnitStoppingDistance = EditorGUILayout.FloatField ("Max Units Stopping Distance: ", Target.MaxUnitStoppingDistance);

		Target.MinBuildingStoppingDistance = EditorGUILayout.FloatField ("Min Buildings Stopping Distance: ", Target.MinBuildingStoppingDistance);
		Target.MaxBuildingStoppingDistance = EditorGUILayout.FloatField ("Max Buildings Stopping Distance: ", Target.MaxBuildingStoppingDistance);

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		Target.AttackOnAssign = EditorGUILayout.Toggle ("Attack On Assign?", Target.AttackOnAssign);
		Target.AttackWhenAttacked = EditorGUILayout.Toggle ("Attack When Attacked?", Target.AttackWhenAttacked);
		Target.AttackInRange = EditorGUILayout.Toggle ("Attack In Range?", Target.AttackInRange);
		Target.AttackRange = EditorGUILayout.FloatField ("Attack Range:", Target.AttackRange);
		Target.FollowRange = EditorGUILayout.FloatField ("Follow Distance:", Target.FollowRange);
		Target.SearchReload = EditorGUILayout.FloatField ("Search Reload:", Target.SearchReload);

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		Target.AreaDamage = EditorGUILayout.Toggle ("Area Damage?", Target.AreaDamage);
		if (Target.AreaDamage == true) {
			AreaDamage = serializedObject.FindProperty("AttackRanges");
			EditorGUILayout.PropertyField (AreaDamage, true);
			serializedObject.ApplyModifiedProperties();
		} else {
			Target.BuildingDamage = EditorGUILayout.FloatField ("Building Damage:", Target.BuildingDamage);
			Target.UnitDamage = EditorGUILayout.FloatField ("Unit Damage:", Target.UnitDamage);
		}

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		Target.DirectAttack = EditorGUILayout.Toggle ("Direct Attack?", Target.DirectAttack);
		Target.MoveOnAttack = EditorGUILayout.Toggle ("Move On Attack?", Target.MoveOnAttack);
		Target.AttackReload = EditorGUILayout.FloatField ("Attack Reload", Target.AttackReload);
		Target.AttackType = (Attack.AttackTypes) EditorGUILayout.EnumPopup ("Attack Type:", Target.AttackType);

		EditorGUILayout.Space ();

		AttackSources = serializedObject.FindProperty("AttackSources");
		EditorGUILayout.PropertyField (AttackSources, true);
		serializedObject.ApplyModifiedProperties();

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		Target.AttackAnimTime = EditorGUILayout.FloatField ("Attack Animation Duration:", Target.AttackAnimTime);

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		EditorGUILayout.LabelField ("Attack Order Audio:");
		Target.AttackOrderSound = EditorGUILayout.ObjectField (Target.AttackOrderSound, typeof(AudioClip), true) as AudioClip;
		EditorGUILayout.LabelField ("Attack Audio:");
		Target.AttackSound = EditorGUILayout.ObjectField (Target.AttackSound, typeof(AudioClip), true) as AudioClip;

		EditorUtility.SetDirty (Target);
	}
}
