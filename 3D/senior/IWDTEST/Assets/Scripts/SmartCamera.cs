using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmartCamera : MonoBehaviour {


	[SerializeField][Range(1,10)]
	private float				_lerpDuration = 4f;

	private Camera				_camera;
	private List<GameObject>	_meshes = new List<GameObject>();

	private Vector3 			_initPos;
	private Vector3				_targetPos;

	private float				_initSize;
	private float				_targetSize;

	private float				_startTime;
	private bool 				_moveCamera = false;
	private bool 				_zoomCamera = false;

	private float				_pauseTime;


	public void				StartMove()
	{
		GameObject[]	sceneObjs;

		_camera = this.gameObject.GetComponent<Camera> ();
		if (_camera == null) 
		{
			Debug.LogWarning ("SmartCamera work with Camera Component");
			return;
		}

		sceneObjs = GameObject.FindGameObjectsWithTag ("focusMesh");
		if (sceneObjs.Length == 0) 
		{
			Debug.LogWarning ("No 3D objects with the tag 'focusMesh' ");
		}
		for (int i = 0; i < sceneObjs.Length; i++)
			_meshes.Add (sceneObjs[i]);
		_initPos =_camera.transform.position;
		MoveCamera ();
	}

	public void				Pause()
	{
		_moveCamera = false;
		_pauseTime = Time.time;
	}

	public void				Continue()
	{
		_moveCamera = true;
		_startTime += (Time.time - _pauseTime);
	}

	public void				Reset()
	{
		_moveCamera = false;
		_camera.transform.position = _initPos;
		if (_camera.orthographic)
			_camera.orthographicSize = _initSize;	
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
	private float		CalcDistMaxObj(List<Plane> planes)
	{
		List<Vector3>	boundPoints = new List<Vector3> ();
		Ray 			ray;
		float 			dist;
		float 			distmax = 0f;

		for (int i = 0; i < _meshes.Count; i++) 
		{
			boundPoints = GetBoundingBoxPoints (_meshes [i]);
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

		for (int i = 0; i < _meshes.Count; i++) 
		{
			boundPoints = GetBoundingBoxPoints (_meshes [i]);
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
		Debug.Log (distmax.ToString ());
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

		distMaxX = CalcDistMaxObj (planes);
		planes.Clear ();

		planes.Add(new Plane (_camera.transform.up, _targetPos));
		planes.Add(new Plane (-_camera.transform.up, _targetPos));
		distMaxY = CalcDistMaxObj (planes);

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

		for (int i = 0; i < _meshes.Count; i++) {
			dist = Vector3.Distance (_targetPos, _meshes [i].transform.position);
			if (dist > maxDist) {
				maxDist = dist;
				farestObj = _meshes [i];
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

	private void		MoveCamera()
	{
		_targetPos = CalcNewPos ();
		if (_camera.orthographic)
		{
			_initSize = _camera.orthographicSize;
			_targetSize = CalcNewSize (_camera.aspect, _initSize);
			_zoomCamera = true;
		}
		_camera.farClipPlane = CalcDistViewCamera ();
		_moveCamera = true;
		_startTime = Time.time;
	}

	// Update is called once per frame
	void Update ()
	{
		if (_moveCamera)
		{
			float timeSinceStart;
			float lerpAdvance;

			timeSinceStart = Time.time - _startTime;
			lerpAdvance = timeSinceStart / _lerpDuration;

			this.transform.position = Vector3.Lerp (_initPos, _targetPos, lerpAdvance);
			if (_zoomCamera)
				_camera.orthographicSize = Mathf.Lerp (_initSize, _targetSize, lerpAdvance);
			if (lerpAdvance >= 1f)
				_moveCamera = false;
		}
	}
}
