using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TVCloseRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material material;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public Settings settings = new Settings();
    private TVClosePass renderPass;
    // Add this at the top of TVCloseEffectFeature.cs
    public static TVCloseRenderFeature Instance { get; private set; }

    // Modify the Create() method:

    public override void Create()
    {
        if (settings.material == null)
        {
            Debug.LogWarning("TVCloseEffect material is not assigned.");
            return;
        }

        renderPass = new TVClosePass(settings.material)
        {
            renderPassEvent = settings.renderPassEvent

        };

        Instance = this; // Set the static instance
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.material == null)
        {
            Debug.LogWarning("TVCloseEffect material is not assigned.");
            return;
        }

        renderer.EnqueuePass(renderPass);
    }

    // Public API to control the effect
    public void PlayCloseEffect(float duration, System.Action onComplete = null)
    {
        if (renderPass == null) return;

        DOTween.Kill(this); // Kill any existing tweens

        // Reset values before starting
        renderPass.ShrinkY = 1f;
        renderPass.ShrinkX = 1f;
        renderPass.EdgeDarkness = 0.5f;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(DOTween.To(() => renderPass.ShrinkY, x => renderPass.ShrinkY = x, 0f, duration * 0.6f).SetEase(Ease.InQuad));
        sequence.Join(DOTween.To(() => renderPass.EdgeDarkness, x => renderPass.EdgeDarkness = x, 1f, duration * 0.6f).SetEase(Ease.InQuad));
        sequence.Join(DOTween.To(() => renderPass.ShrinkX, x => renderPass.ShrinkX = x, 0.7f, duration * 0.6f).SetEase(Ease.InQuad));
        sequence.Append(DOTween.To(() => renderPass.ShrinkX, x => renderPass.ShrinkX = x, 0f, duration * 0.2f).SetEase(Ease.OutCubic));
        sequence.SetUpdate(true);
        sequence.OnComplete(() => onComplete?.Invoke());
    }
    public void PlayOpenEffect(float duration, System.Action onComplete = null)
    {
        if (renderPass == null) return;

        DOTween.Kill(this); // Kill any existing tweens

        // Start from fully closed
        renderPass.ShrinkY = 0f;
        renderPass.ShrinkX = 0f;
        renderPass.EdgeDarkness = 1f;

        Sequence sequence = DOTween.Sequence();

        // Open horizontally a bit first (adds some dynamic motion)
        sequence.Append(DOTween.To(() => renderPass.ShrinkX, x => renderPass.ShrinkX = x, 0.7f, duration * 0.2f).SetEase(Ease.InCubic));

        // Open vertically and increase light while also fully widening
        sequence.Append(DOTween.To(() => renderPass.ShrinkY, y => renderPass.ShrinkY = y, 1f, duration * 0.6f).SetEase(Ease.OutQuad));
        sequence.Join(DOTween.To(() => renderPass.EdgeDarkness, d => renderPass.EdgeDarkness = d, 0.5f, duration * 0.6f).SetEase(Ease.OutQuad));
        sequence.Join(DOTween.To(() => renderPass.ShrinkX, x => renderPass.ShrinkX = x, 1f, duration * 0.6f).SetEase(Ease.OutQuad));

        sequence.OnComplete(() => onComplete?.Invoke());
    }


    public void ResetEffect()
    {
        if (renderPass != null)
        {
            renderPass.ShrinkY = 1f;
            renderPass.ShrinkX = 1f;
            renderPass.EdgeDarkness = 0.5f;
        }
    }

    private class TVClosePass : ScriptableRenderPass
    {
        Material _mat;
        static readonly int _TempID = Shader.PropertyToID("_TVCloseTempRT");

        public float ShrinkY = 1, ShrinkX = 1, EdgeDarkness = 0.5f;
        public TVClosePass(Material mat) { _mat = mat; }

        public override void Execute(ScriptableRenderContext ctx, ref RenderingData data)
        {
            if (_mat == null) return;

            var cmd = CommandBufferPool.Get("TVCloseEffect");

            // inside Execute(...)
            var handle = data.cameraData.renderer.cameraColorTargetHandle;

            // allocate temp just like before...
            cmd.GetTemporaryRT(_TempID, data.cameraData.cameraTargetDescriptor, FilterMode.Bilinear);

            // copy into temp
            cmd.Blit(handle.nameID, _TempID);

            // set shader floats
            _mat.SetFloat("_ShrinkY", ShrinkY);
            _mat.SetFloat("_ShrinkX", ShrinkX);
            _mat.SetFloat("_EdgeDarkness", EdgeDarkness);

            // blit back to camera
            cmd.Blit(_TempID, handle.nameID, _mat, 0);

            // release temp
            cmd.ReleaseTemporaryRT(_TempID);

            cmd.ReleaseTemporaryRT(_TempID);

            ctx.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
