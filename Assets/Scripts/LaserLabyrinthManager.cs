// LabyrinthManager.cs
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LabyrinthManager : MonoBehaviour
{
    //public static LabyrinthManager Instance { get; private set; }

    [Header("Клетки")]
    [Tooltip("Стартовая клетка, на которую изначально светит лазер")]
    public LabyrinthCell startCell;
    [Tooltip("Финальная клетка: дойдя до неё, лазер уходит вправо")]
    public LabyrinthCell finalCell;

    [Header("Слои и пороги")]
    [Tooltip("Имя слоя, в который ставим активные пути")]
    public string pathLayerName = "LaserPath";
    [Tooltip("Слой стен (Collision) — между ними нельзя поворачивать")]
    public LayerMask wallLayerMask;
    [Tooltip("Максимальное расстояние между соседними центрами клеток")]
    public float adjacencyThreshold = 1.1f;

    private List<LabyrinthCell> path = new List<LabyrinthCell>();
    private LabyrinthCell[] allCells;
    private int pathLayer;

    void Awake()
    {
        // if (Instance != null && Instance != this)
        // {
        //     Destroy(gameObject);
        //     return;
        // }
        // Instance = this;

        // собираем все ячейки
        allCells = FindObjectsOfType<LabyrinthCell>();

        pathLayer = LayerMask.NameToLayer(pathLayerName);
        if (pathLayer < 0)
            Debug.LogError($"[LabyrinthManager] Не найден слой '{pathLayerName}'!");

        if (startCell == null)
            Debug.LogError("[LabyrinthManager] Укажите startCell в инспекторе!");
        else
        {
            path.Clear();
            path.Add(startCell);
            startCell.gameObject.layer = pathLayer;
        }
    }

    void Update()
    {
        allCells = allCells
            .Where(cell => cell != null)
            .Distinct()
            .ToArray();
            
        if (Input.GetMouseButton(0))
            ProcessPointer(Input.mousePosition);
        foreach (var t in Input.touches)
            if (t.phase == TouchPhase.Began || t.phase == TouchPhase.Moved)
                ProcessPointer(t.position);
    }

    private void ProcessPointer(Vector2 screenPos)
    {
        Vector2 world = Camera.main.ScreenToWorldPoint(screenPos);
        foreach (var col in Physics2D.OverlapPointAll(world))
        {
            var cell = col.GetComponent<LabyrinthCell>();
            if (cell != null)
            {
                TrySelectCell(cell);
                break;
            }
        }
    }

    private void TrySelectCell(LabyrinthCell cell)
    {
        int idx = path.IndexOf(cell);

        if (idx >= 0)
        {
            if (idx < path.Count - 1)
                RollbackTo(idx);
            return;
        }

        var last = path[path.Count - 1];
        if (!IsAdjacent(last, cell) || IsBlocked(last.transform.position, cell.transform.position))
            return;

        path.Add(cell);
        cell.gameObject.layer = pathLayer;
    }

    private bool IsAdjacent(LabyrinthCell a, LabyrinthCell b)
        => Vector2.Distance(a.transform.position, b.transform.position) <= adjacencyThreshold;

    private bool IsBlocked(Vector2 from, Vector2 to)
        => Physics2D.Linecast(from, to, wallLayerMask);

    private void RollbackTo(int idx)
    {
        for (int i = path.Count - 1; i > idx; i--)
        {
            var c = path[i];
            c.gameObject.layer = c.originalLayer;
            path.RemoveAt(i);
        }
    }

    /// <summary>
    /// Текущий построенный путь, включая startCell в начале
    /// </summary>
    public IReadOnlyList<LabyrinthCell> CurrentPath => path;

    /// <summary>
    /// Финальная клетка, на которой лазер уходит вправо
    /// </summary>
    public LabyrinthCell FinalCell => finalCell;

    /// <summary>
    /// Все ячейки лабиринта покрыты лучом (путь включает каждую из них ровно один раз)
    /// </summary>
    public bool AllCellsVisited => path.Count == allCells.Length;
}
