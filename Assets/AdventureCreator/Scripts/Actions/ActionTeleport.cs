/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2019
 *	
 *	"ActionTeleport.cs"
 * 
 *	This action moves an object to a specified GameObject's position.
 *	Markers are helpful in this regard.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionTeleport : Action
	{

		public int obToMoveParameterID = -1;
		public int obToMoveID = 0;
		public GameObject obToMove;
		protected GameObject runtimeObToMove;

		public int markerParameterID = -1;
		public int markerID = 0;
		public Marker teleporter;
		protected Marker runtimeTeleporter;

		public GameObject relativeGameObject = null;
		public int relativeGameObjectID = 0;
		public int relativeGameObjectParameterID = -1;

		public PositionRelativeTo positionRelativeTo = PositionRelativeTo.Nothing;

		public int relativeVectorParameterID = -1;
		public Vector3 relativeVector;

		public int vectorVarParameterID = -1;
		public int vectorVarID;
		public VariableLocation variableLocation = VariableLocation.Global;

		public bool recalculateActivePathFind = false;
		public bool isPlayer;
		public bool snapCamera;

		public bool copyRotation;
		

		public ActionTeleport ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Object;
			title = "Teleport";
			description = "Moves a GameObject to a Marker instantly. Can also copy the Marker's rotation. The final position can optionally be made relative to the active camera, or the player. For example, if the Marker's position is (0, 0, 1) and Positon relative to is set to Relative To Active Camera, then the object will be teleported in front of the camera.";
		}
		
		
		override public void AssignValues (List<ActionParameter> parameters)
		{
			runtimeObToMove = AssignFile (parameters, obToMoveParameterID, obToMoveID, obToMove);
			runtimeTeleporter = AssignFile <Marker> (parameters, markerParameterID, markerID, teleporter);
			relativeGameObject = AssignFile (parameters, relativeGameObjectParameterID, relativeGameObjectID, relativeGameObject);

			relativeVector = AssignVector3 (parameters, relativeVectorParameterID, relativeVector);
			vectorVarID = AssignVariableID (parameters, vectorVarParameterID, vectorVarID);

			if (isPlayer && KickStarter.player)
			{
				runtimeObToMove = KickStarter.player.gameObject;
			}
		}
		
		
		override public float Run ()
		{
			if (runtimeTeleporter != null && runtimeObToMove != null)
			{
				Vector3 position = runtimeTeleporter.transform.position;
				Quaternion rotation = runtimeTeleporter.transform.rotation;

				if (positionRelativeTo == PositionRelativeTo.RelativeToActiveCamera)
				{
					Transform mainCam = KickStarter.mainCamera.transform;

					float right = runtimeTeleporter.transform.position.x;
					float up = runtimeTeleporter.transform.position.y;
					float forward = runtimeTeleporter.transform.position.z;

					position = mainCam.position + (mainCam.forward * forward) + (mainCam.right * right) + (mainCam.up * up);
					rotation.eulerAngles += mainCam.transform.rotation.eulerAngles;
				}
				else if (positionRelativeTo == PositionRelativeTo.RelativeToPlayer && !isPlayer)
				{
					if (KickStarter.player)
					{
						Transform playerTransform = KickStarter.player.transform;

						float right = runtimeTeleporter.transform.position.x;
						float up = runtimeTeleporter.transform.position.y;
						float forward = runtimeTeleporter.transform.position.z;
						
						position = playerTransform.position + (playerTransform.forward * forward) + (playerTransform.right * right) + (playerTransform.up * up);
						rotation.eulerAngles += playerTransform.rotation.eulerAngles;
					}
				}
				else if (positionRelativeTo == PositionRelativeTo.RelativeToGameObject)
				{
					if (relativeGameObject != null)
					{
						Transform relativeTransform = relativeGameObject.transform;

						float right = runtimeTeleporter.transform.position.x;
						float up = runtimeTeleporter.transform.position.y;
						float forward = runtimeTeleporter.transform.position.z;
						
						position = relativeTransform.position + (relativeTransform.forward * forward) + (relativeTransform.right * right) + (relativeTransform.up * up);
						rotation.eulerAngles += relativeTransform.rotation.eulerAngles;
					}
				}
				else if (positionRelativeTo == PositionRelativeTo.EnteredValue)
				{
					position += relativeVector;
				}
				else if (positionRelativeTo == PositionRelativeTo.VectorVariable)
				{
					if (variableLocation == VariableLocation.Global)
					{
						position += GlobalVariables.GetVector3Value (vectorVarID);
					}
					else if (variableLocation == VariableLocation.Local && !isAssetFile)
					{
						position += LocalVariables.GetVector3Value (vectorVarID);
					}
				}

				Char charToMove = runtimeObToMove.GetComponent <Char>();
				if (copyRotation)
				{
					runtimeObToMove.transform.rotation = rotation;

					if (charToMove != null)
					{
						// Is a character, so set the lookDirection, otherwise will revert back to old rotation
						charToMove.SetLookDirection (runtimeTeleporter.transform.forward, true);
						charToMove.Halt ();
					}
				}

				if (charToMove != null)
				{
					charToMove.Teleport (position, recalculateActivePathFind);
				}
				else
				{
					runtimeObToMove.transform.position = position;
				}

				if (isPlayer && snapCamera)
				{
					if (KickStarter.mainCamera != null && KickStarter.mainCamera.attachedCamera != null && KickStarter.mainCamera.attachedCamera.targetIsPlayer)
					{
						KickStarter.mainCamera.attachedCamera.MoveCameraInstant ();
					}
				}
			}
			
			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		override public void ShowGUI (List<ActionParameter> parameters)
		{
			isPlayer = EditorGUILayout.Toggle ("Is Player?", isPlayer);
			if (!isPlayer)
			{
				obToMoveParameterID = Action.ChooseParameterGUI ("Object to move:", parameters, obToMoveParameterID, ParameterType.GameObject);
				if (obToMoveParameterID >= 0)
				{
					obToMoveID = 0;
					obToMove = null;
				}
				else
				{
					obToMove = (GameObject) EditorGUILayout.ObjectField ("Object to move:", obToMove, typeof(GameObject), true);
					
					obToMoveID = FieldToID (obToMove, obToMoveID);
					obToMove = IDToField (obToMove, obToMoveID, false);
				}
			}

			markerParameterID = Action.ChooseParameterGUI ("Teleport to:", parameters, markerParameterID, ParameterType.GameObject);
			if (markerParameterID >= 0)
			{
				markerID = 0;
				teleporter = null;
			}
			else
			{
				teleporter = (Marker) EditorGUILayout.ObjectField ("Teleport to:", teleporter, typeof (Marker), true);
				
				markerID = FieldToID <Marker> (teleporter, markerID);
				teleporter = IDToField <Marker> (teleporter, markerID, false);
			}
			
			positionRelativeTo = (PositionRelativeTo) EditorGUILayout.EnumPopup ("Position relative to:", positionRelativeTo);

			if (positionRelativeTo == PositionRelativeTo.RelativeToGameObject)
			{
				relativeGameObjectParameterID = Action.ChooseParameterGUI ("Relative GameObject:", parameters, relativeGameObjectParameterID, ParameterType.GameObject);
				if (relativeGameObjectParameterID >= 0)
				{
					relativeGameObjectID = 0;
					relativeGameObject = null;
				}
				else
				{
					relativeGameObject = (GameObject) EditorGUILayout.ObjectField ("Relative GameObject:", relativeGameObject, typeof (GameObject), true);
					
					relativeGameObjectID = FieldToID (relativeGameObject, relativeGameObjectID);
					relativeGameObject = IDToField (relativeGameObject, relativeGameObjectID, false);
				}
			}
			else if (positionRelativeTo == PositionRelativeTo.EnteredValue)
			{
				relativeVectorParameterID = Action.ChooseParameterGUI ("Value:", parameters, relativeVectorParameterID, ParameterType.Vector3);
				if (relativeVectorParameterID < 0)
				{
					relativeVector = EditorGUILayout.Vector3Field ("Value:", relativeVector);
				}
			}
			else if (positionRelativeTo == PositionRelativeTo.VectorVariable)
			{
				if (isAssetFile)
				{
					variableLocation = VariableLocation.Global;
				}
				else
				{
					variableLocation = (VariableLocation) EditorGUILayout.EnumPopup ("Source:", variableLocation);
				}

				if (variableLocation == VariableLocation.Global)
				{
					vectorVarParameterID = Action.ChooseParameterGUI ("Vector3 variable:", parameters, vectorVarParameterID, ParameterType.GlobalVariable);
					if (vectorVarParameterID < 0)
					{
						vectorVarID = AdvGame.GlobalVariableGUI ("Vector3 variable:", vectorVarID, VariableType.Vector3);
					}
				}
				else if (variableLocation == VariableLocation.Local)
				{
					vectorVarParameterID = Action.ChooseParameterGUI ("Vector3 variable:", parameters, vectorVarParameterID, ParameterType.LocalVariable);
					if (vectorVarParameterID < 0)
					{
						vectorVarID = AdvGame.LocalVariableGUI ("Vector3 variable:", vectorVarID, VariableType.Vector3);
					}
				}
			}

			copyRotation = EditorGUILayout.Toggle ("Copy rotation?", copyRotation);

			if (isPlayer)
			{
				snapCamera = EditorGUILayout.Toggle ("Teleport active camera too?", snapCamera);
			}

			if (isPlayer || (obToMove != null && obToMove.GetComponent <Char>()))
			{
				recalculateActivePathFind = EditorGUILayout.Toggle ("Recalculate pathfinding?", recalculateActivePathFind);
			}

			AfterRunningOption ();
		}


		override public void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo && obToMove != null)
			{
				if (obToMove.GetComponent <NPC>())
				{
					AddSaveScript <RememberNPC> (obToMove);
				}
				else if (obToMove.GetComponent <Player>() == null && !isPlayer)
				{
					AddSaveScript <RememberTransform> (obToMove);
				}
			}

			if (!isPlayer)
			{
				AssignConstantID (obToMove, obToMoveID, obToMoveParameterID);
			}
			AssignConstantID <Marker> (teleporter, markerID, markerParameterID);
		}
		
		
		override public string SetLabel ()
		{
			if (teleporter != null)
			{
				if (obToMove != null)
				{
					return obToMove.name + " to " + teleporter.name;
				}
				else if (isPlayer)
				{
					return "Player to " + teleporter.name;
				}
			}
			return string.Empty;
		}
		
		#endif
	}

}