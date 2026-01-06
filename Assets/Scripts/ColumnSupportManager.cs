using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class ColumnSupportManager : MonoBehaviour
{
    [Header("Родитель всех Layout-объектов")]
    public GameObject objectElements;

    void Update()
    {
        // Проверяем и чистим в каждом кадре (работает в Editor и в Play Mode)
        CleanEmptyLayouts();
    }

    /// <summary>
    /// Удаляет из objectElements все дочерние объекты,
    /// имя которых начинается с "Layout" и у которых нет детей.
    /// </summary>
    private void CleanEmptyLayouts()
    {
        if (objectElements == null)
            return;

        var parent = objectElements.transform;
        // Собираем подлежащие удалению
        var toDelete = new List<Transform>();
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith("Layout") && child.childCount == 0)
                toDelete.Add(child);
        }

        // Удаляем — в Editor через DestroyImmediate, в рантайме через Destroy
        foreach (var layout in toDelete)
        {
#if UNITY_EDITOR
            DestroyImmediate(layout.gameObject);
#else
            Destroy(layout.gameObject);
#endif
        }
    }
}
