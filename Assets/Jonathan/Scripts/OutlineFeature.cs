using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// Soft outline around everything on the outline layer (replaces UnityFx.Outline for Unity 6)
public class OutlineFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class OutlineSettings {
        public LayerMask layerMask;
        public Color color = Color.red;
        [Range(1, 32)]
        public int width = 8;
        [Range(0.1f, 10f)]
        public float intensity = 3f;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    [SerializeField] private Shader shader;
    [SerializeField] private OutlineSettings settings = new OutlineSettings();

    private Material material;
    private OutlinePass outlinePass;

    public override void Create() {
        if (shader == null) {
            return;
        }

        material = CoreUtils.CreateEngineMaterial(shader);
        outlinePass = new OutlinePass(material, settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        if (outlinePass == null) {
            return;
        }

        var cameraType = renderingData.cameraData.cameraType;
        if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection) {
            return;
        }

        renderer.EnqueuePass(outlinePass);
    }

    protected override void Dispose(bool disposing) {
        CoreUtils.Destroy(material);
    }

    class OutlinePass : ScriptableRenderPass
    {
        private const int MASK_PASS = 0;
        private const int BLUR_H_PASS = 1;
        private const int BLUR_V_PASS = 2;
        private const int COMPOSITE_PASS = 3;

        private static readonly int colorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int widthId = Shader.PropertyToID("_OutlineWidth");
        private static readonly int intensityId = Shader.PropertyToID("_OutlineIntensity");
        private static readonly int texelSizeId = Shader.PropertyToID("_OutlineTexelSize");
        private static readonly int maskId = Shader.PropertyToID("_OutlineMask");

        private readonly List<ShaderTagId> shaderTags = new List<ShaderTagId> {
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit")
        };

        private Material material;
        private OutlineSettings settings;

        class MaskData {
            public RendererListHandle rendererList;
        }

        class BlitData {
            public TextureHandle source;
            public TextureHandle mask;
            public Material material;
            public int pass;
        }

        public OutlinePass(Material material, OutlineSettings settings) {
            this.material = material;
            this.settings = settings;
            renderPassEvent = settings.renderPassEvent;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            var resourceData = frameData.Get<UniversalResourceData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var lightData = frameData.Get<UniversalLightData>();

            if (resourceData.isActiveTargetBackBuffer) {
                return;
            }

            var desc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
            desc.format = GraphicsFormat.R8_UNorm;
            desc.depthBufferBits = DepthBits.None;
            desc.msaaSamples = MSAASamples.None;
            desc.clearBuffer = true;
            desc.clearColor = Color.clear;

            desc.name = "_OutlineMask";
            var mask = renderGraph.CreateTexture(desc);
            desc.name = "_OutlineBlurH";
            var blurH = renderGraph.CreateTexture(desc);
            desc.name = "_OutlineBlurV";
            var blurV = renderGraph.CreateTexture(desc);

            material.SetColor(colorId, settings.color);
            material.SetFloat(widthId, settings.width);
            material.SetFloat(intensityId, settings.intensity);
            material.SetVector(texelSizeId, new Vector4(1f / desc.width, 1f / desc.height, desc.width, desc.height));

            // draw everything on the outline layer as a white silhouette
            var drawSettings = RenderingUtils.CreateDrawingSettings(shaderTags, renderingData, cameraData, lightData, SortingCriteria.None);
            drawSettings.overrideMaterial = material;
            drawSettings.overrideMaterialPassIndex = MASK_PASS;
            var filterSettings = new FilteringSettings(RenderQueueRange.all, settings.layerMask);

            using (var builder = renderGraph.AddRasterRenderPass<MaskData>("Outline Mask", out var data)) {
                data.rendererList = renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawSettings, filterSettings));
                builder.UseRendererList(data.rendererList);
                builder.SetRenderAttachment(mask, 0);
                builder.SetRenderFunc((MaskData d, RasterGraphContext ctx) => ctx.cmd.DrawRendererList(d.rendererList));
            }

            // blur the silhouette, then draw the part outside of it on top of the scene
            AddBlit(renderGraph, "Outline Blur H", mask, blurH, BLUR_H_PASS, TextureHandle.nullHandle, AccessFlags.Write);
            AddBlit(renderGraph, "Outline Blur V", blurH, blurV, BLUR_V_PASS, TextureHandle.nullHandle, AccessFlags.Write);
            AddBlit(renderGraph, "Outline Composite", blurV, resourceData.activeColorTexture, COMPOSITE_PASS, mask, AccessFlags.ReadWrite);
        }

        private void AddBlit(RenderGraph renderGraph, string name, TextureHandle source, TextureHandle target, int pass, TextureHandle mask, AccessFlags targetAccess) {
            using (var builder = renderGraph.AddRasterRenderPass<BlitData>(name, out var data)) {
                data.source = source;
                data.mask = mask;
                data.material = material;
                data.pass = pass;

                builder.UseTexture(source);
                if (mask.IsValid()) {
                    builder.UseTexture(mask);
                }
                builder.SetRenderAttachment(target, 0, targetAccess);

                builder.SetRenderFunc((BlitData d, RasterGraphContext ctx) => {
                    if (d.mask.IsValid()) {
                        d.material.SetTexture(maskId, d.mask);
                    }
                    Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1, 1, 0, 0), d.material, d.pass);
                });
            }
        }
    }
}
