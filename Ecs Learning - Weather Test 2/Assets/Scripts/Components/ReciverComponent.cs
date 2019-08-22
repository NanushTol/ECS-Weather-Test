using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

[System.Serializable]
public struct Reciver : IComponentData
{
   
}
public class ReciverComponent : ComponentDataProxy<Reciver> { }
