using System.Collections;
using System.Collections.Generic;
using UnitEye;
using UnityEngine;

public class GazeSceneTest : MonoBehaviour
{
    private int focusedCount;

    bool moving;

    GameObject hitObject = null;

    void Start()
    {
    }

    void Update()
    {
        if (focusedCount > 29)
            focusedCount = 30;
        else if (UnitEyeAPI.GetFocusedGameObject() != null)
            focusedCount++;
        else if (!moving)
            focusedCount = 0;

        if (!moving && focusedCount > 29)
        {
            hitObject = UnitEyeAPI.GetFocusedGameObject();
            hitObject.GetComponent<Renderer>().material.color = Color.green;
            moving = true;
        }
        else if (UnitEyeAPI.IsBlinking())
        {
            focusedCount = 0;
            if (hitObject != null) hitObject.GetComponent<Renderer>().material.color = Color.grey;
            hitObject = null;
            moving = false;
        }

    }
    void OnGUI()
    {
        GUI.Label(new Rect(100, 300, 100, 100), $"{focusedCount}");
        var focusedObject = UnitEyeAPI.GetFocusedGameObject();
        GUI.Label(new Rect(100, 320, 100, 100), $"{(focusedObject != null ? focusedObject.name : "None")}");
    }
}
