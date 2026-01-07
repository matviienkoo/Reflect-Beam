using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[ExecuteInEditMode]
public class Block : MonoBehaviour
{
    public enum BlockColor { Pink, Blue, Red, Yellow, Green }

    [Header("Цвет этого блока")]
    public BlockColor color;

    [Header("Спрайты для цветов (должно быть столько же, сколько значений в enum)")]
    public Sprite[] colorSprites;

    [Header("Настройка поиска соседей")]
    [Tooltip("Множитель от размера спрайта (1.0 = точно по размеру), по оси X/Y")]
    public float neighborDistance = 1.05f;

    [Tooltip("Слой, на котором лежат все блоки")] 
    public LayerMask blockLayer;

    // Дополнительный порог «покоя»
    const float velocityThreshold = 0.02f;
    const float angularThreshold = 0.5f;

    private SpriteRenderer sr;
    private Rigidbody2D rb;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        ApplyColorSprite();
    }

    void OnValidate()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        ApplyColorSprite();
    }

    private void ApplyColorSprite()
    {
        int idx = (int)color;
        if (colorSprites != null && idx >= 0 && idx < colorSprites.Length)
            sr.sprite = colorSprites[idx];
        else
            Debug.LogWarning($"[Block] Для цвета {color} не назначен спрайт в {name}!", this);
    }

    // Вычисляем дистанцию между центрами блоков в мировых единицах
    private float WorldNeighborDistance
    {
        get
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();

            // берём ширину спрайта в мировых единицах (можно заменить на Mathf.Max по X/Y)
            float spriteWidth = sr.bounds.size.x;
            return spriteWidth * neighborDistance;
        }
    }

    void OnMouseDown()
    {
        var cluster = GetConnectedCluster();
        if (cluster.Count < 2) return;

        if (IsClusterIdle(cluster))
            DestroyCluster(cluster);
        else
            Debug.Log("[Block] Кластер ещё движется, подождите приземления.", this);
    }

    private bool IsClusterIdle(List<Block> cluster)
    {
        foreach (var b in cluster)
        {
            if (!b.rb.IsSleeping() &&
                (b.rb.linearVelocity.sqrMagnitude > velocityThreshold * velocityThreshold ||
                 Mathf.Abs(b.rb.angularVelocity) > angularThreshold))
            {
                return false;
            }
        }
        return true;
    }

    private void DestroyCluster(List<Block> cluster)
    {
        foreach (var b in cluster)
            DestroyImmediate(b.gameObject);
    }

    private List<Block> GetConnectedCluster()
    {
        var result = new List<Block>();
        var queue = new Queue<Block>();
        var visited = new HashSet<Block>();

        queue.Enqueue(this);
        visited.Add(this);

        float nd = WorldNeighborDistance;
        float overlapRadius = nd * 0.4f;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            foreach (var d in new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right })
            {
                Vector2 origin = (Vector2)current.transform.position + d * nd;
                Collider2D hit = Physics2D.OverlapCircle(origin, overlapRadius, blockLayer);
                if (hit == null) continue;

                var neighbor = hit.GetComponent<Block>();
                if (neighbor != null
                    && neighbor.color == this.color
                    && !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return result;
    }

    void OnDrawGizmosSelected()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        float nd = (sr != null) ? (sr.bounds.size.x * neighborDistance) : neighborDistance;

        Gizmos.color = Color.yellow;
        Vector3 pos = transform.position;
        foreach (var d in new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right })
            Gizmos.DrawWireSphere(pos + d * nd, nd * 0.4f);
    }
}
