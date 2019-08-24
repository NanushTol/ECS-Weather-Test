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
    Texture2D texture;
    NativeArray<Color> colorLookup; // size 256 
    //Vector4[] colorLookup; // size 256 


    protected override void OnCreate()
    {
        
    }

    protected override void OnStartRunning()
    {
        manager = GameObject.Find("Manager").GetComponent<Manager>();
        cellsCount = manager.CellsCount;
        mapSize = new int2(manager.MapWidth, manager.MapHeight);
        weatherMat = manager.WeatherMat;
        temperatureColors = manager.TemperatureColors;
        display = manager.Display;
        display.transform.localScale = new Vector3(mapSize.x, mapSize.y, 1f);

        colorLookup = new NativeArray<Color>(256, Allocator.Persistent);
        //colorLookup = new Vector4[256];

        for (int i = 0; i < 256; i++)
        {
            float val = i / 256.00f;
            Color c = temperatureColors.Evaluate(val);

            colorLookup[i] = new Color(c.r, c.g, c.b, 1f);
        }
    }

    [BurstCompile]
    public struct RemapValue : IJobForEach<Cell, Temperature>
    {
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float> GradVal;
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, int2> Coordinates;

        public void Execute(ref Cell cell, ref Temperature temp)
        {
            float value;
            value = Remap(temp.Value, -50f, 50f, 0f, 1);
            GradVal[cell.ID] = value;
            Coordinates[cell.ID] = cell.Coordinates;
        }
        float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
    }
    [BurstCompile]
    public struct EvaluateGradient : IJobForEachWithEntity<Cell>
    {
        [ReadOnly]
        public NativeHashMap<int, float> GradVal;
        [NativeDisableParallelForRestriction]
        public NativeArray<Color> Data;
        [ReadOnly]
        public NativeArray<Color> ColorLookup;
        [ReadOnly]
        public float2 MapSize;

        public void Execute(Entity entity, int index, ref Cell cell)
        {
            int val = math.clamp((int)(GradVal[index] * 255), 0, 255);
            Color color = ColorLookup[val];
            Data[index] = color;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        texture = new Texture2D(mapSize.x, mapSize.y, TextureFormat.RGBAFloat, true);
        display.GetComponent<Renderer>().material.mainTexture = texture;

        NativeHashMap<int, float> gradVal = new NativeHashMap<int, float>(mapSize.x * mapSize.y, Allocator.TempJob);
        NativeHashMap<int, int2> coordinates = new NativeHashMap<int, int2>(mapSize.x * mapSize.y, Allocator.TempJob);

        for (int i = 0; i < mapSize.x * mapSize.y; i++)
        {
            gradVal.TryAdd(i, 0f);
            coordinates.TryAdd(i, new int2(0, 0));
        }

        RemapValue generateTextureJob = new RemapValue
        {
            GradVal = gradVal,
            Coordinates = coordinates
        };
        JobHandle generateTextureJobHandle = generateTextureJob.Schedule(this, inputDeps);

        // RGBA32 texture format data layout exactly matches Color32 struct
        var data = texture.GetRawTextureData<Color>();

        EvaluateGradient evaluateGradient = new EvaluateGradient
        {
            GradVal = gradVal,
            Data = data,
            ColorLookup = colorLookup,
            MapSize = mapSize
        };
        JobHandle evaluateGradientHandle = evaluateGradient.Schedule(this, generateTextureJobHandle);

        //for (int i = 0; i < mapSize.x * mapSize.y; i++) 
        //{
        //    int val = math.clamp((int)(gradVal[i] * 255), 0, 255);
        //    Color color = colorLookup[val];
        //    data[i] = color;
        //}

        evaluateGradientHandle.Complete();


        // upload to the GPU
        texture.Apply();

        //data.Dispose();
        gradVal.Dispose();
        coordinates.Dispose();
        return evaluateGradientHandle;
    }

    protected override void OnStopRunning()
    {
        colorLookup.Dispose();
    }
}
