using UnityEditor;
using UnityEngine;

namespace HW10.Editor
{
    [CustomEditor(typeof(Star)), CanEditMultipleObjects]
    public class StarEditor : UnityEditor.Editor
    {
        private SerializedProperty _center;
        private SerializedProperty Points;
        private SerializedProperty Frequency;
        private Vector3 _pointSnap = Vector3.one;

        private void OnEnable()
        {
            _center = serializedObject.FindProperty(nameof(_center));
            Points = serializedObject.FindProperty(nameof(Points));
            Frequency = serializedObject.FindProperty(nameof(Frequency));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_center);
            EditorGUILayout.PropertyField(Points);
            EditorGUILayout.IntSlider(Frequency, 1, 20);
            var totalPoints = Frequency.intValue * Points.arraySize;
            if (totalPoints < 3)
                EditorGUILayout.HelpBox("You need greater than 3 points", MessageType.Warning);
            else
                EditorGUILayout.HelpBox(totalPoints + " total points", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
            if (serializedObject.ApplyModifiedProperties() &&
                (Event.current.type != EventType.ExecuteCommand ||
                 Event.current.commandName != "UndoRedoPerformed"))
                return;
            foreach (var obj in targets)
            {
                if (obj is Star star)
                    star.UpdateMesh();
            }
        }
        
        public void OnSceneGUI()
        {
            if (!(target is Star star)) {
                return; }
            var starTransform = star.transform;
            var angle = -360f / (star.Frequency * star.Points.Length);
            for (var i = 0; i < star.Points.Length; i++)
            {
                var rotation = Quaternion.Euler(0f, 0f, angle * i);
                var oldPoint = starTransform.TransformPoint(rotation * star.Points[i].Position);
                var newPoint = Handles.FreeMoveHandle(oldPoint, Quaternion.identity, 0.02f, _pointSnap,
                    Handles.DotHandleCap);
                if (oldPoint == newPoint)
                {
                    continue;
                }

                star.Points[i].Position = Quaternion.Inverse(rotation) * starTransform.InverseTransformPoint(newPoint);
                star.UpdateMesh();
            }
        }
    }
}