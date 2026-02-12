using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool to configure and visualize blacksmith level to upgrade capacity mapping.
/// Window -> 4X Game -> Blacksmith Upgrade Capacity
/// </summary>
public class BlacksmithUpgradeCapacityEditor : EditorWindow
{
    private const string PREF_KEY_BASE_CAPACITY = "BlacksmithUpgrade_BaseCapacity";
    private const string PREF_KEY_PER_LEVEL = "BlacksmithUpgrade_PerLevel";
    
    private int baseCapacity = 3;
    private int upgradesPerLevel = 3;
    private int maxBlacksmithLevel = 6;
    
    private Vector2 scrollPosition;
    
    [MenuItem("Window/4X Game/Blacksmith Upgrade Capacity")]
    public static void ShowWindow()
    {
        var window = GetWindow<BlacksmithUpgradeCapacityEditor>("Blacksmith Upgrades");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }
    
    private void OnEnable()
    {
        LoadSettings();
    }
    
    private void LoadSettings()
    {
        baseCapacity = EditorPrefs.GetInt(PREF_KEY_BASE_CAPACITY, 3);
        upgradesPerLevel = EditorPrefs.GetInt(PREF_KEY_PER_LEVEL, 3);
    }
    
    private void SaveSettings()
    {
        EditorPrefs.SetInt(PREF_KEY_BASE_CAPACITY, baseCapacity);
        EditorPrefs.SetInt(PREF_KEY_PER_LEVEL, upgradesPerLevel);
    }
    
    private void OnGUI()
    {
        GUILayout.Space(10);
        
        EditorGUILayout.LabelField("Blacksmith Upgrade Capacity Configuration", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Configure how many ability upgrades (Range/Damage/FireRate) are available per blacksmith level.\n" +
            "Formula: Max Upgrades = Base Capacity + (Blacksmith Level × Upgrades Per Level)",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        // Configuration Section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        
        baseCapacity = EditorGUILayout.IntField(
            new GUIContent("Base Capacity", "Starting upgrade capacity at blacksmith level 0"),
            baseCapacity
        );
        baseCapacity = Mathf.Max(0, baseCapacity);
        
        upgradesPerLevel = EditorGUILayout.IntField(
            new GUIContent("Upgrades Per Level", "How many upgrades each blacksmith level adds"),
            upgradesPerLevel
        );
        upgradesPerLevel = Mathf.Max(1, upgradesPerLevel);
        
        maxBlacksmithLevel = EditorGUILayout.IntField(
            new GUIContent("Max Blacksmith Level", "Maximum blacksmith level (for preview)"),
            maxBlacksmithLevel
        );
        maxBlacksmithLevel = Mathf.Clamp(maxBlacksmithLevel, 1, 20);
        
        if (EditorGUI.EndChangeCheck())
        {
            SaveSettings();
        }
        
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // Preview Table
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Upgrade Capacity Preview", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
        
        // Table Header
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField("Blacksmith Level", EditorStyles.boldLabel, GUILayout.Width(120));
        EditorGUILayout.LabelField("Max Upgrades/Ability", EditorStyles.boldLabel, GUILayout.Width(150));
        EditorGUILayout.LabelField("Total Upgrades", EditorStyles.boldLabel, GUILayout.Width(120));
        EditorGUILayout.EndHorizontal();
        
        // Table Rows
        for (int level = 0; level <= maxBlacksmithLevel; level++)
        {
            int maxUpgradesPerAbility = CalculateMaxUpgrades(level);
            int totalUpgrades = maxUpgradesPerAbility * 3; // 3 abilities: Range, Damage, FireRate
            
            Color bgColor = level % 2 == 0 ? new Color(0.8f, 0.8f, 0.8f, 0.3f) : new Color(0.7f, 0.7f, 0.7f, 0.2f);
            
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = bgColor;
            
            EditorGUILayout.LabelField($"Level {level}", GUILayout.Width(120));
            EditorGUILayout.LabelField($"{maxUpgradesPerAbility}", GUILayout.Width(150));
            EditorGUILayout.LabelField($"{totalUpgrades}", GUILayout.Width(120));
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // Code Generation Section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Code Update", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Click below to update UpgradeTracker.cs with these values.",
            MessageType.Info
        );
        
        if (GUILayout.Button("Apply to UpgradeTracker.cs", GUILayout.Height(30)))
        {
            ApplyToCode();
        }
        
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // Current Code Values
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Current Code Values", EditorStyles.boldLabel);
        
        string upgradeTrackerPath = "Assets/_Arts/FightScene/scripts/UpgradeTracker.cs";
        if (System.IO.File.Exists(upgradeTrackerPath))
        {
            string code = System.IO.File.ReadAllText(upgradeTrackerPath);
            
            // Try to extract current values from code
            int currentBase = ExtractValueFromCode(code, "return", "+");
            int currentPerLevel = ExtractValueFromCode(code, "UPGRADES_PER_BLACKSMITH_LEVEL = ");
            
            EditorGUILayout.LabelField($"Base Capacity in Code: {currentBase}");
            EditorGUILayout.LabelField($"Per Level in Code: {currentPerLevel}");
            
            if (currentBase != baseCapacity || currentPerLevel != upgradesPerLevel)
            {
                EditorGUILayout.HelpBox("⚠️ Editor values differ from code values!", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("✓ Editor values match code values", MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("UpgradeTracker.cs not found at expected path", MessageType.Warning);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private int CalculateMaxUpgrades(int blacksmithLevel)
    {
        return baseCapacity + (blacksmithLevel * upgradesPerLevel);
    }
    
    private int ExtractValueFromCode(string code, string searchAfter, string searchBefore = ";")
    {
        try
        {
            int startIndex = code.IndexOf(searchAfter);
            if (startIndex < 0) return -1;
            
            startIndex += searchAfter.Length;
            int endIndex = code.IndexOf(searchBefore, startIndex);
            if (endIndex < 0) return -1;
            
            string valueStr = code.Substring(startIndex, endIndex - startIndex).Trim();
            
            // Extract just the number
            string numStr = "";
            foreach (char c in valueStr)
            {
                if (char.IsDigit(c))
                    numStr += c;
                else if (numStr.Length > 0)
                    break;
            }
            
            if (int.TryParse(numStr, out int value))
                return value;
        }
        catch { }
        
        return -1;
    }
    
    private void ApplyToCode()
    {
        string path = "Assets/_Arts/FightScene/scripts/UpgradeTracker.cs";
        
        if (!System.IO.File.Exists(path))
        {
            EditorUtility.DisplayDialog("Error", "UpgradeTracker.cs not found at: " + path, "OK");
            return;
        }
        
        try
        {
            string code = System.IO.File.ReadAllText(path);
            
            // Update UPGRADES_PER_BLACKSMITH_LEVEL constant
            code = System.Text.RegularExpressions.Regex.Replace(
                code,
                @"public const int UPGRADES_PER_BLACKSMITH_LEVEL = \d+;",
                $"public const int UPGRADES_PER_BLACKSMITH_LEVEL = {upgradesPerLevel};"
            );
            
            // Update the MaxLevel property calculation
            code = System.Text.RegularExpressions.Regex.Replace(
                code,
                @"return \d+ \+ \(blacksmithLevel \* UPGRADES_PER_BLACKSMITH_LEVEL\);",
                $"return {baseCapacity} + (blacksmithLevel * UPGRADES_PER_BLACKSMITH_LEVEL);"
            );
            
            System.IO.File.WriteAllText(path, code);
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog(
                "Success",
                $"Updated UpgradeTracker.cs:\n" +
                $"• Base Capacity: {baseCapacity}\n" +
                $"• Upgrades Per Level: {upgradesPerLevel}",
                "OK"
            );
            
            Debug.Log($"[BlacksmithUpgradeCapacityEditor] Updated UpgradeTracker.cs with Base={baseCapacity}, PerLevel={upgradesPerLevel}");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", "Failed to update code: " + e.Message, "OK");
        }
    }
}
