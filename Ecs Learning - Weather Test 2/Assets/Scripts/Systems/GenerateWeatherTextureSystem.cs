using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine.UI;

//[DisableAutoCreation]
[UpdateAfter(typeof(WindSystem))]
public class GenerateWeatherTextureSystem : ComponentSystem
{
    Manager manager;
    int cellsCount;
    int2 mapSize;
    Material weatherMat;
    Gradient temperatureColors;
    GameObject display;

    protected override void OnStartRunning()
    {
        manager = GameObject.Find("Manager").GetComponent<Manager>();
        cellsCount = manager.CellsCount;
        mapSize = new int2(manager.MapWidth, manager.MapHeight);
        weatherMat = manager.WeatherMat;
        temperatureColors = manager.TemperatureColors;
        display = manager.Display;
        display.transform.localScale = new Vector3(mapSize.x, mapSize.y, 1f);
    }

    protected override void OnUpdate()
    {
        // Create a new 2x2 texture ARGB32 (32 bit with alpha) and no mipmaps
        var texture = new Texture2D(mapSize.x, mapSize.y, TextureFormat.ARGB32, true);
        float value;
        Entities.ForEach((Entity entity, ref Cell cell, ref Temperature temperature) =>
        {
            value = Remap(temperature.Value, 0f, 50f, 0f, 1f);
            Color color = temperatureColors.Evaluate(value);
            texture.SetPixel(cell.Coordinates.x, cell.Coordinates.y, color);
        });

        // Apply all SetPixel calls
        texture.Apply();

        // connect texture to material of GameObject this script is attached to
        weatherMat.mainTexture = texture;
    }

    float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}
