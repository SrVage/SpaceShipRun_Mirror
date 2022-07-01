using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;
using float4x4 = Unity.Mathematics.float4x4;
using quaternion = Unity.Mathematics.quaternion;
using Random = UnityEngine.Random;

namespace Mechanics
{
    public class Asteroids:PlanetOrbit
    {
        [Header("Asteroid")]
        [SerializeField] private Mesh _mesh;
        [SerializeField] private Material _material;
        [SerializeField] private int _count;
        private NativeArray<Asteroid> _asteroids;
        private ComputeBuffer _matricesBuffers;
        private NativeArray<float4x4> _matrices;
        private MaterialPropertyBlock _propertyBlock;
        private static readonly int _matricesId = Shader.PropertyToID("_Matrices");



        private struct Asteroid
        {
            public float3 Position;
            public float Scale;
            public float Angle;
            public float3 Offset;
        }
        
        [BurstCompile(CompileSynchronously = true, FloatPrecision = FloatPrecision.Standard, FloatMode = FloatMode.Fast)]

        private struct MoveAsteroid:IJobParallelFor
        {
            public NativeArray<Asteroid> Asteroids;
            public NativeArray<float4x4> Matrices;
            public float DeltaTime;
            public float OffsetSin;
            public float OffsetCos;
            public float RotationSpeed;
            public float Radius;
            public float CircleInSecond;

            public void Execute(int index)
            {
                Asteroid p = Asteroids[index];
                p.Position.x = sin(p.Angle)* Radius * OffsetSin;
                p.Position.y = 0;
                p.Position.z =  cos(p.Angle)* Radius * OffsetCos;
                p.Position += p.Offset;
                p.Angle += Mathf.PI * 2 * CircleInSecond * DeltaTime;
                Asteroids[index] = p;
                Matrices[index] = float4x4.TRS(p.Position, quaternion.identity, float3(p.Scale));
            }
        }
        
        private void Start()
        {
            Initiate(UpdatePhase.FixedUpdate);
            var stride = 16 * 4;
            _asteroids = new NativeArray<Asteroid>(_count, Allocator.Persistent);
            _matricesBuffers = new ComputeBuffer(_count, stride);
            _matrices = new NativeArray<float4x4>(_count, Allocator.Persistent);
            for (int i = 0; i < _count; i++)
            {
                var asteroid = new Asteroid()
                {
                    Position = float3.zero,
                    Scale = Random.Range(0.05f, 0.5f),
                    Angle = Random.Range(0, 360f),
                    Offset = Random.insideUnitSphere*20
                };
                _asteroids[i] = asteroid;
            }
            _propertyBlock ??= new MaterialPropertyBlock();

        }

        protected override void HasAuthorityMovement()
        {
        }

        private void OnDisable() 
        {
            _matricesBuffers.Release(); 
            _asteroids.Dispose();
            _matrices.Dispose();
            _matricesBuffers = null;
        }

        protected void Update()
        {
            /*if (!isServer)
                return;*/
            JobHandle jobHandle = default;
            jobHandle = new MoveAsteroid()
            {
                Asteroids = _asteroids,
                CircleInSecond = circleInSecond,
                OffsetCos = offsetCos,
                OffsetSin = offsetSin,
                Radius = radius,
                Matrices = _matrices,
                DeltaTime = Time.deltaTime
            }.Schedule(_asteroids.Length, 5);
            jobHandle.Complete();
            var bounds = new Bounds(aroundPoint, float3(500));
            var buffer = _matricesBuffers;
            buffer.SetData(_matrices);
            _propertyBlock.SetBuffer(_matricesId, buffer);
            _material.SetBuffer(_matricesId, buffer);
            Graphics.DrawMeshInstancedProcedural(_mesh, 0, _material, bounds, buffer.count, _propertyBlock);
            SendToClients();
        }

        protected override void SendToClients()
        {
            serverPosition = transform.position;
            serverEulers = transform.eulerAngles;
        }

        protected override void FromOwnerUpdate()
        {
            if (!isClient)
                return;

            transform.position = Vector3.SmoothDamp(transform.position, serverPosition, ref currentPositionSmoothVelocity, speed);
            transform.rotation = Quaternion.Euler(serverEulers);
        }
    }
}