using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine.UI;
using UnityEngine.Scripting;

//[DisableAutoCreation]
[UpdateAfter(typeof(WindSystem))]
public class GenerateWeatherTextureSystem : JobComponentSystem
{
    Manager manager;
    int cellsCount;
    int2 mapSize;
    Material weatherMat;
    Gradient temperatureColors;
    Gradient co2Colors;
    GameObject temperatureDisplay;
    GameObject co2Display;
    Renderer temperatureDisplayRenderer;
    Renderer co2DisplayRenderer;
    Texture2D temperatureTexture;
    Texture2D co2Texture;

    ParticleSystemForceField particlesFF;
    Texture2D motionVector2dTexture;
    Texture3D vectorFieldTexture;
    int firstIndex;
    NativeArray<Color> temperatureData;
    NativeArray<Color> co2Data;
    NativeArray<Color> motionVectorTextureData;

    NativeArray<Color> temperatureColorLookup; // size 256 
    NativeArray<Color> co2ColorLookup; // size 256 

    float clearTime = 20f;
    float clearElapsed;

    protected override void OnCreate()
    {
        temperatureData = new NativeArray<Color>(mapSize.x * mapSize.y, Allocator.Persistent);
        co2Data = new NativeArray<Color>(mapSize.x * mapSize.y, Allocator.Persistent);
        motionVectorTextureData = new NativeArray<Color>(mapSize.x * mapSize.x, Allocator.Persistent);
    }

    protected override void OnStartRunning()
    {

        manager = GameObject.Find("Manager").GetComponent<Manager>();
        cellsCount = manager.CellsCount;
        mapSize = new int2(manager.MapWidth, manager.MapHeight);
        weatherMat = manager.WeatherMat;

        temperatureColors = manager.TemperatureColors;
        co2Colors = manager.Co2Colors;
        temperatureTexture = new Texture2D(mapSize.x, mapSize.y, TextureFormat.RGBAFloat, true);

        temperatureDisplay = manager.TemperatureDisplay;
        temperatureDisplay.transform.localScale = new Vector3(mapSize.x, mapSize.y, 1f);
        temperatureDisplayRenderer = temperatureDisplay.GetComponent<Renderer>();

        co2Display = manager.Co2Display;
        co2Display.transform.localScale = new Vector3(mapSize.x, mapSize.y, 1f);
        co2DisplayRenderer = co2Display.GetComponent<Renderer>();

        particlesFF = manager.ParticlesFF;
        particlesFF.endRange = mapSize.x / 2;

        motionVector2dTexture = new Texture2D(mapSize.x, mapSize.x, TextureFormat.RGBAFloat, true);
        firstIndex = ((mapSize.x - mapSize.y) / 2) * mapSize.x;

        temperatureColorLookup = new NativeArray<Color>(256, Allocator.Persistent);
        for (int i = 0; i < 256; i++)
        {
            float val = i / 256.00f;
            Color c = temperatureColors.Evaluate(val);

            temperatureColorLookup[i] = new Color(c.r, c.g, c.b, 1f);
        }
        co2ColorLookup = new NativeArray<Color>(256, Allocator.Persistent);
        for (int i = 0; i < 256; i++)
        {
            float val = i / 256.00f;
            Color c = co2Colors.Evaluate(val);

            co2ColorLookup[i] = new Color(c.r, c.g, c.b, 1f);
        }
    }
    float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }


    [ExcludeComponent(typeof(DummyCell))]
    [BurstCompile]
    public struct EvaluateGradient : IJobForEachWithEntity<Cell, Temperature, Co2>
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<Color> TemperatureData;
        [NativeDisableParallelForRestriction]
        public NativeArray<Color> Co2Data;

        [ReadOnly]
        public NativeArray<Color> TemperatureColorLookup;
        [ReadOnly]
        public NativeArray<Color> Co2ColorLookup;
        

        public void Execute(Entity entity, int index, ref Cell cell, ref Temperature temp, ref Co2 co2 )
        {
            float remapedValue = Remap(co2.Value, 0f, 50f, 0f, 1);

            int val = math.clamp((int)(remapedValue * 255), 0, 255);
            Color color = Co2ColorLookup[val];
            Co2Data[index] = color;


            remapedValue = Remap(temp.Value, -50f, 50f, 0f, 1);

            val = math.clamp((int)(remapedValue * 255), 0, 255);
            color = TemperatureColorLookup[val];
            TemperatureData[index] = color;
        }
        float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
    }

    [ExcludeComponent(typeof(DummyCell))]
    [BurstCompile]
    public struct Populate2dMVMap : IJobForEachWithEntity<Cell, WindData>
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<Color> MotionVectorTextureData;
        public int2 MapSize;
        public int FirstIndex;
        
        public void Execute(Entity entity, int index, ref Cell cell, ref WindData wind)
        {
            //var c = cell.ID;
            if (wind.MotionVector.x != 0f || wind.MotionVector.y != 0f)
            {
                //var normalizedMV = math.normalize(wind.MotionVector);
                var normalizedMV = wind.MotionVector;
                normalizedMV.x = Remap(normalizedMV.x, 0, 2, 0, 1);
                normalizedMV.y = Remap(normalizedMV.y, 0, 2, 0, 1);

                int i = cell.Coordinates.x + (cell.Coordinates.y * MapSize.x);

                MotionVectorTextureData[i + FirstIndex] = new Color(normalizedMV.x, 0, normalizedMV.y);
            }
            else
            {
                MotionVectorTextureData[index] = new Color(0.01f, 0.01f, 0.01f);
            }
        }
        float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Object.Destroy(temperatureTexture);
        Object.Destroy(motionVector2dTexture);
        Object.Destroy(co2Texture);
        Object.Destroy(vectorFieldTexture);

        temperatureTexture = new Texture2D(mapSize.x, mapSize.y, TextureFormat.RGBAFloat, true);
        co2Texture = new Texture2D(mapSize.x, mapSize.y, TextureFormat.RGBAFloat, true);
        vectorFieldTexture = new Texture3D(mapSize.x, 1, mapSize.x, TextureFormat.RGBAFloat, true);
        motionVector2dTexture = new Texture2D(mapSize.x, mapSize.x, TextureFormat.RGBAFloat, true);

        //Create Temperature Color from Data
        temperatureData = temperatureTexture.GetRawTextureData<Color>();
        co2Data = co2Texture.GetRawTextureData<Color>();
        EvaluateGradient evaluateGradient = new EvaluateGradient
        {
            Co2Data = co2Data,
            Co2ColorLookup = co2ColorLookup,
            TemperatureData = temperatureData,
            TemperatureColorLookup = temperatureColorLookup,
        };
        JobHandle evaluateGradientHandle = evaluateGradient.Schedule(this, inputDeps);

        // Create MotionVector map from data
        motionVectorTextureData = motionVector2dTexture.GetRawTextureData<Color>();
        Populate2dMVMap populate2dMVMap = new Populate2dMVMap
        {
            FirstIndex = firstIndex,
            MapSize = mapSize,
            MotionVectorTextureData = motionVectorTextureData
        };
        JobHandle populate2dMVMapHandle = populate2dMVMap.Schedule(this, evaluateGradientHandle);

        populate2dMVMapHandle.Complete();
        evaluateGradientHandle.Complete();

        //Apply Color Textures
        temperatureDisplayRenderer.material.mainTexture = temperatureTexture;
        temperatureTexture.Apply(false, true);
        co2DisplayRenderer.material.mainTexture = co2Texture;
        co2Texture.Apply(false, true);

        //Apply Motion Vector Texture
        particlesFF.vectorField = vectorFieldTexture;
        vectorFieldTexture.SetPixels(motionVectorTextureData.ToArray());
        vectorFieldTexture.Apply(false, true);


        //clearElapsed += Time.deltaTime;
        //if (clearElapsed >= clearTime)
        //{
        //    Resources.UnloadUnusedAssets();
        //    clearElapsed = 0f;
        //}

        return populate2dMVMapHandle;
    }

    protected override void OnStopRunning()
    {
        temperatureColorLookup.Dispose();
    }
}
