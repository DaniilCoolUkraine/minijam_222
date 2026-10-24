using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ShapeInventory.Game
{
    /// <summary>
    /// Main menu logic: Play loads the game scene, Quit exits (hidden on WebGL where quitting is impossible).
    /// The cutscene overlay (<see cref="CutscenePlayer"/>) sits in front of this menu and hides itself when it ends.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Tooltip("Scene loaded by Play. It must be in the Build Settings (the editor menu 'Build Main Menu Scene' takes care of it).")]
        [SerializeField] private string gameSceneName = "Main";
        [SerializeField] private Button playButton;
        [SerializeField] private Button quitButton;
        [Tooltip("Optional. Only used to ignore Play presses while the cutscene is still on screen.")]
        [SerializeField] private CutscenePlayer cutscene;

        private void OnEnable()
        {
            if (playButton != null) playButton.onClick.AddListener(Play);
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
        }

        private void Start()
        {
            // Application.Quit does nothing in a browser, so the button would be dead.
            if (quitButton != null && Application.platform == RuntimePlatform.WebGLPlayer)
                quitButton.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (playButton != null) playButton.onClick.RemoveListener(Play);
            if (quitButton != null) quitButton.onClick.RemoveListener(Quit);
        }

        public void Play()
        {
            if (cutscene != null && cutscene.IsPlaying) return;

            if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                Debug.LogError("MainMenuController: scene '" + gameSceneName + "' is not in the Build Settings.", this);
                return;
            }

            SceneManager.LoadScene(gameSceneName);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
