using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using MirraGames.SDK;
using MirraGames.SDK.Common;

public class LevelScript : MonoBehaviour
{
    public enum GameMode { Tunnel, Labyrinth, SameColors, Obstacles, TimeLimiting }

    [Serializable]
    public struct ModeData
    {
        [Tooltip("Идентификатор режима")] public GameMode mode;
        [Tooltip("Отображаемое имя режима")] public string displayName;
    }

    [Header("Mode System")]
    public ModeData[] modes;

    [Header("UI")]
    public TextMeshProUGUI modeText;

    [Header("Level Selection UI")]
    public Image[] ImgLevels;
    public Color defaultColor;
    public Color completedColor;

    [Header("Scripts")]
    public MeetSelectionScript MeetScript;

    private GameMode currentMode;
    private const int MaxLevels = 15;

    private void Awake()
    {
        if (modes == null || modes.Length == 0)
            Debug.LogError("Modes not configured!");
    }

    private void Start()
    {
        // Восстанавливаем последний выбранный режим
        int saved = MirraSDK.Data.GetInt("GameMode", 0);
        SelectMode((GameMode)Mathf.Clamp(saved, 0, modes.Length - 1));
    }

    /// Выбор игрового режима и обновление UI
    public void SelectModeByIndex(int index)
    {
        if (index < 0 || index >= modes.Length) return;
        SelectMode(modes[index].mode);
        MeetScript.OpenLevel();
    }
    private void SelectMode(GameMode mode)
    {
        currentMode = mode;

        // Обновляем шапку
        foreach (var m in modes)
        {
            if (m.mode == mode && modeText != null)
                modeText.text = m.displayName;
        }

        // Сохраняем выбор
        MirraSDK.Data.SetInt("GameMode", (int)mode);

        // Обновляем подсветку пройденных уровней
        UpdateLevelUI();
    }


    /// <summary>
    /// Вызывает загрузку уровня и сохраняет выбор
    /// </summary>
    public void SelectLevel(int level)
    {
        int clamped = Mathf.Clamp(level, 1, MaxLevels);
        MirraSDK.Data.SetInt("SelectLevel", clamped);
        SceneManager.LoadScene("GameScene");
    }
    private void UpdateLevelUI()
    {
        // Сбрасываем все индикаторы
        for (int i = 0; i < ImgLevels.Length && i < MaxLevels; i++)
        {
            ImgLevels[i].color = defaultColor;
        }

        // Для каждого уровня проверяем, был ли он пройден
        for (int lvl = 1; lvl <= ImgLevels.Length && lvl <= MaxLevels; lvl++)
        {
            string key = $"{currentMode}_Level_{lvl}_Passed";
            if (MirraSDK.Data.GetInt(key, 0) == 1)
                ImgLevels[lvl-1].color = completedColor;
        }
    }
}
