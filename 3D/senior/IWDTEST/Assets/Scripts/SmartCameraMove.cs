using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmartCameraMove : ICameraMove {

	private Camera				_camera;
	private GameObject[]		_targets;

	private Vector3				_initPos;
	private Vector3				_targetPos;

	private float				_initSize;
	private float				_targetSize;

	private float 				_lerpDuration = 4f;
	public float 				LerpDuration {
		get {
			return _lerpDuration;
		}
		set {
			_lerpDuration = value;
		}
	}

	private float 				_startTime = 0f;
	private float				_pauseTime = 0f;

	private bool 				_moveCamera = false;


	public void		InitMove(Camera camera, GameObject[] targets)
	{

		if (_moveCamera) 
		{
			Debug.LogWarning ("Camera already moving");
			return;
		}
		if (camera == null) 
		{
			Debug.LogWarning ("need a Camera to work");
			return;
		}
		if (targets.Length == 0) 
		{
			Debug.LogWarning ("need targets to focus");
			return;
		}
		_camera = camera;
		_targets = targets;
		_initPos =_camera.transform.position;
		CalcMove ();
	}

	public void		Move()
	{
		float timeSinceStart;
		float lerpAdvance;

		if (_moveCamera == false)
			return;
		if (_pauseTime != 0f)
			_startTime += (Time.time - _pauseTime);

		timeSinceStart = Time.time - _startTime;
		lerpAdvance = timeSinceStart / _lerpDuration;

		_camera.transform.position = Vector3.Lerp (_initPos, _targetPos, lerpAdvance);
		if (_camera.orthographic)
			_camera.orthographicSize = Mathf.Lerp (_initSize, _targetSize, lerpAdvance);
		if (lerpAdvance >= 1f)
			_moveCamera = false;

		_pauseTime = 0f;
	}


	public void		StopMove()
	{
		_pauseTime = Time.time;
	}

	public void		Reset()
	{
		_camera.transform.position = _initPos;
		if (_camera.orthographic)
			_camera.orthographicSize = _initSize;

		_pauseTime = 0f;
		_startTime = 0f;	
		_moveCamera = false;
	}
		
	public bool		InProgress()
	{
		return _moveCamera;
	}
		
	private void	CalcMove()
	{
		_targetPos = CalcNewPos ();
		if (_camera.orthographic)
		{
			_initSize = _camera.orthographicSize;
			_targetSize = CalcNewSize (_camera.aspect, _initSize);
		}
		_camera.farClipPlane = CalcDistViewCamera ();
		_moveCamera = true;
		_startTime = Time.time;
		_pauseTime = 0f;
	}

	// return the bounding box points of the mesh
	private List<Vector3>	GetBoundingBoxPoints(GameObject obj)
	{
		Renderer 			mesh;
		Bounds 			bound;
		Vector3 		boundMin;
		Vector3 		boundMax;
		List<Vector3>	boundPoints = new List<Vector3> ();

		mesh = obj.GetComponent<Renderer> ();
		bound = mesh.bounds;
		boundMin = bound.min;
		boundMax = bound.max;

		boundPoints.Add (boundMin);
		boundPoints.Add (boundMax);
		boundPoints.Add (new Vector3 (boundMin.x, boundMin.y, boundMax.z));
		boundPoints.Add (new Vector3 (boundMin.x, boundMax.y, boundMin.z));
		boundPoints.Add (new Vector3 (boundMax.x, boundMin.y, boundMin.z));
		boundPoints.Add (new Vector3 (boundMin.x, boundMax.y, boundMax.z));
		boundPoints.Add (new Vector3 (boundMax.x, boundMin.y, boundMax.z));
		boundPoints.Add (new Vector3 (boundMax.x, boundMax.y, boundMin.z));

		return boundPoints;
	}


	// If meshes out of camera field of view, return max dist between camera sides and meshes (
	private float		CalcDistMax(List<Plane> planes)
	{
		List<Vector3>	boundPoints = new List<Vector3> ();
		Ray 			ray;
		float 			dist;
		float 			distmax = 0f;

		for (int i = 0; i < _targets.Length; i++) 
		{
			boundPoints = GetBoundingBoxPoints (_targets [i]);
			for (int i2 = 0; i2 < boundPoints.Count; i2++) 
			{
				for (int i3 = 0; i3 < planes.Count; i3++) 
				{
					ray = new Ray (boundPoints [i2], -planes[i3].normal);
					if (planes [i3].Raycast (ray, out dist)) {
						if (dist > distmax)
							distmax = dist;
					}
				}
			}
		}
		return distmax;
	}

	// If meshes out of camera field of view, return max dist between camera sides and meshes (dist calculate with camera forward direction)
	private float		CalcDistMaxForward(List<Plane> planes)
	{
		List<Vector3>	boundPoints = new List<Vector3> ();
		Ray 			ray;
		float 			dist;
		float 			distmax = 0f;

		for (int i = 0; i < _targets.Length; i++) 
		{
			boundPoints = GetBoundingBoxPoints (_targets [i]);
			for (int i2 = 0; i2 < boundPoints.Count; i2++) 
			{
				for (int i3 = 0; i3 < planes.Count; i3++) 
				{
					ray = new Ray (boundPoints [i2], _camera.transform.forward);
					if (planes [i3].Raycast (ray, out dist)) {
						if (dist > distmax)
							distmax = dist;
					}
				}
			}
		}
		return distmax;
	}

	private Vector3		CalcNewPos()
	{
		List<Plane> 	planes = new List<Plane> ();
		float 			distmax;

		if (!_camera.orthographic) 
		{
			// Get field of view side planes
			Plane[] frustum = GeometryUtility.CalculateFrustumPlanes (Camera.main);
			planes.Add (frustum [0]);
			planes.Add (frustum [1]);
			planes.Add (frustum [2]);
			planes.Add (frustum [3]);
		} 
		else
			planes.Add (new Plane (_camera.transform.forward, _camera.transform.position));

		distmax = CalcDistMaxForward (planes);
		if (distmax == 0f)
			return _camera.transform.position;
		if (_camera.orthographic)
			distmax += 1f; // Add +1dist to skip visual bug with ortho camera

		return (_camera.transform.position + ((-_camera.transform.forward) * distmax));
	}

	private float		CalcNewSize(float aspect, float size)
	{
		List<Plane> 	planes = new List<Plane> ();
		float 			distY = size;
		float 			distX = size * aspect;
		float 			distMaxX = 0f;
		float 			distMaxY = 0f;

		planes.Add(new Plane (_camera.transform.right, _targetPos));
		planes.Add(new Plane (-_camera.transform.right, _targetPos));

		distMaxX = CalcDistMax (planes);
		planes.Clear ();

		planes.Add(new Plane (_camera.transform.up, _targetPos));
		planes.Add(new Plane (-_camera.transform.up, _targetPos));
		distMaxY = CalcDistMax (planes);

		if (distMaxX < distX && distMaxY < distY)
			return size;
		distMaxX = (distMaxX / size) / aspect;
		if (distMaxX > distMaxY)
			return distMaxX;
		return distMaxY;
	}

	// Calc min camera dist view to see all objs
	private float		CalcDistViewCamera()
	{
		GameObject		farestObj = null;
		float			dist;
		float 			maxDist = 0f;
		List<Vector3>	points = new List<Vector3> ();

		for (int i = 0; i < _targets.Length; i++) {
			dist = Vector3.Distance (_targetPos, _targets [i].transform.position);
			if (dist > maxDist) {
				maxDist = dist;
				farestObj = _targets [i];
			}
		}
		points = GetBoundingBoxPoints (farestObj);
		for (int i = 0; i < points.Count; i++) {
			dist = Vector3.Distance (_targetPos, points[i]);
			if (dist > maxDist)
				maxDist = dist;
		}
		return maxDist;
	}

}
