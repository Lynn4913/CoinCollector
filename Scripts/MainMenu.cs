using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CoinCollector
{
    /// <summary>
    /// 主菜单按钮逻辑：开始游戏与退出。
    /// 退出在打包后的程序中真实生效；在编辑器中通过 Console 日志与停止播放模式来验证。
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Header("Scene")]
        [Tooltip("要加载的关卡场景名，必须与 Build Settings 中的场景名一致")]
        [SerializeField] private string gameSceneName = "MorandiGameScene";

        /// <summary>Start Game 按钮。</summary>
        public void StartGame()
        {
            if (string.IsNullOrEmpty(gameSceneName))
            {
                Debug.LogError("[MainMenu] gameSceneName 为空，无法开始游戏。");
                return;
            }

            if (!IsSceneAvailable(gameSceneName))
            {
                Debug.LogError("[MainMenu] 场景 \"" + gameSceneName + "\" 未加入 Build Settings，无法加载。");
                return;
            }

            SceneManager.LoadScene(gameSceneName);
        }

        /// <summary>Quit 按钮。</summary>
        public void QuitGame()
        {
            Debug.Log("[MainMenu] QuitGame() 已被调用。" + (Application.isEditor ? "（编辑器中不会真正退出应用，此处仅记录日志）" : string.Empty));
            Application.Quit();

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#endif
        }

        private static bool IsSceneAvailable(string sceneName)
        {
            int count = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < count; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (string.Equals(name, sceneName, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
