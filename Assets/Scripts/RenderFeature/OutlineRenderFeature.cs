using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
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
        if (settings.silhouetteShader == null)
            settings.silhouetteShader = Shader.Find("Hidden/OutlineSilhouette");
        if (settings.compositeShader == null)
            settings.compositeShader = Shader.Find("Hidden/OutlineComposite");

        if (settings.silhouetteShader != null)
            _silhouetteMaterial = CoreUtils.CreateEngineMaterial(settings.silhouetteShader);
        if (settings.compositeShader != null)
            _compositeMaterial = CoreUtils.CreateEngineMaterial(settings.compositeShader);

        _outlinePass = new OutlinePass(settings.renderPassEvent, _silhouetteMaterial, _compositeMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // 씬 뷰나 게임 뷰 카메라가 아니면 패스 생략
        if (renderingData.cameraData.cameraType != CameraType.Game &&
            renderingData.cameraData.cameraType != CameraType.SceneView)
        {
            return;
        }

        // 아웃라인 대상 렌더러가 없으면 렌더 패스 자체를 스킵 (비용 0)
        var manager = global::OutlineManager.Instance;
        if (manager == null || manager.ActiveRenderers.Count == 0)
        {
            return;
        }

        if (_silhouetteMaterial == null && settings.silhouetteShader != null)
            _silhouetteMaterial = CoreUtils.CreateEngineMaterial(settings.silhouetteShader);
        if (_compositeMaterial == null && settings.compositeShader != null)
            _compositeMaterial = CoreUtils.CreateEngineMaterial(settings.compositeShader);

        if (_silhouetteMaterial == null || _compositeMaterial == null)
        {
            if (settings.silhouetteShader == null) settings.silhouetteShader = Shader.Find("Hidden/OutlineSilhouette");
            if (settings.compositeShader == null) settings.compositeShader = Shader.Find("Hidden/OutlineComposite");
            if (settings.silhouetteShader != null) _silhouetteMaterial = CoreUtils.CreateEngineMaterial(settings.silhouetteShader);
            if (settings.compositeShader != null) _compositeMaterial = CoreUtils.CreateEngineMaterial(settings.compositeShader);
        }

        if (_outlinePass == null && _silhouetteMaterial != null && _compositeMaterial != null)
        {
            _outlinePass = new OutlinePass(settings.renderPassEvent, _silhouetteMaterial, _compositeMaterial);
        }

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
        private readonly Material _silhouetteMaterial;
        private readonly Material _compositeMaterial;
        private readonly ProfilingSampler _profilingSampler = new("OutlineEffect");

        private RTHandle? _maskTextureHandle;

        private static readonly int MaskTexId = Shader.PropertyToID("_MaskTex");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

        public OutlinePass(RenderPassEvent passEvent, Material silhouetteMaterial, Material compositeMaterial)
        {
            renderPassEvent = passEvent;
            _silhouetteMaterial = silhouetteMaterial;
            _compositeMaterial = compositeMaterial;
        }

        [System.Obsolete]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.colorFormat = RenderTextureFormat.R8;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            RenderingUtils.ReAllocateIfNeeded(ref _maskTextureHandle, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_OutlineMaskTex");
        }

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var manager = global::OutlineManager.Instance;
            if (manager == null || manager.ActiveRenderers.Count == 0 || _maskTextureHandle == null)
            {
                return;
            }

            var cmd = CommandBufferPool.Get("OutlineEffect");
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                // 1. Mask 버퍼 초기화 및 타겟 렌더러 실루엣 그리기
                cmd.SetRenderTarget(_maskTextureHandle);
                cmd.ClearRenderTarget(false, true, Color.black);

                foreach (var r in manager.ActiveRenderers)
                {
                    if (r != null && r.enabled && r.gameObject.activeInHierarchy)
                    {
                        for (int submesh = 0; submesh < r.sharedMaterials.Length; submesh++)
                        {
                            cmd.DrawRenderer(r, _silhouetteMaterial, submesh, 0);
                        }
                    }
                }

                // 2. Composite 아웃라인을 카메라 컬러 타겟에 블렌딩
                var cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
                int width = renderingData.cameraData.cameraTargetDescriptor.width;
                int height = renderingData.cameraData.cameraTargetDescriptor.height;

                _compositeMaterial.SetTexture(MaskTexId, _maskTextureHandle);
                _compositeMaterial.SetVector("_MaskTex_TexelSize", new Vector4(1f / Mathf.Max(1, width), 1f / Mathf.Max(1, height), width, height));
                _compositeMaterial.SetColor(OutlineColorId, manager.CurrentOutlineColor);
                _compositeMaterial.SetFloat(OutlineWidthId, manager.CurrentOutlineWidth);

                cmd.SetRenderTarget(cameraColorTarget);
                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                // 전체 화면 삼각형 그리기
                cmd.DrawProcedural(Matrix4x4.identity, _compositeMaterial, 0, MeshTopology.Triangles, 3);
                cmd.SetViewProjectionMatrices(renderingData.cameraData.GetViewMatrix(), renderingData.cameraData.GetProjectionMatrix());
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        public void Dispose()
        {
            _maskTextureHandle?.Release();
        }
    }
}
}

