//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Jobs;
//using Unity.Collections;
//using Unity.Burst;

//[DisableAutoCreation]
//public class WindSystem : JobComponentSystem
//{
//    Manager manager;
//    int cellsCount;
//    float drag;

//    [BurstCompile]
//    public struct CalculateHeatDifferanceJob : IJobForEachWithEntity<Cell, AdjacentCells, WindData>
//    {
//        [ReadOnly]
//        public NativeHashMap<int, Entity> CellEntities;
//        [ReadOnly]
//        public ComponentDataFromEntity<Temperature> TempFromEntity;

//        public void Execute(Entity entity, int index, ref Cell cell, ref AdjacentCells adjCells, ref WindData windData)
//        {
//            // Calculate Heat Differance
//            windData.TotalTempDifference = 0f;

//            Temperature upTemp = TempFromEntity[adjCells.Up];
//            Temperature rightTemp = TempFromEntity[adjCells.Right];
//            Temperature downTemp = TempFromEntity[adjCells.Down];
//            Temperature leftTemp = TempFromEntity[adjCells.Left];
//            Temperature temp = TempFromEntity[entity];

//            windData.TempDifference[0] = temp.Value - upTemp.Value;
//            windData.TempDifference[1] = temp.Value - rightTemp.Value;
//            windData.TempDifference[2] = temp.Value - downTemp.Value;
//            windData.TempDifference[3] = temp.Value - leftTemp.Value;

//            for (int i = 0; i < 4; i++)
//            {
//                windData.TotalTempDifference += windData.TempDifference[i];
//            }
//        }

//    }
//    [BurstCompile]
//    public struct CalculateDirectionPrecentageJob : IJobForEach<WindData, Cell>
//    {
//        public void Execute(ref WindData windData, [ReadOnly] ref Cell cell)
//        {
//            // Calculate Direction Amplitude
//            windData.DirectionAmplitude[0] = windData.TempDifference[0] - windData.TempDifference[2]; // Up Down Direction
//            windData.DirectionAmplitude[1] = windData.TempDifference[1] - windData.TempDifference[3]; // Left Right Direction


//            if (cell.DownCellId == -1 && windData.DirectionAmplitude[0] > 0) // if Bottom adjacent cell is dummy & amlitude go up
//            {
//                windData.DirectionAmplitude[0] = 0;
//            }
//            else if (cell.UpCellId == -1 && windData.DirectionAmplitude[0] < 0) // if Top adjacent cell is dummy & amlitude go down
//            {
//                windData.DirectionAmplitude[0] = 0;
//            }

//            // Calculate direction Precantage
//            windData.DirectionPrecentage[0] = windData.DirectionAmplitude[0] / windData.TotalTempDifference;
//            windData.DirectionPrecentage[1] = windData.DirectionAmplitude[1] / windData.TotalTempDifference;

//            for (int i = 0; i < 2; i++)
//            {
//                if (windData.DirectionPrecentage[i] != windData.DirectionPrecentage[i]) windData.DirectionPrecentage[i] = 0; // if precentage = NaN
//            }
//        }
//    }
//    [BurstCompile]
//    public struct CalculateMotionVectorJob : IJobForEach<Cell, WindData>
//    {
//        public void Execute(ref Cell cell, ref WindData windData)
//        {
//            float differenceAvrage = windData.TotalTempDifference / 4;

//            float2 newMotionVector = new float2(windData.DirectionPrecentage[1] * differenceAvrage, windData.DirectionPrecentage[0] * differenceAvrage);

//            windData.MotionVector = newMotionVector + windData.RecivedMotionVector;

//            //windData.RecivedMotionVector = new float2(0f, 0f);
//        }
//    }
//    [BurstCompile]
//    public struct TransferMotionVectorJob : IJobForEach<Cell, AdjacentCells, WindData, ToTransfer>
//    {
//        public float Drag;
//        [NativeDisableParallelForRestriction]
//        public NativeArray<float> Tst;

//        public void Execute(ref Cell cell, ref AdjacentCells adjacent, ref WindData windData, ref ToTransfer toTransfer)
//        {
//            float ratio;
//            float2 cellTransferRatio;

//            float2 passedMV = windData.MotionVector - windData.MotionVector * Drag;
//            // Calculate Transfer Ratios between X & Y
//            if (math.abs(windData.MotionVector.x) > math.abs(windData.MotionVector.y) && windData.MotionVector.y != 0f)
//            {
//                ratio = math.abs(windData.MotionVector.y / windData.MotionVector.x);
//                cellTransferRatio = new float2((0.5f * ratio) + (1f - ratio), (0.5f * ratio));
//            }
//            else if (math.abs(windData.MotionVector.x) < math.abs(windData.MotionVector.y) && windData.MotionVector.x != 0f)
//            {
//                ratio = math.abs(windData.MotionVector.x / windData.MotionVector.y);
//                cellTransferRatio = new float2((0.5f * ratio), (0.5f * ratio) + (1f - ratio));
//            }
//            else cellTransferRatio = new float2(0.5f, 0.5f);

//            if (windData.MotionVector.x > 0) // Right Movement
//            {
//                toTransfer.TransferXCellId = cell.RightCellId;
//                toTransfer.TransferMotionVectorX = passedMV * cellTransferRatio.x;
//            }
//            else if (windData.MotionVector.x < 0) // left Movement
//            {
//                toTransfer.TransferXCellId = cell.LeftCellId;
//                toTransfer.TransferMotionVectorX = passedMV * cellTransferRatio.x;
//            }

//            if (windData.MotionVector.y > 0) // Up Movement
//            {
//                toTransfer.TransferYCellId = cell.UpCellId;
//                toTransfer.TransferMotionVectorY = passedMV * cellTransferRatio.y;
//            }
//            else if (windData.MotionVector.y < 0) // Down Movement
//            {
//                toTransfer.TransferYCellId = cell.DownCellId;
//                toTransfer.TransferMotionVectorY = passedMV * cellTransferRatio.y;
//            }
//            Tst[4] = 10;
//        }
//    }
//    [BurstCompile]
//    public struct PullMotionVector : IJobForEach<Cell, AdjacentCells, WindData>
//    {
//        [ReadOnly] public ComponentDataFromEntity<ToTransfer> TransferFromEntity;
//        public void Execute(ref Cell cell, ref AdjacentCells adjacent, ref WindData windData)
//        {

//            ToTransfer up = TransferFromEntity[adjacent.Up];
//            ToTransfer right = TransferFromEntity[adjacent.Right];
//            ToTransfer down = TransferFromEntity[adjacent.Down];
//            ToTransfer left = TransferFromEntity[adjacent.Left];

//            if (right.TransferXCellId == cell.ID)
//                windData.RecivedMotionVector += right.TransferMotionVectorX;
//            if (left.TransferXCellId == cell.ID)
//                windData.RecivedMotionVector += left.TransferMotionVectorX;
//            if (up.TransferXCellId == cell.ID)
//                windData.RecivedMotionVector += up.TransferMotionVectorY;
//            if (down.TransferXCellId == cell.ID)
//                windData.RecivedMotionVector += down.TransferMotionVectorY;
//        }
//    }


//    public struct Test : IJobForEachWithEntity<Cell, WindData>
//    {
//        public NativeHashMap<int, int> ToWhoIndecies;
//        public NativeArray<float> Value;
//        public NativeArray<float> Testa;
//        public void Execute(Entity entity, int index, ref Cell cell, ref WindData windData)
//        {
//            Value[index] = index + 1;
//            ToWhoIndecies[index] = cell.RightCellId;
//        }
//    }

//    public struct Consolidator : IJobParallelFor
//    {
//        public NativeArray<int> CellsIndecies;
//        public NativeHashMap<int, int> ToWhoIndecies;
//        public NativeArray<float> Value;
//        public NativeHashMap<int, float> ConsolidatedValue;


//        public void Execute(int index)
//        {
//            int currentIdToCheck = CellsIndecies[index];
//            float tmp = 0;

//            for (int i = 0; i < CellsIndecies.Length; i++)
//            {
//                if (ToWhoIndecies[i] == currentIdToCheck)
//                    tmp += Value[i];
//                ConsolidatedValue[currentIdToCheck] = tmp;
//            }
//        }
//    }

//    public struct PullTest : IJobForEach<Cell, WindData>
//    {
//        public NativeHashMap<int, float> ConsolidatedValue;

//        public void Execute(ref Cell cell, ref WindData windData)
//        {
//            windData.test = ConsolidatedValue[cell.ID];
//        }
//    }

//    protected override void OnCreate()
//    {
//    }

//    protected override void OnStartRunning()
//    {
//        manager = GameObject.Find("Manager").GetComponent<Manager>();
//        cellsCount = manager.CellEntities.Length;
//        drag = manager.Drag;
//    }

//    protected override JobHandle OnUpdate(JobHandle inputDeps)
//    {

//        ComponentDataFromEntity<Temperature> tempFromEntity = GetComponentDataFromEntity<Temperature>(true);


//        CalculateHeatDifferanceJob calculateHeatDifferanceJob = new CalculateHeatDifferanceJob
//        {
//            CellEntities = manager.CellEntities,
//            TempFromEntity = tempFromEntity
//        };
//        JobHandle calculateHeatDifferanceHandle = calculateHeatDifferanceJob.Schedule(this, inputDeps);


//        CalculateDirectionPrecentageJob calculateDirectionPrecentageJob = new CalculateDirectionPrecentageJob { };
//        JobHandle calculateDirectionPrecentageHandle = calculateDirectionPrecentageJob.Schedule(this, calculateHeatDifferanceHandle);


//        CalculateMotionVectorJob calculateMotionVectorJob = new CalculateMotionVectorJob
//        {

//        };
//        JobHandle calculateMotionVectorHandle = calculateMotionVectorJob.Schedule(this, calculateDirectionPrecentageHandle);

//        ComponentDataFromEntity<ToTransfer> toTransferFromEntity = GetComponentDataFromEntity<ToTransfer>(true);

//        NativeArray<float> tst = new NativeArray<float>(5, Allocator.TempJob);

//        TransferMotionVectorJob transferMotionVectorJob = new TransferMotionVectorJob
//        {
//            Drag = drag,
//            Tst = tst
//        };
//        JobHandle transferMotionVectorHandle = transferMotionVectorJob.Schedule(this, calculateMotionVectorHandle);

//        PullMotionVector pullMotionVector = new PullMotionVector
//        {
//            TransferFromEntity = toTransferFromEntity
//        };
//        JobHandle pullMotionVectorHandle = pullMotionVector.Schedule(this, transferMotionVectorHandle);


//        return pullMotionVectorHandle;
//    }

//    protected override void OnStopRunning()
//    {
//    }
//}
