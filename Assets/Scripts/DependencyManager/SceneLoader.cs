using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public LoadingCanvas loadingCanvas;
    public UnityEvent sceneLoaded;

    public Scene CurrentScene
    {
        get
        {
            if (currentScene == null)
            {
                var currScene = SceneManager.GetActiveScene();
                currentScene = (Scene)currScene.buildIndex;
            }

            return currentScene.Value;
        }
        set => currentScene = value;
    }

    Scene? currentScene;

    public void LoadScene(Scene scene)
    {
        currentScene = scene;
        Debug.Log($"SCENE: {CurrentScene}");
        StartCoroutine(PlayTransitionAndLoad());
    }

    public void LoadSceneAdditive(Scene scene)
    {
        SceneManager.LoadSceneAsync((int)CurrentScene,LoadSceneMode.Additive);
    }

    public void UnloadSceneAdditive(Scene scene)
    {
        SceneManager.UnloadSceneAsync((int)scene);
    }
    public void UnloadSceneAdditive(int index)
    {
        SceneManager.UnloadSceneAsync(index);
    }

    private IEnumerator PlayTransitionAndLoad()
    {
        yield return new WaitUntil(() => TVCloseRenderFeature.Instance != null);

        TVCloseRenderFeature.Instance.PlayCloseEffect(2f, () =>
        {
            loadingCanvas.ShowLoadingCanvas();
            StartCoroutine(LoadSceneAsyncWithUI());
        });
    }

    private IEnumerator DelayedStartSceneLoad(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(LoadSceneAsyncWithUI());
    }

    private IEnumerator LoadSceneAsyncWithUI()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync((int)CurrentScene);
        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            Debug.Log($"Loading progress: {progress}");
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);
        sceneLoaded.Invoke();

        loadingCanvas.HideLoadingCanvas();
        TVCloseRenderFeature.Instance.PlayOpenEffect(2f);
    }
}

public enum Scene
{
    Initialization = 0,
    MainMenu = 1,


    //Add another scene like this ^
}