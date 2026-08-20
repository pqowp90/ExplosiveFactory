using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace ExplosiveFactory
{
    public class OutlineRenderFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class OutlineSettings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            public Shader? silhouetteShader;
            public Shader? compositeShader;
        }

        [SerializeField] private OutlineSettings settings = new();

        private Material? _silhouetteMaterial;
        private Material? _compositeMaterial;
        private OutlinePass? _outlinePass;

        public override void Create()
        {
            EnsureMaterials();
            _outlinePass = new OutlinePass(settings.renderPassEvent, this);
        }

        private void EnsureMaterials()
        {
            if (settings.silhouetteShader == null)
                settings.silhouetteShader = Shader.Find("Hidden/OutlineSilhouette");
            if (settings.compositeShader == null)
                settings.compositeShader = Shader.Find("Hidden/OutlineComposite");

            if (_silhouetteMaterial == null && settings.silhouetteShader != null)
                _silhouetteMaterial = CoreUtils.CreateEngineMaterial(settings.silhouetteShader);
            if (_compositeMaterial == null && settings.compositeShader != null)
                _compositeMaterial = CoreUtils.CreateEngineMaterial(settings.compositeShader);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // 게임 뷰 또는 씬 뷰가 아니면 렌더 패스 생략
            if (renderingData.cameraData.cameraType != CameraType.Game &&
                renderingData.cameraData.cameraType != CameraType.SceneView)
            {
                return;
            }

            var manager = global::OutlineManager.Instance;
            if (manager == null || manager.ActiveRenderers.Count == 0)
            {
                return;
            }

            EnsureMaterials();

            if (_outlinePass != null && _silhouetteMaterial != null && _compositeMaterial != null)
            {
                renderer.EnqueuePass(_outlinePass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _outlinePass?.Dispose();
            CoreUtils.Destroy(_silhouetteMaterial);
            CoreUtils.Destroy(_compositeMaterial);
        }

        private class OutlinePass : ScriptableRenderPass
        {
            private readonly OutlineRenderFeature _feature;
            private readonly ProfilingSampler _profilingSampler = new("OutlineEffect");

            private static readonly int MaskTexId = Shader.PropertyToID("_MaskTex");
            private static readonly int MaskTexTexelSizeId = Shader.PropertyToID("_MaskTex_TexelSize");
            private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
            private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

            private RTHandle? _legacyMaskTextureHandle;

            // RenderGraph용 PassData
            private class SilhouettePassData
            {
                public Material silhouetteMaterial = null!;
                public readonly List<Renderer> renderers = new();
            }

            private class CompositePassData
            {
                public Material compositeMaterial = null!;
                public TextureHandle maskTexture;
                public Color outlineColor;
                public float outlineWidth;
                public Vector4 texelSize;
            }

            public OutlinePass(RenderPassEvent passEvent, OutlineRenderFeature feature)
            {
                renderPassEvent = passEvent;
                _feature = feature;
            }

            #region Unity 6 Render Graph Pipeline

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var manager = global::OutlineManager.Instance;
                if (manager == null || manager.ActiveRenderers.Count == 0)
                {
                    return;
                }

                _feature.EnsureMaterials();
                var silMat = _feature._silhouetteMaterial;
                var compMat = _feature._compositeMaterial;
                if (silMat == null || compMat == null) return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                if (cameraData.cameraType != CameraType.Game && cameraData.cameraType != CameraType.SceneView)
                {
                    return;
                }

                var colorDesc = cameraData.cameraTargetDescriptor;
                var maskDesc = new RenderTextureDescriptor(colorDesc.width, colorDesc.height, GraphicsFormat.R8_UNorm, 0)
                {
                    msaaSamples = 1,
                    useMipMap = false
                };

                TextureHandle maskTextureHandle = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph,
                    maskDesc,
                    "_OutlineMaskTex",
                    clear: true
                );

                // 1. Silhouette Raster Pass
                using (var builder = renderGraph.AddRasterRenderPass<SilhouettePassData>("Outline Silhouette Pass", out var passData, _profilingSampler))
                {
                    passData.silhouetteMaterial = silMat;
                    passData.renderers.Clear();
                    foreach (var r in manager.ActiveRenderers)
                    {
                        if (r != null && r.enabled && r.gameObject.activeInHierarchy)
                        {
                            passData.renderers.Add(r);
                        }
                    }

                    if (passData.renderers.Count == 0) return;

                    builder.SetRenderAttachment(maskTextureHandle, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((SilhouettePassData data, RasterGraphContext context) =>
                    {
                        for (int i = 0; i < data.renderers.Count; i++)
                        {
                            var r = data.renderers[i];
                            if (r != null && r.enabled && r.gameObject.activeInHierarchy)
                            {
                                int submeshCount = r.sharedMaterials.Length;
                                for (int submesh = 0; submesh < submeshCount; submesh++)
                                {
                                    context.cmd.DrawRenderer(r, data.silhouetteMaterial, submesh, 0);
                                }
                            }
                        }
                    });
                }

                // 2. Composite Raster Pass
                using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>("Outline Composite Pass", out var passData, _profilingSampler))
                {
                    passData.compositeMaterial = compMat;
                    passData.maskTexture = maskTextureHandle;
                    passData.outlineColor = manager.CurrentOutlineColor;
                    passData.outlineWidth = manager.CurrentOutlineWidth;
                    int w = colorDesc.width;
                    int h = colorDesc.height;
                    passData.texelSize = new Vector4(1f / Mathf.Max(1, w), 1f / Mathf.Max(1, h), w, h);

                    builder.UseTexture(maskTextureHandle, AccessFlags.Read);
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.ReadWrite);
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((CompositePassData data, RasterGraphContext context) =>
                    {
                        data.compositeMaterial.SetTexture(MaskTexId, data.maskTexture);
                        data.compositeMaterial.SetVector(MaskTexTexelSizeId, data.texelSize);
                        data.compositeMaterial.SetColor(OutlineColorId, data.outlineColor);
                        data.compositeMaterial.SetFloat(OutlineWidthId, data.outlineWidth);

                        Blitter.BlitTexture(context.cmd, data.maskTexture, new Vector4(1, 1, 0, 0), data.compositeMaterial, 0);
                    });
                }
            }

            #endregion

            #region Legacy Non-RenderGraph Fallback

            [System.Obsolete]
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.colorFormat = RenderTextureFormat.R8;
                descriptor.depthBufferBits = 0;
                descriptor.msaaSamples = 1;

                RenderingUtils.ReAllocateIfNeeded(ref _legacyMaskTextureHandle, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_OutlineMaskTex");
            }

            [System.Obsolete]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var manager = global::OutlineManager.Instance;
                if (manager == null || manager.ActiveRenderers.Count == 0 || _legacyMaskTextureHandle == null)
                {
                    return;
                }

                _feature.EnsureMaterials();
                var silMat = _feature._silhouetteMaterial;
                var compMat = _feature._compositeMaterial;
                if (silMat == null || compMat == null) return;

                var cmd = CommandBufferPool.Get("OutlineEffect");
                using (new ProfilingScope(cmd, _profilingSampler))
                {
                    // 1. Silhouette Mask
                    cmd.SetRenderTarget(_legacyMaskTextureHandle);
                    cmd.ClearRenderTarget(false, true, Color.black);

                    foreach (var r in manager.ActiveRenderers)
                    {
                        if (r != null && r.enabled && r.gameObject.activeInHierarchy)
                        {
                            for (int submesh = 0; submesh < r.sharedMaterials.Length; submesh++)
                            {
                                cmd.DrawRenderer(r, silMat, submesh, 0);
                            }
                        }
                    }

                    // 2. Composite
                    var cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
                    int width = renderingData.cameraData.cameraTargetDescriptor.width;
                    int height = renderingData.cameraData.cameraTargetDescriptor.height;

                    compMat.SetTexture(MaskTexId, _legacyMaskTextureHandle);
                    compMat.SetVector(MaskTexTexelSizeId, new Vector4(1f / Mathf.Max(1, width), 1f / Mathf.Max(1, height), width, height));
                    compMat.SetColor(OutlineColorId, manager.CurrentOutlineColor);
                    compMat.SetFloat(OutlineWidthId, manager.CurrentOutlineWidth);

                    Blitter.BlitCameraTexture(cmd, _legacyMaskTextureHandle, cameraColorTarget, compMat, 0);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            #endregion

            public void Dispose()
            {
                _legacyMaskTextureHandle?.Release();
            }
        }
    }
}

