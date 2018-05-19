using System.Collections;
using System.Collections.Generic;
using UnityEngine;


class FocusObject
{
	private GameObject			_obj;
	public GameObject 			Obj {
		get {
			return _obj;
		}
		set {
			_obj = value;
		}
	}

	private List<Vector3>		_boundingBoxPoints;
	public List<Vector3> 		BoundingBoxPoints {
		get {
			return _boundingBoxPoints;
		}
		set {
			_boundingBoxPoints = value;
		}
	}
}

public class SmartCameraMove : ICameraMove {

	private Camera				_camera;
	private List<FocusObject>	_targets = new List<FocusObject>();

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

	private Vector3				 _objsCenter;


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
		for (int i = 0; i < targets.Length; i++) {
			FocusObject focusObj = new FocusObject ();
			focusObj.Obj = targets [i];
			focusObj.BoundingBoxPoints = GetBoundingBoxPoints (targets [i]);
			_targets.Add (focusObj);
		}
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
		_camera.transform.position = CalcCenter ();
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
		_camera.transform.position = _initPos;
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

	/*
	private Vector3		CalcCenter2()
	{
		Ray ray;
		Plane plane = new Plane (_camera.transform.forward, _camera.transform.position);
		//Plane plane2 = new Plane (-_camera.transform.forward, _camera.transform.position);
		float dist;
		Vector3 pos = Vector3.one;

		for (int i = 0; i < _targets.Count; i++) {
			for (int i2 = 0; i2 < _targets [i].BoundingBoxPoints.Count; i2++) 
			{
				ray = new Ray (_targets [i].BoundingBoxPoints [i2], -_camera.transform.forward);
				if (plane.Raycast (ray, out dist) )
				{
					pos = _targets [i].BoundingBoxPoints [i2] + (dist * -_camera.transform.forward);
					GameObject obj = GameObject.CreatePrimitive (PrimitiveType.Sphere);
					obj.transform.position = pos;
					pos = _camera.transform.InverseTransformPoint (pos);
					Debug.Log (pos.ToString());
				}

			}
		}
		return pos;
	}
*/
	private Vector3		CalcCenter()
	{
		Vector3	min = Vector3.one;
		Vector3	max = Vector3.one;
		Vector3 center = Vector3.one;

		for (int i = 0; i < _targets.Count; i++) {
			for (int i2 = 0; i2 < _targets [i].BoundingBoxPoints.Count; i2++) 
			{
				if (i == 0 && i2 == 0) {
					min = _targets [i].BoundingBoxPoints [i2];
					max = _targets [i].BoundingBoxPoints [i2];
				}

				if (_targets [i].BoundingBoxPoints [i2].x > max.x)
					max.x = _targets [i].BoundingBoxPoints [i2].x;
				if (_targets [i].BoundingBoxPoints [i2].y > max.y)
					max.y = _targets [i].BoundingBoxPoints [i2].y;
				if (_targets [i].BoundingBoxPoints [i2].z > max.z)
					max.z = _targets [i].BoundingBoxPoints [i2].z;

				if (_targets [i].BoundingBoxPoints [i2].x < min.x)
					min.x = _targets [i].BoundingBoxPoints [i2].x;
				if (_targets [i].BoundingBoxPoints [i2].y < min.y)
					min.y = _targets [i].BoundingBoxPoints [i2].y;
				if (_targets [i].BoundingBoxPoints [i2].z < min.z)
					min.z = _targets [i].BoundingBoxPoints [i2].z;
			}
		}
		return new Vector3 ((min.x+max.x)/2, (min.y+max.y)/2, (min.z+max.z)/2);
	}


	// If meshes out of camera field of view, return max dist between camera sides and meshes (
	private float		CalcDistMax(List<Plane> planes)
	{
		Ray 			ray;
		float 			dist;
		float 			distmax = 0f;

		for (int i = 0; i < _targets.Count; i++) 
		{
			for (int i2 = 0; i2 < _targets[i].BoundingBoxPoints.Count; i2++) 
			{
				for (int i3 = 0; i3 < planes.Count; i3++) 
				{
					ray = new Ray (_targets[i].BoundingBoxPoints[i2], -planes[i3].normal);
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
		Ray 			ray;
		float 			dist;
		float 			distmax = 0f;

		for (int i = 0; i < _targets.Count; i++) 
		{
			for (int i2 = 0; i2 < _targets[i].BoundingBoxPoints.Count; i2++) 
			{
				for (int i3 = 0; i3 < planes.Count; i3++) 
				{
					ray = new Ray (_targets[i].BoundingBoxPoints[i2], _camera.transform.forward);
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
		Vector3 		targetPos;
		Vector3 		translation;

		//DESTINATION - ORIGINE
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

		targetPos = _camera.transform.position + ((-_camera.transform.forward) * distmax);
		targetPos = CalcNewPosObjTooClose (targetPos);
		return targetPos;
	}

	private Vector3		CalcNewPosObjTooClose(Vector3 target)
	{
		float			dist;
		float 			minDist = 0f;
		Ray 			ray;

		Plane plane = new Plane (_camera.transform.forward, target);

		for (int i = 0; i < _targets.Count; i++) 
		{
			for (int i2 = 0; i2 < _targets[i].BoundingBoxPoints.Count; i2++) 
			{
				ray = new Ray (_targets[i].BoundingBoxPoints[i2], -_camera.transform.forward);
				if (plane.Raycast (ray, out dist)) {
					if (dist < minDist || (i == 0 && i2 == 0))
						minDist = dist;
				}
			}
		}
		if (minDist < _camera.nearClipPlane)
			target = target + ((-_camera.transform.forward) * (_camera.nearClipPlane-minDist));
		return target;
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
		//GameObject		farestObj = null;
		float			dist;
		float 			maxDist = 0f;
		List<Vector3>	points = new List<Vector3> ();

		for (int i = 0; i < _targets.Count; i++) 
		{
			for (int i2 = 0; i2 < _targets[i].BoundingBoxPoints.Count; i2++) 
			{
				dist = Vector3.Distance (_targetPos,_targets[i].BoundingBoxPoints[i2]);
				if (dist > maxDist)
					maxDist = dist;
			}
		}
		return maxDist;
	}

}
