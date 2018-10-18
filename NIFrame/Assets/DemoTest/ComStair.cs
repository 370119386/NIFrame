using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public class ComStair : MonoBehaviour
{
    public Vector3 mForce = Vector3.one;
    public Vector3 mInitSpeed = Vector3.up;
    public UnityEvent onStairBegin;
    public UnityEvent onStairEnd;

    // Use  for initialization
    void Start()
    {

    }

    void OnTriggerEnter(Collider collider)
    {
        if (null != onStairBegin)
        {
            onStairBegin.Invoke();
        }

        Rigidbody rigid = collider.GetComponent<Rigidbody>();
        if (null != rigid)
        {
            rigid.AddForce(mInitSpeed, ForceMode.VelocityChange);
        }
    }

    void OnTriggerExit(Collider collider)
    {
        if (null != onStairEnd)
        {
            onStairEnd.Invoke();
        }
    }

    void OnTriggerStay(Collider collider)
    {
        Rigidbody rigid = collider.GetComponent<Rigidbody>();
        if (null != rigid)
        {
            rigid.AddForce(mForce, ForceMode.Force);
        }
    }
}