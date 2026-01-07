using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameMode = LevelScript.GameMode;
using UnityEngine.SceneManagement;
using MirraGames.SDK;
using MirraGames.SDK.Common;

[RequireComponent(typeof(LineRenderer))]
public class LaserReflect2D : MonoBehaviour
{
    [Header("Общие настройки")]
    public int maxReflections = 8;
    public float maxDistance    = 30f;
    public LayerMask[] interactMasks;

    [Header("Визуализация")]
    public Color  laserColor    = Color.red;
    public float  baseWidth     = 0.05f;
    public float  flickerSpeed  = 25f;
    public float  flickerAmount = 0.015f;

    [Header("Менеджер лабиринта")]
    public LabyrinthManager labyrinthManager;

    [Header("Параметры победы")]
    public Animation animLevel;
    public float finishDelay = 2f;

    private readonly float[] allowedAngles = { 0f, 90f, 180f, 270f };
    private LineRenderer line;
    private bool isActive = false;
    private bool hasWon   = false;
    private GameMode currentMode;
    private float finishTimer = 0f;
    private bool  isTouchingFinishThisFrame = false;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.sortingOrder      = 5;
        line.useWorldSpace     = true;
        line.positionCount     = 0;
        line.numCapVertices    = 0;
        line.numCornerVertices = 0;

        switch (MirraSDK.Data.GetInt("SelectedLine", 1))
        {
            case 1: laserColor = Color.red;    break;
            case 2: laserColor = Color.white;  break;
            case 3: laserColor = Color.yellow; break;
        }
        var mat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        mat.SetColor("_TintColor", laserColor);
        line.material = mat;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(laserColor, 0f), new GradientColorKey(laserColor, 1f) },
            new[] { new GradientAlphaKey(1f, 0f),        new GradientAlphaKey(1f, 1f) }
        );
        line.colorGradient = grad;

        int saved = MirraSDK.Data.GetInt("GameMode", 0);
        int maxIdx = Enum.GetValues(typeof(GameMode)).Length - 1;
        currentMode = (GameMode)Mathf.Clamp(saved, 0, maxIdx);
    }

    IEnumerator Start()
    {
        yield return new WaitForSeconds(1.5f);
        isActive = true;
    }

    void Update()
    {
        if (!isActive || hasWon)
            return;

        isTouchingFinishThisFrame = false;

        if (currentMode == GameMode.Labyrinth)
            DrawLabyrinthLaser();
        else if (currentMode == GameMode.TimeLimiting)
            DrawTimeLimitingLaser();
        else
            CastLaser();

        UpdateFinishTimer();
    }

    // копия DrawLabyrinthLaser с условием полного покрытия ячеек
    private void DrawTimeLimitingLaser()
    {
        float f = Mathf.Sin(Time.time * flickerSpeed) * flickerAmount;
        line.startWidth = line.endWidth = baseWidth + f;

        var pts  = new List<Vector3> { transform.position };
        var path = labyrinthManager.CurrentPath;
        var fin  = labyrinthManager.FinalCell;
        bool complete = labyrinthManager.AllCellsVisited;

        foreach (var cell in path)
        {
            Vector3 center = cell.transform.position;
            pts.Add(center);

            if (cell == fin && complete)
            {
                isTouchingFinishThisFrame = true;
                pts.Add(center + Vector3.right * maxDistance);
                break;
            }
            // тег Finish учитываем тоже только при complete
            if (cell.CompareTag("Finish") && complete)
            {
                isTouchingFinishThisFrame = true;
            }
        }

        line.positionCount = pts.Count;
        for (int i = 0; i < pts.Count; i++)
            line.SetPosition(i, pts[i]);
    }

    private void DrawLabyrinthLaser()
    {
        float f = Mathf.Sin(Time.time * flickerSpeed) * flickerAmount;
        line.startWidth = line.endWidth = baseWidth + f;

        var pts  = new List<Vector3> { transform.position };
        var path = labyrinthManager.CurrentPath;
        var fin  = labyrinthManager.FinalCell;

        foreach (var cell in path)
        {
            Vector3 center = cell.transform.position;
            pts.Add(center);

            if (cell == fin)
            {
                isTouchingFinishThisFrame = true;
                pts.Add(center + Vector3.right * maxDistance);
                break;
            }
            if (cell.CompareTag("Finish"))
                isTouchingFinishThisFrame = true;
        }

        line.positionCount = pts.Count;
        for (int i = 0; i < pts.Count; i++)
            line.SetPosition(i, pts[i]);
    }

    private void CastLaser()
    {
        Vector2 origin    = transform.position;
        Vector2 direction = transform.up;
        var     points    = new List<Vector3> { origin };

        int  combinedMask = interactMasks.Aggregate(0, (acc, m) => acc | m.value);
        bool ignoreBoxes  = (currentMode == GameMode.Tunnel);

        for (int i = 0; i < maxReflections; i++)
        {
            // flicker по ширине
            float f = Mathf.Sin(Time.time * flickerSpeed + i) * flickerAmount;
            line.startWidth = line.endWidth = baseWidth + f;

            var hits = Physics2D.RaycastAll(origin, direction, maxDistance, combinedMask);
            if (hits.Length == 0)
            {
                points.Add(origin + direction * maxDistance);
                break;
            }

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            // первый валидный враг, учитывая Tunnel
            RaycastHit2D hit = default;
            bool found = false;
            foreach (var h in hits)
            {
                if (ignoreBoxes && h.collider is BoxCollider2D)
                    continue;
                hit = h; found = true; break;
            }
            if (!found)
            {
                points.Add(origin + direction * maxDistance);
                break;
            }

            points.Add(hit.point);

            // если это Finish — помечаем касание и прерываем
            if (hit.collider.CompareTag("Finish"))
            {
                isTouchingFinishThisFrame = true;
                break;
            }
            else if (hit.collider.CompareTag("Mirror"))
            {
                direction = Vector2.Reflect(direction, hit.normal).normalized;
                direction = QuantizeDirection(direction);
                origin = hit.point + direction * 0.01f;
            }
            else if (hit.collider.CompareTag("Glass"))
            {
                origin = hit.point + direction * 0.01f;
            }
            else
            {
                break;
            }
        }

        line.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
            line.SetPosition(i, points[i]);
    }

    private void UpdateFinishTimer()
    {
        if (isTouchingFinishThisFrame)
            finishTimer += Time.deltaTime;
        else
            finishTimer = 0f;

        if (!hasWon && finishTimer >= finishDelay)
        {
            StartCoroutine(OnLevelComplete());
            hasWon = true;
        }
    }

    private IEnumerator OnLevelComplete()
    {
        // 1) Анимация деактивации уровня
        line.enabled = false;
        OutcomeScript.Instance.WinAudio();
        animLevel.Play("animLevelDeactive");
        yield return new WaitForSeconds(1.5f);

        // 2) Сохраняем прохождение ТОЛЬКО этого уровня для текущего режима
        int selectedLevel = MirraSDK.Data.GetInt("SelectLevel", 1);
        string levelKey  = $"{currentMode}_Level_{selectedLevel}_Passed";
        MirraSDK.Data.SetInt(levelKey, 1);

        // (Опционально) обновляем максимум, если он нужен где-то ещё
        string maxKey    = $"{currentMode}_PassedLevel";
        int prevMax      = MirraSDK.Data.GetInt(maxKey, 0);
        if (selectedLevel > prevMax)
            MirraSDK.Data.SetInt(maxKey, selectedLevel);

        // Гарантируем запись на диск
        MirraSDK.Data.Save();

        // 3) Переходим дальше
        // if (selectedLevel >= 15)
        // {
        //     SceneManager.LoadScene("MeetScene");
        //     yield break;
        // }
        // Иначе — спавним следующий уровень в StructureScript
        int nextLevel = selectedLevel + 1;
        MirraSDK.Data.SetInt("SelectLevel", nextLevel);
        MirraSDK.Data.Save();
        StructureScript.Instance.SpawnLevelMode();
    }

    private Vector2 QuantizeDirection(Vector2 dir)
    {
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float nearest = allowedAngles.OrderBy(a => Mathf.Abs(Mathf.DeltaAngle(ang, a))).First();
        float rad = nearest * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }
}
