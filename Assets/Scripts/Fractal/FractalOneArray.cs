using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using float4x4 = Unity.Mathematics.float4x4;
using quaternion = Unity.Mathematics.quaternion;

namespace Fractal
{
    public class FractalOneArray:MonoBehaviour
    {
        [BurstCompile(CompileSynchronously = true, FloatPrecision = FloatPrecision.Standard, FloatMode = FloatMode.Fast)]
        private struct UpdateFractalLevelJob : IJobParallelFor
        {
            public float SpinAngleDelta;
            public NativeArray<FractalPart> Parts;
            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public NativeArray<FractalPart> Parents;
            [WriteOnly] public NativeArray<float4x4> MatricesHigh;
            [NativeDisableContainerSafetyRestriction]
            [WriteOnly] public NativeArray<float4x4> MatricesLow;

            public void Execute(int index)
            {
                if (index==0)
                    return;
                var part = Parts[index];
                int parentIndex = Parents[index].Parent;
                part.SpinAngle += SpinAngleDelta;
                part.WorldRotation = mul(Parents[parentIndex].WorldRotation, mul(part.Rotation, quaternion.RotateY(part.SpinAngle)));
                part.WorldPosition = Parents[parentIndex].WorldPosition + mul(Parents[parentIndex].WorldRotation, _positionOffset*part.Scale*part.Direction);
                Parts[index] = part;
                if (part.Mesh==0) 
                    MatricesHigh[index] = float4x4.TRS(part.WorldPosition, part.WorldRotation, float3(part.Scale));
                else 
                    MatricesLow[index-MatricesHigh.Length] = float4x4.TRS(part.WorldPosition, part.WorldRotation, float3(part.Scale));
            }
        }

        private struct FractalPart 
        {
            public float3 Direction; 
            public quaternion Rotation; 
            public float3 WorldPosition; 
            public quaternion WorldRotation; 
            public float SpinAngle;
            public float Scale;
            public int Parent;
            public int Mesh;
            
            public override string ToString() => 
                $"{SpinAngle}, {Scale}, {Parent}, {Direction}, {WorldPosition}";
        }
        [SerializeField] private Mesh[] _mesh; 
        [SerializeField] private Material _material;
        [SerializeField, Range(1, 10)] private int _depth = 4; 
        [SerializeField, Range(0, 1)] private float _rotationSpeed;
        [SerializeField] [Range(0,20)] private int _innerloopBatchCount = 20;
        [SerializeField] [Range(1, 8)] private int _depthOfLowPoly;
        private List<FractalPart> _fractalParts = new List<FractalPart>();
        private NativeArray<FractalPart> _parts;
        private NativeArray<FractalPart>[] _partsCreate;
        private NativeArray<float4x4> _matricesHigh;
        private NativeArray<float4x4> _matricesLow;
        private const float _positionOffset = 1.5f;
        private const float _scaleBias = .5f;
        private const int _childCount = 5;
        private ComputeBuffer _matricesBuffersHigh;
        private ComputeBuffer _matricesBuffersLow;
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
            var stride = 16 * 4;
            _partsCreate = new NativeArray<FractalPart>[_depth];
            for (int i = 0, length = 1; i < _depth; i++, length *= _childCount)
                _partsCreate[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
            float scale = 1;
            CreateRootPart(scale);
            CreateFractal(scale);
            _parts = new NativeArray<FractalPart>(_fractalParts.Count, Allocator.Persistent);
            _matricesHigh = new NativeArray<float4x4>(_fractalParts.Count(k => k.Mesh==0), Allocator.Persistent);
            _matricesLow = new NativeArray<float4x4>(_fractalParts.Count(k => k.Mesh!=0), Allocator.Persistent);
            _matricesBuffersHigh = new ComputeBuffer(_fractalParts.Count(k => k.Mesh==0), stride);
            _matricesBuffersLow = new ComputeBuffer(_fractalParts.Count(k => k.Mesh!=0), stride);
            AddParentsToChild();
            _propertyBlock ??= new MaterialPropertyBlock();
            ClearUnusedLists();
        }

        private void ClearUnusedLists()
        {
            _fractalParts.Clear();
            _fractalParts = null;
            foreach (var part in _partsCreate)
                part.Dispose();
        }

        private void AddParentsToChild()
        {
            int k = 0;
            for (int i = 0; i < _partsCreate.Length; i++)
            {
                var part = _partsCreate[i];
                var parent = _partsCreate[i > 0 ? i - 1 : 0];
                for (int j = 0; j < part.Length; j++)
                {
                    var par = part[j];
                    par.Parent = parent[j / _childCount].Parent;
                    _parts[k++] = par;
                }
            }
        }

        private void CreateFractal(float scale)
        {
            int fp = 1;
            for (var li = 1; li < _partsCreate.Length; li++)
            {
                scale *= _scaleBias;
                var levelParts = _partsCreate[li];
                for (var fpi = 0; fpi < levelParts.Length; fpi += _childCount)
                {
                    for (var ci = 0; ci < _childCount; ci++)
                    {
                        levelParts[fpi + ci] = CreatePart(ci, scale, fp++, li/_depthOfLowPoly);
                        _fractalParts.Add(CreatePart(ci, scale, fp + fpi, li/_depthOfLowPoly));
                    }
                }
            }
        }

        private void CreateRootPart(float scale)
        {
            _partsCreate[0][0] = CreatePart(0, scale, 0, 0);
            _fractalParts.Add(CreatePart(0, scale, 0, 0));
        }

        private void OnDisable() 
        {
            _matricesBuffersHigh.Release(); 
            _matricesBuffersLow.Release(); 
            _parts.Dispose();
            _matricesHigh.Dispose();
            _matricesLow.Dispose();
            _matricesBuffersHigh = null;
            _matricesBuffersLow = null;
        }

        private void Update()
        {
            var spinAngleDelta = _rotationSpeed *PI* Time.deltaTime; 
            var rootPart = UpdateRootPart(spinAngleDelta);
            JobHandle jobHandle = default;
            jobHandle = new UpdateFractalLevelJob
            {
                SpinAngleDelta = spinAngleDelta,
                Parts = _parts,
                Parents = _parts,
                MatricesHigh = _matricesHigh,
                MatricesLow = _matricesLow
            }.Schedule(_parts.Length, _innerloopBatchCount);
            jobHandle.Complete();
            var bounds = new Bounds(rootPart.WorldPosition, float3(3));
            var buffer = _matricesBuffersHigh;
            buffer.SetData(_matricesHigh);
            _propertyBlock.SetBuffer(_matricesId, buffer);
            _material.SetBuffer(_matricesId, buffer);
            Graphics.DrawMeshInstancedProcedural(_mesh[0], 0, _material, bounds, buffer.count, _propertyBlock);
            buffer = _matricesBuffersLow;
            buffer.SetData(_matricesLow);
            _propertyBlock.SetBuffer(_matricesId, buffer);
            _material.SetBuffer(_matricesId, buffer);
            Graphics.DrawMeshInstancedProcedural(_mesh[1], 0, _material, bounds, buffer.count, _propertyBlock);
        }

        private FractalPart UpdateRootPart(float spinAngleDelta)
        {
            var rootPart = _parts[0];
            rootPart.SpinAngle += spinAngleDelta;
            rootPart.WorldRotation = mul(rootPart.Rotation, quaternion.RotateY(rootPart.SpinAngle));
            _parts[0] = rootPart;
            _matricesHigh[0] = float4x4.TRS(rootPart.WorldPosition, rootPart.WorldRotation, float3(1));
            return rootPart;
        }

        private FractalPart CreatePart(int childIndex, float scale, int parent, int mesh) => new FractalPart {
            Direction = _directions[childIndex],
            Rotation = _rotations[childIndex],
            Scale = scale,
            Parent = parent,
            Mesh = mesh
        };
    }
}