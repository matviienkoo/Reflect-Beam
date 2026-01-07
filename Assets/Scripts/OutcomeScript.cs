using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MirraGames.SDK;
using MirraGames.SDK.Common;

public class OutcomeScript : MonoBehaviour
{
	public static OutcomeScript Instance { get; private set; }
	private LevelScript.GameMode currentMode;
    private int currentLevel;

	[Header("Main Panels")]
	public GameObject objectRestart;
	public GameObject objectAutoWin;

	[Header("Configuration Panels")]
    public GameObject[] objectOther;

    [Header("Audio")]
    public AudioSource LevelWin;

    [Header("Scritps")]
    public SceneScript ScriptScene;

	private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    public void DeativePanels()
    {
    	Time.timeScale = 1f;
        for (int i = 0; i < objectOther.Length; i++)
            objectOther[i].SetActive(false);
    }

	public void Lose ()
	{
		DeativePanels();
		ScriptScene.Restart();
	}
	
	public void WinAudio ()
	{
		LevelWin.Play();
	}
	public void Win ()
	{
		DeativePanels();
		objectAutoWin.SetActive(false);
		MirraSDK.Data.SetInt("AutoWinCount", 0);
        MirraSDK.Data.Save();

		LaserReflect2D laser = FindObjectOfType<LaserReflect2D>();
        if (laser != null)
        {
            laser.StartCoroutine("OnLevelComplete");
        }
        else
        {
            Debug.LogWarning("LaserReflect2D не найден на сцене!");
        }
	}
}