using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICameraMove {

	void	InitMove (Camera camera, GameObject[] targets);
	void	StopMove ();
	void	Reset();
	void	Move();
	bool	InProgress();
}
