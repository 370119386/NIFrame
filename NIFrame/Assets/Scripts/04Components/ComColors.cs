using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NI
{
    public class ComColors : MonoBehaviour
    {
        public Color[] colors = new Color[0];

        public void SetTextColor(Text text,int index)
        {
            if(index >= 0 && index < colors.Length)
            {
                if(null != text)
                {
                    text.color = colors[index];
                }
            }
        }
    }
}