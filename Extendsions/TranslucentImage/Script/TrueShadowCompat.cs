#if LETAI_TRUESHADOW
using System.Collections.Generic;
using LeTai.TrueShadow.PluginInterfaces;
using UnityEngine;
using UnityEngine.UI;

namespace LeTai.Asset.TranslucentImage
{
public partial class TranslucentImage : //ITrueShadowCasterClearColorProvider,
    ITrueShadowCasterMaterialPropertiesModifier,
    ITrueShadowRendererNormalMaterialProvider,
    ITrueShadowRendererMeshModifier
{
    TrueShadow.TrueShadow shadow;

    TrueShadow.TrueShadow Shadow
    {
        get
        {
            if (!shadow)
                shadow = GetComponent<TrueShadow.TrueShadow>();
            return shadow;
        }
    }

    // Camera canvasCam;
    //
    // Vector2 GetCanvasSize()
    // {
    //     return canvasCam ? canvasCam.pixelRect.size : new Vector2(Screen.width, Screen.height);
    // }
    //
    // void InitTrueShadowCompat()
    // {
    //     switch (canvas.renderMode)
    //     {
    //         case RenderMode.ScreenSpaceCamera:
    //         case RenderMode.WorldSpace:
    //             canvasCam = canvas.worldCamera;
    //             break;
    //         default:
    //         case RenderMode.ScreenSpaceOverlay:
    //             canvasCam = null;
    //             break;
    //     }
    // }

    // static readonly Vector3[] WORLD_CORNERS  = new Vector3[4];
    // static readonly Vector3[] SCREEN_CORNERS = new Vector3[4];

    public void ModifyTrueShadowCasterMaterialProperties(MaterialPropertyBlock propertyBlock)
    {
        propertyBlock.SetTexture(_blurTexPropId, Texture2D.whiteTexture);

        // Would consume too much perf

        // rectTransform.GetWorldCorners(WORLD_CORNERS);
        // for (int i = 0; i < WORLD_CORNERS.Length; i++)
        //     SCREEN_CORNERS[i] = RectTransformUtility.WorldToScreenPoint(canvasCam, WORLD_CORNERS[i]);
        //
        // var screenRectMin  = SCREEN_CORNERS[0];
        // var screenRectMax  = SCREEN_CORNERS[2];
        // var screenRectSize = screenRectMax - screenRectMin;
        // var canvasSize     = GetCanvasSize();
        // var invertCropRegion = new Vector4(
        //     -screenRectMin.x / screenRectSize.x,
        //     -screenRectMin.y / screenRectSize.y,
        //     (canvasSize.x - screenRectMin.x) / screenRectSize.x,
        //     (canvasSize.y - screenRectMin.y) / screenRectSize.y
        // );
        //
        // propertyBlock.SetTexture(_blurTexPropId, source.BlurredScreen);
        // propertyBlock.SetVector(ShaderId.CROP_REGION, invertCropRegion);
    }

    // public Color GetTrueShadowCasterClearColor()
    // {
    //     return Color.Lerp(Color.white, color, spriteBlending).WithA(0);
    // }

    public Material GetTrueShadowRendererNormalMaterial()
    {
        return defaultMaterial;
    }

    readonly List<UIVertex> trueShadowRendererVertices = new List<UIVertex>();

    public void ModifyTrueShadowRenderMesh(VertexHelper vertexHelper)
    {
        trueShadowRendererVertices.Clear();
        vertexHelper.GetUIVertexStream(trueShadowRendererVertices);

        for (var i = 0; i < trueShadowRendererVertices.Count; i++)
        {
            UIVertex vertex = trueShadowRendererVertices[i];
            vertex.color = Shadow.Color;
            vertex.uv1 = new Vector2(1, // spriteBlending
                                     0  // No use for this yet
            );
            trueShadowRendererVertices[i] = vertex;
        }

        vertexHelper.Clear();
        vertexHelper.AddUIVertexTriangleStream(trueShadowRendererVertices);
    }
}
}
#endif
