using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmartCamera : MonoBehaviour 
{

	private SmartCameraMove		_cameraMove;
	private GameObject[] 		_targetsObj;
	private bool				_pause = false;

	[SerializeField][Range(1,10)]
	private float 				_moveDuration = 4f;

	// Use this for initialization
	public void Init (GameObject[] targetsObj) 
	{
		_cameraMove = new SmartCameraMove ();
		_targetsObj = targetsObj;
	}
		
	// Update is called once per frame
	void Update () 
	{
		if (_cameraMove.InProgress() && _pause == false)
			_cameraMove.Move ();
	}

	public void		StartMove()
	{
		_cameraMove.InitMove (this.GetComponent<Camera>(), _targetsObj);
		_cameraMove.LerpDuration = _moveDuration;
	}

	public void		ContinueMove()
	{
		_pause = false;
	}

	public void		StopMove()
	{
		_cameraMove.StopMove ();
		_pause = true;
	}

	public void		ResetMove()
	{
		_cameraMove.Reset ();
		_pause = false;
	}
		
}
