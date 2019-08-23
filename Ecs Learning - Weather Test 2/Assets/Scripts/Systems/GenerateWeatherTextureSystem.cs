using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine.UI;

//[DisableAutoCreation]
[UpdateAfter(typeof(WindSystem))]
public class GenerateWeatherTextureSystem : JobComponentSystem
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

    public struct GenerateTextureJob : IJobForEach<Cell, Temperature>
    {
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float> GradVal;
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, int2> Coordinates;

        public void Execute(ref Cell cell, ref Temperature temp)
        {
            float value;
            value = Remap(temp.Value, -50f, 50f, 0f, 1f);
            GradVal[cell.ID] = value;
            Coordinates[cell.ID] = cell.Coordinates;
        }
        float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var texture = new Texture2D(mapSize.x, mapSize.y, TextureFormat.ARGB32, true);
        NativeHashMap<int, float> gradVal = new NativeHashMap<int, float>(mapSize.x * mapSize.y, Allocator.TempJob);
        NativeHashMap<int, int2> coordinates = new NativeHashMap<int, int2>(mapSize.x * mapSize.y, Allocator.TempJob);

        for (int i = 0; i < mapSize.x * mapSize.y; i++)
        {
            gradVal.TryAdd(i, 0f);
            coordinates.TryAdd(i, new int2(0, 0));
        }

        GenerateTextureJob generateTextureJob = new GenerateTextureJob
        {
            GradVal = gradVal,
            Coordinates = coordinates
        };
        JobHandle generateTextureJobHandle = generateTextureJob.Schedule(this, inputDeps);

        generateTextureJobHandle.Complete();

        for (int i = 0; i < mapSize.x * mapSize.y; i++)
        {
            Color color = temperatureColors.Evaluate(gradVal[i]);
            texture.SetPixel(coordinates[i].x, coordinates[i].y, color);
        }

        //Apply all SetPixel calls
        texture.Apply();

        // connect texture to material of GameObject this script is attached to
        weatherMat.mainTexture = texture;

        gradVal.Dispose();
        coordinates.Dispose();
        return generateTextureJobHandle;
    }
}
