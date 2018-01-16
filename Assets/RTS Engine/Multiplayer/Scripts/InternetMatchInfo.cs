using UnityEngine;
using System.Collections;
using UnityEngine.Networking.Match;
using UnityEngine.UI;

/* Multiplayer Faction Manager: script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

public class InternetMatchInfo : MonoBehaviour {

	[HideInInspector]
	public int ID;

	public Text MatchName;
	public Text MatchSize;

	NetworkMapManager NetworkMapMgr;

	void Start () {
		NetworkMapMgr = FindObjectOfType (typeof(NetworkMapManager)) as NetworkMapManager;
	}
	
	public void JoinInternetMatch ()
	{
		NetworkMapMgr.JoinInternetMatch (ID);
	}
}
