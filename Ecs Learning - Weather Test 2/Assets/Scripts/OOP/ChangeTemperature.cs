using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;


public class ChangeTemperature : ComponentSystem
{
    public Vector3 MouseClickPosition;
    public bool AddTemp;
    public bool RemoveTemp;
    public GameObject Heater;
    public GameObject Coller;
    EntityArchetype tempChanger;

    protected override void OnStartRunning()
    {
        tempChanger = EntityManager.CreateArchetype(typeof(Heater), typeof(Translation));
    }

    protected override void OnUpdate()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
        {
            RaycastHit hit;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                var heater = EntityManager.CreateEntity(tempChanger);
                EntityManager.SetComponentData(heater, new Translation { Value = hit.point });
                EntityManager.SetComponentData(heater, new Heater { TempValue = 2f });
            }
        }
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButton(1))
        {
            RaycastHit hit;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                var coller = EntityManager.CreateEntity(tempChanger);
                EntityManager.SetComponentData(coller, new Translation { Value = hit.point });
                EntityManager.SetComponentData(coller, new Heater { TempValue = -2f });
            }
        }
    }
}
