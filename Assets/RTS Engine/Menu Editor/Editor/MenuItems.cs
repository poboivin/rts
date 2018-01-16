using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MenuItems : MonoBehaviour {

	[MenuItem("RTS Engine/Create New Map")]
	private static void MapOption()
	{
		GameObject MapSettingsClone = Instantiate(Resources.Load("MapSettingsPrefab", typeof(GameObject))) as GameObject;

		if (MapSettingsClone != null) {
			for (int i = MapSettingsClone.transform.childCount-1; i >= 0; i--) {
				MapSettingsClone.transform.GetChild (0).SetParent (null, true);
			}
		}

		DestroyImmediate (MapSettingsClone);
	}

	[MenuItem("RTS Engine/Single Player Menu")]
	private static void SingleMapOption()
	{
		GameObject SinglePlayerMenu = Instantiate(Resources.Load("SinglePlayerMenu", typeof(GameObject))) as GameObject;

		if (SinglePlayerMenu != null) {
			for (int i = SinglePlayerMenu.transform.childCount-1; i >= 0; i--) {
				SinglePlayerMenu.transform.GetChild (0).SetParent (null, true);
			}
		}

		DestroyImmediate (SinglePlayerMenu);
	}

	[MenuItem("RTS Engine/Multiplayer Menu")]
	private static void MultiplayerMenu()
	{
		GameObject MultiPlayerMenu = Instantiate(Resources.Load("MultiPlayerMenu", typeof(GameObject))) as GameObject;

		if (MultiPlayerMenu != null) {
			for (int i = MultiPlayerMenu.transform.childCount-1; i >= 0; i--) {
				MultiPlayerMenu.transform.GetChild (0).SetParent (null, true);
			}
		}

		DestroyImmediate (MultiPlayerMenu);
	}
}
