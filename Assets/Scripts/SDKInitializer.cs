using UnityEngine;
using UnityEngine.SceneManagement;
using MirraGames.SDK;

public class SDKInitializer : MonoBehaviour {

    [SerializeField] private string _mainSceneName = "Init";

    private void Start() {
        MirraSDK.WaitForProviders(() => {
            SceneManager.LoadScene(_mainSceneName);
        });
    }
}