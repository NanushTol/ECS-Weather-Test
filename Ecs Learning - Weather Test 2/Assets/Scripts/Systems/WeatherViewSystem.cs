using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine.Experimental.VFX;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;

[DisableAutoCreation]
public class WeatherViewSystem : ComponentSystem
{
    Manager manager;

    protected override void OnStartRunning()
    {
        manager = GameObject.Find("Manager").GetComponent<Manager>();
    }

    protected override void OnUpdate()
    {

        Entities.ForEach((Entity entity, ref Cell cell, ref Temperature temperature) =>
        {

        });
    }
}
