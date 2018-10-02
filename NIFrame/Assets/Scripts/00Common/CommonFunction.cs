using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CommonFunction
{
    public static void CustomActive(this GameObject gameObject, bool active)
    {
        if (null != gameObject)
        {
            if (gameObject.activeSelf != active)
            {
                gameObject.SetActive(active);
            }
        }
    }

    public static void CustomActive(this Component component, bool active)
    {
        if (null != component && null != component.gameObject)
        {
            if (component.gameObject.activeSelf != active)
            {
                component.gameObject.SetActive(active);
            }
        }
    }

    public static void Shuffle(int[] intArray)
    {
        for (int i = 0; i < intArray.Length; i++)
        {
            int temp = intArray[i];
            int randomIndex = Random.Range(0, intArray.Length);
            intArray[i] = intArray[randomIndex];
            intArray[randomIndex] = temp;
        }
    }

    public static GameObject FindCanvasRoot(string layername)
    {
        var layer = GameObject.Find(layername);
        return layer;
    }
}