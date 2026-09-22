using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;

namespace Hearthhold.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        static ProjectSetup()
        {
            EditorApplication.delayCall += delegate
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !File.Exists("Assets/Hearthhold/Scenes/Main.unity")) Prepare();
            };
        }

        [MenuItem("Hearthhold/Prepare project")]
        public static void Prepare()
        {
            ConfigureGeneratedTexture("Assets/Hearthhold/Resources/GeneratedArt/KeepV061.png");
            ConfigureGeneratedTexture("Assets/Hearthhold/Resources/GeneratedArt/MineV061.png");
            ConfigureGeneratedTexture("Assets/Hearthhold/Resources/GeneratedArt/ReservoirV061.png");
            ConfigureGeneratedTexture("Assets/Hearthhold/Resources/GeneratedArt/BarracksV061.png");
            ConfigureGeneratedTexture("Assets/Hearthhold/Resources/GeneratedArt/CannonV061.png");
            ConfigureGeneratedTexture("Assets/Hearthhold/Resources/GeneratedArt/WatchtowerV061.png");
            ConfigureGeneratedTexture("Assets/Hearthhold/Resources/GeneratedArt/TroopAtlasV061.png");
            ConfigureGeneratedTexture("Assets/Hearthhold/Resources/UI/ExpeditionEmblemV1.png");
            Directory.CreateDirectory("Assets/Hearthhold/Settings");
            Directory.CreateDirectory("Assets/Hearthhold/Resources");
            Directory.CreateDirectory("Assets/Hearthhold/Scenes");
            EnsureTmpEssentials();
            const string pipelinePath = "Assets/Hearthhold/Settings/HearthholdURP.asset";
            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                UniversalRendererData renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, "Assets/Hearthhold/Settings/HearthholdRenderer.asset");
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.msaaSampleCount = 4;
                pipeline.shadowDistance = 100;
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            EnsureAlwaysIncludedShader("TextMeshPro/Mobile/Distance Field");
            if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Hearthhold/Resources/ModelPalette.mat") == null)
            {
                Shader shader = Shader.Find("Hearthhold/VertexLit");
                if (shader == null) throw new BuildFailedException("Hearthhold/VertexLit shader failed to import.");
                AssetDatabase.CreateAsset(new Material(shader), "Assets/Hearthhold/Resources/ModelPalette.mat");
            }
            if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Hearthhold/Resources/WorldPalette.mat") == null)
            {
                Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetFloat("_Smoothness", 0.12f);
                AssetDatabase.CreateAsset(material, "Assets/Hearthhold/Resources/WorldPalette.mat");
            }
            if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Hearthhold/Resources/GeneratedSpritePalette.mat") == null)
            {
                Shader shader = Shader.Find("Hearthhold/ChromaKeySprite");
                if (shader == null) throw new BuildFailedException("Hearthhold/ChromaKeySprite shader failed to import.");
                AssetDatabase.CreateAsset(new Material(shader), "Assets/Hearthhold/Resources/GeneratedSpritePalette.mat");
            }
            if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Hearthhold/Resources/OverlayPalette.mat") == null)
            {
                Shader shader = Shader.Find("Hearthhold/OverlayUnlit");
                if (shader == null) throw new BuildFailedException("Hearthhold/OverlayUnlit shader failed to import.");
                AssetDatabase.CreateAsset(new Material(shader), "Assets/Hearthhold/Resources/OverlayPalette.mat");
            }
            PlayerSettings.companyName = "Hearthhold Studio";
            PlayerSettings.productName = "Hearthhold";
            PlayerSettings.bundleVersion = "0.13.0-preview";
            PlayerSettings.defaultScreenWidth = 1440;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            // Input only uses built-in legacy mouse/keyboard APIs in this milestone.
            UnityEngine.Object[] playerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (playerAssets.Length > 0)
            {
                SerializedObject settings = new SerializedObject(playerAssets[0]);
                SerializedProperty input = settings.FindProperty("activeInputHandler");
                if (input != null) { input.intValue = 0; settings.ApplyModifiedPropertiesWithoutUndo(); }
            }
            if (!File.Exists("Assets/Hearthhold/Scenes/Main.unity"))
            {
                // Batch mode starts with an unsaved, empty scene and Unity 6.6 refuses to
                // create an additive scene beside it. Reuse that scene when possible.
                var scene = EditorSceneManager.GetActiveScene();
                if (!scene.IsValid() || !string.IsNullOrEmpty(scene.path) || scene.rootCount > 0)
                {
                    if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                    scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }
                EditorSceneManager.SaveScene(scene, "Assets/Hearthhold/Scenes/Main.unity");
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Hearthhold/Scenes/Main.unity", true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ConfigureGeneratedTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            bool changed = importer.textureCompression != TextureImporterCompression.Uncompressed || importer.mipmapEnabled || importer.wrapMode != TextureWrapMode.Clamp || importer.maxTextureSize < 2048;
            if (!changed) return;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static void EnsureAlwaysIncludedShader(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new BuildFailedException("Required shader failed to import: " + shaderName);
            SerializedObject graphics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            SerializedProperty shaders = graphics.FindProperty("m_AlwaysIncludedShaders");
            for (int i = 0; i < shaders.arraySize; i++) if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader) return;
            shaders.InsertArrayElementAtIndex(shaders.arraySize);
            shaders.GetArrayElementAtIndex(shaders.arraySize - 1).objectReferenceValue = shader;
            graphics.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureTmpEssentials()
        {
            if (Shader.Find("TextMeshPro/Mobile/Distance Field") != null && Resources.Load<TMP_Settings>("TMP Settings") != null) return;
            throw new BuildFailedException("TMP Essential Resources are missing from Assets/TextMesh Pro. Restore the tracked package resources before building.");
        }

        [MenuItem("Hearthhold/Open main scene")]
        public static void OpenScene()
        {
            Prepare();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene("Assets/Hearthhold/Scenes/Main.unity");
        }

        [MenuItem("Hearthhold/Build Windows x64")]
        public static void BuildWindows()
        {
            Prepare();
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../artifacts/WindowsUnity/Hearthhold.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Hearthhold/Scenes/Main.unity" },
                target = BuildTarget.StandaloneWindows64,
                locationPathName = output,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Windows build failed: " + report.summary.result);
            Debug.Log("Windows build ready: " + output);
        }
    }
}
