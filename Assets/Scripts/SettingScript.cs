using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MirraGames.SDK;
using MirraGames.SDK.Common;

public class SettingScript : MonoBehaviour
{
    private bool IsMainScene => SceneManager.GetActiveScene().name == "MeetScene";

    [Header("Music")]
    public bool musicEnabled;
    public GameObject audioMusic;
    public Button buttonMusic;
    public Animation animMusic;

    [Header("Images and Sprites")]
    public Image imageMusic;
    public Sprite spriteOn;
    public Sprite spriteOff;

    private void Start()
    {
        SyncFromPlayerPrefs();
        InitializeMusic();
    }

    [Tooltip("MUSIC/SOUND TOGGLE")]
    public void ToggleMusic()
    {
        musicEnabled = !musicEnabled; // Переключаем состояние
        SyncToPlayerPrefs();

        if (musicEnabled)
        {
            audioMusic.SetActive(true);
            animMusic.Play("animToggleOn");
        }
        else
        {
            audioMusic.SetActive(false);
            animMusic.Play("animToggleOff");
        }

        // Передаем кнопку, соответствующий Image и текущее состояние
        StartCoroutine(EnableButtonAfterDelay(buttonMusic, imageMusic, musicEnabled));
    }

    // Корутинa отключает кнопку, ждет 0.2 сек, затем меняет спрайт в зависимости от состояния,
    // и снова делает кнопку активной.
    private IEnumerator EnableButtonAfterDelay(Button button, Image image, bool isEnabled)
    {
        button.interactable = false;
        yield return new WaitForSeconds(0.2f);
        image.sprite = isEnabled ? spriteOn : spriteOff;
        button.interactable = true;
    }

    [Tooltip("MUSIC/SOUND PLAYERPREFS")]
    private void SyncFromPlayerPrefs()
    {
        musicEnabled = MirraSDK.Data.GetInt("intMusic", 0) == 0; // 0 значит включено
    }

    private void SyncToPlayerPrefs()
    {
        MirraSDK.Data.SetInt("intMusic", musicEnabled ? 0 : 1);
    }

    [Tooltip("MUSIC/SOUND INITIALIZE")]
    private void InitializeMusic()
    {
        if (musicEnabled)
        {
            if (IsMainScene) animMusic.Play("animToggleOn");
            audioMusic.SetActive(true);
            if (imageMusic != null) imageMusic.sprite = spriteOn;
        }
        else
        {
            if (IsMainScene) animMusic.Play("animToggleOff");
            audioMusic.SetActive(false);
            if (imageMusic != null) imageMusic.sprite = spriteOff;
        }
    }
}