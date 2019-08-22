using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;


public class WindSystem : JobComponentSystem
{
    Manager manager;
    int cellsCount;
    float drag;
    float heatTransferRatio;
    bool debug;

    [BurstCompile]
    public struct CalculateHeatDifferanceJob : IJobForEachWithEntity<Cell, AdjacentCells, WindData>
    {
        [ReadOnly]
        public ComponentDataFromEntity<Temperature> TempFromEntity;

        public void Execute(Entity entity, int index, ref Cell cell, ref AdjacentCells adjCells, ref WindData windData)
        {
            // Calculate Heat Differance
            windData.TotalTempDifference = 0f;

            Temperature upTemp      = TempFromEntity[adjCells.Up];
            Temperature rightTemp   = TempFromEntity[adjCells.Right];
            Temperature downTemp    = TempFromEntity[adjCells.Down];
            Temperature leftTemp    = TempFromEntity[adjCells.Left];
            Temperature temp        = TempFromEntity[entity];

            windData.TempDifference[0] = temp.Value - upTemp.Value;
            windData.TempDifference[1] = temp.Value - rightTemp.Value;
            windData.TempDifference[2] = temp.Value - downTemp.Value;
            windData.TempDifference[3] = temp.Value - leftTemp.Value;

            for (int i = 0; i < 4; i++)
            {
                windData.TotalTempDifference += windData.TempDifference[i];
            }

            if (cell.ID == 44 || cell.ID == 53 || cell.ID == 54 || cell.ID == 55 || cell.ID == 64)
            {

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

            if (cell.ID == 44 || cell.ID == 53 || cell.ID == 54 || cell.ID == 55 || cell.ID == 64)
            {

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

            if (cell.ID == 44 || cell.ID == 53 || cell.ID == 54 || cell.ID == 55 || cell.ID == 64)
            {

            }

            windData.RecivedMotionVector = new float2(0f, 0f);
        }
    }
    [BurstCompile]
    public struct PopulateHash : IJobForEach<Cell, WindData, Water, Co2, Oxygen, Temperature>
    {
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float2> TransferMV;
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float> WaterTransfer;
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float> Co2Transfer;
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float> OxyTransfer;
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float> TempTransfer;

        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float> WaterContent;
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float> Co2Content;
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float> OxyContent;
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float> TempContent;

        public void Execute(ref Cell cell, ref WindData wind, ref Water water, ref Co2 co2, ref Oxygen oxy, ref Temperature temp)
        {
            TransferMV.TryAdd(cell.ID, new float2(0, 0));
            WaterTransfer.TryAdd(cell.ID, 0);
            Co2Transfer.TryAdd(cell.ID, 0);
            OxyTransfer.TryAdd(cell.ID, 0);
            TempTransfer.TryAdd(cell.ID, 0);

            WaterContent.TryAdd(cell.ID, water.Value);
            Co2Content.TryAdd(cell.ID, co2.Value);
            OxyContent.TryAdd(cell.ID, oxy.Value);
            TempContent.TryAdd(cell.ID, temp.Value);

            if (cell.ID == 44 || cell.ID == 53 || cell.ID == 54 || cell.ID == 55 || cell.ID == 64)
            {

            }
        }
    }
    [BurstCompile]
    public struct TransferCellContent : IJobForEach<Cell, WindData, Water, Co2, Oxygen, Temperature>
    {
        public float Dt;

        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float> WaterTransfer;
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float> Co2Transfer;
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float> OxyTransfer;
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float> TempTransfer;

        [ReadOnly]
        public NativeHashMap<int, float> WaterContent;
        [ReadOnly]
        public NativeHashMap<int, float> Co2Content;
        [ReadOnly]
        public NativeHashMap<int, float> OxyContent;
        //[NativeDisableParallelForRestriction]
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
                    TransferContent(cell.ID, cell.UpCellId, -math.abs(windData.MotionVector.y));
                }
                else if (windData.TempDifference[0] < 0) // recive
                {
                    TransferContent(cell.ID, cell.UpCellId, math.abs(windData.MotionVector.y));
                }
            }
            else if (math.abs(windData.TempDifference[0]) < math.abs(windData.TempDifference[2])) // interact with bottom cell
            {
                if (windData.TempDifference[2] > 0) // give
                {
                    TransferContent(cell.ID, cell.DownCellId, -math.abs(windData.MotionVector.y));
                }
                else if (windData.TempDifference[2] < 0) // recive
                {
                    TransferContent(cell.ID, cell.DownCellId, math.abs(windData.MotionVector.y));
                }
            }

            if (math.abs(windData.TempDifference[1]) > math.abs(windData.TempDifference[3])) // Interact with Right cell
            {
                if (windData.TempDifference[1] > 0) // give
                {
                    TransferContent(cell.ID, cell.RightCellId, -math.abs(windData.MotionVector.x));
                }
                else if (windData.TempDifference[1] < 0) // recive
                {
                    TransferContent(cell.ID, cell.RightCellId, math.abs(windData.MotionVector.x));
                }
            }
            else if (math.abs(windData.TempDifference[1]) < math.abs(windData.TempDifference[3])) // interact with Left cell
            {
                if (windData.TempDifference[3] > 0) // give
                {
                    TransferContent(cell.ID, cell.LeftCellId, -math.abs(windData.MotionVector.x));
                }
                else if (windData.TempDifference[3] < 0) // recive
                {
                    TransferContent(cell.ID, cell.LeftCellId, math.abs(windData.MotionVector.x));
                }
            }
        }
        void TransferContent(int cellId, int adjacentId, float motionAxisValue)
        {
            if (adjacentId != -1)
            {
            float transfer = 0;
            float tmp = 0;

                if (cellId == 44 || cellId == 53 || cellId == 54 || cellId == 55 || cellId == 64)
                {

                }

                transfer = motionAxisValue / TempContent[adjacentId] * HeatTransferRatio;

                //_tempCells[cellId].Content[c] += transfer;
                tmp = TempContent[cellId] + transfer;
                TempContent[cellId] = tmp;

                //_tempCells[_adjacentId[dir]].Content[c] -= transfer;
                tmp = TempContent[adjacentId] - transfer;
                TempContent[adjacentId] = tmp;
            }
        }
    }
    [BurstCompile]
    public struct PullContent : IJobForEach<Cell, Water, Co2, Oxygen, Temperature>
    {
        [ReadOnly]
        public NativeHashMap<int, float> WaterTransfer;
        [ReadOnly]
        public NativeHashMap<int, float> Co2Transfer;
        [ReadOnly]
        public NativeHashMap<int, float> OxyTransfer;
        [ReadOnly]
        public NativeHashMap<int, float> TempContent;

        public void Execute(ref Cell cell, ref Water water, ref Co2 co2, ref Oxygen oxy, ref Temperature temp)
        {
            water.Value = WaterTransfer[cell.ID];
            co2.Value = Co2Transfer[cell.ID];
            oxy.Value = OxyTransfer[cell.ID];
            temp.Value = TempContent[cell.ID];
            if (cell.ID == 44 || cell.ID == 53 || cell.ID == 54 || cell.ID == 55 || cell.ID == 64)
            {

            }
        }
    }
    [BurstCompile]
    public struct TransferMotionVectorJob : IJobForEach<Cell, WindData>
    {
        public float Dt;
        public float Drag;

        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float2> TransferMV;

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
                tmp = TransferMV[cell.RightCellId] + passedMV * cellTransferRatio.x;
                TransferMV[cell.RightCellId] = tmp;
            }
            else if (windData.MotionVector.x < 0) // left Movement
            {
                tmp = TransferMV[cell.LeftCellId] + passedMV * cellTransferRatio.x;
                TransferMV[cell.LeftCellId] = tmp;
            }

            if (windData.MotionVector.y > 0 && cell.UpCellId != -1) // Up Movement
            {
                tmp = TransferMV[cell.UpCellId] + passedMV * cellTransferRatio.y;
                TransferMV[cell.UpCellId] = tmp;
            }
            else if (windData.MotionVector.y < 0 && cell.DownCellId != -1) // Down Movement
            {
                tmp = TransferMV[cell.DownCellId] + passedMV * cellTransferRatio.y;
                TransferMV[cell.DownCellId] = tmp;
            }

            if (cell.ID == 44 || cell.ID == 53 || cell.ID == 54 || cell.ID == 55 || cell.ID == 64)
            {

            }
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

    /// <summary>
    /// asdasd
    /// </summary>
    protected override void OnCreate()
    {
    }

    protected override void OnStartRunning()
    {
        manager = GameObject.Find("Manager").GetComponent<Manager>();
        cellsCount = manager.CellsCount;
        drag = manager.Drag;
        heatTransferRatio = manager.HeatTransfer;
        debug = manager.DebugWind;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {

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


        NativeHashMap<int, float2>  transferMV      = new NativeHashMap<int, float2>(cellsCount ,Allocator.TempJob);
        NativeHashMap<int, float>   waterTransfer   = new NativeHashMap<int, float> (cellsCount, Allocator.TempJob);
        NativeHashMap<int, float>   co2Transfer     = new NativeHashMap<int, float> (cellsCount, Allocator.TempJob);
        NativeHashMap<int, float>   oxyTransfer     = new NativeHashMap<int, float> (cellsCount, Allocator.TempJob);
        NativeHashMap<int, float>   tempTransfer    = new NativeHashMap<int, float> (cellsCount, Allocator.TempJob);

        NativeHashMap<int, float> waterContent = new NativeHashMap<int, float>(cellsCount, Allocator.TempJob);
        NativeHashMap<int, float> co2Content = new NativeHashMap<int, float>(cellsCount, Allocator.TempJob);
        NativeHashMap<int, float> oxyContent = new NativeHashMap<int, float>(cellsCount, Allocator.TempJob);
        NativeHashMap<int, float> tempContent = new NativeHashMap<int, float>(cellsCount, Allocator.TempJob);


        PopulateHash populateTransferHash = new PopulateHash
        {
            TransferMV = transferMV,
            WaterTransfer = waterTransfer,
            Co2Transfer = co2Transfer,
            OxyTransfer = oxyTransfer,
            TempTransfer = tempTransfer,

            WaterContent = waterContent,
            Co2Content = co2Content,
            OxyContent = oxyContent,
            TempContent = tempContent
        };
        JobHandle populateTransferHashHandle;
        if (debug)
        {
            populateTransferHashHandle = populateTransferHash.Run(this, calculateMotionVectorHandle);
        }
        else
        {
            populateTransferHashHandle = populateTransferHash.ScheduleSingle(this, calculateMotionVectorHandle);
        }


        TransferCellContent transferCellContentJob = new TransferCellContent
        {
            Dt = Time.deltaTime,
            WaterTransfer = waterTransfer,
            Co2Transfer = co2Transfer,
            OxyTransfer = oxyTransfer,
            TempTransfer = tempTransfer,

            WaterContent = waterContent,
            Co2Content = co2Content,
            OxyContent = oxyContent,
            TempContent = tempContent,

            HeatTransferRatio = heatTransferRatio
        };
        JobHandle transferCellContentHandle;
        if (debug)
        {
            transferCellContentHandle = transferCellContentJob.Run(this, populateTransferHashHandle);
        }
        else
        {
            transferCellContentHandle = transferCellContentJob.Schedule(this, populateTransferHashHandle);
        }


        PullContent pullContentJob = new PullContent
        {
            WaterTransfer = waterTransfer,
            Co2Transfer = co2Transfer,
            OxyTransfer = oxyTransfer,
            TempContent = tempContent
        };
        JobHandle pullContentHandle;
        if (debug)
        {
            pullContentHandle = pullContentJob.Run(this, transferCellContentHandle);
        }
        else
        {
            pullContentHandle = pullContentJob.Schedule(this, transferCellContentHandle);
        }


        TransferMotionVectorJob transferMotionVectorJob = new TransferMotionVectorJob
        {
            Dt = Time.deltaTime,
            Drag = drag,
            TransferMV = transferMV
        };
        JobHandle transferMotionVectorHandle;
        if (debug)
        {
            transferMotionVectorHandle = transferMotionVectorJob.Run(this, pullContentHandle);
        }
        else
        {
            transferMotionVectorHandle = transferMotionVectorJob.Schedule(this, pullContentHandle);
        }


        PullMotionVector pullMotionVector = new PullMotionVector
        {
            TransferMV = transferMV
        };
        JobHandle pullMotionVectorHandle;
        if (debug)
        {
            pullMotionVectorHandle = pullMotionVector.Run(this, transferMotionVectorHandle);
        }
        else
        {
            pullMotionVectorHandle = pullMotionVector.Schedule(this, transferMotionVectorHandle);
        }


        pullMotionVectorHandle.Complete();
        transferMV.Dispose();
        waterTransfer.Dispose();
        co2Transfer.Dispose();
        oxyTransfer.Dispose();
        tempTransfer.Dispose();


        return pullMotionVectorHandle;
    }

    protected override void OnStopRunning()
    {
    }
}
