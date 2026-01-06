using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider2D))]
public class RotateOnClick : MonoBehaviour, IPointerClickHandler
{
    private void Awake()
    {
        // Чтобы IPointerClickHandler работал на 2D-спрайте, 
        // на камере должен быть Physics2DRaycaster и в сцене — EventSystem.
        var cam = Camera.main;
        if (cam != null && cam.GetComponent<Physics2DRaycaster>() == null)
            cam.gameObject.AddComponent<Physics2DRaycaster>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Проигрываем звук, если есть
        //MechanicScript.Instance?.AudioTap();

        // Поворачиваем этот объект на -90° вокруг оси Z
        transform.Rotate(0f, 0f, -90f);
    }
}
