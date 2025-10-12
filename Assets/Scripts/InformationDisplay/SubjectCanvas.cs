using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SubjectCanvas : MonoBehaviour
{
    public Image background;
    public CanvasGroup startInfo;
    public TMP_Text endInfo;
    public GameObject calibrationCanvas;
    public void StartTask()
    {
        background.DOFade(0.0f, 1f);
        startInfo.DOFade(0.0f, 1f);
    }

    public void StopTask()
    {
        background.DOFade(0.9f, 1f);
        endInfo.DOFade(1f, 1f);
    }

    public void RestartTask()
    {
        background.DOFade(0.9f, 1f);
        startInfo.DOFade(1.0f, 1f);
        endInfo.DOFade(0f, 0.1f);

    }

    public void ShowCalibration()
    {
        calibrationCanvas.SetActive(true);
    }
    public void HideCalibration()
    {
        calibrationCanvas.SetActive(false);
    }
}