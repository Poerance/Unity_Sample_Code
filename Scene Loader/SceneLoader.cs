using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Eflatun.SceneReference;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour {
    public static SceneLoader Instance { get; private set; }

    private bool isCoreSystemLoading;
    private bool isScreensLoading;
    private bool isFirstTimeLoad = true;

    private readonly string[] loadingTexts = {
        "Loading",
        "Loading.",
        "Loading..",
        "Loading...",
        "Loading..",
        "Loading.",
    };
    private readonly string[] gameOverTexts = {
        "",
        "Press R to restart game"
    };
    private const float TEXT_ANIMATION_INTERVAL = 0.25f;

    [Header("Game Configuration")]
    [SerializeField] private SceneReference coreSystem;
    [SerializeField] private List<SceneReference> levelScenes;

    [Header("Screens Configuration")]
    [SerializeField] private SceneReference screens;

    [SerializeField] private CanvasGroup loadingScreenCanvasGroup;
    [SerializeField] private CanvasGroup gameOverScreenCanvasGroup;

    [SerializeField] private float loadingScreenTransitionDuration;
    [SerializeField] private float gameOverScreenTransitionDuration;

    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private TMP_Text gameOverText;
    private Slider loadingBar;

    private bool isAnimateLoadingText;
    private bool isAnimateGameOverText;
    private CancellationTokenSource cts;

    private void OnDisable() {
        cts?.Cancel();
    }

    private void OnDestroy() {
        cts?.Dispose();
    }

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private async void Start() {
        for (int i = 0; i < levelScenes.Count; i++) {
            SceneList.AddScene(levelScenes[i]);
        }

        cts = new CancellationTokenSource();

        await EnsureCoreSystemLoaded(cts.Token);

        loadingBar = loadingScreenCanvasGroup.GetComponentInChildren<Slider>();

        await LoadScene(levelScenes[0], cts.Token);
    }

    private async UniTask EnsureCoreSystemLoaded(CancellationToken token) {
        if (SceneManager.GetSceneByName(coreSystem.Name).isLoaded || isCoreSystemLoading) return;

        isCoreSystemLoading = true;
        AsyncOperation loadCoreSystem = SceneManager.LoadSceneAsync(coreSystem.Name, LoadSceneMode.Additive);

        if (token.IsCancellationRequested) return;

        await loadCoreSystem.ToUniTask(cancellationToken: token);

        isCoreSystemLoading = false;
    }

    public async UniTask LoadScene(SceneReference loadScene, CancellationToken token) {
        if (loadScene == coreSystem || loadScene == screens) return;

        await ShowLoadingScreen(token);

        await EnsureCoreSystemLoaded(token);

        AsyncOperation asyncLoadScene = SceneManager.LoadSceneAsync(loadScene.Name, LoadSceneMode.Additive);
        asyncLoadScene.allowSceneActivation = false;

        float progress = 0f;

        while (asyncLoadScene.progress < 0.9f) {
            progress = Mathf.MoveTowards(progress, asyncLoadScene.progress, Time.deltaTime);
            UpdateLoadingBar(progress);
            await UniTask.Yield(token);
        }

        while (progress < 1f) {
            progress += Time.deltaTime;
            UpdateLoadingBar(progress);
            await UniTask.Yield(token);
        }

        if (!token.IsCancellationRequested) {
            asyncLoadScene.allowSceneActivation = true;
            await asyncLoadScene.ToUniTask(cancellationToken: token);
        }

        await HideLoadingScreen(token);
    }

    public async UniTask LoadScene(SceneReference loadScene, SceneReference unloadScene, CancellationToken token) {
        if (unloadScene == coreSystem || unloadScene == screens) return;

        await SceneManager.UnloadSceneAsync(unloadScene.Name).ToUniTask(cancellationToken: token);
        await LoadScene(loadScene, token);

    }

    private async UniTask Fading(CanvasGroup canvasGroup, float start, float end, float transitionDuration, CancellationToken token) {
        float timeElapsed = 0f;

        canvasGroup.alpha = start;

        while (timeElapsed < transitionDuration) {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, end, Time.deltaTime / transitionDuration);
            timeElapsed += Time.deltaTime;
            await UniTask.Yield(token);
        }

        canvasGroup.alpha = end;
    }

    private async UniTask ShowLoadingScreen(CancellationToken token) {
        if (isFirstTimeLoad) {
            isFirstTimeLoad = false;

            isAnimateLoadingText = true;
            AnimateLoadingText(token).Forget();
            await Fading(loadingScreenCanvasGroup, 0, 1f, loadingScreenTransitionDuration, token);
            return;
        }
        isAnimateLoadingText = true;
        AnimateLoadingText(token).Forget();
        await Fading(loadingScreenCanvasGroup, 0, 1f, loadingScreenTransitionDuration, token);
    }

    private async UniTask HideLoadingScreen(CancellationToken token) {
        isAnimateLoadingText = false;
        await Fading(loadingScreenCanvasGroup, 1f, 0, loadingScreenTransitionDuration, token);
        loadingScreenCanvasGroup.alpha = 0;
    }

    private async UniTask AnimateLoadingText(CancellationToken token) {
        while (isAnimateLoadingText) {
            foreach (string loadingText in loadingTexts) {
                this.loadingText.text = loadingText;

                if (!isAnimateLoadingText) return;

                await UniTask.Delay((int)(TEXT_ANIMATION_INTERVAL * 1000), cancellationToken: token);
            }
        }
    }

    private async UniTask AnimateGameOverText(CancellationToken token) {
        while (isAnimateGameOverText) {
            foreach (string gameOverText in gameOverTexts) {
                this.gameOverText.text = gameOverText;

                if (!isAnimateGameOverText) return;

                await UniTask.Delay((int)(TEXT_ANIMATION_INTERVAL * 1000), cancellationToken: token);
            }
        }
    }

    private void UpdateLoadingBar(float progress) {
        loadingBar.value = progress;
    }

    public async UniTask ShowGameOverScreen(CancellationToken token) {
        isAnimateGameOverText = true;
        AnimateGameOverText(token).Forget();
        await Fading(gameOverScreenCanvasGroup, 0, 1f, gameOverScreenTransitionDuration, token);

    }

    private async UniTask HideGameOverScreen(CancellationToken token) {
        await Fading(gameOverScreenCanvasGroup, 1f, 0, gameOverScreenTransitionDuration, token);
        isAnimateGameOverText = false;
        gameOverScreenCanvasGroup.alpha = 0;
    }

    public async void RestartGame() {
        CancellationToken token = new CancellationTokenSource().Token;
        await SceneManager.UnloadSceneAsync(SceneList.GetScene(0).Name).ToUniTask(cancellationToken: token);
        await HideGameOverScreen(token);
        await LoadScene(levelScenes[0], token);
    }
}
