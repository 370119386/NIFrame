using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GG
{
    class ComFollowPlayer : MonoBehaviour
    {
        public Camera main = null;
        public Vector2 xRange = Vector2.zero;
        public Transform player = null;
        public float cameraXSize = 5.33f;

        protected void Update()
        {
            if(null != player && null != main)
            {
                float ll = player.transform.position.x - cameraXSize * 0.50f;
                float rr = player.transform.position.x + cameraXSize * 0.50f;
                if (ll >= xRange.x && rr <= xRange.y)
                {
                    main.transform.position = player.transform.position;
                }
                else if(ll < xRange.x)
                {
                    var pos = main.transform.position;
                    pos.x = xRange.x + cameraXSize * 0.50f;
                    main.transform.position = pos;
                }
                else
                {
                    var pos = main.transform.position;
                    pos.x = xRange.y - cameraXSize * 0.50f;
                    main.transform.position = pos;
                }
            }
        }
    }
}
