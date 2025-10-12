using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingCanvas : MonoBehaviour
{
    [SerializeField] private GameObject background;
    [SerializeField] private GameObject loadingText;
    [SerializeField] private Transform loadingSpinner;
    [SerializeField] GameObject epilepsyInfoPanel;

    private Tween spinnerTween;

    public void ShowLoadingCanvas()
    {
        background.SetActive(true);
        loadingText.SetActive(true);
        loadingSpinner.gameObject.SetActive(true);

        spinnerTween = loadingSpinner.DORotate(
                new Vector3(0, 0, -360),
                2f,
                RotateMode.FastBeyond360
            )
            .SetLoops(-1)
            .SetEase(Ease.Linear);
    }

    public void ShowLoadingCanvasWithEpilepsyInfo()
    {
        ShowLoadingCanvas();
        epilepsyInfoPanel.SetActive(true);
    }

    public void HideLoadingCanvas()
    {
        spinnerTween?.Kill();
        loadingSpinner.gameObject.SetActive(false);
        loadingText.SetActive(false);
        background.SetActive(false);
        epilepsyInfoPanel.SetActive(false);
    }
}
