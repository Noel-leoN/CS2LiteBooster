using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Creatures;
using Game.Net;
using Game.Pathfind;
using Game.Routes;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;

namespace GameLiteBooster.Systems
{
	[CompilerGenerated]
	public partial class TaxiDispatchSystem : GameSystemBase
	{
		private struct VehicleDispatch
		{
			public Entity m_Request;

			public Entity m_Source;

			public VehicleDispatch(Entity request, Entity source)
			{
				this.m_Request = request;
				this.m_Source = source;
			}
		}

		[BurstCompile]
		private struct TaxiDispatchJob : IJobChunk
		{
			[ReadOnly]
			public EntityTypeHandle m_EntityType;

			[ReadOnly]
			public ComponentTypeHandle<TaxiRequest> m_TaxiRequestType;

			[ReadOnly]
			public ComponentTypeHandle<Dispatched> m_DispatchedType;

			[ReadOnly]
			public ComponentTypeHandle<PathInformation> m_PathInformationType;

			[ReadOnly]
			public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

			public ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

			[ReadOnly]
			public ComponentLookup<TaxiRequest> m_TaxiRequestData;

			[ReadOnly]
			public ComponentLookup<CurrentRoute> m_CurrentRouteData;

			[ReadOnly]
			public ComponentLookup<BoardingVehicle> m_BoardingVehicleData;

			[ReadOnly]
			public BufferLookup<ServiceDispatch> m_ServiceDispatches;

			[ReadOnly]
			public BufferLookup<DispatchedRequest> m_DispatchedRequests;

			[NativeDisableParallelForRestriction]
			public ComponentLookup<TaxiStand> m_TaxiStandData;

			[NativeDisableParallelForRestriction]
			public ComponentLookup<TransportDepot> m_TransportDepotData;

			[NativeDisableParallelForRestriction]
			public ComponentLookup<Taxi> m_TaxiData;

			[NativeDisableParallelForRestriction]
			public ComponentLookup<RideNeeder> m_RideNeederData;

			[ReadOnly]
			public uint m_UpdateFrameIndex;

			[ReadOnly]
			public uint m_NextUpdateFrameIndex;

			public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

			public NativeQueue<VehicleDispatch>.ParallelWriter m_VehicleDispatches;

			public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				uint index = chunk.GetSharedComponent(this.m_UpdateFrameType).m_Index;
				if (index == this.m_NextUpdateFrameIndex && !chunk.Has(ref this.m_DispatchedType) && !chunk.Has(ref this.m_PathInformationType))
				{
					NativeArray<Entity> nativeArray = chunk.GetNativeArray(this.m_EntityType);
					NativeArray<TaxiRequest> nativeArray2 = chunk.GetNativeArray(ref this.m_TaxiRequestType);
					NativeArray<ServiceRequest> nativeArray3 = chunk.GetNativeArray(ref this.m_ServiceRequestType);
					for (int i = 0; i < nativeArray2.Length; i++)
					{
						Entity entity = nativeArray[i];
						TaxiRequest taxiRequest = nativeArray2[i];
						ServiceRequest serviceRequest = nativeArray3[i];
						if ((serviceRequest.m_Flags & ServiceRequestFlags.Reversed) != 0)
						{
							if (!this.ValidateReversed(entity, taxiRequest.m_Seeker))
							{
								this.m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
								continue;
							}
							if (SimulationUtils.TickServiceRequest(ref serviceRequest))
							{
								this.FindVehicleTarget(unfilteredChunkIndex, entity, taxiRequest.m_Seeker);
							}
						}
						else
						{
							if (!this.ValidateTarget(entity, taxiRequest.m_Seeker, dispatched: false))
							{
								this.m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
								continue;
							}
							if (SimulationUtils.TickServiceRequest(ref serviceRequest))
							{
								this.FindVehicleSource(unfilteredChunkIndex, entity, taxiRequest.m_Seeker, taxiRequest.m_Type, taxiRequest.m_Priority);
							}
						}
						nativeArray3[i] = serviceRequest;
					}
				}
				if (index != this.m_UpdateFrameIndex)
				{
					return;
				}
				NativeArray<Dispatched> nativeArray4 = chunk.GetNativeArray(ref this.m_DispatchedType);
				NativeArray<TaxiRequest> nativeArray5 = chunk.GetNativeArray(ref this.m_TaxiRequestType);
				NativeArray<ServiceRequest> nativeArray6 = chunk.GetNativeArray(ref this.m_ServiceRequestType);
				if (nativeArray4.Length != 0)
				{
					NativeArray<Entity> nativeArray7 = chunk.GetNativeArray(this.m_EntityType);
					for (int j = 0; j < nativeArray4.Length; j++)
					{
						Entity entity2 = nativeArray7[j];
						Dispatched dispatched = nativeArray4[j];
						TaxiRequest taxiRequest2 = nativeArray5[j];
						ServiceRequest serviceRequest2 = nativeArray6[j];
						if (this.ValidateHandler(entity2, dispatched.m_Handler))
						{
							serviceRequest2.m_Cooldown = 0;
						}
						else if (serviceRequest2.m_Cooldown == 0)
						{
							serviceRequest2.m_Cooldown = 1;
						}
						else
						{
							if (!this.ValidateTarget(entity2, taxiRequest2.m_Seeker, dispatched: true))
							{
								this.m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity2);
								continue;
							}
							this.ResetFailedRequest(unfilteredChunkIndex, entity2, dispatched: true, ref serviceRequest2);
						}
						nativeArray6[j] = serviceRequest2;
					}
					return;
				}
				NativeArray<PathInformation> nativeArray8 = chunk.GetNativeArray(ref this.m_PathInformationType);
				if (nativeArray8.Length == 0)
				{
					return;
				}
				NativeArray<Entity> nativeArray9 = chunk.GetNativeArray(this.m_EntityType);
				for (int k = 0; k < nativeArray5.Length; k++)
				{
					Entity entity3 = nativeArray9[k];
					TaxiRequest taxiRequest3 = nativeArray5[k];
					PathInformation pathInformation = nativeArray8[k];
					ServiceRequest serviceRequest3 = nativeArray6[k];
					if ((serviceRequest3.m_Flags & ServiceRequestFlags.Reversed) != 0)
					{
						if (!this.ValidateReversed(entity3, taxiRequest3.m_Seeker))
						{
							this.m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity3);
							continue;
						}
						if (pathInformation.m_Destination != Entity.Null)
						{
							this.ResetReverseRequest(unfilteredChunkIndex, entity3, pathInformation, ref serviceRequest3);
						}
						else
						{
							this.ResetFailedRequest(unfilteredChunkIndex, entity3, dispatched: false, ref serviceRequest3);
						}
					}
					else
					{
						if (!this.ValidateTarget(entity3, taxiRequest3.m_Seeker, dispatched: false))
						{
							this.m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity3);
							continue;
						}
						if (pathInformation.m_Origin != Entity.Null)
						{
							this.DispatchVehicle(unfilteredChunkIndex, entity3, pathInformation);
						}
						else
						{
							this.ResetFailedRequest(unfilteredChunkIndex, entity3, dispatched: false, ref serviceRequest3);
						}
					}
					nativeArray6[k] = serviceRequest3;
				}
			}

			private bool ValidateReversed(Entity entity, Entity source)
			{
				if (this.m_TransportDepotData.TryGetComponent(source, out var componentData))
				{
					if ((componentData.m_Flags & TransportDepotFlags.HasAvailableVehicles) == 0)
					{
						return false;
					}
					if (componentData.m_TargetRequest != entity)
					{
						if (this.m_TaxiRequestData.HasComponent(componentData.m_TargetRequest))
						{
							return false;
						}
						componentData.m_TargetRequest = entity;
						this.m_TransportDepotData[source] = componentData;
					}
					return true;
				}
				if (this.m_TaxiData.TryGetComponent(source, out var componentData2))
				{
					if ((componentData2.m_State & (TaxiFlags.Requested | TaxiFlags.RequiresMaintenance | TaxiFlags.Dispatched | TaxiFlags.Disabled)) != 0)
					{
						return false;
					}
					if (this.m_CurrentRouteData.TryGetComponent(source, out var componentData3) && this.m_BoardingVehicleData.HasComponent(componentData3.m_Route))
					{
						return false;
					}
					if (componentData2.m_TargetRequest != entity)
					{
						if (this.m_TaxiRequestData.HasComponent(componentData2.m_TargetRequest))
						{
							return false;
						}
						componentData2.m_TargetRequest = entity;
						this.m_TaxiData[source] = componentData2;
					}
					return true;
				}
				return false;
			}

			private bool ValidateHandler(Entity entity, Entity handler)
			{
				if (this.m_ServiceDispatches.HasBuffer(handler))
				{
					DynamicBuffer<ServiceDispatch> dynamicBuffer = this.m_ServiceDispatches[handler];
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						if (dynamicBuffer[i].m_Request == entity)
						{
							return true;
						}
					}
				}
				return false;
			}

			private bool ValidateTarget(Entity entity, Entity target, bool dispatched)
			{
				if (this.m_TaxiStandData.HasComponent(target))
				{
					TaxiStand value = this.m_TaxiStandData[target];
					if ((value.m_Flags & TaxiStandFlags.RequireVehicles) == 0)
					{
						return false;
					}
					if (this.m_DispatchedRequests.TryGetBuffer(target, out var bufferData))
					{
						for (int i = 0; i < bufferData.Length; i++)
						{
							if (bufferData[i].m_VehicleRequest == entity)
							{
								return !dispatched;
							}
						}
					}
					if (value.m_TaxiRequest != entity)
					{
						if (this.m_TaxiRequestData.HasComponent(value.m_TaxiRequest))
						{
							return false;
						}
						value.m_TaxiRequest = entity;
						this.m_TaxiStandData[target] = value;
					}
					return true;
				}
				if (this.m_RideNeederData.HasComponent(target))
				{
					RideNeeder value2 = this.m_RideNeederData[target];
					if (value2.m_RideRequest != entity)
					{
						if (this.m_TaxiRequestData.HasComponent(value2.m_RideRequest))
						{
							return false;
						}
						value2.m_RideRequest = entity;
						this.m_RideNeederData[target] = value2;
					}
					return true;
				}
				return false;
			}

			private void ResetReverseRequest(int jobIndex, Entity entity, PathInformation pathInformation, ref ServiceRequest serviceRequest)
			{
				VehicleDispatch value = new VehicleDispatch(entity, pathInformation.m_Destination);
				this.m_VehicleDispatches.Enqueue(value);
				SimulationUtils.ResetReverseRequest(ref serviceRequest);
				this.m_CommandBuffer.RemoveComponent<PathInformation>(jobIndex, entity);
			}

			private void ResetFailedRequest(int jobIndex, Entity entity, bool dispatched, ref ServiceRequest serviceRequest)
			{
				SimulationUtils.ResetFailedRequest(ref serviceRequest);
				this.m_CommandBuffer.RemoveComponent<PathInformation>(jobIndex, entity);
				this.m_CommandBuffer.RemoveComponent<PathElement>(jobIndex, entity);
				if (dispatched)
				{
					this.m_CommandBuffer.RemoveComponent<Dispatched>(jobIndex, entity);
				}
			}

			private void DispatchVehicle(int jobIndex, Entity entity, PathInformation pathInformation)
			{
				VehicleDispatch value = new VehicleDispatch(entity, pathInformation.m_Origin);
				this.m_VehicleDispatches.Enqueue(value);
				this.m_CommandBuffer.AddComponent(jobIndex, entity, new Dispatched(pathInformation.m_Origin));
			}

			private void FindVehicleSource(int jobIndex, Entity requestEntity, Entity seeker, TaxiRequestType type, int priority)
			{
				PathfindParameters pathfindParameters = default(PathfindParameters);
				pathfindParameters.m_MaxSpeed = 111.111115f;
				pathfindParameters.m_WalkSpeed = 5.555556f;
				pathfindParameters.m_Weights = new PathfindWeights(1f, 1f, 1f, 1f);
				pathfindParameters.m_Methods = PathMethod.Road | PathMethod.Boarding;
				pathfindParameters.m_IgnoredFlags = EdgeFlags.ForbidCombustionEngines | EdgeFlags.ForbidHeavyTraffic | EdgeFlags.ForbidPrivateTraffic | EdgeFlags.ForbidSlowTraffic;
				PathfindParameters parameters = pathfindParameters;
				SetupQueueTarget setupQueueTarget = default(SetupQueueTarget);
				setupQueueTarget.m_Type = SetupTargetType.Taxi;
				setupQueueTarget.m_Methods = PathMethod.Road | PathMethod.Boarding;
				setupQueueTarget.m_RoadTypes = RoadTypes.Car;
				SetupQueueTarget origin = setupQueueTarget;
				setupQueueTarget = default(SetupQueueTarget);
				setupQueueTarget.m_Type = SetupTargetType.CurrentLocation;
				setupQueueTarget.m_RoadTypes = RoadTypes.Car;
				setupQueueTarget.m_Entity = seeker;
				SetupQueueTarget destination = setupQueueTarget;
				if (type == TaxiRequestType.Stand)
				{
					destination.m_Methods = PathMethod.Road;
				}
				else
				{
					destination.m_Methods = PathMethod.Boarding;
				}
				this.m_PathfindQueue.Enqueue(new SetupQueueItem(requestEntity, parameters, origin, destination));
				this.m_CommandBuffer.AddComponent(jobIndex, requestEntity, default(PathInformation));
				this.m_CommandBuffer.AddBuffer<PathElement>(jobIndex, requestEntity);
			}

			private void FindVehicleTarget(int jobIndex, Entity requestEntity, Entity vehicleSource)
			{
				PathfindParameters pathfindParameters = default(PathfindParameters);
				pathfindParameters.m_MaxSpeed = 111.111115f;
				pathfindParameters.m_WalkSpeed = 5.555556f;
				pathfindParameters.m_Weights = new PathfindWeights(1f, 1f, 1f, 1f);
				pathfindParameters.m_Methods = PathMethod.Road | PathMethod.Boarding;
				pathfindParameters.m_IgnoredFlags = EdgeFlags.ForbidCombustionEngines | EdgeFlags.ForbidHeavyTraffic | EdgeFlags.ForbidPrivateTraffic | EdgeFlags.ForbidSlowTraffic;
				PathfindParameters parameters = pathfindParameters;
				SetupQueueTarget setupQueueTarget = default(SetupQueueTarget);
				setupQueueTarget.m_Type = SetupTargetType.CurrentLocation;
				setupQueueTarget.m_Methods = PathMethod.Road | PathMethod.Boarding;
				setupQueueTarget.m_RoadTypes = RoadTypes.Car;
				setupQueueTarget.m_Entity = vehicleSource;
				SetupQueueTarget origin = setupQueueTarget;
				setupQueueTarget = default(SetupQueueTarget);
				setupQueueTarget.m_Type = SetupTargetType.TaxiRequest;
				setupQueueTarget.m_Methods = PathMethod.Road | PathMethod.Boarding;
				setupQueueTarget.m_RoadTypes = RoadTypes.Car;
				SetupQueueTarget destination = setupQueueTarget;
				if (this.m_TaxiData.TryGetComponent(vehicleSource, out var componentData) && (componentData.m_State & TaxiFlags.Transporting) != 0)
				{
					origin.m_Flags |= SetupTargetFlags.PathEnd;
				}
				this.m_PathfindQueue.Enqueue(new SetupQueueItem(requestEntity, parameters, origin, destination));
				this.m_CommandBuffer.AddComponent(jobIndex, requestEntity, default(PathInformation));
			}

			void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
			}
		}

		[BurstCompile]
		private struct DispatchVehiclesJob : IJob
		{
			public NativeQueue<VehicleDispatch> m_VehicleDispatches;

			public ComponentLookup<ServiceRequest> m_ServiceRequestData;

			public BufferLookup<ServiceDispatch> m_ServiceDispatches;

			public void Execute()
			{
				VehicleDispatch item;
				while (this.m_VehicleDispatches.TryDequeue(out item))
				{
					ServiceRequest componentData;
					if (this.m_ServiceDispatches.TryGetBuffer(item.m_Source, out var bufferData))
					{
						bufferData.Add(new ServiceDispatch(item.m_Request));
					}
					else if (this.m_ServiceRequestData.TryGetComponent(item.m_Source, out componentData))
					{
						componentData.m_Flags |= ServiceRequestFlags.SkipCooldown;
						this.m_ServiceRequestData[item.m_Source] = componentData;
					}
				}
			}
		}

		private struct TypeHandle
		{
			[ReadOnly]
			public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<TaxiRequest> __Game_Simulation_TaxiRequest_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Dispatched> __Game_Simulation_Dispatched_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentTypeHandle;

			public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

			public ComponentTypeHandle<ServiceRequest> __Game_Simulation_ServiceRequest_RW_ComponentTypeHandle;

			[ReadOnly]
			public ComponentLookup<TaxiRequest> __Game_Simulation_TaxiRequest_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<CurrentRoute> __Game_Routes_CurrentRoute_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<BoardingVehicle> __Game_Routes_BoardingVehicle_RO_ComponentLookup;

			[ReadOnly]
			public BufferLookup<ServiceDispatch> __Game_Simulation_ServiceDispatch_RO_BufferLookup;

			[ReadOnly]
			public BufferLookup<DispatchedRequest> __Game_Routes_DispatchedRequest_RO_BufferLookup;

			public ComponentLookup<TaxiStand> __Game_Routes_TaxiStand_RW_ComponentLookup;

			public ComponentLookup<TransportDepot> __Game_Buildings_TransportDepot_RW_ComponentLookup;

			public ComponentLookup<Taxi> __Game_Vehicles_Taxi_RW_ComponentLookup;

			public ComponentLookup<RideNeeder> __Game_Creatures_RideNeeder_RW_ComponentLookup;

			public ComponentLookup<ServiceRequest> __Game_Simulation_ServiceRequest_RW_ComponentLookup;

			public BufferLookup<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferLookup;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
				this.__Game_Simulation_TaxiRequest_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TaxiRequest>(isReadOnly: true);
				this.__Game_Simulation_Dispatched_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Dispatched>(isReadOnly: true);
				this.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathInformation>(isReadOnly: true);
				this.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
				this.__Game_Simulation_ServiceRequest_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceRequest>();
				this.__Game_Simulation_TaxiRequest_RO_ComponentLookup = state.GetComponentLookup<TaxiRequest>(isReadOnly: true);
				this.__Game_Routes_CurrentRoute_RO_ComponentLookup = state.GetComponentLookup<CurrentRoute>(isReadOnly: true);
				this.__Game_Routes_BoardingVehicle_RO_ComponentLookup = state.GetComponentLookup<BoardingVehicle>(isReadOnly: true);
				this.__Game_Simulation_ServiceDispatch_RO_BufferLookup = state.GetBufferLookup<ServiceDispatch>(isReadOnly: true);
				this.__Game_Routes_DispatchedRequest_RO_BufferLookup = state.GetBufferLookup<DispatchedRequest>(isReadOnly: true);
				this.__Game_Routes_TaxiStand_RW_ComponentLookup = state.GetComponentLookup<TaxiStand>();
				this.__Game_Buildings_TransportDepot_RW_ComponentLookup = state.GetComponentLookup<TransportDepot>();
				this.__Game_Vehicles_Taxi_RW_ComponentLookup = state.GetComponentLookup<Taxi>();
				this.__Game_Creatures_RideNeeder_RW_ComponentLookup = state.GetComponentLookup<RideNeeder>();
				this.__Game_Simulation_ServiceRequest_RW_ComponentLookup = state.GetComponentLookup<ServiceRequest>();
				this.__Game_Simulation_ServiceDispatch_RW_BufferLookup = state.GetBufferLookup<ServiceDispatch>();
			}
		}

		private EndFrameBarrier m_EndFrameBarrier;

		private SimulationSystem m_SimulationSystem;

		private PathfindSetupSystem m_PathfindSetupSystem;

		private EntityQuery m_RequestQuery;

		private TypeHandle __TypeHandle;

		public override int GetUpdateInterval(SystemUpdatePhase phase)
		{
			return 16;
		}

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			this.m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
			this.m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
			this.m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
			this.m_RequestQuery = base.GetEntityQuery(ComponentType.ReadOnly<TaxiRequest>(), ComponentType.ReadOnly<UpdateFrame>());
			base.RequireForUpdate(this.m_RequestQuery);
		}

		[Preserve]
		protected override void OnUpdate()
		{
			uint num = (this.m_SimulationSystem.frameIndex >> 4) & 0xFu;
			uint nextUpdateFrameIndex = (num + 4) & 0xFu;
			NativeQueue<VehicleDispatch> vehicleDispatches = new NativeQueue<VehicleDispatch>(Allocator.TempJob);
			this.__TypeHandle.__Game_Creatures_RideNeeder_RW_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Vehicles_Taxi_RW_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Buildings_TransportDepot_RW_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Routes_TaxiStand_RW_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Routes_DispatchedRequest_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Simulation_ServiceDispatch_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Routes_BoardingVehicle_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Routes_CurrentRoute_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Simulation_TaxiRequest_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Simulation_ServiceRequest_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Simulation_Dispatched_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Simulation_TaxiRequest_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
			TaxiDispatchJob taxiDispatchJob = default(TaxiDispatchJob);
			taxiDispatchJob.m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
			taxiDispatchJob.m_TaxiRequestType = this.__TypeHandle.__Game_Simulation_TaxiRequest_RO_ComponentTypeHandle;
			taxiDispatchJob.m_DispatchedType = this.__TypeHandle.__Game_Simulation_Dispatched_RO_ComponentTypeHandle;
			taxiDispatchJob.m_PathInformationType = this.__TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle;
			taxiDispatchJob.m_UpdateFrameType = this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle;
			taxiDispatchJob.m_ServiceRequestType = this.__TypeHandle.__Game_Simulation_ServiceRequest_RW_ComponentTypeHandle;
			taxiDispatchJob.m_TaxiRequestData = this.__TypeHandle.__Game_Simulation_TaxiRequest_RO_ComponentLookup;
			taxiDispatchJob.m_CurrentRouteData = this.__TypeHandle.__Game_Routes_CurrentRoute_RO_ComponentLookup;
			taxiDispatchJob.m_BoardingVehicleData = this.__TypeHandle.__Game_Routes_BoardingVehicle_RO_ComponentLookup;
			taxiDispatchJob.m_ServiceDispatches = this.__TypeHandle.__Game_Simulation_ServiceDispatch_RO_BufferLookup;
			taxiDispatchJob.m_DispatchedRequests = this.__TypeHandle.__Game_Routes_DispatchedRequest_RO_BufferLookup;
			taxiDispatchJob.m_TaxiStandData = this.__TypeHandle.__Game_Routes_TaxiStand_RW_ComponentLookup;
			taxiDispatchJob.m_TransportDepotData = this.__TypeHandle.__Game_Buildings_TransportDepot_RW_ComponentLookup;
			taxiDispatchJob.m_TaxiData = this.__TypeHandle.__Game_Vehicles_Taxi_RW_ComponentLookup;
			taxiDispatchJob.m_RideNeederData = this.__TypeHandle.__Game_Creatures_RideNeeder_RW_ComponentLookup;
			taxiDispatchJob.m_UpdateFrameIndex = num;
			taxiDispatchJob.m_NextUpdateFrameIndex = nextUpdateFrameIndex;
			taxiDispatchJob.m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter();
			taxiDispatchJob.m_VehicleDispatches = vehicleDispatches.AsParallelWriter();
			taxiDispatchJob.m_PathfindQueue = this.m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter();
			TaxiDispatchJob jobData = taxiDispatchJob;
			this.__TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Simulation_ServiceRequest_RW_ComponentLookup.Update(ref base.CheckedStateRef);
			DispatchVehiclesJob dispatchVehiclesJob = default(DispatchVehiclesJob);
			dispatchVehiclesJob.m_VehicleDispatches = vehicleDispatches;
			dispatchVehiclesJob.m_ServiceRequestData = this.__TypeHandle.__Game_Simulation_ServiceRequest_RW_ComponentLookup;
			dispatchVehiclesJob.m_ServiceDispatches = this.__TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferLookup;
			DispatchVehiclesJob jobData2 = dispatchVehiclesJob;
			JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, this.m_RequestQuery, base.Dependency);
			JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
			vehicleDispatches.Dispose(jobHandle2);
			this.m_PathfindSetupSystem.AddQueueWriter(jobHandle);
			this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
			base.Dependency = jobHandle2;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void __AssignQueries(ref SystemState state)
		{
		}

		protected override void OnCreateForCompiler()
		{
			base.OnCreateForCompiler();
			this.__AssignQueries(ref base.CheckedStateRef);
			this.__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
		}

		[Preserve]
		public TaxiDispatchSystem()
		{
		}
	}
}
