using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneScript : MonoBehaviour
{
    [Header("Pause")]
    public GameObject objectPause;
    public Animation panelAnimation;
    private Coroutine pauseCoroutine;
    private bool isPausing;

    // Пауза
    public void Pause()
    {
        if (pauseCoroutine != null)
            StopCoroutine(pauseCoroutine);

        objectPause.SetActive(true);
        panelAnimation.Play();

        isPausing = true;
        pauseCoroutine = StartCoroutine(PauseRoutine());
    }
    public void Resume()
    {
        isPausing = false;
        if (pauseCoroutine != null)
        {
            StopCoroutine(pauseCoroutine);
            pauseCoroutine = null;
        }

        objectPause.SetActive(false);
        Time.timeScale = 1f;
    }
    private IEnumerator PauseRoutine()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        if (isPausing)
            Time.timeScale = 0f;

        pauseCoroutine = null;
    }

    // Переход на главный экран
    public void MainPage()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MeetScene");
    }

    // Перезапуск текущего уровня
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameScene");
    }
}