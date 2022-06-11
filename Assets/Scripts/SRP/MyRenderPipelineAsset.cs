using UnityEngine;
using UnityEngine.Rendering;

namespace SRP
{
    [CreateAssetMenu(menuName = "Rendering/MyRenderPipelineAsset")]
    public class MyRenderPipelineAsset:RenderPipelineAsset
    {
        public int value;
        protected override RenderPipeline CreatePipeline()
        {
            return new MyRenderPipeline();
        }
    }
}