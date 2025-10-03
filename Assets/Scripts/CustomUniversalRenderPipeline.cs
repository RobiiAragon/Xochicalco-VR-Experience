using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class CustomUniversalRenderPipeline
{
    // ...existing code...
    public static void RenderSingleCameraWithSample(
        ScriptableRenderContext context,
        Camera camera,
        ref UniversalAdditionalCameraData additionalCameraData,
        bool isLastBaseCamera = true)
    {
        CommandBuffer cmd = CommandBufferPool.Get();
        try
        {
            cmd.BeginSample("RenderSingleCamera");
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            UniversalRenderPipeline.RenderSingleCameraInternal(context, camera, ref additionalCameraData, isLastBaseCamera);

            cmd.EndSample("RenderSingleCamera");
            context.ExecuteCommandBuffer(cmd);
        }
        finally
        {
            CommandBufferPool.Release(cmd);
        }
    }
}
            context.ExecuteCommandBuffer(cmd);
        }
        finally
        {
            CommandBufferPool.Release(cmd);
        }
    }
}
