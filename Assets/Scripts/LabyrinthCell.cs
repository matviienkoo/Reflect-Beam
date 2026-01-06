using System.Collections.Generic;
using UnityEngine;

public class LabyrinthCell : MonoBehaviour
{
    [HideInInspector] public int originalLayer;
    void Awake() => originalLayer = gameObject.layer;
}
