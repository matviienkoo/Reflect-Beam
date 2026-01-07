using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MirraGames.SDK;
using MirraGames.SDK.Common;

public class StructureScript : MonoBehaviour
{
    public enum GameMode { Tunnel, Labyrinth, SameColors, Obstacles, TimeLimiting }
    public static StructureScript Instance { get; private set; }

    [Header("Tunnel")]
    public List<GameObject> levelTunnel = new List<GameObject>(); 

    [Header("Labyrinth")]
    public List<GameObject> levelLabyrinth = new List<GameObject>(); 

    [Header("SameColors")]
    public List<GameObject> levelSameColors = new List<GameObject>(); 

    [Header("Obstacles")]
    public List<GameObject> levelObstacles = new List<GameObject>(); 

    [Header("TimeLimiting")]
    public List<GameObject> levelTimeLimiting = new List<GameObject>(); 

    [Header("UI")]
    public Transform parentLevel;
    public TextMeshProUGUI textLevel;

    [Header("Timer (TimeLimiting Mode)")]
    public GameObject objectTimer;
    public GameObject objectPause;
    public TextMeshProUGUI textTimer;
    public float timeLimitSeconds = 30f;

    [Header("Auto-Win Bonus UI")]
    public GameObject objectAutoWin;

    private float currentTime;
    private Coroutine timerCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        SpawnLevelMode();

        // показываем объект автовыигрыша, только если бонус есть
        int bonusCount = MirraSDK.Data.GetInt("AutoWinCount", 0);
        objectAutoWin.SetActive(bonusCount > 0);
    }

    public void SpawnLevelMode()
    {
        int level = MirraSDK.Data.GetInt("SelectLevel", 1);
        int modeIndex = MirraSDK.Data.GetInt("GameMode", 0);
        GameMode currentMode = (GameMode)Mathf.Clamp(modeIndex, 0, System.Enum.GetValues(typeof(GameMode)).Length - 1);

        // Обновляем текст уровня
        if (MirraSDK.Language.Current == LanguageType.English)
        {
            textLevel.text = "LEVEL " + level;
        }
        else
        {
            textLevel.text = "УРОВЕНЬ " + level;
        }

        // Очищаем предыдущий уровень
        foreach (Transform child in parentLevel)
        {
            Destroy(child.gameObject);
        }

        // Отключаем по умолчанию
        objectTimer.SetActive(false);
        objectPause.SetActive(false);
        StopTimer();

        // Выбираем список по режиму
        List<GameObject> listToUse = levelTunnel;
        switch (currentMode)
        {
            case GameMode.Tunnel:
                listToUse = levelTunnel;
                break;
            case GameMode.Labyrinth:
                listToUse = levelLabyrinth;
                break;
            case GameMode.SameColors:
                listToUse = levelSameColors;
                break;
            case GameMode.Obstacles:
                listToUse = levelObstacles;
                break;
            case GameMode.TimeLimiting:
                listToUse = levelTimeLimiting;
                if (objectTimer != null && textTimer != null)
                {
                    objectTimer.SetActive(true);
                    objectPause.SetActive(true);
                    StartTimer();
                }
                break;
        }

        // Спавним выбранный уровень (индекс level-1 для списка)
        int idx = Mathf.Clamp(level - 1, 0, listToUse.Count - 1);
        GameObject selectedLevel = listToUse[idx];
        if (selectedLevel != null)
            Instantiate(selectedLevel, parentLevel);
        else
            Debug.LogError($"Уровень не найден в списке для режима {currentMode} на позиции {level}");
    }

    private void StartTimer()
    {
        currentTime = timeLimitSeconds;
        UpdateTimerDisplay();
        timerCoroutine = StartCoroutine(TimerCoroutine());
    }

    private void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }

    private IEnumerator TimerCoroutine()
    {
        // Задержка перед началом отсчёта
        yield return new WaitForSeconds(2f);

        while (currentTime > 0f)
        {
            yield return null;
            currentTime -= Time.deltaTime;
            UpdateTimerDisplay();
        }
        OnTimeExpired();
    }

    private void UpdateTimerDisplay()
    {
        if (textTimer != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60f);
            int seconds = Mathf.FloorToInt(currentTime % 60f);
            textTimer.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    private void OnTimeExpired()
    {
        textTimer.text = "00:00";
        OutcomeScript.Instance.Lose();
    }

    /// <summary>
    /// Сброс таймера для режима TimeLimiting
    /// </summary>
    public void ResetTimer()
    {
        StopTimer();
        StartTimer();
    }
}
