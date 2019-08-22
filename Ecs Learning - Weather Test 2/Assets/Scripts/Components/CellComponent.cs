﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

[System.Serializable]
public struct Cell : IComponentData
{
    public int ID;
    public int2 Coordinates;

    public int UpCellId;
    public int DownCellId;
    public int LeftCellId;
    public int RightCellId;

    public Entity Up;
    public Entity Down;
    public Entity Left;
    public Entity Right;
}
public class CellComponent : ComponentDataProxy<Cell> { }
