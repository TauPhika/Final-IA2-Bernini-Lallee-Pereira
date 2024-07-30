using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour
{
	public void Open()
    {
		var rb = GetComponent<Rigidbody>();
		rb.AddForce(10f*(Vector3.up + Random.Range(-0.1f, 0.1f) * Vector3.right + Random.Range(-0.1f, 0.1f) * Vector3.forward), ForceMode.Impulse);
		rb.AddTorque(new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)), ForceMode.Impulse);
		rb.detectCollisions = false;

		Destroy(gameObject, 3f);
	}
}