using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using float4x4 = Unity.Mathematics.float4x4;
using quaternion = Unity.Mathematics.quaternion;

namespace Fractal
{
    public class FractalWithBook:MonoBehaviour
    {
        [BurstCompile(CompileSynchronously = true, FloatPrecision = FloatPrecision.Standard, FloatMode = FloatMode.Fast)]
        private struct UpdateFractalLevelJob : IJobFor
        {
            public float SpinAngleDelta;
            public float Scale;
            [ReadOnly] public NativeArray<FractalPart> Parents;
            public NativeArray<FractalPart> Parts;
            [WriteOnly] public NativeArray<float4x4> Matrices;

            public void Execute(int index)
            {
                var parent = Parents[index / _childCount];
                var part = Parts[index];
                part.SpinAngle += SpinAngleDelta;
                part.WorldRotation = mul(parent.WorldRotation, mul(part.Rotation, quaternion.RotateY(part.SpinAngle)));
                part.WorldPosition = parent.WorldPosition + mul(parent.WorldRotation, _positionOffset*Scale*part.Direction);
                Parts[index] = part;
                Matrices[index] = float4x4.TRS(part.WorldPosition, part.WorldRotation, float3(Scale));
            }
        }

        private struct FractalPart 
        {
            public float3 Direction; 
            public quaternion Rotation; 
            public float3 WorldPosition; 
            public quaternion WorldRotation; 
            public float SpinAngle;
        }
        [SerializeField] private Mesh _mesh; 
        [SerializeField] private Material _material;
        [SerializeField, Range(1, 8)] private int _depth = 4; 
        [SerializeField, Range(0, 1)] private float _rotationSpeed;
        private NativeArray<FractalPart>[] _parts;
        private NativeArray<float4x4>[] _matrices;
        private const float _positionOffset = 1.5f; 
        private const float _scaleBias = .5f; 
        private const int _childCount = 5;
        private ComputeBuffer[] _matricesBuffers;
        private static readonly int _matricesId = Shader.PropertyToID("_Matrices"); 
        private static MaterialPropertyBlock _propertyBlock;

        private static readonly float3[] _directions = new float3[] {
            up(), 
            left(), 
            right(), 
            forward(), 
            back(),
        };
        private static readonly quaternion[] _rotations = new quaternion[] {
            quaternion.identity,
            quaternion.RotateZ(.5f * PI),
            quaternion.RotateZ(-.5f * PI),
            quaternion.RotateX(.5f * PI), 
            quaternion.RotateX(-.5f * PI),
        };
        private void OnEnable()
        {
            _parts = new NativeArray<FractalPart>[_depth];
            _matrices = new NativeArray<float4x4>[_depth];
            _matricesBuffers = new ComputeBuffer[_depth];
            var stride = 16 * 4;
            for (int i = 0, length = 1; i < _parts.Length; i++, length *= _childCount)
            {
                _parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent); 
                _matrices[i] = new NativeArray<float4x4>(length, Allocator.Persistent);
                _matricesBuffers[i] = new ComputeBuffer(length, stride);
            }
            _parts[0][0] = CreatePart(0);
            for (var li = 1; li < _parts.Length; li++)
            {
                var levelParts = _parts[li];
                for (var fpi = 0; fpi < levelParts.Length; fpi += _childCount) 
                {
                    for (var ci = 0; ci < _childCount; ci++) 
                    {
                        levelParts[fpi + ci] = CreatePart(ci); 
                    }
                }
            }
            _propertyBlock ??= new MaterialPropertyBlock();
        }
        
        private void OnDisable() 
        {
            for (var i = 0; i < _matricesBuffers.Length; i++) 
            {
                _matricesBuffers[i].Release(); 
                _parts[i].Dispose();
                _matrices[i].Dispose();
            }
            _parts = null; 
            _matrices = null; 
            _matricesBuffers = null;
        }
        private void OnValidate() 
        {
            if (_parts is null || !enabled) 
                return;
            OnDisable();
            OnEnable(); 
        }

        private void Update()
        {
            var spinAngleDelta = _rotationSpeed *PI* Time.deltaTime;
            var rootPart = _parts[0][0];
            rootPart.SpinAngle += spinAngleDelta;
            rootPart.WorldRotation = mul(rootPart.Rotation, quaternion.RotateY(rootPart.SpinAngle));
            _parts[0][0] = rootPart;
            _matrices[0][0] = float4x4.TRS(rootPart.WorldPosition, rootPart.WorldRotation, float3(1));
            var scale = 1.0f;
            JobHandle jobHandle = default;
            for (var li = 1; li < _parts.Length; li++)
            {
                scale *= _scaleBias;
                jobHandle = new UpdateFractalLevelJob
                {
                    SpinAngleDelta = spinAngleDelta,
                    Scale = scale,
                    Parents = _parts[li - 1],
                    Parts = _parts[li],
                    Matrices = _matrices[li]
                }.Schedule(_parts[li].Length, jobHandle);
            }
            jobHandle.Complete();
            var bounds = new Bounds(rootPart.WorldPosition, float3(3));
            for (var i = 0; i < _matricesBuffers.Length; i++)
            {
                var buffer = _matricesBuffers[i];
                buffer.SetData(_matrices[i]);
                _propertyBlock.SetBuffer(_matricesId, buffer);
                _material.SetBuffer(_matricesId, buffer);
                Graphics.DrawMeshInstancedProcedural(_mesh, 0, _material, bounds, buffer.count, _propertyBlock);
            }
        }
        private FractalPart CreatePart(int childIndex) => new FractalPart {
            Direction = _directions[childIndex],
            Rotation = _rotations[childIndex], };
    }
}