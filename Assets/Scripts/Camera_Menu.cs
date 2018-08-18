//
// Menu Camera which smoothly floats near the menu buttons
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Menu : MonoBehaviour
{
	public Vector3 initialPos = new Vector3();
	public Vector3 finalPos = new Vector3();


	// Initialize
	void Start()
	{
		// start at initial position
		Camera.main.transform.position = initialPos;
	}


	// Update camera position
	void Update()
	{
        Camera.main.transform.position += (finalPos - Camera.main.transform.position) / 50;
		Camera.main.transform.LookAt(Vector3.zero);
	}
}
