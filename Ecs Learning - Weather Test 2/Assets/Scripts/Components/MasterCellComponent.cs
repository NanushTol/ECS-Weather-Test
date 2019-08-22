using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

[System.Serializable]
public struct MasterCell : IComponentData
{
   
}
public class MasterCellComponent : ComponentDataProxy<MasterCell> { }
