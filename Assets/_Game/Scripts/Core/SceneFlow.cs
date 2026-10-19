using UnityEngine;
using UnityEngine.SceneManagement;

namespace SixtyOne.Core
{
    /// <summary>Scene changes between the main menu, the tutorial and the game, and quitting. Clears pause / input statics first.</summary>
    public static class SceneFlow
    {
        public const string MainMenuScene = "MainMenu";
        public const string GameScene = "Test_Building";
        public const string TutorialScene = "Tutorial";

        public static void StartGame() => Load(GameScene);

        public static void StartTutorial() => Load(TutorialScene);

        public static void LoadMainMenu() => Load(MainMenuScene);

        public static void Quit()
        {
            PauseState.ForceClear();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void Load(string scene)
        {
            PauseState.ForceClear();
            InputGate.Reset();
            NavigationEvents.ClearAll();
            SceneManager.LoadScene(scene);
        }
    }
}
