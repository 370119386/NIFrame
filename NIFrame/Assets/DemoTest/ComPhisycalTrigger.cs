using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum ComPhisycalTriggerType
{
    CPTT_SPEED = 0,
    CPTT_STAIR = 1,
}

[RequireComponent(typeof(BoxCollider))]
public class ComPhisycalTrigger : MonoBehaviour
{
    public ComPhisycalTriggerType eTriggerType = ComPhisycalTriggerType.CPTT_SPEED;
    public Vector3 mForce = Vector3.one;
    public bool mHasDir = true;
    public UnityEvent onStairBegin;
    public UnityEvent onStairEnd;

    // Use  for initialization
    void Start ()
    {

	}

    void OnTriggerEnter(Collider collider)
    {
        if (eTriggerType == ComPhisycalTriggerType.CPTT_SPEED)
        {
            Rigidbody rigid = collider.GetComponent<Rigidbody>();
            if (null != rigid)
            {
                if(mHasDir)
                {
                    rigid.AddForce(mForce, ForceMode.VelocityChange);
                }
                else
                {
                    var tmpForce = mForce;
                    if(collider.transform.position.x < transform.position.x)
                    {
                        if(tmpForce.x < 0)
                        {
                            tmpForce.x = -tmpForce.x;
                        }
                    }
                    else
                    {
                        if (tmpForce.x > 0)
                        {
                            tmpForce.x = -tmpForce.x;
                        }
                    }
                    rigid.AddForce(tmpForce, ForceMode.VelocityChange);
                }
            }
        }

        if (eTriggerType == ComPhisycalTriggerType.CPTT_STAIR)
        {
            if (null != onStairBegin)
            {
                onStairBegin.Invoke();
            }

            Rigidbody rigid = collider.GetComponent<Rigidbody>();
            if (null != rigid)
            {
                //rigid.AddForce(mForce, ForceMode.Force);
                rigid.AddForce(Vector3.up * 0.2f, ForceMode.VelocityChange);
            }
        }
    }

    void OnTriggerExit(Collider collider)
    {
        if (eTriggerType == ComPhisycalTriggerType.CPTT_STAIR)
        {
            if (null != onStairEnd)
            {
                onStairEnd.Invoke();
            }
        }
    }

    void OnTriggerStay(Collider collider)
    {
        if (eTriggerType == ComPhisycalTriggerType.CPTT_STAIR)
        {
            Rigidbody rigid = collider.GetComponent<Rigidbody>();
            if (null != rigid)
            {
                rigid.AddForce(mForce, ForceMode.Force);
            }
        }
    }
}