using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Editor window to validate and debug the Fridge Glow System setup
/// Access via: Window > Cook Project > Fridge Glow Setup Validator
/// </summary>
public class FridgeGlowSetupValidator : EditorWindow
{
    private const string GLOW_LAYER_NAME = "FridgeGlow";
    private const string GLOW_SHADER_PATH = "Custom/FridgeGlow_AlwaysOnTop";
    
    private Vector2 scrollPosition;
    private bool setupComplete = false;
    
    [MenuItem("Window/Cook Project/Fridge Glow Setup Validator")]
    public static void ShowWindow()
    {
        var window = GetWindow<FridgeGlowSetupValidator>("Fridge Glow Setup");
        window.minSize = new Vector2(400, 500);
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Fridge Glow System - Setup Validator", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Unity 6.2 URP", EditorStyles.miniLabel);
        EditorGUILayout.Space(10);
        
        DrawSeparator();
        
        // Check 1: Layer
        bool layerExists = CheckLayer();
        
        DrawSeparator();
        
        // Check 2: Shader
        bool shaderExists = CheckShader();
        
        DrawSeparator();
        
        // Check 3: Render Feature
        bool renderFeatureExists = CheckRenderFeature();
        
        DrawSeparator();
        
        // Check 4: Scripts
        bool scriptsExist = CheckScripts();
        
        DrawSeparator();
        
        // Summary
        setupComplete = layerExists && shaderExists && renderFeatureExists && scriptsExist;
        
        EditorGUILayout.Space(10);
        if (setupComplete)
        {
            EditorGUILayout.HelpBox("✓ Setup Complete! All components are configured correctly.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("⚠ Setup Incomplete - Please fix the issues above.", MessageType.Warning);
        }
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Refresh Validation", GUILayout.Height(30)))
        {
            Repaint();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private bool CheckLayer()
    {
        EditorGUILayout.LabelField("1. Layer Configuration", EditorStyles.boldLabel);
        
        int layerIndex = LayerMask.NameToLayer(GLOW_LAYER_NAME);
        
        if (layerIndex == -1)
        {
            EditorGUILayout.HelpBox($"✗ Layer '{GLOW_LAYER_NAME}' not found!", MessageType.Error);
            EditorGUILayout.LabelField("Action Required:");
            EditorGUILayout.LabelField("1. Go to Edit > Project Settings > Tags and Layers");
            EditorGUILayout.LabelField("2. Find an empty User Layer");
            EditorGUILayout.LabelField($"3. Name it: {GLOW_LAYER_NAME}");
            
            if (GUILayout.Button("Open Tags & Layers Settings"))
            {
                SettingsService.OpenProjectSettings("Project/Tags and Layers");
            }
            
            return false;
        }
        else
        {
            EditorGUILayout.HelpBox($"✓ Layer '{GLOW_LAYER_NAME}' exists (Layer {layerIndex})", MessageType.Info);
            return true;
        }
    }
    
    private bool CheckShader()
    {
        EditorGUILayout.LabelField("2. Shader Configuration", EditorStyles.boldLabel);
        
        Shader shader = Shader.Find(GLOW_SHADER_PATH);
        
        if (shader == null)
        {
            EditorGUILayout.HelpBox($"✗ Shader '{GLOW_SHADER_PATH}' not found!", MessageType.Error);
            EditorGUILayout.LabelField("Action Required:");
            EditorGUILayout.LabelField("1. Check if shader file exists: Assets/Shaders/FridgeGlow_AlwaysOnTop.shader");
            EditorGUILayout.LabelField("2. Verify no compilation errors in Console");
            EditorGUILayout.LabelField("3. Try Assets > Reimport All");
            
            if (GUILayout.Button("Locate Shader File"))
            {
                var shaderAsset = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/FridgeGlow_AlwaysOnTop.shader");
                if (shaderAsset != null)
                {
                    Selection.activeObject = shaderAsset;
                    EditorGUIUtility.PingObject(shaderAsset);
                }
                else
                {
                    Debug.LogError("Shader file not found at: Assets/Shaders/FridgeGlow_AlwaysOnTop.shader");
                }
            }
            
            return false;
        }
        else
        {
            EditorGUILayout.HelpBox($"✓ Shader '{GLOW_SHADER_PATH}' found", MessageType.Info);
            
            // Additional shader info
            EditorGUILayout.LabelField($"Render Queue: {shader.renderQueue}");
            
            // Check if shader has compilation errors
            if (ShaderUtil.GetShaderMessageCount(shader) > 0)
            {
                EditorGUILayout.HelpBox("⚠ Shader has compilation warnings/errors. Check Console.", MessageType.Warning);
            }
            
            return true;
        }
    }
    
    private bool CheckRenderFeature()
    {
        EditorGUILayout.LabelField("3. Render Feature Configuration", EditorStyles.boldLabel);
        
        // Try to find URP asset
        var urpAsset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        
        if (urpAsset == null)
        {
            EditorGUILayout.HelpBox("✗ URP Asset not found! Is URP properly configured?", MessageType.Error);
            return false;
        }
        
        // We can't directly check if the feature is added, so we provide instructions
        EditorGUILayout.HelpBox("⚠ Manual Verification Required", MessageType.Warning);
        EditorGUILayout.LabelField("Please verify:");
        EditorGUILayout.LabelField("1. Forward Renderer has 'Fridge Glow Render Feature' added");
        EditorGUILayout.LabelField("2. Feature is enabled (checkbox)");
        EditorGUILayout.LabelField($"3. Layer mask is set to '{GLOW_LAYER_NAME}'");
        EditorGUILayout.LabelField("4. Render Pass Event is 'AfterRenderingTransparents'");
        
        if (GUILayout.Button("Open Graphics Settings"))
        {
            SettingsService.OpenProjectSettings("Project/Graphics");
        }
        
        // Check if the script exists
        var featureScript = Resources.FindObjectsOfTypeAll<FridgeGlowRenderFeature>();
        if (featureScript != null && featureScript.Length > 0)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("✓ FridgeGlowRenderFeature script found in project", MessageType.Info);
            return true;
        }
        else
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("✗ FridgeGlowRenderFeature script not found", MessageType.Error);
            return false;
        }
    }
    
    private bool CheckScripts()
    {
        EditorGUILayout.LabelField("4. Required Scripts", EditorStyles.boldLabel);
        
        bool allScriptsExist = true;
        
        // Check FridgeGlowController
        if (CheckScriptExists("FridgeGlowController"))
        {
            EditorGUILayout.LabelField("✓ FridgeGlowController.cs", EditorStyles.miniLabel);
        }
        else
        {
            EditorGUILayout.LabelField("✗ FridgeGlowController.cs - Missing!", EditorStyles.miniLabel);
            allScriptsExist = false;
        }
        
        // Check FridgeGlowRenderFeature
        if (CheckScriptExists("FridgeGlowRenderFeature"))
        {
            EditorGUILayout.LabelField("✓ FridgeGlowRenderFeature.cs", EditorStyles.miniLabel);
        }
        else
        {
            EditorGUILayout.LabelField("✗ FridgeGlowRenderFeature.cs - Missing!", EditorStyles.miniLabel);
            allScriptsExist = false;
        }
        
        // Check FridgeGlowRenderPass
        if (CheckScriptExists("FridgeGlowRenderPass"))
        {
            EditorGUILayout.LabelField("✓ FridgeGlowRenderPass.cs", EditorStyles.miniLabel);
        }
        else
        {
            EditorGUILayout.LabelField("✗ FridgeGlowRenderPass.cs - Missing!", EditorStyles.miniLabel);
            allScriptsExist = false;
        }
        
        if (allScriptsExist)
        {
            EditorGUILayout.HelpBox("✓ All required scripts are present", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("✗ Some scripts are missing. Check Assets/Scripts/", MessageType.Error);
        }
        
        return allScriptsExist;
    }
    
    private bool CheckScriptExists(string scriptName)
    {
        string[] guids = AssetDatabase.FindAssets($"t:Script {scriptName}");
        return guids.Length > 0;
    }
    
    private void DrawSeparator()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(5);
    }
}
