using UnityEngine;
using UnityEngine.Rendering;

namespace SRP
{
    public class MyRenderPipeline:RenderPipeline
    {
        private CameraRenderer _cameraRenderer=new CameraRenderer();
        
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (var camera in cameras)
            {
                _cameraRenderer.Render(context, camera);
            }
        }
    }
}