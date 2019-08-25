using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;


//[DisableAutoCreation]
public class WindSystem : JobComponentSystem
{
    Manager manager;
    int cellsCount;
    float drag;
    float heatTransferRatio;
    bool debug;
    BufferFromEntity<Received> receivedBufferComponent;
    NativeHashMap<int, Entity> cellEntities;
    BeginInitializationEntityCommandBufferSystem entityCommandBufferSystem;


    [BurstCompile]
    public struct CalculateHeatDifferanceJob : IJobForEachWithEntity<Cell, WindData>
    {
        [ReadOnly]
        public ComponentDataFromEntity<Temperature> TempFromEntity;

        public void Execute(Entity entity, int index, ref Cell cell, ref WindData windData)
        {
            // Calculate Heat Differance
            windData.TotalTempDifference = 0f;

            Temperature upTemp      = TempFromEntity[cell.Up];
            Temperature rightTemp   = TempFromEntity[cell.Right];
            Temperature downTemp    = TempFromEntity[cell.Down];
            Temperature leftTemp    = TempFromEntity[cell.Left];
            Temperature temp        = TempFromEntity[entity];

            windData.TempDifference[0] = temp.Value - upTemp.Value;
            windData.TempDifference[1] = temp.Value - rightTemp.Value;
            windData.TempDifference[2] = temp.Value - downTemp.Value;
            windData.TempDifference[3] = temp.Value - leftTemp.Value;

            for (int i = 0; i < 4; i++)
            {
                windData.TotalTempDifference += math.abs(windData.TempDifference[i]);
            }
        }

    }
    [BurstCompile]
    public struct CalculateDirectionPrecentageJob : IJobForEach<WindData, Cell>
    {
        public void Execute(ref WindData windData, [ReadOnly] ref Cell cell)
        {
            
            // Calculate Direction Amplitude
            windData.DirectionAmplitude[0] = windData.TempDifference[0] - windData.TempDifference[2]; // Up Down Direction
            windData.DirectionAmplitude[1] = windData.TempDifference[1] - windData.TempDifference[3]; // Left Right Direction


            if (cell.DownCellId == -1 &&  windData.DirectionAmplitude[0] > 0) // if Bottom adjacent cell is dummy & amlitude go up
            {
                windData.DirectionAmplitude[0] = 0;
            }
            else if (cell.UpCellId == -1 && windData.DirectionAmplitude[0] < 0) // if Top adjacent cell is dummy & amlitude go down
            {
                windData.DirectionAmplitude[0] = 0;
            }

            // Calculate direction Precantage
            windData.DirectionPrecentage[0] = windData.DirectionAmplitude[0] / windData.TotalTempDifference;
            windData.DirectionPrecentage[1] = windData.DirectionAmplitude[1] / windData.TotalTempDifference;

            for (int i = 0; i < 2; i++)
            {
                if (windData.DirectionPrecentage[i] != windData.DirectionPrecentage[i]) windData.DirectionPrecentage[i] = 0; // if precentage = NaN
            }
        }
    }
    [BurstCompile]
    public struct CalculateMotionVectorJob : IJobForEach<Cell, WindData>
    {
        public void Execute(ref Cell cell, ref WindData windData)
        {
            
            float differenceAvrage = windData.TotalTempDifference / 4;

            float2 newMotionVector = new float2(windData.DirectionPrecentage[1] * differenceAvrage, windData.DirectionPrecentage[0] * differenceAvrage);

            windData.MotionVector = newMotionVector + windData.RecivedMotionVector;

            windData.RecivedMotionVector = new float2(0f, 0f);
        }
    }
    [BurstCompile]
    public struct PopulateHash : IJobForEach<Cell, WindData, Water, Co2, Oxygen, Temperature>
    {
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float> WaterContent;
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float> Co2Content;
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float> OxyContent;
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float> TempContent;

        public void Execute([ReadOnly]ref Cell cell, [ReadOnly]ref WindData wind, [ReadOnly]ref Water water, 
                            [ReadOnly]ref Co2 co2, [ReadOnly]ref Oxygen oxy, [ReadOnly]ref Temperature temp)
        {
            WaterContent.TryAdd(cell.ID, water.Value);
            Co2Content.TryAdd(cell.ID, co2.Value);
            OxyContent.TryAdd(cell.ID, oxy.Value);
            TempContent.TryAdd(cell.ID, temp.Value);
        }
    }
    [BurstCompile]
    public struct ReciveFromObjects : IJobForEachWithEntity<Heater, Translation>
    {
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<Received> ReceivedBufferComponentLookup;
        [ReadOnly]
        public NativeHashMap<int, Entity> CellEntities;
        [ReadOnly]
        public int2 MapSize;

        public EntityCommandBuffer.Concurrent CommandBuffer;

        public void Execute(Entity entity, int index, ref Heater heater, ref Translation translation)
        {
            int2 position = new int2((int)translation.Value.x, (int)translation.Value.z);
            int2 mapCenter = new int2(MapSize.x / 2, MapSize.y / 2);

            int cellId = MapSize.x * (position.y + mapCenter.y) + (position.x + mapCenter.x);
            Entity cellEntity = CellEntities[cellId];

            var buffer = ReceivedBufferComponentLookup[cellEntity];
            Received receivedElement = new Received();

            receivedElement.TemperatureReceived = heater.TempValue;

            buffer.Add(receivedElement);

            CommandBuffer.DestroyEntity(index, entity);
        }
    }
    [BurstCompile]
    public struct TransferCellContent : IJobForEach<Cell, WindData, Water, Co2, Oxygen, Temperature>
    {
        public float Dt;

        [NativeDisableParallelForRestriction]
        public BufferFromEntity<Received> ReceivedBufferComponentLookup;

        [ReadOnly]
        public NativeHashMap<int, float> WaterContent;
        [ReadOnly]
        public NativeHashMap<int, float> Co2Content;
        [ReadOnly]
        public NativeHashMap<int, float> OxyContent;
        [ReadOnly]
        public NativeHashMap<int, float> TempContent;
        [ReadOnly]
        public float HeatTransferRatio;

        public void Execute(ref Cell cell, ref WindData windData, ref Water water, ref Co2 co2, ref Oxygen oxy, ref Temperature temp)
        {
            // check give or recive by sign of the bigger: - recive, + give
            if (math.abs(windData.TempDifference[0]) > math.abs(windData.TempDifference[2])) // Interact with top cell
            {
                if (windData.TempDifference[0] > 0) // give
                {
                    TransferContent(cell.ID, cell.UpCellId, -math.abs(windData.MotionVector.y), cell.Up, water, co2, oxy, ref temp);
                }
                else if (windData.TempDifference[0] < 0) // recive
                {
                    TransferContent(cell.ID, cell.UpCellId, math.abs(windData.MotionVector.y), cell.Up, water, co2, oxy, ref temp);
                }
            }
            else if (math.abs(windData.TempDifference[0]) < math.abs(windData.TempDifference[2])) // interact with bottom cell
            {
                if (windData.TempDifference[2] > 0) // give
                {
                    TransferContent(cell.ID, cell.DownCellId, -math.abs(windData.MotionVector.y), cell.Down, water, co2, oxy, ref temp);
                }
                else if (windData.TempDifference[2] < 0) // recive
                {
                    TransferContent(cell.ID, cell.DownCellId, math.abs(windData.MotionVector.y), cell.Down, water, co2, oxy, ref temp);
                }
            }

            if (math.abs(windData.TempDifference[1]) > math.abs(windData.TempDifference[3])) // Interact with Right cell
            {
                if (windData.TempDifference[1] > 0) // give
                {
                    TransferContent(cell.ID, cell.RightCellId, -math.abs(windData.MotionVector.x), cell.Right, water, co2, oxy, ref temp);
                }
                else if (windData.TempDifference[1] < 0) // recive
                {
                    TransferContent(cell.ID, cell.RightCellId, math.abs(windData.MotionVector.x), cell.Right, water, co2, oxy, ref temp);
                }
            }
            else if (math.abs(windData.TempDifference[1]) < math.abs(windData.TempDifference[3])) // interact with Left cell
            {
                if (windData.TempDifference[3] > 0) // give
                {
                    TransferContent(cell.ID, cell.LeftCellId, -math.abs(windData.MotionVector.x), cell.Left, water, co2, oxy, ref temp);
                }
                else if (windData.TempDifference[3] < 0) // recive
                {
                    TransferContent(cell.ID, cell.LeftCellId, math.abs(windData.MotionVector.x), cell.Left, water, co2, oxy, ref temp);
                }
            }
        }
        void TransferContent(int cellId, int adjacentId, float motionAxisValue, Entity adjacentCell, 
                            Water water, Co2 co2, Oxygen oxy, ref Temperature temp)
        {
            var buffer = ReceivedBufferComponentLookup[adjacentCell];
            Received receivedElement = new Received();

            if (adjacentId != -1)
            {
                float transfer;
                float cont;
                if (TempContent[adjacentId] == 0)   
                {
                    cont = 0.001f;
                }
                else
                {
                    cont = TempContent[adjacentId];
                }

                transfer = motionAxisValue / cont * HeatTransferRatio;

                // Transfer To Self0
                float tmp;
                tmp = temp.Value + transfer;
                temp.Value += transfer;

                //Transfer To Adjacent Cell
                receivedElement.cellID = cellId;
                receivedElement.TemperatureReceived = -transfer;
                buffer.Add(receivedElement);
            }
        }
    }
    [BurstCompile]
    public struct TransferMotionVectorJob : IJobForEach<Cell, WindData>
    {
        public float Dt;
        public float Drag;

        [NativeDisableParallelForRestriction]
        public BufferFromEntity<Received> ReceivedBufferComponentLookup;

        public void Execute(ref Cell cell, ref WindData windData)
        {
            float ratio;
            float2 cellTransferRatio;

            float2 passedMV = math.abs(windData.MotionVector) - math.abs(windData.MotionVector) * Drag;
            // Calculate Transfer Ratios between X & Y
            if (math.abs(windData.MotionVector.x) > math.abs(windData.MotionVector.y) && windData.MotionVector.y != 0f)
            {
                ratio = math.abs(windData.MotionVector.y / windData.MotionVector.x);
                cellTransferRatio = new float2((0.5f * ratio) + (1f - ratio), (0.5f * ratio));
            }
            else if (math.abs(windData.MotionVector.x) < math.abs(windData.MotionVector.y) && windData.MotionVector.x != 0f)
            {
                ratio = math.abs(windData.MotionVector.x / windData.MotionVector.y);
                cellTransferRatio = new float2((0.5f * ratio), (0.5f * ratio) + (1f - ratio));
            }
            else cellTransferRatio = new float2(0.5f, 0.5f);

            float2 tmp = 0;
            if (windData.MotionVector.x > 0) // Right Movement
            {
                var buffer = ReceivedBufferComponentLookup[cell.Right];
                Received receivedElement = new Received();

                receivedElement.MVReceived = passedMV * cellTransferRatio.x;
                buffer.Add(receivedElement);
            }
            else if (windData.MotionVector.x < 0) // left Movement
            {
                var buffer = ReceivedBufferComponentLookup[cell.Left];
                Received receivedElement = new Received();

                receivedElement.MVReceived = passedMV * cellTransferRatio.x;
                buffer.Add(receivedElement);
            }

            if (windData.MotionVector.y > 0 && cell.UpCellId != -1) // Up Movement
            {
                var buffer = ReceivedBufferComponentLookup[cell.Up];
                Received receivedElement = new Received();

                receivedElement.MVReceived = passedMV * cellTransferRatio.y;
                buffer.Add(receivedElement);
            }
            else if (windData.MotionVector.y < 0 && cell.DownCellId != -1) // Down Movement
            {
                var buffer = ReceivedBufferComponentLookup[cell.Down];
                Received receivedElement = new Received();

                receivedElement.MVReceived = passedMV * cellTransferRatio.y;
                buffer.Add(receivedElement);
            }
        }
    }
    [BurstCompile]
    public struct PullContent : IJobForEachWithEntity<Cell, Water, Co2, Oxygen, Temperature, WindData>
    {
        [ReadOnly]
        public BufferFromEntity<Received> ReceivedBufferComponentLookup;

        public void Execute(Entity entity, int index, ref Cell cell, ref Water water, ref Co2 co2, ref Oxygen oxy, ref Temperature temp, ref WindData wind)
        {
            DynamicBuffer<Received> bufferElement = ReceivedBufferComponentLookup[entity];
            for (int i = 0; i < bufferElement.Length; i++) 
            {
                int tmp;
                Received content = bufferElement[i];
                tmp = content.cellID;
                water.Value += content.WaterReceived;
                co2.Value += content.Co2Received;
                oxy.Value += content.OxyReceived;
                temp.Value += content.TemperatureReceived;
                wind.RecivedMotionVector += content.MVReceived;
            }
            bufferElement.Clear();
        }
    }
    [BurstCompile]
    public struct PullMotionVector : IJobForEach<Cell, WindData>
    {
        [ReadOnly] public NativeHashMap<int, float2> TransferMV;

        public void Execute(ref Cell cell, ref WindData windData)
        {
            windData.RecivedMotionVector = TransferMV[cell.ID];
        }
    }

    protected override void OnCreate()
    {
        entityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    protected override void OnStartRunning()
    {
        manager = GameObject.Find("Manager").GetComponent<Manager>();
        cellEntities = World.Active.GetExistingSystem<CreateMediumSystem>().CellEntities;

        cellsCount = manager.CellsCount;
        drag = manager.Drag;
        heatTransferRatio = manager.HeatTransfer;
        debug = manager.DebugWind;
        
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        receivedBufferComponent = GetBufferFromEntity<Received>(false);
        ComponentDataFromEntity<Temperature> tempFromEntity = GetComponentDataFromEntity<Temperature>(true);


        CalculateHeatDifferanceJob calculateHeatDifferanceJob = new CalculateHeatDifferanceJob
        {
            TempFromEntity = tempFromEntity
        };
        JobHandle calculateHeatDifferanceHandle;
        if (debug)
        {
            calculateHeatDifferanceHandle = calculateHeatDifferanceJob.Run(this, inputDeps);
        }
        else
        {
            calculateHeatDifferanceHandle = calculateHeatDifferanceJob.Schedule(this, inputDeps);
        }


        CalculateDirectionPrecentageJob calculateDirectionPrecentageJob = new CalculateDirectionPrecentageJob { };
        JobHandle calculateDirectionPrecentageHandle;
        if (debug)
        {
            calculateDirectionPrecentageHandle = calculateDirectionPrecentageJob.Run(this, calculateHeatDifferanceHandle);
        }
        else
        {
            calculateDirectionPrecentageHandle = calculateDirectionPrecentageJob.Schedule(this, calculateHeatDifferanceHandle);
        }
         

        CalculateMotionVectorJob calculateMotionVectorJob = new CalculateMotionVectorJob
        {

        };
        JobHandle calculateMotionVectorHandle;
        if (debug)
        {
            calculateMotionVectorHandle = calculateMotionVectorJob.Run(this, calculateDirectionPrecentageHandle);
        }
        else
        {
            calculateMotionVectorHandle = calculateMotionVectorJob.Schedule(this, calculateDirectionPrecentageHandle);
        }


        NativeHashMap<int, float> waterContent = new NativeHashMap<int, float>(cellsCount, Allocator.TempJob);
        NativeHashMap<int, float> co2Content = new NativeHashMap<int, float>(cellsCount, Allocator.TempJob);
        NativeHashMap<int, float> oxyContent = new NativeHashMap<int, float>(cellsCount, Allocator.TempJob);
        NativeHashMap<int, float> tempContent = new NativeHashMap<int, float>(cellsCount, Allocator.TempJob);


        PopulateHash populateHash = new PopulateHash
        {
            WaterContent = waterContent,
            Co2Content = co2Content,
            OxyContent = oxyContent,
            TempContent = tempContent
        };
        JobHandle populateHashHandle;
        if (debug)
        {
            populateHashHandle = populateHash.Run(this, calculateMotionVectorHandle);
        }
        else
        {
            populateHashHandle = populateHash.ScheduleSingle(this, calculateMotionVectorHandle);
        }
        populateHashHandle.Complete();

        TransferCellContent transferCellContentJob = new TransferCellContent
        {
            Dt = Time.deltaTime,
            ReceivedBufferComponentLookup = receivedBufferComponent,

            WaterContent = waterContent,
            Co2Content = co2Content,
            OxyContent = oxyContent,
            TempContent = tempContent,

            HeatTransferRatio = heatTransferRatio
        };
        JobHandle transferCellContentHandle;
        if (debug)
        {
            transferCellContentHandle = transferCellContentJob.Run(this, populateHashHandle);
        }
        else
        {
            transferCellContentHandle = transferCellContentJob.Schedule(this, populateHashHandle);
        }

        TransferMotionVectorJob transferMotionVectorJob = new TransferMotionVectorJob
        {
            Dt = Time.deltaTime,
            Drag = drag,
            ReceivedBufferComponentLookup = receivedBufferComponent
        };
        JobHandle transferMotionVectorHandle;
        if (debug)
        {
            transferMotionVectorHandle = transferMotionVectorJob.Run(this, transferCellContentHandle);
        }
        else
        {
            transferMotionVectorHandle = transferMotionVectorJob.Schedule(this, transferCellContentHandle);
        }

        ReciveFromObjects reciveFromObjects = new ReciveFromObjects
        {
            CellEntities = cellEntities,
            MapSize = new int2(manager.MapWidth, manager.MapHeight),
            ReceivedBufferComponentLookup = receivedBufferComponent,
            CommandBuffer = entityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        };
        JobHandle reciveFromObjectsHandle = reciveFromObjects.Schedule(this, transferMotionVectorHandle);


        PullContent pullContentJob = new PullContent
        {
            ReceivedBufferComponentLookup = receivedBufferComponent
        };
        JobHandle pullContentHandle;
        if (debug)
        {
            pullContentHandle = pullContentJob.Run(this, reciveFromObjectsHandle);
        }
        else
        {
            pullContentHandle = pullContentJob.Schedule(this, reciveFromObjectsHandle);
        }


        transferCellContentHandle.Complete();

        entityCommandBufferSystem.AddJobHandleForProducer(reciveFromObjectsHandle);

        waterContent.Dispose();
        co2Content.Dispose();
        oxyContent.Dispose();
        tempContent.Dispose();

        return pullContentHandle;
    }

    protected override void OnStopRunning()
    {
    }
}
