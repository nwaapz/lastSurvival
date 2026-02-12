using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor script to auto-generate Scenario Step assets for levels 1-10.
/// Each level gets 3 steps: MoveToLocation -> WaitForMapIconClick -> GoToFight
/// </summary>
public class GenerateLevelScenariosEditor : EditorWindow
{
    private string fightSceneName = "Fight";
    private string mapIconIdPrefix = "FightIcon_Level";
    
    [MenuItem("Tools/Scenario/Generate First 10 Levels")]
    public static void ShowWindow()
    {
        GetWindow<GenerateLevelScenariosEditor>("Generate Levels 1-10");
    }

    private void OnGUI()
    {
        GUILayout.Label("Generate Scenario Steps for Levels 1-10", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        fightSceneName = EditorGUILayout.TextField("Fight Scene Name", fightSceneName);
        mapIconIdPrefix = EditorGUILayout.TextField("Map Icon ID Prefix", mapIconIdPrefix);
        
        GUILayout.Space(10);
        GUILayout.Label("Each level will get 3 steps:", EditorStyles.miniLabel);
        GUILayout.Label("  1. MoveUnitToLocationStep (Hero → CustomN)", EditorStyles.miniLabel);
        GUILayout.Label("  2. WaitForMapIconClickStep (FightIcon_LevelN)", EditorStyles.miniLabel);
        GUILayout.Label("  3. GoToFightStep (→ Fight scene)", EditorStyles.miniLabel);
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("Generate All Level Assets", GUILayout.Height(40)))
        {
            GenerateAllLevels();
        }
    }

    private void GenerateAllLevels()
    {
        // LocationName enum values for Custom1-Custom10
        LocationName[] customLocations = new LocationName[]
        {
            LocationName.Custom1,
            LocationName.Custom2,
            LocationName.Custom3,
            LocationName.Custom4,
            LocationName.Custom5,
            LocationName.Custom6,
            LocationName.Custom7,
            LocationName.Custom8,
            LocationName.Custom9,
            LocationName.Custom10
        };

        for (int level = 1; level <= 10; level++)
        {
            string folderPath = $"Assets/Scenario/level{level}";
            
            // Create folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parentPath = "Assets/Scenario";
                if (!AssetDatabase.IsValidFolder(parentPath))
                {
                    AssetDatabase.CreateFolder("Assets", "Scenario");
                }
                AssetDatabase.CreateFolder(parentPath, $"level{level}");
            }

            // 1. Create MoveUnitToLocationStep
            var moveStep = CreateInstance<MoveUnitToLocationStep>();
            moveStep.description = $"Level {level}: Hero walks to Custom{level}";
            moveStep.characterToMove = CharacterName.Hero;
            moveStep.targetLocation = customLocations[level - 1];
            moveStep.movementMode = MovementMode.Walk;
            moveStep.arrivalRadius = 1f;
            moveStep.isBlocking = true;
            moveStep.saveOnComplete = true;
            moveStep.activeScene = "basebuilder"; // Set the scene this runs in
            
            string moveAssetPath = $"{folderPath}/Level_{level}_MoveToCustom{level}.asset";
            AssetDatabase.CreateAsset(moveStep, moveAssetPath);

            // 2. Create WaitForMapIconClickStep
            var iconStep = CreateInstance<WaitForMapIconClickStep>();
            iconStep.description = $"Level {level}: Click fight icon to start battle";
            iconStep.targetIconId = $"{mapIconIdPrefix}{level}";
            iconStep.activateIcon = true;
            iconStep.blockOtherInput = true;
            iconStep.moveCameraToIcon = true;
            iconStep.cameraMoveTime = 0.5f;
            iconStep.hintText = "Tap to start the battle!";
            iconStep.showTutorialHand = true;
            iconStep.isBlocking = true;
            iconStep.saveOnComplete = true;
            iconStep.activeScene = "basebuilder";
            
            string iconAssetPath = $"{folderPath}/Level_{level}_WaitForFightIcon.asset";
            AssetDatabase.CreateAsset(iconStep, iconAssetPath);

            // 3. Create GoToFightStep
            var fightStep = CreateInstance<GoToFightStep>();
            fightStep.description = $"Level {level}: Transition to fight scene";
            fightStep.fightSceneName = fightSceneName;
            fightStep.waitForButtonClick = false; // Transition immediately after icon click
            fightStep.enemyCount = 10 + (level * 2); // Scale enemies per level
            fightStep.difficultyMultiplier = 1f + (level * 0.1f);
            fightStep.isBlocking = true;
            fightStep.saveOnComplete = true;
            fightStep.activeScene = "basebuilder";
            
            string fightAssetPath = $"{folderPath}/Level_{level}_GoToFight.asset";
            AssetDatabase.CreateAsset(fightStep, fightAssetPath);

            // 4. Create or Update LevelScenarioConfig
            var scenarioConfig = CreateInstance<LevelScenarioConfig>();
            scenarioConfig.levelNumber = level;
            scenarioConfig.advanceLevelOnComplete = true;
            scenarioConfig.startingSceneName = "basebuilder";
            scenarioConfig.steps.Add(moveStep);
            scenarioConfig.steps.Add(iconStep);
            scenarioConfig.steps.Add(fightStep);
            
            string configAssetPath = $"{folderPath}/Level_{level}_Scenario.asset";
            AssetDatabase.CreateAsset(scenarioConfig, configAssetPath);

            Debug.Log($"[GenerateLevelScenarios] Created Level {level} scenario assets at {folderPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Success!", 
            "Generated scenario assets for levels 1-10!\n\n" +
            "Next steps:\n" +
            "1. Place NamedLocation markers at Custom1-10 in basebuilder scene\n" +
            "2. Place MapIcon objects near those locations (IconId: FightIcon_Level1, etc.)\n" +
            "3. Assign scenarios to ScenarioManager.levelScenarios list",
            "OK");
    }
}
