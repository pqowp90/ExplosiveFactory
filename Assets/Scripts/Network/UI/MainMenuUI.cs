using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplosiveFactory.Network.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button hostButton = null!;
        [SerializeField] private Button quitButton = null!;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI titleText = null!;
        [SerializeField] private TextMeshProUGUI versionText = null!;

        private void Start()
        {
            if (hostButton != null) hostButton.onClick.AddListener(OnClickHost);
            if (quitButton != null) quitButton.onClick.AddListener(OnClickQuit);

            if (versionText != null)
            {
                versionText.text = $"v{Application.version}";
            }
        }

        private void OnClickHost()
        {
            if (CustomNetworkManager.singleton != null)
            {
                CustomNetworkManager.singleton.HostLobby(maxPlayers: 4, isFriendsOnly: true);
            }
            else
            {
                Debug.LogError("[MainMenuUI] CustomNetworkManager singleton is missing!");
            }
        }

        private void OnClickQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
