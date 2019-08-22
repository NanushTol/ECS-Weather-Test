using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

[System.Serializable]
public struct WindData : IComponentData
{
    public float TotalTempDifference;

    public float4 TempDifference;

    public float2 DirectionAmplitude;

    public float2 DirectionPrecentage;

    public float2 MotionVector;
    public float2 RecivedMotionVector;
}
public class WindDataComponent : ComponentDataProxy<WindData> { }
