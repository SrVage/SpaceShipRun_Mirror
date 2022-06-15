using UnityEngine;

namespace Fractal
{
    public class Fractal:MonoBehaviour
    {
        private FractalPart[][] _parts;
        [SerializeField, Range(1, 8)] private int _depth = 4;
        [SerializeField, Range(1, 360)] private int _rotationSpeed; 
        private const float _positionOffset = 1f;
        private const float _scaleBias = 0.5f;
        private const int _childCount = 5;
        [SerializeField] private Mesh mesh;
        [SerializeField] private Material material;
        private struct FractalPart 
        {
            public Vector3 Direction;
            public Quaternion Rotation;
            public Transform Transform;
        }
        private static readonly Vector3[] _directions = new Vector3[] {
            Vector3.up, 
            Vector3.left, 
            Vector3.right, 
            Vector3.forward, 
            Vector3.back,
        };
        private static readonly Quaternion[] _rotations = new Quaternion[] {
            Quaternion.identity, 
            Quaternion.Euler(0f, 0f, 90f), 
            Quaternion.Euler(0f, 0f, -90f), 
            Quaternion.Euler(90f, 0f, 0f), 
            Quaternion.Euler(-90f, 0f, 0f),
        };
        private void OnEnable() {
            _parts = new FractalPart[_depth][];
            for (int i = 0, length = 1; i < _parts.Length; i++, length *= _childCount)
            {
                _parts[i] = new FractalPart[length];
            }
            var scale = 1f;
            _parts[0][0] = CreatePart(0, 0, scale);
            for (var li = 1; li < _parts.Length; li++) 
            {
                scale *= _scaleBias;
                var levelParts = _parts[li];
                for (var fpi = 0; fpi < levelParts.Length; fpi += _childCount) {
                    for (var ci = 0; ci < _childCount; ci++)
                    {
                        levelParts[fpi + ci] = CreatePart(li, ci, scale);
                    }
                }
            } 
        }

        private FractalPart CreatePart(int levelIndex, int childIndex, float scale)
        {
            var go = new GameObject($"Fractal Path L{levelIndex} C{childIndex}"); 
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one*scale;
            go.AddComponent<MeshFilter>().mesh = mesh; 
            go.AddComponent<MeshRenderer>().material = material;
            return new FractalPart() {
                Direction = _directions[childIndex], 
                Rotation = _rotations[childIndex], 
                Transform = go.transform
            };
        }

        private void Update()
        {
            var deltaRotation = Quaternion.Euler(0f, _rotationSpeed * Time.deltaTime, 0f);
            var rootPart = _parts[0][0];
            rootPart.Rotation *= deltaRotation;
            rootPart.Transform.localRotation = rootPart.Rotation;
            _parts[0][0] = rootPart;
            for (var li = 1; li < _parts.Length; li++)
            {
                var parentParts = _parts[li - 1];
                var levelParts = _parts[li];
                for (var fpi = 0; fpi < levelParts.Length; fpi++)
                {
                    var parentTransform = parentParts[fpi / _childCount].Transform;
                    var part = levelParts[fpi];
                    part.Rotation *= deltaRotation;
                    part.Transform.localRotation =
                        parentTransform.localRotation * part.Rotation;
                    part.Transform.localPosition =
                        parentTransform.localPosition + parentTransform.localRotation *
                        (_positionOffset * part.Transform.localScale.x * part.Direction);
                    levelParts[fpi] = part;
                }
            }
        }
    }
}