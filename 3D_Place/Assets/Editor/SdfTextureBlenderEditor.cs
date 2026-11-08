
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SdfTextureBlender))]
public class SdfTextureBlenderEditor : Editor
{
    private static readonly string[] InputLabels =
    {
        "Element 0 - 180°",
        "Element 1 - 157.5°",
        "Element 2 - 135°",
        "Element 3 - 112.5°",
        "Element 4 - 90°",
        "Element 5 - 67.5°",
        "Element 6 - 45°",
        "Element 7 - 22.5°",
        "Element 8 - 0°"
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SdfTextureBlender blender =
            (SdfTextureBlender)target;

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField(
            "SDF Texture Blender",
            EditorStyles.boldLabel
        );

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("blendShader"),
            new GUIContent("SDF Blend Shader")
        );

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("useRGB"),
            new GUIContent("Use RGB (Instead of Alpha)")
        );

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField(
            "Input Textures (9)",
            EditorStyles.boldLabel
        );

        SerializedProperty textures =
            serializedObject.FindProperty("inputTextures");

        // 입력 텍스처 개수 고정
        if (textures.arraySize != 9)
            textures.arraySize = 9;

        for (int i = 0; i < 9; i++)
        {
            EditorGUILayout.PropertyField(
                textures.GetArrayElementAtIndex(i),
                new GUIContent(InputLabels[i])
            );
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("delta"),
            new GUIContent("Delta (Smoothness)")
        );

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("sampleCount"),
            new GUIContent("Sample Count")
        );

        EditorGUILayout.Space(5);

        serializedObject.ApplyModifiedProperties();

        // 입력 텍스처 유효성 확인
        bool ready = blender.ValidateInputs(
            out string message
        );

        if (!ready)
        {
            EditorGUILayout.HelpBox(
                message,
                MessageType.Warning
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "All 9 SDF textures are ready.",
                MessageType.Info
            );
        }

        // 입력 크기 표시
        if (blender.inputTextures != null &&
            blender.inputTextures.Length > 0 &&
            blender.inputTextures[0] != null)
        {
            Texture2D first = blender.inputTextures[0];

            EditorGUILayout.LabelField(
                "Output Size",
                $"{first.width} x {first.height}"
            );
        }

        EditorGUILayout.Space(10);

        // 블렌딩 및 저장 버튼
        EditorGUILayout.BeginHorizontal();

        using (new EditorGUI.DisabledScope(!ready))
        {
            if (GUILayout.Button(
                "Texture Blending",
                GUILayout.Height(35)))
            {
                blender.BlendTextures();
            }
        }

        using (new EditorGUI.DisabledScope(
            blender.resultTexture == null))
        {
            if (GUILayout.Button(
                "Save Result",
                GUILayout.Height(35)))
            {
                SaveResult(blender);
            }
        }

        EditorGUILayout.EndHorizontal();

        // 결과 텍스처
        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField(
            "Result Texture",
            EditorStyles.boldLabel
        );

        EditorGUILayout.ObjectField(
            blender.resultTexture,
            typeof(Texture2D),
            false
        );

        if (blender.resultTexture != null)
        {
            EditorGUILayout.LabelField(
                "Preview",
                EditorStyles.boldLabel
            );

            Rect previewRect = GUILayoutUtility.GetRect(
                200,
                280,
                GUILayout.ExpandWidth(true)
            );

            EditorGUI.DrawPreviewTexture(
                previewRect,
                blender.resultTexture,
                null,
                ScaleMode.ScaleToFit
            );
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "SDF 결과 텍스처는 sRGB를 끄고 " +
            "데이터 텍스처로 사용하는 것을 권장합니다.",
            MessageType.Info
        );
    }

    private static void SaveResult(
        SdfTextureBlender blender)
    {
        if (blender.resultTexture == null)
            return;

        // 첫 번째 입력 텍스처 위치 확인
        string sourcePath = AssetDatabase.GetAssetPath(
            blender.inputTextures[0]
        );

        string parentFolder = string.IsNullOrEmpty(sourcePath)
            ? "Assets"
            : Path.GetDirectoryName(sourcePath)
                .Replace("\\", "/");

        if (string.IsNullOrEmpty(parentFolder))
            parentFolder = "Assets";

        // Converted 폴더 생성
        string outputFolder = parentFolder + "/Converted";

        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            AssetDatabase.CreateFolder(
                parentFolder,
                "Converted"
            );
        }

        // 저장 위치 선택
        string savePath = EditorUtility.SaveFilePanelInProject(
            "Save Face Shadow Lightmap",
            "Face_Sdf_lightmap",
            "png",
            "결과 텍스처를 저장하세요.",
            outputFolder
        );

        if (string.IsNullOrEmpty(savePath))
            return;

        // PNG 저장
        string absolutePath = Path.GetFullPath(savePath);

        File.WriteAllBytes(
            absolutePath,
            blender.resultTexture.EncodeToPNG()
        );

        AssetDatabase.ImportAsset(
            savePath,
            ImportAssetOptions.ForceSynchronousImport
        );

        // Linear 데이터 텍스처 설정
        TextureImporter importer =
            AssetImporter.GetAtPath(savePath)
            as TextureImporter;

        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;

            importer.textureCompression =
                TextureImporterCompression.Uncompressed;

            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;

            importer.alphaSource =
                TextureImporterAlphaSource.None;

            importer.SaveAndReimport();
        }

        Texture2D saved =
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                savePath
            );

        EditorGUIUtility.PingObject(saved);

        Debug.Log(
            "Face Shadow Lightmap 저장 완료: " + savePath
        );
    }
}
