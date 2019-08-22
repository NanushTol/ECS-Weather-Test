//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Jobs;
//using Unity.Collections;
//using Unity.Burst;


//public class testInitiate : ComponentSystem
//{
//    NativeHashMap<int, Entity> cellEntities;
//    Manager manager;

//    protected override void OnStartRunning()
//    {
//        manager = GameObject.Find("Manager").GetComponent<Manager>();

//        cellEntities = new NativeHashMap<int, Entity>(1, Allocator.Persistent);
//    }
//    protected override void OnUpdate()
//    {
//        Entities.ForEach((Entity entity, ref Reciver receiver) =>
//        {
//            cellEntities[0] = entity;
//        });

//        manager.TestEntities = cellEntities;
//    }

//    protected override void OnDestroy()
//    {
//        cellEntities.Dispose();
//    }
//}
