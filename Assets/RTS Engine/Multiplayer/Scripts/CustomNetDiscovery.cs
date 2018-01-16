using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CustomNetDiscovery : NetworkDiscovery {
	public NetworkMapManager MapMgr;
	public bool Connected = false;

	public override void OnReceivedBroadcast (string fromAddress, string data)
	{
		if (Connected == false) {
			MapMgr.StartLocalGame (fromAddress);
			Connected = true;
		}
	}
}
