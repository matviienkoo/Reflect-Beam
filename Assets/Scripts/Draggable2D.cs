using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
public class Draggable2D : MonoBehaviour
{
    public enum MovementMode { Horizontal, Vertical, Both }
    [Tooltip("Horizontal — по X, Vertical — по Y, Both — выбрать ось по первому движению")]
    public MovementMode movementMode = MovementMode.Horizontal;

    private BoxCollider2D boxCollider;
    private Rigidbody2D rb;

    private bool dragging = false;
    private int activeFingerId = -1;
    private Vector3 offset;
    private Vector2 dragStartWorld;          // точка старта драга в мире
    private bool axisLocked = false;         // для режима Both: ось уже выбрана?
    private MovementMode lockedAxis;         // заблокированная ось в режиме Both

    private ContactFilter2D contactFilter;
    private RaycastHit2D[] hitBuffer = new RaycastHit2D[1];
    private const float skinWidth = 0.01f;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;

        contactFilter = new ContactFilter2D {
            useTriggers = false,
            useLayerMask = false
        };
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))      TryBeginDrag(Input.mousePosition, -1);
        if (dragging && Input.GetMouseButton(0)) MoveTo(Input.mousePosition);
        if (Input.GetMouseButtonUp(0))        EndDrag();
#else
        foreach (Touch t in Input.touches)
        {
            if (t.phase == TouchPhase.Began)
                TryBeginDrag(t.position, t.fingerId);
            else if (dragging && t.fingerId == activeFingerId &&
                    (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary))
                MoveTo(t.position);
            else if (dragging && t.fingerId == activeFingerId &&
                    (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled))
                EndDrag();
        }
#endif
    }

    private void TryBeginDrag(Vector2 screenPos, int fingerId)
    {
        Vector3 wp = Camera.main.ScreenToWorldPoint(screenPos);
        Collider2D hit = Physics2D.OverlapPoint(wp);
        if (hit == boxCollider)
        {
            dragging       = true;
            activeFingerId = fingerId;
            offset         = transform.position - wp;
            dragStartWorld = wp;
            axisLocked     = false;
        }
    }

    private void MoveTo(Vector2 screenPos)
    {
        Vector3 wp3 = Camera.main.ScreenToWorldPoint(screenPos) + offset;
        Vector2 current = rb.position;
        Vector2 target  = current;

        MovementMode modeToUse = movementMode;
        if (movementMode == MovementMode.Both)
        {
            // на первом движении определяем ось
            if (!axisLocked)
            {
                Vector2 delta = (wp3 - (Vector3)dragStartWorld);
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    lockedAxis = MovementMode.Horizontal;
                else
                    lockedAxis = MovementMode.Vertical;
                axisLocked = true;
            }
            modeToUse = lockedAxis;
        }

        if (modeToUse == MovementMode.Horizontal)
            target = new Vector2(wp3.x, current.y);
        else if (modeToUse == MovementMode.Vertical)
            target = new Vector2(current.x, wp3.y);

        DragAlongAxis(current, target, modeToUse);
    }

    private void DragAlongAxis(Vector2 from, Vector2 to, MovementMode mode)
    {
        Vector2 axis = (mode == MovementMode.Horizontal) ? Vector2.right : Vector2.up;
        Vector2 delta = to - from;
        float dist = Vector2.Dot(delta, axis);
        if (Mathf.Abs(dist) < Mathf.Epsilon) return;

        Vector2 dir = (dist > 0 ? axis : -axis);
        float castDist = Mathf.Abs(dist) + skinWidth;
        int hits = boxCollider.Cast(dir, contactFilter, hitBuffer, castDist);

        float travel = Mathf.Abs(dist);
        if (hits > 0)
            travel = Mathf.Max(hitBuffer[0].distance - skinWidth, 0f);

        rb.position = from + dir * travel;
    }

    private void EndDrag()
    {
        dragging       = false;
        activeFingerId = -1;
    }
}
