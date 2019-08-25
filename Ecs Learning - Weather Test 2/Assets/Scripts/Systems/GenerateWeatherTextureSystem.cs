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
    GameObject display;
    Texture2D texture;

    ParticleSystemForceField particlesFF;
    Texture2D motionVector2dTexture;
    Texture3D vectorFieldTexture;

    NativeArray<Color> colorLookup; // size 256 

    float clearTime = 10f;
    float clearElapsed;

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
        texture = new Texture2D(mapSize.x, mapSize.y, TextureFormat.RGBAFloat, true);

        display = manager.Display;
        display.transform.localScale = new Vector3(mapSize.x, mapSize.y, 1f);
        display.GetComponent<Renderer>().material.mainTexture = texture;

        particlesFF = manager.ParticlesFF;
        particlesFF.endRange = mapSize.x / 2;

        motionVector2dTexture = new Texture2D(mapSize.x, mapSize.y, TextureFormat.RGBAFloat, true);


        colorLookup = new NativeArray<Color>(256, Allocator.Persistent);
        for (int i = 0; i < 256; i++)
        {
            float val = i / 256.00f;
            Color c = temperatureColors.Evaluate(val);

            colorLookup[i] = new Color(c.r, c.g, c.b, 1f);
        }
    }
    float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }


    [ExcludeComponent(typeof(DummyCell))]
    [BurstCompile]
    public struct EvaluateGradient : IJobForEachWithEntity<Cell, Temperature>
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<Color> Data;
        [ReadOnly]
        public NativeArray<Color> ColorLookup;

        public void Execute(Entity entity, int index, ref Cell cell, ref Temperature temp)
        {
            float remapedValue = Remap(temp.Value, -50f, 50f, 0f, 1);

            int val = math.clamp((int)(remapedValue * 255), 0, 255);
            Color color = ColorLookup[val];
            Data[index] = color;
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
        
        public void Execute(Entity entity, int index, ref Cell cell, ref WindData wind)
        {
            //var c = cell.ID;
            if (wind.MotionVector.x != 0f || wind.MotionVector.y != 0f)
            {
                var normalizedMV = math.normalize(wind.MotionVector);
                
                MotionVectorTextureData[index] = new Color(normalizedMV.x, 0, normalizedMV.y);
            }
            else
            {
                MotionVectorTextureData[index] = new Color(0, 0, 0);
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        vectorFieldTexture = new Texture3D(mapSize.x, 1, mapSize.y, TextureFormat.RGBAFloat, true);

        //Create Temperature Color from Data
        var data = texture.GetRawTextureData<Color>();
        EvaluateGradient evaluateGradient = new EvaluateGradient
        {
            Data = data,
            ColorLookup = colorLookup,
        };
        JobHandle evaluateGradientHandle = evaluateGradient.Schedule(this, inputDeps);

        // Create MotionVector map from data
        var motionVectorTextureData = motionVector2dTexture.GetRawTextureData<Color>();
        Populate2dMVMap populate2dMVMap = new Populate2dMVMap
        {
            MotionVectorTextureData = motionVectorTextureData
        };
        JobHandle populate2dMVMapHandle = populate2dMVMap.ScheduleSingle(this, evaluateGradientHandle);

        populate2dMVMapHandle.Complete();
        evaluateGradientHandle.Complete();

        //Apply Color Textures
        texture.Apply();

        //Apply Motion Vector Texture
        particlesFF.vectorField = vectorFieldTexture;
        vectorFieldTexture.SetPixels(motionVectorTextureData.ToArray());
        vectorFieldTexture.Apply();


        clearElapsed += Time.deltaTime;
        if (clearElapsed >= clearTime)
        {
            Resources.UnloadUnusedAssets();
            clearElapsed = 0f;
        }

        return populate2dMVMapHandle;
    }

    protected override void OnStopRunning()
    {
        colorLookup.Dispose();
    }
}
