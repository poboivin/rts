using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/* Camera Movement script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

public class CameraMovement : MonoBehaviour {

	[Header("General Settings:")]
	public Camera MainCamera; //Main camera object
	public float CameraHeight; //Main camera's height.
	//other:
	public GameObject FlatTerrain;

	[Header("Camera Movement:")]
	public float MvtSpeed = 1.00f; //Camera movement speed;
	//Camera movement speed:
	public KeyCode MoveUpKey = KeyCode.UpArrow;
	public KeyCode MoveDownKey = KeyCode.DownArrow;
	public KeyCode MoveRightKey = KeyCode.RightArrow;
	public KeyCode MoveLeftKey = KeyCode.LeftArrow;

	//screen edge movement:
	public bool MoveOnScreenEdge = false; //Move the camera when the mouse is the screen edge.
	public int ScreenEdgeSize = 25; //Screen edge size.
	//Screen edges:
	Rect DownRect;
	Rect UpRect;
	Rect LeftRect;
	Rect RightRect;

	bool CanMoveOnEdge = true;

	//Camera position limits: //The Y axis here refers to the Z axis of the camera position.
	public bool ScreenLimit = false;
	public Vector2 MinPos; 
	public Vector2 MaxPos;
	[Header("Panning:")]
	//Panning:
	public bool Panning = true;
	public KeyCode PanningKey = KeyCode.Space;
	Vector2 MouseAxis;
	public float PanningSpeed = 15f;
	[Header("Zoom:")]
	//Camera zoom in/zoom out:
	public bool ZoomEnabled = true; //Is zooming enabled?
	//can zoom with keys? 
	public bool CanZoomWithKey = true;
	//Zoom keys:
	public KeyCode ZoomInKey = KeyCode.PageUp;
	public KeyCode ZoomOutKey = KeyCode.PageDown;
	public float MaxFOV = 60.0f; //Maximum value of the field of view
	public float MinFOV = 40.0f; //Minimum value of the field of view
	//Zooming in/out smooth damp vars:
	public float ZoomSmoothTime = 1.0f;
	float ZoomVelocity;
	//Use mouse wheel for zooming in and out?
	public bool ZoomOnMouseWheel = false;
	public float ZoomScrollWheelSensitivty = 5.0f; //sensitivity for zooming in/out with mouse scroll wheel
	public float ZoomScrollWheelSpeed = 15.0f; //speed for zooming with the scrol wheel.
	float FOV; //current field of view.

	/*//Rot:
	public bool KeyboardRot = true;
	public KeyCode RotRightKey = KeyCode.D;
	public KeyCode RotLeftKey = KeyCode.Q;
	public float KeyboardRotSpeed = 10.0f;

	//Mouse Rotation
	public bool MouseRot = true;
	public KeyCode MouseRotKey = KeyCode.M;
	public float MouseRotSpeed = 10.0f;*/
	[Header("Follow Unit:")]
	//Follow unit:
	public bool CanFollowUnit = true; //can follow a unit.
	[HideInInspector]
	public Unit UnitToFollow; // the unit to follow.
	public KeyCode FollowUnitKey = KeyCode.Space;
	[Header("Minimap Camera:")]
	//Minimap:
	public Camera MinimapCam;
	public float OffsetX;
	public float OffsetZ;
	//UI: 
	public Canvas MinimapCanvas;
	public Image MinimapCursor;

	bool Moved = false;

	SelectionManager SelectionMgr;

	void Start ()
	{
		FOV = GetComponent<Camera> ().fieldOfView;
		UnitToFollow = null;

		SelectionMgr = GameManager.Instance.SelectionMgr;

	    if(MainCamera == null) 
		{
			Debug.LogError("Please set the main camera");
		}
		else
		{
			MainCamera.transform.position = new Vector3(MainCamera.transform.position.x,CameraHeight,MainCamera.transform.position.z);
			MainCamera.transform.eulerAngles = new Vector3(45.0f,45.0f,0.0f);
		}

		Moved = true;
	}

	void Update () 
	{

		MouseAxis = new Vector2 (Input.GetAxis ("Mouse X"), Input.GetAxis ("Mouse Y"));
		Vector3 TargetMvt = Vector3.zero;

		if (Panning == true && Input.GetKey (PanningKey) && MouseAxis != Vector2.zero) {
			TargetMvt = new Vector3 (-MouseAxis.x, 0.0f, -MouseAxis.y);

			TargetMvt *= PanningSpeed;
			TargetMvt *= Time.deltaTime;

			transform.Translate (TargetMvt, Space.World);
		} else {
			//check if the player can move the camera on screen edge:
			CanMoveOnEdge = false;
			if (MoveOnScreenEdge == true) {
				if (!EventSystem.current.IsPointerOverGameObject())
				{
					CanMoveOnEdge = true;
				}
			}

			//Screen edges rects:
			DownRect = new Rect (0.0f, 0.0f, Screen.width, ScreenEdgeSize);
			UpRect = new Rect (0.0f, Screen.height-ScreenEdgeSize, Screen.width, ScreenEdgeSize);
			LeftRect = new Rect (0.0f, 0.0f, ScreenEdgeSize, Screen.height);
			RightRect = new Rect (Screen.width-ScreenEdgeSize, 0.0f, ScreenEdgeSize, Screen.height);

			//move on edge/keyboard:

			Moved = false;

			bool MoveUp = (UpRect.Contains (Input.mousePosition) && CanMoveOnEdge == true);
			bool MoveDown = (DownRect.Contains (Input.mousePosition) && CanMoveOnEdge == true);
			bool MoveRight = (RightRect.Contains (Input.mousePosition) && CanMoveOnEdge == true);
			bool MoveLeft = (LeftRect.Contains (Input.mousePosition) && CanMoveOnEdge == true);

			TargetMvt.x = MoveRight ? 1 : MoveLeft ? -1 : 0; 
			TargetMvt.z = MoveUp ? 1 : MoveDown ? -1 : 0;

			if (TargetMvt != Vector3.zero) {
				TargetMvt *= MvtSpeed;
				TargetMvt *= Time.deltaTime;
				TargetMvt = Quaternion.Euler (new Vector3 (0f, transform.eulerAngles.y, 0f)) * TargetMvt;

				transform.Translate (TargetMvt, Space.World);

				Moved = true;
			} else if(Mathf.Abs(Input.GetAxis ("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis ("Vertical"))  > 0.1f) {
				transform.Translate (Vector3.right.normalized * Input.GetAxis ("Horizontal") * MvtSpeed * Time.deltaTime);
				transform.Translate ((Vector3.up + Vector3.forward).normalized * Input.GetAxis ("Vertical") * MvtSpeed * Time.deltaTime);

				Moved = true;
			}
		}

		if (Moved == true) {
			UnitToFollow = null;
		}
		//Camera position limits:
		if (ScreenLimit == true) {
			transform.position = new Vector3 (Mathf.Clamp (transform.position.x, MinPos.x, MaxPos.x), transform.position.y, Mathf.Clamp (transform.position.z, MinPos.y, MaxPos.y));
		}

		/*//Rot: 
		//Keyboard rotation:
		if (KeyboardRot == true) {
			int RotDirection;

			bool RotRight = Input.GetKey (RotRightKey);
			bool RotLeft = Input.GetKey (RotLeftKey);
			if (RotLeft && RotRight) {
				RotDirection = 0;
			} else if (RotLeft && !RotRight) {
				RotDirection = -1;
			} else if (!RotLeft && RotRight) {
				RotDirection = 1;
			} else {
				RotDirection = 0;
			}

			transform.Rotate(Vector3.up, RotDirection * Time.deltaTime * KeyboardRotSpeed, Space.World);

			Moved = true;
		}

		if (MouseRot && Input.GetKey (MouseRotKey)) {
			transform.Rotate (Vector3.up, -MouseAxis.x * Time.deltaTime * MouseRotSpeed, Space.World);
			Moved = true;
		}
			
		MinimapCam.transform.eulerAngles = new Vector3 (MinimapCam.transform.eulerAngles.x, transform.eulerAngles.y, MinimapCam.transform.eulerAngles.z);
*/


		if (ZoomEnabled == true) {

			//Zoom in/out:

			//If the player presses the zoom in and out keys:
			if (Input.GetKey (ZoomInKey) && CanZoomWithKey == true) {
				if (ZoomVelocity > 0) {
					ZoomVelocity = 0.0f;
				}
				//Smoothly zoom in:
				FOV = Mathf.SmoothDamp (FOV, MinFOV, ref ZoomVelocity, ZoomSmoothTime);
			} else if (Input.GetKey (ZoomOutKey) && CanZoomWithKey == true) {
				if (ZoomVelocity < 0) {
					ZoomVelocity = 0.0f;
				}
				//Smoothly zoom out:
				FOV = Mathf.SmoothDamp (FOV, MaxFOV, ref ZoomVelocity, ZoomSmoothTime);
			} else if (ZoomOnMouseWheel == true) {
				FOV -= Input.GetAxis("Mouse ScrollWheel") * ZoomScrollWheelSensitivty;
			}
			//Always keep the field of view between the max and the min values:
			FOV = Mathf.Clamp(FOV, MinFOV, MaxFOV);
		}

		//Follow a unit:

		//if we can actually follow units:
		if (CanFollowUnit == true) {
			if (SelectionMgr.SelectedUnits.Count == 1) {
				//can only work with one unit selected:
				if (SelectionMgr.SelectedUnits [0] != null) {
					if (Input.GetKeyDown (FollowUnitKey)) { //if the player presses the follow key
						UnitToFollow = SelectionMgr.SelectedUnits [0]; //make the selected unit, the unit to follow.
					}
				}
			}
		}


		//Minimap movement :

		//If the player clicks on the left mose button:
		if (Input.GetMouseButtonUp (0) || Input.GetMouseButtonUp (1) || Moved == true) {
			Ray RayCheck;
			RaycastHit[] Hits;

			if (Input.GetMouseButtonUp (0) || Input.GetMouseButtonUp (1)) {
				//create a raycast
				RayCheck = MinimapCam.ScreenPointToRay (Input.mousePosition);
				Hits = Physics.RaycastAll (RayCheck, 100.0f);

				if (Hits.Length > 0) {
					for (int i = 0; i < Hits.Length; i++) {
						//If we clicked on a part of the terrain:
						if (Hits [i].transform.gameObject == SelectionMgr.TerrainObj.gameObject) {
							if (Input.GetMouseButtonUp (0) && SelectionMgr.SelectionBoxEnabled == false) {
								UnitToFollow = null;
								//make the camera look at it.
								LookAt (Hits [i].point);
							} 
							if (Input.GetMouseButtonUp (1)) {
								SelectionMgr.MoveSelectedUnits (Hits[i].point);
							}
						}
					}
				}
			}

			//another raycast on the minimap screen:n
			RayCheck = MainCamera.ScreenPointToRay(new Vector3(Screen.width/2,Screen.height/2,0.0f));
			Hits = Physics.RaycastAll (RayCheck, 100.0f);

			if (Hits.Length > 0) {
				for (int i = 0; i < Hits.Length; i++) {
					//If we clicked on a part of the terrain:
					if (Hits [i].transform.gameObject == SelectionMgr.TerrainObj.gameObject) {
						//change the mini map cursor position
						SetMiniMapCursorPos (Hits[i].point);
					}
				}
			}
		}

	}

	void FixedUpdate ()
	{
		GetComponent<Camera>().fieldOfView = Mathf.Lerp (GetComponent<Camera>().fieldOfView, FOV, Time.deltaTime * ZoomScrollWheelSpeed);

		//if we can actually follow units:
		if (CanFollowUnit == true) {
			if (UnitToFollow != null) { //if the camera is following a unit:
				LookAt(UnitToFollow.transform.position);
			}
		}
	}

	//looks at the selected unit:
	public void LookAtSelectedUnit ()
	{
		if (SelectionMgr.SelectedUnits.Count == 1) {
			LookAt (SelectionMgr.SelectedUnits [0].transform.position);
		}
	}

	public void LookAt (Vector3 LookAtPos)
	{
		transform.position = new Vector3 (LookAtPos.x + OffsetX, transform.position.y, LookAtPos.z + OffsetZ);
	}

	public void SetMiniMapCursorPos (Vector3 NewPos)
	{
		Vector2 CanvasPos = Vector2.zero;
		RectTransformUtility.ScreenPointToLocalPointInRectangle (MinimapCanvas.GetComponent<RectTransform> (), MinimapCam.WorldToScreenPoint(NewPos),MinimapCam, out CanvasPos);
		MinimapCursor.GetComponent<RectTransform> ().localPosition = new Vector3 (CanvasPos.x, CanvasPos.y, MinimapCursor.GetComponent<RectTransform> ().localPosition.z);
	}

}
