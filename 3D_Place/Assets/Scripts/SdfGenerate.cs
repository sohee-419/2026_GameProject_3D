
using System;
using UnityEngine;

public class SdfGenerate : MonoBehaviour
{
    public enum MaskChannel
    {
        Red,
        Alpha
    }

    [Header("Input Texture")]
    public Texture2D sourceTexture;

    public MaskChannel sourceChannel = MaskChannel.Red;

    [Range(0f, 1f)]
    public float threshold = 0.5f;

    public bool invertMask = false;

    [Header("SDF Settings")]
    [Min(1)]
    public int spread = 64;

    private const float INF = 1e20f;

    public Texture2D GenerateTexture()
    {
        if (sourceTexture == null)
            throw new InvalidOperationException(
                "Source Texture가 지정되지 않았습니다.");

        if (!sourceTexture.isReadable)
            throw new InvalidOperationException(
                "Source Texture의 Read/Write Enabled를 켜주세요.");

        int width = sourceTexture.width;
        int height = sourceTexture.height;
        int count = width * height;

        // 1. 원본 텍스처의 픽셀 읽기
        Color32[] pixels = sourceTexture.GetPixels32();

        // 2. 흑백 마스크를 이진 데이터로 변환
        bool[] inside = new bool[count];

        for (int i = 0; i < count; i++)
        {
            float sample = sourceChannel == MaskChannel.Alpha
                ? pixels[i].a / 255f
                : pixels[i].r / 255f;

            bool value = sample >= threshold;

            inside[i] = invertMask ? !value : value;
        }

        // 3. 내부와 외부 각각의 거리 계산
        float[] distanceToInside = new float[count];
        float[] distanceToOutside = new float[count];

        ComputeDistanceField(
            inside, width, height, true, distanceToInside);

        ComputeDistanceField(
            inside, width, height, false, distanceToOutside);

        // 4. SDF 값으로 변환
        Color32[] output = new Color32[count];

        float maxDistance = Mathf.Max(1, spread);

        for (int i = 0; i < count; i++)
        {
            bool isInside = inside[i];

            float squaredDistance = isInside
                ? distanceToOutside[i]
                : distanceToInside[i];

            float boundaryDistance;

            if (squaredDistance >= INF * 0.5f)
            {
                boundaryDistance = maxDistance;
            }
            else
            {
                // 픽셀 중심 간 거리를 실제 경계까지의
                // 거리로 보정 (인접 픽셀 사이의 경계는 0.5px)
                boundaryDistance = Mathf.Max(
                    0f,
                    Mathf.Sqrt(squaredDistance) - 0.5f
                );
            }

            boundaryDistance = Mathf.Min(
                boundaryDistance, maxDistance);

            float signedValue =
                isInside ? 1f : -1f;

            float sdfValue = Mathf.Clamp01(
                0.5f + signedValue *
                boundaryDistance / (2f * maxDistance)
            );

            byte gray = (byte)Mathf.RoundToInt(
                sdfValue * 255f
            );

            output[i] = new Color32(gray, gray, gray, 255);
        }

        // 5. 결과 텍스처 생성
        Texture2D result = new Texture2D(
            width,
            height,
            TextureFormat.RGB24,
            false,
            true
        );

        result.name = "Sdf_" + sourceTexture.name;

        result.SetPixels32(output);
        result.Apply(false, false);

        result.filterMode = FilterMode.Bilinear;
        result.wrapMode = TextureWrapMode.Clamp;

        return result;
    }

    // 이진 마스크에서 특정 영역까지의
    // 최소 제곱 유클리드 거리를 계산
    private static void ComputeDistanceField(
        bool[] mask,
        int width,
        int height,
        bool targetInside,
        float[] result)
    {
        float[] temp = new float[width * height];

        int maxSize = Math.Max(width, height);

        float[] f = new float[maxSize];
        float[] d = new float[maxSize];

        int[] v = new int[maxSize];
        double[] z = new double[maxSize + 1];

        // 첫 번째 단계: 가로 방향 거리 계산
        for (int y = 0; y < height; y++)
        {
            int row = y * width;

            for (int x = 0; x < width; x++)
            {
                f[x] = mask[row + x] == targetInside
                    ? 0f
                    : INF;
            }

            DistanceTransform1D(
                f, width, d, v, z);

            for (int x = 0; x < width; x++)
            {
                temp[row + x] = d[x];
            }
        }

        // 두 번째 단계: 세로 방향 거리 계산
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                f[y] = temp[y * width + x];
            }

            DistanceTransform1D(
                f, height, d, v, z);

            for (int y = 0; y < height; y++)
            {
                result[y * width + x] = d[y];
            }
        }
    }

    // 1차원 유클리드 거리 변환
    private static void DistanceTransform1D(
        float[] f,
        int length,
        float[] d,
        int[] v,
        double[] z)
    {
        int first = -1;

        for (int q = 0; q < length; q++)
        {
            if (f[q] < INF * 0.5f)
            {
                first = q;
                break;
            }
        }

        if (first < 0)
        {
            for (int q = 0; q < length; q++)
                d[q] = INF;

            return;
        }

        int k = 0;

        v[0] = first;
        z[0] = double.NegativeInfinity;
        z[1] = double.PositiveInfinity;

        for (int q = first + 1; q < length; q++)
        {
            if (f[q] >= INF * 0.5f)
                continue;

            double s;

            while (true)
            {
                int vk = v[k];

                s = (
                    (double)f[q] + q * (double)q -
                    ((double)f[vk] + vk * (double)vk)
                ) / (2.0 * (q - vk));

                if (s > z[k])
                    break;

                k--;
            }

            k++;

            v[k] = q;
            z[k] = s;
            z[k + 1] = double.PositiveInfinity;
        }

        k = 0;

        for (int q = 0; q < length; q++)
        {
            while (z[k + 1] < q)
                k++;

            double delta = q - v[k];

            d[q] = (float)(
                delta * delta + f[v[k]]
            );
        }
    }
}
