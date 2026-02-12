using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class OutlineRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class OutlineSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        public LayerMask outlineLayerMask = -1;
        [ColorUsage(true, true)]
        public Color outlineColor = Color.black;
        [Range(0f, 10f)]
        public float outlineWidth = 2f;
    }

    public OutlineSettings settings = new OutlineSettings();
    
    private OutlineRenderPass outlinePass;
    private Material outlineMaterial;

    public override void Create()
    {
        var shader = Shader.Find("Hidden/OutlineEffect");
        if (shader != null)
        {
            outlineMaterial = CoreUtils.CreateEngineMaterial(shader);
        }
        
        outlinePass = new OutlineRenderPass(settings, outlineMaterial);
        outlinePass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (outlineMaterial == null) return;
        
        outlinePass.Setup(renderer);
        renderer.EnqueuePass(outlinePass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(outlineMaterial);
    }
}

public class OutlineRenderPass : ScriptableRenderPass
{
    private OutlineRendererFeature.OutlineSettings settings;
    private Material outlineMaterial;
    private RTHandle tempRT;
    private RTHandle maskRT;
    private ScriptableRenderer renderer;

    private static readonly int OutlineColorID = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineWidthID = Shader.PropertyToID("_OutlineWidth");
    private static readonly int MaskTexID = Shader.PropertyToID("_MaskTex");

    public OutlineRenderPass(OutlineRendererFeature.OutlineSettings settings, Material material)
    {
        this.settings = settings;
        this.outlineMaterial = material;
    }

    public void Setup(ScriptableRenderer renderer)
    {
        this.renderer = renderer;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        var desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        
        RenderingUtils.ReAllocateIfNeeded(ref tempRT, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TempOutlineRT");
        RenderingUtils.ReAllocateIfNeeded(ref maskRT, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_MaskRT");
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (outlineMaterial == null) return;

        CommandBuffer cmd = CommandBufferPool.Get("Outline Effect");

        var cameraColorTarget = renderer.cameraColorTargetHandle;

        // Set material properties
        outlineMaterial.SetColor(OutlineColorID, settings.outlineColor);
        outlineMaterial.SetFloat(OutlineWidthID, settings.outlineWidth);

        // Pass 0: Create mask of outlined objects
        Blitter.BlitCameraTexture(cmd, cameraColorTarget, maskRT, outlineMaterial, 0);
        
        // Pass 1: Apply outline based on mask edges
        cmd.SetGlobalTexture(MaskTexID, maskRT);
        Blitter.BlitCameraTexture(cmd, cameraColorTarget, tempRT, outlineMaterial, 1);
        Blitter.BlitCameraTexture(cmd, tempRT, cameraColorTarget);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
    }

    public void Dispose()
    {
        tempRT?.Release();
        maskRT?.Release();
    }
}
