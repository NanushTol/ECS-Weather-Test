using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;


public class CreateMediumSystem : ComponentSystem
{
    int2 _mapSize;
    Manager manager;
    public NativeHashMap<int, Entity> CellEntities;

    protected override void OnStartRunning()
    {
        manager = GameObject.Find("Manager").GetComponent<Manager>();
        _mapSize = new int2 (manager.MapWidth, manager.MapHeight);

        CellEntities = new NativeHashMap<int, Entity>(_mapSize.x * _mapSize.y + 1, Allocator.Persistent);

        CreateMap();

        GetAdjacentCellIDJob getAdjacentID = new GetAdjacentCellIDJob
        {
            MapSize = _mapSize,
        };

        JobHandle getAdjacentIDHandle = getAdjacentID.Schedule(this);
        getAdjacentIDHandle.Complete();



        GetAdjacentEntitiesJob getAdjacentEntities = new GetAdjacentEntitiesJob
        {
            CellEntities = CellEntities
        };

        JobHandle getAdjacentEntitiesHandle = getAdjacentEntities.Schedule(this, getAdjacentIDHandle);
        getAdjacentEntitiesHandle.Complete();

        //manager.CellEntities = CellEntities;
        manager.CellsCount = CellEntities.Length;

        CellEntities.Dispose();
    }

    public struct GetAdjacentCellIDJob : IJobForEach<Cell, Translation>
    {
        public int2 MapSize;
        public void Execute(ref Cell cell, ref Translation position)
        {
            // Get Sides Adjacent Cells
            if (position.Value.x - 0.5f == -MapSize.x / 2)              // if tile is on the Left edge
            {
                cell.RightCellId = cell.ID + 1;                       // Right Cell ID
                cell.LeftCellId  = cell.ID + MapSize.x - 1;           // Left Cell ID
            }
            else if (position.Value.x - 0.5f == (MapSize.x / 2) - 1)    // if tile is on the Right edge
            {
                cell.RightCellId = cell.ID - (MapSize.x - 1);         // Right Cell ID
                cell.LeftCellId  = cell.ID - 1;                       // Left Cell ID
            }
            else                                                        // else
            {
                cell.RightCellId = cell.ID + 1;                       // Right Cell ID
                cell.LeftCellId  = cell.ID - 1;                       // Left Cell ID
            }

            // Get Top/Down Adjacent Cells
            if (position.Value.z - 0.5f == -MapSize.y / 2)              // if tile is on the Buttom edge
            {
                cell.UpCellId = cell.ID + MapSize.x;                  //Top Cell ID 
                cell.DownCellId = -1;                            // Buttom Cell ID = Dummy Cell
            }
            else if (position.Value.z - 0.5f == (MapSize.y / 2) - 1)    // if tile is on the Top edge
            {
                cell.UpCellId = -1;                              // Top Cell ID = Dummy Cell
                cell.DownCellId = cell.ID - MapSize.x;                // Buttom Cell ID
            }
            else                                                        // else
            {
                cell.UpCellId = cell.ID + MapSize.x;                  // Top Cell ID
                cell.DownCellId = cell.ID - MapSize.x;                // Buttom Cell ID
            }
        }
    }

    public struct GetAdjacentEntitiesJob : IJobForEach<Cell, AdjacentCells>
    {
        [ReadOnly]
        public NativeHashMap<int, Entity> CellEntities;

        public void Execute(ref Cell cell, ref AdjacentCells adjacentCells)
        {
            adjacentCells.Up = CellEntities[cell.UpCellId];
            adjacentCells.Down = CellEntities[cell.DownCellId];
            adjacentCells.Left = CellEntities[cell.LeftCellId];
            adjacentCells.Right = CellEntities[cell.RightCellId];
        }
    }

    private void CreateMap()
    {
        Entities.ForEach((Entity entity, ref MasterCell masterCell) =>
        {
            int count = 0;
            for (int y = 0; y < _mapSize.y; y++)
            {
                for (int x = 0; x < _mapSize.x; x++)
                {
                    var cell = EntityManager.Instantiate(entity);

                    EntityManager.SetComponentData(cell, new Cell { ID = count , Coordinates = new int2(x,y)});

                    EntityManager.SetComponentData(cell, new Water { Value = 0 });
                    EntityManager.SetComponentData(cell, new Co2 { Value = 0 });
                    EntityManager.SetComponentData(cell, new Oxygen { Value = 0 });
                    EntityManager.SetComponentData(cell, new Temperature { Value = 0});
                    if(x == _mapSize.x / 2 && y == _mapSize.y / 2)
                    {
                        EntityManager.SetComponentData(cell, new Temperature { Value = 40f });
                    }
                    //UnityEngine.Random.Range(1f, 10f) 
                    EntityManager.SetComponentData(cell, new Translation
                    {
                        Value = new float3
                        (x - _mapSize.x / 2 + 0.5f, 0, y - _mapSize.y / 2 + 0.5f)
                    });

                    EntityManager.RemoveComponent(cell, typeof(MasterCell));

                    CellEntities.TryAdd(count, cell);

                    EntityManager.SetName(cell, "cell " + count);

                    count++;
                }
            }

            var dummy = EntityManager.Instantiate(entity);
            EntityManager.AddComponentData(dummy, new DummyCell { ID = -1 });
            EntityManager.RemoveComponent(dummy, typeof(MasterCell));
            EntityManager.RemoveComponent(dummy, typeof(Cell));
            EntityManager.SetComponentData(dummy, new Translation
            {
                Value = new float3
                        (0, -50f, 0)
            });
            CellEntities.TryAdd(-1, dummy);
            EntityManager.SetName(dummy, "Dummy");


            EntityManager.DestroyEntity(entity);
        });

    }

    protected override void OnUpdate()
    {
    }

    protected override void OnDestroy()
    {
        //manager.CellEntities.Dispose();
    }
}
