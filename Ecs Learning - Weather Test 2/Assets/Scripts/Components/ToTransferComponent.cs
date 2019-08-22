using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

[System.Serializable]
public struct ToTransfer : IComponentData
{
    public int TransferXCellId;
    public int TransferYCellId;
    public float2 TransferMotionVectorX;
    public float2 TransferMotionVectorY;
}
public class ToTransferComponent : ComponentDataProxy<ToTransfer> { }
