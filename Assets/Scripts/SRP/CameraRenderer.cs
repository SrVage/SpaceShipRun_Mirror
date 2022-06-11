using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SRP
{
    public partial class CameraRenderer
    {
        private static readonly List<ShaderTagId> drawingShaderTagIds =
            new List<ShaderTagId>
            {
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("SRPDefaultUnlit2"),
                new ShaderTagId("SRPDefaultUnlit3"),
                new ShaderTagId("SRPDefaultUnlit4"),
            };
        private const string bufferName = "Camera Render";
        private ScriptableRenderContext _scriptableRenderContext;
        private Camera _camera;
        private readonly CommandBuffer _commandBuffer = new CommandBuffer {name = bufferName};
        private CullingResults _cullingResults;

        public void Render(ScriptableRenderContext context, Camera camera)
        {
            _scriptableRenderContext = context;
            _camera = camera;
            SetBufferName();
            DrawUI();
            if (!Cull(out var parameters))
                return;
            Settings(parameters);
            DrawVisible();
            DrawUnsupportedShaders();
            DrawGizmos();
            Submit();
        }

        private void DrawVisible()
        {
            var drawingSettings = CreateDrawingSettings(drawingShaderTagIds, SortingCriteria.CommonOpaque, out var sortingSettings);
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
            _scriptableRenderContext.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
            _scriptableRenderContext.DrawSkybox(_camera);
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;
            _scriptableRenderContext.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }

        private bool Cull(out ScriptableCullingParameters cullingParameters) => 
            _camera.TryGetCullingParameters(out cullingParameters);

        private void Settings(ScriptableCullingParameters parameters) {
            _cullingResults = _scriptableRenderContext.Cull(ref parameters);
            _scriptableRenderContext.SetupCameraProperties(_camera);
            _commandBuffer.ClearRenderTarget(true,true,Color.clear);
            _commandBuffer.BeginSample(bufferName);
            ExecuteCommandBuffer();
        }
        
        private void Submit() {
            _commandBuffer.EndSample(bufferName);
            ExecuteCommandBuffer();
            _scriptableRenderContext.Submit();
        }
        private void ExecuteCommandBuffer() {
            _scriptableRenderContext.ExecuteCommandBuffer(_commandBuffer);
            _commandBuffer.Clear(); 
        }
        
        private DrawingSettings CreateDrawingSettings(List<ShaderTagId> shaderTags, SortingCriteria sortingCriteria, out SortingSettings sortingSettings)
        {
            sortingSettings = new SortingSettings(_camera) {criteria = sortingCriteria, };
            var drawingSettings = new DrawingSettings(shaderTags[0], sortingSettings); 
            
            for (var i = 1; i < shaderTags.Count; i++)
            {
                drawingSettings.SetShaderPassName(i, shaderTags[i]); 
            }
            return drawingSettings; 
        }
    }

    public partial class CameraRenderer
    {
        partial void DrawUnsupportedShaders();
        partial void DrawGizmos();
        partial void DrawUI();
        partial void SetBufferName();
#if UNITY_EDITOR
        private static readonly ShaderTagId[] _legacyShaderTagIds = {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"), new ShaderTagId("PrepassBase"), new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"), new ShaderTagId("VertexLM"),
        };
        private static Material _errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        
        partial void DrawUnsupportedShaders() {
            var drawingSettings = new DrawingSettings(_legacyShaderTagIds[0], new SortingSettings(_camera))
            {
                overrideMaterial = _errorMaterial,
            };
            for (var i = 1; i < _legacyShaderTagIds.Length; i++) {
                drawingSettings.SetShaderPassName(i, _legacyShaderTagIds[i]); }
            var filteringSettings = FilteringSettings.defaultValue;
            _scriptableRenderContext.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }
        
        partial void DrawGizmos() {
            if (!Handles.ShouldRenderGizmos()) {
                return; }
            _scriptableRenderContext.DrawGizmos(_camera, GizmoSubset.PreImageEffects);
            _scriptableRenderContext.DrawGizmos(_camera, GizmoSubset.PostImageEffects); 
        }

        partial void DrawUI()
        {
            if (_camera.cameraType==CameraType.SceneView)
                ScriptableRenderContext.EmitWorldGeometryForSceneView(_camera);
        }

        partial void SetBufferName()
        {
            _commandBuffer.name = _camera.name;
        }
#endif
    }
}