using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class SetAnchorsByImageCorners : MonoBehaviour
{
    [MenuItem ("CONTEXT/RectTransform/Set Anchor by corners")]
    static void SetAnchor (MenuCommand command) 
    {
        // Получаем RectTransform текущего объекта и его родителя
        var rt = command.context as RectTransform;
        if (rt == null) return;
        
        var rtParent = rt.parent as RectTransform;
        if (rtParent == null) return;

        // Получаем углы текущего объекта и его родителя в мировых координатах
        Vector3[] rtCorners = new Vector3[4];
        Vector3[] rtParentCorners = new Vector3[4];
        rt.GetWorldCorners(rtCorners);
        rtParent.GetWorldCorners(rtParentCorners);

        // Вычисляем позиции углов (левый нижний и правый верхний) текущего RectTransform относительно родителя
        Vector3 rtP1 = rtCorners[0];  // Левый нижний угол
        Vector3 rtP2 = rtCorners[2];  // Правый верхний угол
        Vector3 rtParentP1 = rtParentCorners[0];  // Левый нижний угол родителя
        Vector3 rtParentP2 = rtParentCorners[2];  // Правый верхний угол родителя

        // Приводим углы объекта к локальным координатам родителя
        rtP1 -= rtParentP1;
        rtP2 -= rtParentP1;
        rtParentP2 -= rtParentP1;

        // Вычисляем новые значения anchorMin и anchorMax как отношение позиции объекта к размеру родителя
        Vector2 anchorMin = new Vector2(rtP1.x / rtParentP2.x, rtP1.y / rtParentP2.y);
        Vector2 anchorMax = new Vector2(rtP2.x / rtParentP2.x, rtP2.y / rtParentP2.y);

        // Устанавливаем anchorMin и anchorMax для текущего RectTransform
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;

        // Сбрасываем sizeDelta и anchoredPosition, чтобы зафиксировать объект по новым анкорам
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero; 
        rt.localScale = new Vector2(1f, 1f);
    }
}