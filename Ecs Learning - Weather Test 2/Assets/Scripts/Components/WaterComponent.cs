using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

[System.Serializable]
public struct Water : IComponentData
{
    public float Value;
}
public class WaterComponent : ComponentDataProxy<Water> { }
