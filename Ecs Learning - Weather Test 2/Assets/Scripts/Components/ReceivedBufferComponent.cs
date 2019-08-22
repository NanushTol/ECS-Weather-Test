using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

[System.Serializable]
[InternalBufferCapacity(8)]
public struct Received : IBufferElementData
{
    public float WaterReceived;
    public float Co2Received;
    public float OxyReceived;
    public float TemperatureReceived;

    public float2 MVReceived;
}
public class ReceivedBufferComponent : DynamicBufferProxy<Received> { }
