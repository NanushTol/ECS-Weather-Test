//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Unity.Entities;
//using Unity.Collections;
//using Unity.Mathematics;
//using Unity.Jobs;
//using Unity.Burst;

////[UpdateAfter(typeof(testInitiate))]
//[DisableAutoCreation]
//public class PushSystemTest : JobComponentSystem
//{
//    Manager manager;
//    NativeHashMap<int, Entity> recivers;
//    JobHandle populateReciversHandle;
//    protected override void OnStartRunning()
//    {
//        //manager = GameObject.Find("Manager").GetComponent<Manager>();
//        //recivers = manager.TestEntities;
//        recivers = new NativeHashMap<int, Entity>(1, Allocator.Persistent);

//        PopulateRecivers populateRecivers = new PopulateRecivers
//        {
//            Recivers = recivers
//        };
//        populateReciversHandle = populateRecivers.ScheduleSingle(this);
//    }

//    public struct PushTestJob : IJobForEach<MasterCell, Temperature>
//    {
//        [NativeDisableParallelForRestriction]
//        public BufferFromEntity<Received> ReceivedLookup;
//        [ReadOnly]
//        public NativeHashMap<int, Entity> Recivers;
//        public void Execute([ReadOnly]ref MasterCell pushCell, ref Temperature temp)
//        {
//            var buffer = ReceivedLookup[Recivers[0]];
//            Received receivedElement = new Received();
//            receivedElement.TemperatureReceived += 2;
//            buffer.Add(receivedElement);
//        }
//    }



//    public struct PopulateRecivers : IJobForEachWithEntity<Reciver>
//    {
//        [NativeDisableParallelForRestriction]
//        public NativeHashMap<int, Entity> Recivers;
//        public void Execute(Entity entity, int index, [ReadOnly]ref Reciver c0)
//        {
//            Recivers[0] = entity;
//        }
//    }

//    public struct CollectRecived : IJobForEachWithEntity<Reciver, Temperature>
//    {
//        [ReadOnly]
//        public BufferFromEntity<Received> ReceivedComponent;

//        public void Execute(Entity entity, int index, [ReadOnly]ref Reciver reciver, ref Temperature temp)
//        {
//            DynamicBuffer<Received> receivedBuffer = ReceivedComponent[entity];
//            for (int i = 0; i < 2; i++) 
//            {
//                Received buffer = receivedBuffer[i];
//                temp.Value += buffer.TemperatureReceived;
//            }
//            receivedBuffer.Clear();
//        }
//    }


//    protected override JobHandle OnUpdate(JobHandle inputDeps)
//    {
//        BufferFromEntity<Received> lookup = GetBufferFromEntity<Received>(false);

//        //var buffer = lookup[recivers[0]];
//        //Received receivedElement = new Received();
//        //receivedElement.RecivedTemperature += 2;
//        //buffer.Add(receivedElement);

//        PushTestJob pushTestJob = new PushTestJob
//        {
//            Recivers = recivers,
//            ReceivedLookup = lookup
//        };
//        JobHandle pushTestHandle = pushTestJob.ScheduleSingle(this, populateReciversHandle);

//        CollectRecived collectRecived = new CollectRecived
//        {
//            ReceivedComponent = lookup
//        };
//        JobHandle collectRecivedHandle = collectRecived.Schedule(this, pushTestHandle);

//        return collectRecivedHandle;
//    }

//    protected override void OnStopRunning()
//    {
//        recivers.Dispose();
//    }
//}
