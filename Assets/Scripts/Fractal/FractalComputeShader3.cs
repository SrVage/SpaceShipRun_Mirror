using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using float4x4 = Unity.Mathematics.float4x4;
using Matrix4x4 = System.Numerics.Matrix4x4;
using quaternion = Unity.Mathematics.quaternion;

namespace Fractal
{
    public class FractalComputeShader3:MonoBehaviour
    {
        
        struct Part
        {
            float3 Direction; 
            float4 Rotation; 
            float3 WorldPosition; 
            float4 WorldRotation; 
            float SpinAngle;
            float3 ParentPosition;
            float4 ParentRotation;
            float Scale;
        };
        
        private struct FractalPart 
        {
            public float3 Direction; 
            public quaternion Rotation; 
            public float3 WorldPosition; 
            public quaternion WorldRotation; 
            public float SpinAngle;
            public float Scale;
            public int Parent;
            
            public override string ToString() => 
                $"{SpinAngle}, {Scale}, {Parent}, {Direction}, {WorldPosition}";
        }
        [SerializeField] private Mesh _mesh; 
        [SerializeField] private Material _material;
        [SerializeField, Range(1, 8)] private int _depth = 4; 
        [SerializeField, Range(0, 1)] private float _rotationSpeed;
        private List<FractalPart> _fractalParts = new List<FractalPart>();
        private FractalPart[] _parts;
        private FractalPart[][] _partsCreate;
        private float4x4[] _matrices;
        private const float _positionOffset = 1.5f; 
        private const float _scaleBias = .5f; 
        private const int _childCount = 5;
        private ComputeBuffer _matricesBuffers;
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

        private ComputeShader _shader;
        private int _kernelIndex;
        private uint _threadGroupSize;
        private int _objectsCount;
        private ComputeBuffer _buffer;

        private void OnEnable()
        {
            var stride = 16 * 4;
            _partsCreate = new FractalPart[_depth][];
            for (int i = 0, length = 1; i < _depth; i++, length *= _childCount)
                _partsCreate[i] = new FractalPart[length];
            float scale = 1;
            _partsCreate[0][0] = CreatePart(0, scale, 0);
            _fractalParts.Add(CreatePart(0, scale, 0));
            int fp = 1;
            for (var li = 1; li < _partsCreate.Length; li++)
            {
                scale *= _scaleBias;
                var levelParts = _partsCreate[li];
                for (var fpi = 0; fpi < levelParts.Length; fpi += _childCount)
                {
                    for (var ci = 0; ci < _childCount; ci++) 
                    {
                        levelParts[fpi + ci] = CreatePart(ci, scale, fp++);
                        _fractalParts.Add(CreatePart(ci, scale, fp+fpi));
                    }
                }
            }

            _objectsCount = _fractalParts.Count;
            _parts = new FractalPart[_fractalParts.Count];
            _matrices = new float4x4[_fractalParts.Count];
            _matricesBuffers = new ComputeBuffer(_fractalParts.Count, stride);

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
            _propertyBlock ??= new MaterialPropertyBlock();
            _kernelIndex = _shader.FindKernel("CSMain");
            _shader.GetKernelThreadGroupSizes(_kernelIndex, out _threadGroupSize, out _, out _);
            _objectsCount *= (int)_threadGroupSize;
            _buffer = new ComputeBuffer(_objectsCount, sizeof(float) * 3);
            _shader.SetFloat("PositionOffset", _positionOffset);
        }

        private void OnDisable() 
        {
            _matricesBuffers.Release();
        }

        private void Update()
        {
            var spinAngleDelta = _rotationSpeed *PI* Time.deltaTime; 
            var rootPart = _parts[0];
            rootPart.SpinAngle += spinAngleDelta;
            rootPart.WorldRotation = mul(rootPart.Rotation, quaternion.RotateY(rootPart.SpinAngle));
            _parts[0] = rootPart;
            _matrices[0] = float4x4.TRS(rootPart.WorldPosition, rootPart.WorldRotation, float3(1));
            
            _shader.SetFloat("SpinAngleDelta", spinAngleDelta);
            
            var bounds = new Bounds(_parts[0].WorldPosition, float3(3));
            var buffer = _matricesBuffers;
            buffer.SetData(_matrices);
            _propertyBlock.SetBuffer(_matricesId, buffer);
            _material.SetBuffer(_matricesId, buffer);
            Graphics.DrawMeshInstancedProcedural(_mesh, 0, _material, bounds, buffer.count, _propertyBlock);
        }
        private FractalPart CreatePart(int childIndex, float scale, int parent) => new FractalPart {
            Direction = _directions[childIndex],
            Rotation = _rotations[childIndex],
            Scale = scale,
            Parent = parent
        };
    }
}