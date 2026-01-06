using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SelectionScript : MonoBehaviour
{
    private bool IsMainScene => SceneManager.GetActiveScene().name == "MeetScene";

    [Header("Music Settings")]
    [SerializeField] private List<Button> musicButtons = new();
    [SerializeField] private List<GameObject> musicObjects = new();

    [Header("Line Buttons")]
    [SerializeField] private List<Button> lineButtons = new();

    private const string MusicKey = "SelectedMusic";
    private const string LineKey  = "SelectedLine";

    private void Awake()
    {
        if (!PlayerPrefs.HasKey(MusicKey)) PlayerPrefs.SetInt(MusicKey, 1);

        if (IsMainScene)
        {
            if (!PlayerPrefs.HasKey(LineKey))  PlayerPrefs.SetInt(LineKey, 1);
        }
    }

    private void Start()
    {
        int savedMusic = PlayerPrefs.GetInt(MusicKey);
        int savedLine  = PlayerPrefs.GetInt(LineKey);

        ApplySelection(musicButtons, musicObjects, savedMusic);

        if (IsMainScene)
        {
            ApplySelection(lineButtons, savedLine);
        }
    }

    public void SelectMusic(int index) => HandleSelection(musicButtons, musicObjects, index, MusicKey);
    public void SelectLine(int index)  => HandleSelection(lineButtons, index, LineKey);

    private void HandleSelection(List<Button> buttons, List<GameObject> objects, int index, string key)
    {
        int valid = Mathf.Clamp(index, 1, buttons.Count);
        PlayerPrefs.SetInt(key, valid);
        PlayerPrefs.Save();
        ApplySelection(buttons, objects, valid);
    }

    private void HandleSelection(List<Button> buttons, int index, string key)
    {
        int valid = Mathf.Clamp(index, 1, buttons.Count);
        PlayerPrefs.SetInt(key, valid);
        PlayerPrefs.Save();
        ApplySelection(buttons, valid);
    }
    private void ApplySelection(List<Button> buttons, List<GameObject> objects, int selected)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            bool isActive = (i == selected - 1);
            buttons[i].interactable = !isActive;
            if (i < objects.Count)
                objects[i].SetActive(isActive);
        }
    }
    private void ApplySelection(List<Button> buttons, int selected)
    {
        for (int i = 0; i < buttons.Count; i++)
            buttons[i].interactable = (i != selected - 1);
    }
}
