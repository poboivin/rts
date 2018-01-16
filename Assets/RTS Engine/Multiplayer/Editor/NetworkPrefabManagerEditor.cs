using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NetworkPrefabManager))]
public class NetworkPrefabManagerEditor : Editor {
	

	public override void OnInspectorGUI ()
	{
		NetworkPrefabManager Target = (NetworkPrefabManager)target;

		Target.NetworkMapMgr = EditorGUILayout.ObjectField (Target.NetworkMapMgr, typeof(NetworkMapManager), true) as NetworkMapManager;
		if (GUILayout.Button ("Update Spawnable Prefabs:")) {
			Object[] Objects = Resources.LoadAll ("Prefabs", typeof(GameObject));
			foreach (GameObject Obj in Objects) {
				if(!Target.NetworkMapMgr.spawnPrefabs.Contains(Obj.gameObject))
				{
					if (Obj.gameObject.GetComponent<Building> () || Obj.gameObject.GetComponent<Unit> () || Obj.gameObject.GetComponent<Resource> ()) {
						Target.NetworkMapMgr.spawnPrefabs.Add (Obj.gameObject);
					}
				}
			}
		}
		if (GUILayout.Button ("Reset Spawnable Prefabs:")) {
			Target.NetworkMapMgr.spawnPrefabs.Clear ();
		}
	}
}
