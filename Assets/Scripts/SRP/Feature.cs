using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SRP
{
    public class Feature:ScriptableRenderPass
    {
        RenderTargetIdentifier colorBuffer, temporaryBuffer;
        Material material;
        const string ProfilerTag = "Template Pass";


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(); 
            using (new ProfilingScope(cmd, new ProfilingSampler(ProfilerTag)))
            {
                // Blit from the color buffer to a temporary buffer and back. This is needed for a two-pass shader.
                Blit(cmd, colorBuffer, temporaryBuffer, material, 0); // shader pass 0
                Blit(cmd, temporaryBuffer, colorBuffer, material, 1); // shader pass 1
            }

            // Execute the command buffer and release it.
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}