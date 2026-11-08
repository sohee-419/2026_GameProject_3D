
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class SdfTextureBlender : MonoBehaviour
{
    private const int RequiredCount = 9;

    [Header("SDF Blend Shader")]
    public Shader blendShader;

    [Header("Blend Settings")]
    [Tooltip("RGB 채널을 사용합니다. Alpha 대신 RGB를 읽습니다.")]
    public bool useRGB = true;

    [Range(0f, 0.05f)]
    public float delta = 0.005f;

    [Range(16, 256)]
    public int sampleCount = 256;

    [Header("Input Textures")]
    [Tooltip("180, A, B, C, D, E, F, G, H 순서")]
    public Texture2D[] inputTextures =
        new Texture2D[RequiredCount];

    [NonSerialized]
    public Texture2D resultTexture;

    private static readonly string[] TextureProperties =
    {
        "_SDF0", "_SDF1", "_SDF2",
        "_SDF3", "_SDF4", "_SDF5",
        "_SDF6", "_SDF7", "_SDF8"
    };

    // 입력 텍스처 및 설정 확인
    public bool ValidateInputs(out string message)
    {
        if (blendShader == null &&
            Shader.Find("Hidden/FaceShadow/SDFBlend") == null)
        {
            message = "SDF Blend Shader를 찾을 수 없습니다.";
            return false;
        }

        if (inputTextures == null ||
            inputTextures.Length != RequiredCount)
        {
            message = "SDF 텍스처 9장이 필요합니다.";
            return false;
        }

        if (inputTextures[0] == null)
        {
            message = "첫 번째 SDF 텍스처가 비어 있습니다.";
            return false;
        }

        int width = inputTextures[0].width;
        int height = inputTextures[0].height;

        for (int i = 0; i < RequiredCount; i++)
        {
            if (inputTextures[i] == null)
            {
                message = $"SDF 입력 슬롯 {i}번이 비어 있습니다.";
                return false;
            }

            if (inputTextures[i].width != width ||
                inputTextures[i].height != height)
            {
                message = "모든 SDF 텍스처의 해상도가 같아야 합니다.";
                return false;
            }
        }

        message = "Ready";
        return true;
    }

    // SDF Texture Blending
    public void BlendTextures()
    {
        if (!ValidateInputs(out string message))
        {
            Debug.LogError(message, this);
            return;
        }

        Shader shader = blendShader != null
            ? blendShader
            : Shader.Find("Hidden/FaceShadow/SDFBlend");

        if (shader == null || !shader.isSupported)
        {
            Debug.LogError(
                "SDF Blend Shader가 지원되지 않습니다.",
                this
            );
            return;
        }

        int width = inputTextures[0].width;
        int height = inputTextures[0].height;

        // RenderTexture 생성 (Linear Data)
        RenderTexture rt = new RenderTexture(
            width,
            height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear
        );

        rt.name = "Face_Sdf_Lightmap_RT";
        rt.filterMode = FilterMode.Bilinear;
        rt.wrapMode = TextureWrapMode.Clamp;
        rt.Create();

        // 임시 Material 생성
        Material material = new Material(shader);
        material.hideFlags = HideFlags.HideAndDontSave;

        Texture2D generated = null;

        RenderTexture previousRT = RenderTexture.active;
        bool previousSRGBWrite = GL.sRGBWrite;

        try
        {
            // 9장의 SDF 연결
            for (int i = 0; i < RequiredCount; i++)
            {
                material.SetTexture(
                    TextureProperties[i],
                    inputTextures[i]
                );
            }

            // 블렌딩 설정
            material.SetFloat("_Delta", delta);
            material.SetFloat(
                "_UseRGB", useRGB ? 1f : 0f
            );
            material.SetFloat(
                "_SampleCount",
                Mathf.Clamp(sampleCount, 16, 256)
            );

            // Linear 데이터로 렌더링
            GL.sRGBWrite = false;

            Graphics.Blit(
                inputTextures[0],
                rt,
                material,
                0
            );

            // RenderTexture를 Texture2D로 변환
            RenderTexture.active = rt;

            generated = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false,
                true
            );

            generated.name = "Face_Sdf_lightmap";

            generated.ReadPixels(
                new Rect(0, 0, width, height),
                0,
                0
            );

            generated.Apply(false, false);

            generated.filterMode = FilterMode.Bilinear;
            generated.wrapMode = TextureWrapMode.Clamp;

            // 기존 Preview 정리
            if (resultTexture != null)
            {
                DestroyImmediate(resultTexture);
            }

            resultTexture = generated;
            generated = null;

            Debug.Log(
                $"SDF Blending 완료: {width} x {height}",
                this
            );
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
        finally
        {
            // 임시 메모리 정리
            RenderTexture.active = previousRT;
            GL.sRGBWrite = previousSRGBWrite;

            if (generated != null)
                DestroyImmediate(generated);

            rt.Release();
            DestroyImmediate(rt);
            DestroyImmediate(material);
        }
    }

    private void OnDestroy()
    {
        if (resultTexture != null)
        {
            DestroyImmediate(resultTexture);
            resultTexture = null;
        }
    }
}
