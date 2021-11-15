using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting.APIUpdating;


namespace LeTai.Asset.TranslucentImage.UniversalRP
{
[MovedFrom("LeTai.Asset.TranslucentImage.LWRP")]
public class TranslucentImageBlurSource : ScriptableRendererFeature
{
    readonly Dictionary<Camera, TranslucentImageSource> tisCache = new Dictionary<Camera, TranslucentImageSource>();

    TranslucentImageBlurRenderPass pass;
    IBlurAlgorithm                 blurAlgorithm;


    /// <summary>
    /// When adding new Translucent Image Source to existing Camera at run time, the new Source must be registered here
    /// </summary>
    /// <param name="source"></param>
    public void RegisterSource(TranslucentImageSource source)
    {
        tisCache[source.GetComponent<Camera>()] = source;
    }

    public override void Create()
    {
        ShaderId.Init(32); //hack for now

        blurAlgorithm = new ScalableBlur();

        pass                 = new TranslucentImageBlurRenderPass();
        pass.renderPassEvent = RenderPassEvent.AfterRendering;

        tisCache.Clear();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer,
                                         ref RenderingData  renderingData)
    {
        var tis = GetTIS(renderingData.cameraData.camera);

        if (tis == null || !tis.shouldUpdateBlur())
            return;

        tis.OnBeforeBlur();
        blurAlgorithm.Init(tis.BlurConfig);
        var passData = new TISPassData {
            cameraColorTarget = renderer.cameraColorTarget,
            blurAlgorithm     = blurAlgorithm, //hack for now
            blurSource        = tis,
            isPreviewing      = tis.preview
        };

        pass.Setup(passData);

        renderer.EnqueuePass(pass);
    }

    TranslucentImageSource GetTIS(Camera camera)
    {
        if (!tisCache.ContainsKey(camera))
        {
            tisCache.Add(camera, camera.GetComponent<TranslucentImageSource>());
        }

        return tisCache[camera];
    }
}
}
