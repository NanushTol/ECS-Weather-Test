using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;

public class Manager : MonoBehaviour
{
    [Header("Map Settings")]
    public int MapWidth;
    public int MapHeight;
    public Material WeatherMat;
    public Gradient TemperatureColors;
    public Gradient Co2Colors;
    public GameObject TemperatureDisplay;
    public GameObject Co2Display;
    public GameObject Floor;
    public ParticleSystem Particles;
    public ParticleSystemForceField ParticlesFF;


    [Header("Wind Settings")]
    public float Drag;
    public float HeatTransfer;
    public bool DebugWind;

    //public NativeHashMap<int, Entity> TestEntities;
    [HideInInspector]
    public int CellsCount;



    void Start()
    {
        Floor.transform.localScale = new Vector3(MapWidth / 10, 1, MapHeight / 10);
        var shape = Particles.shape;
        shape.scale = new Vector3(MapWidth, 0, MapHeight);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))   
        {
            Application.Quit();
        }
    }

}
