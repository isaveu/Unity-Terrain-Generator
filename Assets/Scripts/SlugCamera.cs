using UnityEngine;
using System.Collections;

public class SlugCamera : MonoBehaviour {

	public float _mouseSensitivity;
	private Vector3 _lastMousePos;

	// Use this for initialization
	void Start () {
	
		Camera.main.orthographicSize = Screen.height / SlugUtils.PIXEL_UNIT;
	}
	
	// Update is called once per frame
	void Update () {

		if (Input.GetMouseButtonDown(0))
		{
			_lastMousePos = Input.mousePosition;
		}

		if (Input.GetMouseButton(0))
		{
			Vector2 delta = Input.mousePosition - _lastMousePos;
			delta *= -_mouseSensitivity * Time.deltaTime;

			transform.Translate(delta);

			_lastMousePos = Input.mousePosition;
		}
	}
}
