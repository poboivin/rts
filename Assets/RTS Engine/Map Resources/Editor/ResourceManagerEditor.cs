using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

[CustomEditor(typeof(ResourceManager))]
public class ResourceManagerEditor : Editor {

	List<Resource> ResourcePrefabs = new List<Resource>();

	public override void OnInspectorGUI ()
	{
		//draw the default inspector as well
		DrawDefaultInspector ();

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		ResourceManager Target = (ResourceManager)target;

		if (GUILayout.Button ("Generate Resources List")) {

			bool Error = false;

			//Store all resources in the level:
			if (Target.ResourcesParent != null) {
				//if all the resources are all children of the same parent object, then get them using the method below:
				Target.SceneResources = Target.ResourcesParent.transform.GetComponentsInChildren<Resource> (true);
			} else {
				Error = true;
				Debug.LogError ("Can't find the Resources parent object, please assign in Resource Manager");
			}
				
			ResourcePrefabs.Clear();
			Object[] Objects = Resources.LoadAll ("Prefabs", typeof(GameObject));

			foreach (GameObject Obj in Objects) {
				if(Obj.gameObject.GetComponent<Resource>())
				{
					ResourcePrefabs.Add(Obj.gameObject.GetComponent<Resource>());
				}
			}
				
			if (Target.SceneResources.Length > 0) {
				Target.SpawnResources.Clear ();

				for (int i = 0; i < Target.SceneResources.Length; i++) {
					
					Resource Prefab = GetPrefab (Target.SceneResources [i].Code);
					if (Prefab) {
						ResourceManager.SpawnResourcesVars NewResource = new ResourceManager.SpawnResourcesVars();
						NewResource.Prefab = Prefab.gameObject;
						NewResource.Pos = Target.SceneResources [i].gameObject.transform.position;
						NewResource.Rot = Target.SceneResources [i].gameObject.transform.rotation;
						Target.SpawnResources.Add (NewResource);
					} else {
						Error = true;
						Debug.LogError ("Can't find prefab resource for resource name: " + Target.SceneResources [i].gameObject.name);
					}
				}
			}

			if(Error == false)
				Debug.Log ("Successfully created resources list!");
		}
	}

	public Resource GetPrefab (string Code)
	{
		foreach (Resource R in ResourcePrefabs) {
			if (R.Code == Code) {
				return R;
			}
		}

		return null;
	}
}
