using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ComPlayer : MonoBehaviour
{
    public float speed = 1.0f;
    public Vector3 jumpSpeed = Vector3.up;
    Rigidbody rigidBody;
    // Use this for initialization
    void Start ()
    {
        rigidBody = GetComponent<Rigidbody>();
    }
	
	// Update is called once per frame
	void Update ()
    {
		if(Input.GetKey(KeyCode.RightArrow))
        {
            transform.Translate(Vector3.right * speed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Translate(Vector3.left * speed * Time.deltaTime);
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            rigidBody.AddForce(jumpSpeed, ForceMode.VelocityChange);
        }
    }
}
