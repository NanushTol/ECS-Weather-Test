using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

[System.Serializable]
public struct Co2 : IComponentData
{
    public float Value;
}
public class Co2Component : ComponentDataProxy<Co2> { }
