namespace Scene_Loader{
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using Eflatun.SceneReference;
  using TMPro;
  using UnityEngine;
  using UnityEngine.SceneManagement;
  using UnityEngine.UI;
  
  public class SceneLoader : MonoBehaviour {
  
      public static SceneLoader Instance { get; private set; }
  
      [Header("Level Setting")]
      [SerializeField] private SceneReference Core_System;
      [SerializeField] public List<SceneReference> Level_Scene;
  
      [Header("Loading Setting")]
      [SerializeField] private CanvasGroup loadingCanvas;
      [SerializeField] private float loadingTransitionTime = 2f;
      [SerializeField] private TextMeshProUGUI loadingText;
      [SerializeField] private Slider loadingBar;
  
      private String[] loadingTextArray = { "Loading", "Loading .", "Loading ..", "Loading ..." };
      private bool isLoadingTextRunning;
      [SerializeField] private float lerpSpeed = 4f;
  
      private bool isCoreSystemLoading;
  
      private void Awake() {
          if (Instance == null) {
              Instance = this;
              DontDestroyOnLoad(gameObject);
          } else {
              Destroy(gameObject);
          }
          StartCoroutine(LoadScene(Level_Scene[0]));
      }
  
      public IEnumerator EnsureCoreSystemLoaded() {
          if (SceneManager.GetSceneByName(Core_System.Name).isLoaded || isCoreSystemLoading) yield break;
  
          isCoreSystemLoading = true;
          AsyncOperation loadCoreSystem = SceneManager.LoadSceneAsync(Core_System.Name, LoadSceneMode.Additive);
  
          while (!loadCoreSystem.isDone) {
              yield return null;
          }
          isCoreSystemLoading = false;
      }
  
      public IEnumerator LoadScene(SceneReference scene) {
          yield return StartCoroutine(ShowLoadingScreen());
  
          yield return StartCoroutine(EnsureCoreSystemLoaded());
  
          AsyncOperation loadScene = SceneManager.LoadSceneAsync(scene.Name, LoadSceneMode.Additive);
          loadScene.allowSceneActivation = false;
  
          float progress = 0f;
  
          while (loadScene.progress < 0.9f) {
              Debug.Log("Progressing loading bar to 90%");
              progress = Mathf.Lerp(progress, loadScene.progress, 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime));
              if (Mathf.Abs(progress - loadScene.progress) < 0.01f) progress = loadScene.progress;
              UpdateLoadingBar(progress);
              yield return null;
          }
  
          while (progress < 1f) {
              Debug.Log("Progressing loading bar to 100%");
              progress = Mathf.Lerp(progress, 1f, 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime));
              if (Mathf.Abs(progress - 1f) < 0.01f) progress = 1f;
              UpdateLoadingBar(progress);
              yield return null;
          }
  
  
          loadScene.allowSceneActivation = true;
          yield return loadScene;
          Debug.Log("Scene succesfully loaded.");
  
          yield return SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene().buildIndex);
          Debug.Log($"Unloading current scene : {SceneManager.GetActiveScene().name}");
  
          yield return StartCoroutine(HideLoadingBar());
      }
  
      private IEnumerator HideLoadingBar() {
          Debug.Log("Hide Loading Bar");
          yield return StartCoroutine(Fading(1f, 0f));
      }
  
      private void UpdateLoadingBar(float progress) {
          loadingBar.value = progress;
      }
  
      private IEnumerator ShowLoadingScreen() {
          Debug.Log("Show Loading Screen");
          yield return StartCoroutine(Fading(0f, 1f));
      }
  
      private IEnumerator Fading(float start, float end) {
          float timeElapsed = 0f;
          isLoadingTextRunning = true;
          Coroutine loadingTextCoroutine = StartCoroutine(LoadingText());
          while (timeElapsed < loadingTransitionTime) {
              loadingCanvas.alpha = Mathf.Lerp(start, end, timeElapsed / loadingTransitionTime);
              timeElapsed += Time.deltaTime;
              yield return null;
          }
          loadingCanvas.alpha = end;
          isLoadingTextRunning = false;
      }
  
      private IEnumerator LoadingText() {
          while (isLoadingTextRunning) {
              foreach (String text in loadingTextArray) {
                  if (!isLoadingTextRunning) yield break;
  
                  loadingText.text = text;
                  yield return new WaitForSeconds(0.2f);
              }
          }
      }
  }
}
