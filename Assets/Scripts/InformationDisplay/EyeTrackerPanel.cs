using ReadyPlayerMe.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EyeTrackerPanel : MonoBehaviour
{
    public Gaze gaze;
    public RectTransform crosshair;
    public RawImage eyeleft;
    public RawImage eyeright;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        eyeleft.texture = gaze.ModelRunner.LeftEyeTexture;
        eyeright.texture = gaze.ModelRunner.RightEyeTexture;
        crosshair.anchoredPosition = new Vector2(gaze.gazeLocation.x/2, -gaze.gazeLocation.y/2);

    }
    Vector2 ClampLocalPointToRect(RectTransform rt, Vector2 localPoint)
    {
        Vector2 size = rt.rect.size;
        Vector2 half = size * 0.5f;
        float x = Mathf.Clamp(localPoint.x, -half.x, half.x);
        float y = Mathf.Clamp(localPoint.y, -half.y, half.y);
        return new Vector2(x, y);
    }
}
