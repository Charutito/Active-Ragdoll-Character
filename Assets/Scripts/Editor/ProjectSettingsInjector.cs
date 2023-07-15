using UnityEditor;
using UnityEngine;

public class ProjectSettingsInjector : EditorWindow
{
    private static ProjectSettingsInjector instance;
    
    [MenuItem("Tools/Active Ragdoll Project Settings Injector")]
    static void APRPlayerSettingsInjectorWindow()
    {
        if(instance == null)
        {
            ProjectSettingsInjector window = CreateInstance(typeof(ProjectSettingsInjector)) as ProjectSettingsInjector;
            window.maxSize = new Vector2(350f, 190f);
            window.minSize = window.maxSize;
            window.ShowUtility();
        }
    }
    
    void OnEnable()
    {
        instance = this;
    }
    
    void OnGUI()
    { 
        GUI.skin.label.wordWrap = true;

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        GUILayout.Label("This tool will apply all the required project settings needed like new layers, tags, world gravity and physics iterations");
        EditorGUILayout.Space();
        if(GUILayout.Button("Inject Project Settings"))
        {
            Physics.gravity = new Vector3(0, -25, 0);
            Physics.defaultSolverIterations = 25;
            Physics.defaultSolverVelocityIterations = 15;
            
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
 
            SerializedProperty layersProp = tagManager.FindProperty("layers");
 
            string canBeGrabbed = "CanBeGrabbed";
 
            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(canBeGrabbed))
                {
                    found = true; break;
                }
            }
 
            if (!found)
            {
                tagsProp.InsertArrayElementAtIndex(0);
                SerializedProperty n = tagsProp.GetArrayElementAtIndex(0);
                n.stringValue = canBeGrabbed;
            }
            
            string layerName_1 = "Player_1";
            string layerName_2 = "Player_2";
            string layerName_3 = "Player_3";
            string layerName_4 = "Player_4";
            string layerName_5 = "Ground";
 
            SerializedProperty sp1 = layersProp.GetArrayElementAtIndex(10);
            if (sp1 != null)
            {
                sp1.stringValue = layerName_1;
            }
            
            SerializedProperty sp2 = layersProp.GetArrayElementAtIndex(11);
            if (sp2 != null)
            {
                sp2.stringValue = layerName_2;
            }
            
            SerializedProperty sp3 = layersProp.GetArrayElementAtIndex(12);
            if (sp3 != null)
            {
                sp3.stringValue = layerName_3;
            }
            
            SerializedProperty sp4 = layersProp.GetArrayElementAtIndex(13);
            if (sp4 != null)
            {
                sp4.stringValue = layerName_4;
            }
            
            SerializedProperty sp5 = layersProp.GetArrayElementAtIndex(14);
            if (sp5 != null)
            {
                sp5.stringValue = layerName_5;
            }

            tagManager.ApplyModifiedProperties();
            
            Debug.Log("The project settings has been successfully injected");
            
            this.Close();
        }
    }
    
    void OnDisable()
    {
        instance = null;
    }
}