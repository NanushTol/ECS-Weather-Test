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
    public GameObject Display;
    public GameObject Floor;


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
    }

    void Update()
    {
        
    }

}
