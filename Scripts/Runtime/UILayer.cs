using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UILayer : MonoBehaviour
{
    private void Awake()
    {
        ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        var r = Screen.safeArea;
        var rt = transform as RectTransform;
        var anchorMin = r.position;
        var anchorMax = r.position + r.size;
        
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
    }

}
