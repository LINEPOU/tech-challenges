using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour {

	[SerializeField]
	private SmartCamera		_camera;


	public void			Start()
	{
		GameObject[] objs = GameObject.FindGameObjectsWithTag ("focusMesh");
		_camera.Init (objs);
	}

	public void		ButtonStart()
	{
		_camera.StartMove ();
	}

	public void		ButtonPause()
	{
		_camera.StopMove ();
	}

	public void		ButtonReset()
	{
		_camera.ResetMove ();
	}

	public void		ButtonContinue()
	{
		_camera.ContinueMove ();	
	}

}
