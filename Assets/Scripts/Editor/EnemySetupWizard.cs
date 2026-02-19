using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using DungeonDredge.AI;
using DungeonDredge.Core;
using DungeonDredge.Inventory;

namespace DungeonDredge.Editor
{
    /// <summary>
    /// Editor wizard to help set up enemies from the creature asset pack.
    /// Creates EnemyData and EnemyAnimationData ScriptableObjects.
    /// </summary>
    public class EnemySetupWizard : EditorWindow
    {
        private Vector2 scrollPosition;
        private string animControllerFolder = "Assets/Settings/AnimControllers";
        private string prefabFolder = "Assets/Creatures";
        private string outputDataFolder = "Assets/ScriptableObjects/Enemies/Data";
        private string outputAnimFolder = "Assets/ScriptableObjects/Enemies/AnimData";

        // Enemy rank mapping
        private Dictionary<string, DungeonRank> rankMapping = new Dictionary<string, DungeonRank>
        {
            // Rank F - Flee behavior (pests)
            { "Bug", DungeonRank.F },
            { "Slug", DungeonRank.F },
            { "Creature_Creeping_01", DungeonRank.F },
            { "Creature_Creeping_02", DungeonRank.F },
            { "Creature_Creeping_03", DungeonRank.F },
            
            // Rank E - Basic aggressive
            { "Creature_Creeping_04", DungeonRank.E },
            { "Creature_Creeping_05", DungeonRank.E },
            { "Creature_Insect_01", DungeonRank.E },
            { "Creature_Insect_02", DungeonRank.E },
            { "Creature_Insect_03", DungeonRank.E },
            { "Creature_Insect_04", DungeonRank.E },
            { "Creature_Insect_05", DungeonRank.E },
            { "Creature_01", DungeonRank.E },
            { "Creature_02", DungeonRank.E },
            { "Creature_03", DungeonRank.E },
            
            // Rank D - Stalkers
            { "Creature_04", DungeonRank.D },
            { "Creature_05", DungeonRank.D },
            { "Creature_06", DungeonRank.D },
            { "Creature_07", DungeonRank.D },
            { "Creature_08", DungeonRank.D },
            { "Creature_Humanoid_01", DungeonRank.D },
            { "Creature_Humanoid_02", DungeonRank.D },
            { "Creature_Humanoid_03", DungeonRank.D },
            { "Creepy", DungeonRank.D },
            { "Alien", DungeonRank.D },
            
            // Rank C - Dangerous
            { "Creature_09", DungeonRank.C },
            { "Creature_10", DungeonRank.C },
            { "Creature_Humanoid_04", DungeonRank.C },
            { "Creature_Humanoid_05", DungeonRank.C },
            { "Daemon", DungeonRank.C },
            { "Hunter", DungeonRank.C },
            { "Nasty", DungeonRank.C },
            { "Creature_Rock", DungeonRank.C },
            { "Creature_Shell", DungeonRank.C },
            { "Creature_Spider", DungeonRank.C },
            
            // Rank B - Very dangerous
            { "Troll", DungeonRank.B },
            { "Fat", DungeonRank.B },
            { "Horned", DungeonRank.B },
            { "Pangolin", DungeonRank.B },
            { "Ripper_Dog", DungeonRank.B },
            { "Creature_Devourer", DungeonRank.B },
            { "Creature_mutant", DungeonRank.B },
            
            // Rank A - Elite
            { "Titan", DungeonRank.A },
            { "Creature_exterminator", DungeonRank.A },
            
            // Rank S - Boss
            { "Arachnid_Boss", DungeonRank.S },
        };

        [MenuItem("DungeonDredge/Enemy Setup Wizard")]
        public static void ShowWindow()
        {
            GetWindow<EnemySetupWizard>("Enemy Setup Wizard");
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("Enemy Setup Wizard", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool helps create EnemyData and EnemyAnimationData ScriptableObjects from your creature assets.", 
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Folder settings
            EditorGUILayout.LabelField("Folder Settings", EditorStyles.boldLabel);
            animControllerFolder = EditorGUILayout.TextField("Animation Controllers", animControllerFolder);
            prefabFolder = EditorGUILayout.TextField("Prefab Folder", prefabFolder);
            outputDataFolder = EditorGUILayout.TextField("Output Data Folder", outputDataFolder);
            outputAnimFolder = EditorGUILayout.TextField("Output Anim Folder", outputAnimFolder);

            EditorGUILayout.Space(10);

            // Action buttons
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("1. Create Output Folders"))
            {
                CreateOutputFolders();
            }

            if (GUILayout.Button("2. Generate EnemyAnimationData Assets"))
            {
                GenerateAnimationDataAssets();
            }

            if (GUILayout.Button("3. Generate EnemyData Assets"))
            {
                GenerateEnemyDataAssets();
            }

            if (GUILayout.Button("4. Link Animation Controllers to AnimData"))
            {
                LinkAnimationControllers();
            }

            EditorGUILayout.Space(10);

            // Quick setup
            EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);
            if (GUILayout.Button("Run All Steps"))
            {
                CreateOutputFolders();
                GenerateAnimationDataAssets();
                GenerateEnemyDataAssets();
                LinkAnimationControllers();
                EditorUtility.DisplayDialog("Complete", "Enemy setup complete! Check the ScriptableObjects folder.", "OK");
            }

            EditorGUILayout.Space(20);

            // Help section
            EditorGUILayout.LabelField("Instructions", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Copy animation controllers to: " + animControllerFolder + "\n" +
                "2. Copy/create prefabs in: " + prefabFolder + "\n" +
                "3. Run the wizard steps above\n" +
                "4. Manually assign prefabs to EnemyData assets in the Inspector",
                MessageType.None);

            EditorGUILayout.EndScrollView();
        }

        private void CreateOutputFolders()
        {
            CreateFolderIfNeeded(outputDataFolder);
            CreateFolderIfNeeded(outputAnimFolder);
            AssetDatabase.Refresh();
            Debug.Log("Created output folders");
        }

        private void CreateFolderIfNeeded(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                string folderName = Path.GetFileName(path);
                
                // Create parent folders recursively
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    CreateFolderIfNeeded(parent);
                }
                
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private void GenerateAnimationDataAssets()
        {
            int created = 0;

            // Get all animation controllers
            string[] controllerGuids = AssetDatabase.FindAssets("t:AnimatorController", new[] { animControllerFolder });

            foreach (string guid in controllerGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);
                
                // Extract enemy name from controller name (e.g., "Bug_Controller" -> "Bug")
                string enemyName = fileName.Replace("_Controller", "").Replace("Controller", "");

                string outputPath = $"{outputAnimFolder}/{enemyName}_AnimData.asset";

                // Skip if already exists
                if (AssetDatabase.LoadAssetAtPath<EnemyAnimationData>(outputPath) != null)
                {
                    continue;
                }

                // Create new asset
                EnemyAnimationData animData = ScriptableObject.CreateInstance<EnemyAnimationData>();
                AssetDatabase.CreateAsset(animData, outputPath);
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created {created} EnemyAnimationData assets");
        }

        private void GenerateEnemyDataAssets()
        {
            int created = 0;

            // For each known enemy in rank mapping
            foreach (var kvp in rankMapping)
            {
                string enemyName = kvp.Key;
                DungeonRank rank = kvp.Value;

                string outputPath = $"{outputDataFolder}/{enemyName}_Data.asset";

                // Skip if already exists
                if (AssetDatabase.LoadAssetAtPath<EnemyData>(outputPath) != null)
                {
                    continue;
                }

                // Create new asset
                EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>();
                
                // Set basic properties
                enemyData.enemyId = enemyName.ToLower().Replace(" ", "_");
                enemyData.enemyName = enemyName.Replace("_", " ");
                enemyData.minimumRank = rank;
                
                // Set behavior based on rank
                if (rank == DungeonRank.F)
                {
                    enemyData.behaviorType = EnemyBehaviorType.Flee;
                    enemyData.health = 50f;
                    enemyData.walkSpeed = 2f;
                    enemyData.chaseSpeed = 4f;
                    enemyData.attackDamage = 5f;
                }
                else if (rank == DungeonRank.E)
                {
                    enemyData.behaviorType = EnemyBehaviorType.Aggressive;
                    enemyData.health = 80f;
                    enemyData.walkSpeed = 2.5f;
                    enemyData.chaseSpeed = 5f;
                    enemyData.attackDamage = 10f;
                }
                else if (rank == DungeonRank.S)
                {
                    enemyData.behaviorType = EnemyBehaviorType.Stalker;
                    enemyData.health = 500f;
                    enemyData.walkSpeed = 3f;
                    enemyData.chaseSpeed = 7f;
                    enemyData.attackDamage = 30f;
                    enemyData.sightRange = 25f;
                }
                else
                {
                    enemyData.behaviorType = EnemyBehaviorType.Stalker;
                    float rankMultiplier = 1f + ((int)rank * 0.25f);
                    enemyData.health = 100f * rankMultiplier;
                    enemyData.walkSpeed = 2.5f + ((int)rank * 0.2f);
                    enemyData.chaseSpeed = 5f + ((int)rank * 0.3f);
                    enemyData.attackDamage = 10f * rankMultiplier;
                }

                // Try to find and link animation data
                string animDataPath = $"{outputAnimFolder}/{enemyName}_AnimData.asset";
                EnemyAnimationData animData = AssetDatabase.LoadAssetAtPath<EnemyAnimationData>(animDataPath);
                if (animData != null)
                {
                    enemyData.animationData = animData;
                }

                AssetDatabase.CreateAsset(enemyData, outputPath);
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created {created} EnemyData assets");
        }

        private void LinkAnimationControllers()
        {
            int linked = 0;

            // Get all animation data assets
            string[] animDataGuids = AssetDatabase.FindAssets("t:EnemyAnimationData", new[] { outputAnimFolder });

            foreach (string guid in animDataGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EnemyAnimationData animData = AssetDatabase.LoadAssetAtPath<EnemyAnimationData>(path);

                if (animData == null) continue;

                string fileName = Path.GetFileNameWithoutExtension(path);
                string enemyName = fileName.Replace("_AnimData", "");

                // Try to find matching controller
                RuntimeAnimatorController controller = FindControllerForEnemy(enemyName);

                if (controller != null)
                {
                    // Use SerializedObject to set private field
                    SerializedObject so = new SerializedObject(animData);
                    SerializedProperty controllerProp = so.FindProperty("_animatorController");
                    
                    if (controllerProp != null)
                    {
                        controllerProp.objectReferenceValue = controller;
                        so.ApplyModifiedProperties();
                        linked++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Linked {linked} animation controllers");
        }

        private RuntimeAnimatorController FindControllerForEnemy(string enemyName)
        {
            // Try exact match first
            string[] patterns = new[]
            {
                $"{enemyName}_Controller",
                $"{enemyName}Controller",
                enemyName
            };

            foreach (string pattern in patterns)
            {
                string[] guids = AssetDatabase.FindAssets($"{pattern} t:AnimatorController", new[] { animControllerFolder });
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
                }
            }

            // Try partial match for creature types
            if (enemyName.StartsWith("Creature_Humanoid"))
            {
                string[] guids = AssetDatabase.FindAssets("Creature_Humanoid_Controller t:AnimatorController", new[] { animControllerFolder });
                if (guids.Length > 0)
                {
                    return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
            else if (enemyName.StartsWith("Creature_Insect"))
            {
                string[] guids = AssetDatabase.FindAssets("Insect_Controller t:AnimatorController", new[] { animControllerFolder });
                if (guids.Length > 0)
                {
                    return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
            else if (enemyName.StartsWith("Creature_Creeping"))
            {
                string[] guids = AssetDatabase.FindAssets("Creeping_Controller t:AnimatorController", new[] { animControllerFolder });
                if (guids.Length > 0)
                {
                    return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }

            return null;
        }
    }
}
