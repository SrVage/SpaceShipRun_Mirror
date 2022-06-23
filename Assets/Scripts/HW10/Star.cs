using System;
using UnityEngine;

namespace HW10
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [ExecuteInEditMode]
    public class Star : MonoBehaviour
    {
        [SerializeField] private ColorPoint _center;
        [SerializeField] public ColorPoint[] Points;
        [SerializeField] public int Frequency = 10;
        private Vector3[] _vertices;
        private Color[] _colors;
        private int[] _triangles;
        private Mesh _mesh;

        void OnEnable() =>
            UpdateMesh();

        private void Reset() =>
            UpdateMesh();


        public void UpdateMesh()
        {
            GetComponent<MeshFilter>().mesh = _mesh = new Mesh();
            _mesh.name = "Star Mesh";
            if (Frequency < 1)
            {
                Frequency = 1;
            }

            Points ??= Array.Empty<ColorPoint>();
            var numberOfPoints = Frequency * Points.Length;
            if (_vertices == null || _vertices.Length != numberOfPoints + 1)
            {
                _vertices = new Vector3[numberOfPoints + 1];
                _colors = new Color[numberOfPoints + 1];
                _triangles = new int[numberOfPoints * 3];
                _mesh.Clear();
            }

            if (numberOfPoints >= 3)
            {
                _vertices[0] = _center.Position;
                _colors[0] = _center.Color;
                var angle = -360f / numberOfPoints;
                for (int repetitions = 0, v = 1, t = 1; repetitions < Frequency; repetitions++)
                {
                    for (var p = 0; p < Points.Length; p++, v++, t += 3)
                    {
                        _vertices[v] = Quaternion.Euler(0f, 0f, angle * (v - 1)) * Points[p].Position;
                        _colors[v] = Points[p].Color;
                        _triangles[t] = v;
                        _triangles[t + 1] = v + 1;
                    }
                }

                _triangles[_triangles.Length - 1] = 1;
            }

            _mesh.vertices = _vertices;
            _mesh.colors = _colors;
            _mesh.triangles = _triangles;
        }
    }
}