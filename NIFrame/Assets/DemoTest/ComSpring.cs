using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public class ComSpring : MonoBehaviour
{
    public Vector3 mForce = Vector3.one;
    public bool mHasDir = true;
    public UnityEvent onTrigger;

    // Use  for initialization
    void Start()
    {

    }

    void OnTriggerEnter(Collider collider)
    {
        Rigidbody rigid = collider.GetComponent<Rigidbody>();
        if (null != rigid)
        {
            if (mHasDir)
            {
                rigid.AddForce(mForce, ForceMode.VelocityChange);
            }
            else
            {
                var tmpForce = mForce;
                if (collider.transform.position.x < transform.position.x)
                {
                    if (tmpForce.x < 0)
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

    //void OnTriggerExit(Collider collider)
    //{

    //}

    //void OnTriggerStay(Collider collider)
    //{

    //}
}