using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

[System.Serializable]
public struct AdjacentCells : IComponentData
{
    public Entity Up;
    public Entity Down;
    public Entity Left;
    public Entity Right;
}
public class AdjacentCellsComponent : ComponentDataProxy<AdjacentCells> { }
