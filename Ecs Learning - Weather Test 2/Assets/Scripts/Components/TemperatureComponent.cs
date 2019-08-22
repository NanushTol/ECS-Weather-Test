using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

[System.Serializable]
public struct Temperature : IComponentData
{
    public float Value;
}
public class TemperatureComponent : ComponentDataProxy<Temperature> { }
