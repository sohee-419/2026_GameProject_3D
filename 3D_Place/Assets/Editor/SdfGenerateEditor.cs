
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SdfGenerate))]
public class SdfGenerateEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SdfGenerate generator = (SdfGenerate)target;

        EditorGUILayout.Space(10);

        if (generator.sourceTexture != null &&
            !generator.sourceTexture.isReadable)
        {
            EditorGUILayout.HelpBox(
                "원본 텍스처의 Read/Write Enabled를 켜주세요.",
                MessageType.Warning
            );
        }

        EditorGUILayout.Space(5);

        using (new EditorGUI.DisabledScope(
            generator.sourceTexture == null ||
            !generator.sourceTexture.isReadable))
        {
            if (GUILayout.Button("Generate & Save SDF"))
            {
                GenerateAndSave(generator);
            }
        }
    }

    private static void GenerateAndSave(
        SdfGenerate generator)
    {
        Texture2D generated = null;

        try
        {
            // SDF 생성
            generated = generator.GenerateTexture();

            // 원본 텍스처의 폴더 확인
            string sourcePath =
                AssetDatabase.GetAssetPath(
                    generator.sourceTexture
                );

            string outputFolder = "Assets";

            if (!string.IsNullOrEmpty(sourcePath))
            {
                string sourceFolder =
                    Path.GetDirectoryName(sourcePath)
                    .Replace("\\", "/");

                if (!string.IsNullOrEmpty(sourceFolder))
                {
                    outputFolder = sourceFolder + "/Output";

                    if (!AssetDatabase.IsValidFolder(
                        outputFolder))
                    {
                        AssetDatabase.CreateFolder(
                            sourceFolder,
                            "Output"
                        );
                    }
                }
            }

            string fileName =
                "Sdf_" + generator.sourceTexture.name;

            // 저장 위치 선택
            string savePath =
                EditorUtility.SaveFilePanelInProject(
                    "Save SDF Texture",
                    fileName,
                    "png",
                    "SDF 텍스처를 저장할 위치를 선택하세요.",
                    outputFolder
                );

            if (string.IsNullOrEmpty(savePath))
                return;

            // PNG 저장
            string absolutePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                savePath
            );

            File.WriteAllBytes(
                absolutePath,
                generated.EncodeToPNG()
            );

            AssetDatabase.ImportAsset(savePath);

            // SDF는 데이터 텍스처이므로 Linear 설정
            TextureImporter importer =
                AssetImporter.GetAtPath(savePath)
                as TextureImporter;

            if (importer != null)
            {
                importer.sRGBTexture = false;
                importer.mipmapEnabled = false;

                importer.textureCompression =
                    TextureImporterCompression.Uncompressed;

                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;

                importer.SaveAndReimport();
            }

            Texture2D saved =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    savePath
                );

            EditorGUIUtility.PingObject(saved);

            Debug.Log(
                "SDF 생성 완료: " + savePath,
                saved
            );
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, generator);
        }
        finally
        {
            if (generated != null)
                DestroyImmediate(generated);
        }
    }
}
