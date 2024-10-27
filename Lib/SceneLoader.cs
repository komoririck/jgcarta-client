using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    private AsyncOperation _loadingOperation;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyManager.DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Loads a scene asynchronously and triggers a callback once it's loaded.
    /// </summary>
    public void LoadSceneAsync(string sceneName, Action onComplete = null)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName, onComplete));
    }

    /// <summary>
    /// Loads a scene asynchronously by build index and triggers a callback.
    /// </summary>
    public void LoadSceneAsync(int buildIndex, Action onComplete = null)
    {
        StartCoroutine(LoadSceneCoroutine(buildIndex, onComplete));
    }

    /// <summary>
    /// Stops the current loading operation if it's in progress.
    /// </summary>
    public void StopLoading()
    {
        if (_loadingOperation != null && !_loadingOperation.isDone)
        {
            StopAllCoroutines();
            _loadingOperation = null;
        }
    }

    // Coroutine to load the scene asynchronously by name
    private IEnumerator LoadSceneCoroutine(string sceneName, Action onComplete)
    {
        _loadingOperation = SceneManager.LoadSceneAsync(sceneName);
        _loadingOperation.allowSceneActivation = false;  // Prevent auto scene activation

        while (_loadingOperation.progress < 0.9f)
        {
            yield return null;  // Wait until loading is almost complete
        }

        // Optional: Add loading screen logic here

        // Activate the scene
        _loadingOperation.allowSceneActivation = true;

        // Wait for the scene to activate
        while (!_loadingOperation.isDone)
        {
            yield return null;
        }

        _loadingOperation = null;

        // Trigger the callback if any
        onComplete?.Invoke();
    }

    // Coroutine to load the scene asynchronously by build index
    private IEnumerator LoadSceneCoroutine(int buildIndex, Action onComplete)
    {
        _loadingOperation = SceneManager.LoadSceneAsync(buildIndex);
        _loadingOperation.allowSceneActivation = false;

        while (_loadingOperation.progress < 0.9f)
        {
            yield return null;
        }

        // Optional: Add loading screen logic here

        // Activate the scene
        _loadingOperation.allowSceneActivation = true;

        // Wait for the scene to activate
        while (!_loadingOperation.isDone)
        {
            yield return null;
        }

        _loadingOperation = null;

        // Trigger the callback if any
        onComplete?.Invoke();
    }
}
