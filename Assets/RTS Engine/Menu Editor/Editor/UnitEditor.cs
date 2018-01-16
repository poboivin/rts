using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/* Unit Editor script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

[CustomEditor(typeof(Unit))]
public class UnitEditor : Editor {

	public SerializedProperty UnitColors1;
	public SerializedProperty UnitColors2;

	public override void OnInspectorGUI ()
	{
		Unit Target = (Unit)target;

		GUIStyle TitleGUIStyle = new GUIStyle ();
		TitleGUIStyle.fontSize = 20;
		TitleGUIStyle.alignment = TextAnchor.MiddleCenter;
		TitleGUIStyle.fontStyle = FontStyle.Bold;

		EditorGUILayout.LabelField ("Unit:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		TitleGUIStyle.fontSize = 15;
		EditorGUILayout.LabelField ("General Unit Settings:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		Target.Name = EditorGUILayout.TextField ("Unit Name: ", Target.Name);
		Target.Code = EditorGUILayout.TextField ("Unit Code: ", Target.Code);
		Target.Category = EditorGUILayout.TextField ("Unit Category: ", Target.Category);
		Target.Description = EditorGUILayout.TextField ("Unit Description: ", Target.Description);
		EditorGUILayout.LabelField ("Unit Icon:");
		Target.Icon = EditorGUILayout.ObjectField (Target.Icon, typeof(Sprite), true) as Sprite;
		Target.CanBeMoved = EditorGUILayout.Toggle ("Can the unit be moved?", Target.CanBeMoved);
		Target.FreeUnit = EditorGUILayout.Toggle ("Free Unit? (Belongs to no faction)", Target.FreeUnit);
		Target.FactionID = EditorGUILayout.IntField ("Faction ID: ", Target.FactionID);
		Target.UnitHeight = EditorGUILayout.FloatField ("Unit Height: ", Target.UnitHeight);
		Target.FlyingUnit = EditorGUILayout.Toggle ("Flying Unit:", Target.FlyingUnit);
		Target.MaxHealth = EditorGUILayout.FloatField ("Maximum Unit Health: ", Target.MaxHealth);
		Target.DestroyObjTime = EditorGUILayout.FloatField ("Destroy Object Time: ", Target.DestroyObjTime);

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.LabelField ("Unit Movement Settings:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		Target.Speed = EditorGUILayout.FloatField ("Movement Speed: ", Target.Speed);
		Target.RotationDamping = EditorGUILayout.FloatField ("Rotation Damping: ", Target.RotationDamping);

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.LabelField ("Unit Components:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		EditorGUILayout.LabelField ("Unit Animator:");
		Target.AnimMgr = EditorGUILayout.ObjectField (Target.AnimMgr, typeof(Animator), true) as Animator;
		EditorGUILayout.LabelField ("Unit Plane:");
		Target.UnitPlane = EditorGUILayout.ObjectField (Target.UnitPlane, typeof(GameObject), true) as GameObject;
		EditorGUILayout.LabelField ("Unit Selection Component:");
		Target.PlayerSelection = EditorGUILayout.ObjectField (Target.PlayerSelection, typeof(SelectionObj), true) as SelectionObj;
		EditorGUILayout.LabelField ("Main Animator Controller:");
		Target.AnimController = EditorGUILayout.ObjectField (Target.AnimController, typeof(AnimatorOverrideController), true) as AnimatorOverrideController;
		EditorGUILayout.LabelField ("Unit Damage Effect:");
		Target.DamageEffect = EditorGUILayout.ObjectField (Target.DamageEffect, typeof(GameObject), true) as GameObject;
		EditorGUILayout.LabelField ("Color Objects (Skinned Mesh Renderers only):");
		UnitColors1 = serializedObject.FindProperty("FactionColorObjs");
		EditorGUILayout.PropertyField (UnitColors1, true);
		serializedObject.ApplyModifiedProperties();
		EditorGUILayout.LabelField ("Color Objects (Mesh Renderers only):");
		UnitColors2 = serializedObject.FindProperty("FactionColorObjs2");
		EditorGUILayout.PropertyField (UnitColors2, true);
		serializedObject.ApplyModifiedProperties();

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.LabelField ("Audio Clips:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		EditorGUILayout.LabelField ("Selection Sound Effect:");
		Target.SelectionAudio = EditorGUILayout.ObjectField (Target.SelectionAudio, typeof(AudioClip), true) as AudioClip;
		EditorGUILayout.LabelField ("Movement Order Sound Effect:");
		Target.MvtOrderAudio = EditorGUILayout.ObjectField (Target.MvtOrderAudio, typeof(AudioClip), true) as AudioClip;
		EditorGUILayout.LabelField ("Invalid Movement Path Sound Effect:");
		Target.InvalidMvtPathAudio = EditorGUILayout.ObjectField (Target.InvalidMvtPathAudio, typeof(AudioClip), true) as AudioClip;

		EditorUtility.SetDirty (Target);
	}
}
