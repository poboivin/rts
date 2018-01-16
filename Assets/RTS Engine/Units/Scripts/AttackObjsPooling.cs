using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* Attack Object Pool script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

public class AttackObjsPooling : MonoBehaviour {
	[HideInInspector]
	public List<AttackObject> AttackObjects; //Includes all the attack objects in the scene.

	//This method searches for a hidden attack object with a certain code so that it can be used again.
	public AttackObject GetFreeAttackObject (string Code)
	{
		AttackObject Result = null;
		//Loop through all the spawned attack objects:
		if (AttackObjects.Count > 0) {
			int i = 0;

			while (Result == null && i < AttackObjects.Count) {

				if (AttackObjects [i] != null) {
					//If the current attack object's code mathes the one we're looking for:
					if (AttackObjects [i].Code == Code) {
						//We can re-use non active attack objects, so we'll check for that as well:
						if (AttackObjects [i].gameObject.activeInHierarchy == false) {
							//This matches all what we're looking for so make it the result;
							Result = AttackObjects [i];
						}
					}
				}

				i++;
			}
		}

		//return the result:
		return Result;
	}
}
