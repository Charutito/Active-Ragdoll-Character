using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RagdollBinder : EditorWindow
{
    private static RagdollBinder instance;
    
    private GameObject MixamoModel;
    private const string prefix = "mixamorig:";
    private string[] boneNames = { "Hips", "Spine", "Head", "RightArm", "RightForeArm", "RightHand", "LeftArm", "LeftForeArm", "LeftHand", "RightUpLeg", "RightLeg", "RightFoot", "LeftUpLeg", "LeftLeg", "LeftFoot" };

    //Container
    private GameObject refContainer;
    private GameObject Container;

    //COMP
    private GameObject refCOMP;

    //Root
    private GameObject refRoot;
    private GameObject RootChildren;
    private GameObject Root;

    //Body
    private GameObject refBody;
    private GameObject BodyChildren;
    private GameObject Body;

    //Head
    private GameObject refHead;
    private GameObject HeadChildren;
    private GameObject Head;

    //UpperRightArm
    private GameObject refUpperRightArm;
    private GameObject UpperRightArmChildren;
    private GameObject UpperRightArm;

    //LowerRightArm
    private GameObject refLowerRightArm;
    private GameObject LowerRightArmChildren;
    private GameObject LowerRightArm;

    //RightHand
    private GameObject refRightHand;
    private GameObject RightHandChildren;
    private GameObject RightHand;

    //UpperLeftArm
    private GameObject refUpperLeftArm;
    private GameObject UpperLeftArmChildren;
    private GameObject UpperLeftArm;

    //LowerLeftArm
    private GameObject refLowerLeftArm;
    private GameObject LowerLeftArmChildren;
    private GameObject LowerLeftArm;

    //RightHand
    private GameObject refLeftHand;
    private GameObject LeftHandChildren;
    private GameObject LeftHand;

    //UpperRightLeg
    private GameObject refUpperRightLeg;
    private GameObject UpperRightLegChildren;
    private GameObject UpperRightLeg;

    //LowerRightLeg
    private GameObject refLowerRightLeg;
    private GameObject LowerRightLegChildren;
    private GameObject LowerRightLeg;

    //RightFoot
    private GameObject refRightFoot;
    private GameObject RightFootChildren;
    private GameObject RightFoot;

    //UpperLeftLeg
    private GameObject refUpperLeftLeg;
    private GameObject UpperLeftLegChildren;
    private GameObject UpperLeftLeg;

    //LowerLeftLeg
    private GameObject refLowerLeftLeg;
    private GameObject LowerLeftLegChildren;
    private GameObject LowerLeftLeg;

    //LeftFoot
    private GameObject refLeftFoot;
    private GameObject LeftFootChildren;
    private GameObject LeftFoot;
    
    [MenuItem("Tools/Active Ragdoll Binder")]
    static void RagdollBinderWindow()
    {
        if(instance == null)
        {
            RagdollBinder window = CreateInstance(typeof(RagdollBinder)) as RagdollBinder;
            window.maxSize = new Vector2(350f, 640f);
            window.minSize = window.maxSize;
            window.ShowUtility();
        }
    }
    
    void OnEnable()
    {
        instance = this;
    }
    
    private bool showMixamoModel;
    private bool showBoneFields;

    private void OnGUI()
    {
        GUI.skin.label.wordWrap = true;

        ShowInstructions();
        ShowWarningsAndInfo();

        ShowMixamoModelButton();
        ShowBoneFields();

        ShowBindButton();
    }

    private void ShowInstructions()
    {
        EditorGUILayout.Space();
        GUILayout.Label("Import the ActiveRagdoll_Player into the scene, align and scale it to fit your model, then link the respective bones of your model below");
    }

    private void ShowWarningsAndInfo()
    {
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Note: The ActiveRagdoll_Player box model will represent your colliders as well", MessageType.Warning);
        EditorGUILayout.Space();

        EditorGUILayout.Space();
    }

    private void ShowMixamoModelButton()
    {
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Note: If you have a Mixamo model you can autocomplete the Bone Fields! Drag the root of the model below", MessageType.Info);
        EditorGUILayout.Space();

        MixamoModel = DrawObjectField("Mixamo Model", MixamoModel);
        if (GUILayout.Button("Autocomplete Bone Fields with the Mixamo Model"))
        {
            if (MixamoModel != null)
            {
                ImportMixamoModel();
            }
            else
            {
                Debug.LogError("Mixamo Model is null");
            }
        }
    }

    private void ShowBoneFields()
    {
        EditorGUILayout.Space();

        showBoneFields = EditorGUILayout.Foldout(showBoneFields, "Bone Fields");
        if (showBoneFields)
        {
            EditorGUILayout.BeginVertical();

            Container = DrawObjectField("Model Container", Container);
            Root = DrawObjectField("Root Bone", Root);
            Body = DrawObjectField("Body Bone", Body);
            Head = DrawObjectField("Head Bone", Head);
            UpperRightArm = DrawObjectField("Upper Right Arm Bone", UpperRightArm);
            LowerRightArm = DrawObjectField("Lower Right Arm Bone", LowerRightArm);
            RightHand = DrawObjectField("Right Hand Bone", RightHand);
            UpperLeftArm = DrawObjectField("Upper Left Arm Bone", UpperLeftArm);
            LowerLeftArm = DrawObjectField("Lower Left Arm Bone", LowerLeftArm);
            LeftHand = DrawObjectField("Left Hand Bone", LeftHand);
            UpperRightLeg = DrawObjectField("Upper Right Leg Bone", UpperRightLeg);
            LowerRightLeg = DrawObjectField("Lower Right Leg Bone", LowerRightLeg);
            RightFoot = DrawObjectField("Right Foot Bone", RightFoot);
            UpperLeftLeg = DrawObjectField("Upper Left Leg Bone", UpperLeftLeg);
            LowerLeftLeg = DrawObjectField("Lower Left Leg Bone", LowerLeftLeg);
            LeftFoot = DrawObjectField("Left Foot Bone", LeftFoot);

            EditorGUILayout.EndVertical();
        }
    }

    private GameObject DrawObjectField(string label, GameObject value)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label);
        value = (GameObject)EditorGUILayout.ObjectField(value, typeof(GameObject), true, GUILayout.Width(180));
        EditorGUILayout.EndHorizontal();

        return value;
    }

    private void ShowBindButton()
    {
        EditorGUILayout.Space();
        if (GUILayout.Button("Bind Active Physics Ragdoll Player"))
        {
            BindRagdoll();
        }
    }
    
    private void ImportMixamoModel()
    {
        Transform[] allTransforms = MixamoModel.GetComponentsInChildren<Transform>(true);
        
        Dictionary<string, GameObject> bonesMatchDict = new Dictionary<string, GameObject>();

        foreach (var t in boneNames)
        {
            foreach (Transform boneTransform in allTransforms)
            {
                if (boneTransform.name == (prefix + t))
                {
                    bonesMatchDict.Add(t, boneTransform.gameObject);
                }
            }
        }
        
        Container = MixamoModel;
        Root = bonesMatchDict["Hips"];
        Body = bonesMatchDict["Spine"];
        Head = bonesMatchDict["Head"];
        UpperRightArm = bonesMatchDict["RightArm"];
        LowerRightArm = bonesMatchDict["RightForeArm"];
        RightHand = bonesMatchDict["RightHand"];
        UpperLeftArm = bonesMatchDict["LeftArm"];
        LowerLeftArm = bonesMatchDict["LeftForeArm"];
        LeftHand = bonesMatchDict["LeftHand"];
        UpperRightLeg = bonesMatchDict["RightUpLeg"];
        LowerRightLeg = bonesMatchDict["RightLeg"];
        RightFoot = bonesMatchDict["RightFoot"];
        UpperLeftLeg = bonesMatchDict["LeftUpLeg"];
        LowerLeftLeg = bonesMatchDict["LeftLeg"];
        LeftFoot = bonesMatchDict["LeftFoot"];
    }

    void BindRagdoll()
    {
        refContainer = GameObject.Find("ActiveRagdoll_Player");
        
        if(PrefabUtility.GetPrefabInstanceStatus(refContainer) != PrefabInstanceStatus.NotAPrefab)
        {
            PrefabUtility.UnpackPrefabInstance(refContainer, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
        
        if(PrefabUtility.GetPrefabInstanceStatus(Container) != PrefabInstanceStatus.NotAPrefab)
        {
            PrefabUtility.UnpackPrefabInstance(Container, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
        
        refRoot = GameObject.Find("ActiveRagdoll_Root");
        refBody = GameObject.Find("ActiveRagdoll_Body");
        refHead = GameObject.Find("ActiveRagdoll_Head");
        refUpperRightArm = GameObject.Find("ActiveRagdoll_UpperRightArm");
        refLowerRightArm = GameObject.Find("ActiveRagdoll_LowerRightArm");
        refRightHand = GameObject.Find("ActiveRagdoll_RightHand");
        refUpperLeftArm = GameObject.Find("ActiveRagdoll_UpperLeftArm");
        refLowerLeftArm = GameObject.Find("ActiveRagdoll_LowerLeftArm");
        refLeftHand = GameObject.Find("ActiveRagdoll_LeftHand");
        refUpperRightLeg = GameObject.Find("ActiveRagdoll_UpperRightLeg");
        refLowerRightLeg = GameObject.Find("ActiveRagdoll_LowerRightLeg");
        refRightFoot = GameObject.Find("ActiveRagdoll_RightFoot");
        refUpperLeftLeg = GameObject.Find("ActiveRagdoll_UpperLeftLeg");
        refLowerLeftLeg = GameObject.Find("ActiveRagdoll_LowerLeftLeg");
        refLeftFoot = GameObject.Find("ActiveRagdoll_LeftFoot");
        refCOMP = GameObject.Find("ActiveRagdoll_COMP");
		
		
        //Root
        RootChildren = Root.transform.gameObject;
        refRoot.transform.parent = Root.transform.parent;
        RootChildren.transform.parent = refRoot.transform;
        DestroyImmediate(refRoot.GetComponent<MeshRenderer>());
        DestroyImmediate(refRoot.GetComponent<MeshFilter>());
		
        //Body
        BodyChildren = Body.transform.gameObject;
        refBody.transform.parent = Body.transform.parent;
        BodyChildren.transform.parent = refBody.transform;
        DestroyImmediate(refBody.GetComponent<MeshRenderer>());
        DestroyImmediate(refBody.GetComponent<MeshFilter>());
		
        //Head
        HeadChildren = Head.transform.gameObject;
        refHead.transform.parent = Head.transform.parent;
        HeadChildren.transform.parent = refHead.transform;
        DestroyImmediate(refHead.GetComponent<MeshRenderer>());
        DestroyImmediate(refHead.GetComponent<MeshFilter>());
		
        //UpperRightArm
        UpperRightArmChildren = UpperRightArm.transform.gameObject;
        refUpperRightArm.transform.parent = UpperRightArm.transform.parent;
        UpperRightArmChildren.transform.parent = refUpperRightArm.transform;
        DestroyImmediate(refUpperRightArm.GetComponent<MeshRenderer>());
        DestroyImmediate(refUpperRightArm.GetComponent<MeshFilter>());
		
        //LowerRightArm
        LowerRightArmChildren = LowerRightArm.transform.gameObject;
        refLowerRightArm.transform.parent = LowerRightArm.transform.parent;
        LowerRightArmChildren.transform.parent = refLowerRightArm.transform;
        DestroyImmediate(refLowerRightArm.GetComponent<MeshRenderer>());
        DestroyImmediate(refLowerRightArm.GetComponent<MeshFilter>());
        
        //RightHand
        RightHandChildren = RightHand.transform.gameObject;
        refRightHand.transform.parent = RightHand.transform.parent;
        RightHandChildren.transform.parent = refRightHand.transform;
        DestroyImmediate(refRightHand.GetComponent<MeshRenderer>());
        DestroyImmediate(refRightHand.GetComponent<MeshFilter>());
		
        //UpperLeftArm
        UpperLeftArmChildren = UpperLeftArm.transform.gameObject;
        refUpperLeftArm.transform.parent = UpperLeftArm.transform.parent;
        UpperLeftArmChildren.transform.parent = refUpperLeftArm.transform;
        DestroyImmediate(refUpperLeftArm.GetComponent<MeshRenderer>());
        DestroyImmediate(refUpperLeftArm.GetComponent<MeshFilter>());
		
        //LowerLeftArm
        LowerLeftArmChildren = LowerLeftArm.transform.gameObject;
        refLowerLeftArm.transform.parent = LowerLeftArm.transform.parent;
        LowerLeftArmChildren.transform.parent = refLowerLeftArm.transform;
        DestroyImmediate(refLowerLeftArm.GetComponent<MeshRenderer>());
        DestroyImmediate(refLowerLeftArm.GetComponent<MeshFilter>());
        
        //LeftHand
        LeftHandChildren = LeftHand.transform.gameObject;
        refLeftHand.transform.parent = LeftHand.transform.parent;
        LeftHandChildren.transform.parent = refLeftHand.transform;
        DestroyImmediate(refLeftHand.GetComponent<MeshRenderer>());
        DestroyImmediate(refLeftHand.GetComponent<MeshFilter>());
		
        //UpperRightLeg
        UpperRightLegChildren = UpperRightLeg.transform.gameObject;
        refUpperRightLeg.transform.parent = UpperRightLeg.transform.parent;
        UpperRightLegChildren.transform.parent = refUpperRightLeg.transform;
        DestroyImmediate(refUpperRightLeg.GetComponent<MeshRenderer>());
        DestroyImmediate(refUpperRightLeg.GetComponent<MeshFilter>());
		
        //LowerRightLeg
        LowerRightLegChildren = LowerRightLeg.transform.gameObject;
        refLowerRightLeg.transform.parent = LowerRightLeg.transform.parent;
        LowerRightLegChildren.transform.parent = refLowerRightLeg.transform;
        DestroyImmediate(refLowerRightLeg.GetComponent<MeshRenderer>());
        DestroyImmediate(refLowerRightLeg.GetComponent<MeshFilter>());
		
        //RightFoot
        RightFootChildren = RightFoot.transform.gameObject;
        refRightFoot.transform.parent = RightFoot.transform.parent;
        RightFootChildren.transform.parent = refRightFoot.transform;
        DestroyImmediate(refRightFoot.GetComponent<MeshRenderer>());
        DestroyImmediate(refRightFoot.GetComponent<MeshFilter>());
		
        //UpperLeftLeg
        UpperLeftLegChildren = UpperLeftLeg.transform.gameObject;
        refUpperLeftLeg.transform.parent = UpperLeftLeg.transform.parent;
        UpperLeftLegChildren.transform.parent = refUpperLeftLeg.transform;
        DestroyImmediate(refUpperLeftLeg.GetComponent<MeshRenderer>());
        DestroyImmediate(refUpperLeftLeg.GetComponent<MeshFilter>());
		
        //LowerLeftLeg
        LowerLeftLegChildren = LowerLeftLeg.transform.gameObject;
        refLowerLeftLeg.transform.parent = LowerLeftLeg.transform.parent;
        LowerLeftLegChildren.transform.parent = refLowerLeftLeg.transform;
        DestroyImmediate(refLowerLeftLeg.GetComponent<MeshRenderer>());
        DestroyImmediate(refLowerLeftLeg.GetComponent<MeshFilter>());
		
        //LeftFoot
        LeftFootChildren = LeftFoot.transform.gameObject;
        refLeftFoot.transform.parent = LeftFoot.transform.parent;
        LeftFootChildren.transform.parent = refLeftFoot.transform;
        DestroyImmediate(refLeftFoot.GetComponent<MeshRenderer>());
        DestroyImmediate(refLeftFoot.GetComponent<MeshFilter>());
		
        //COMP
        refCOMP.transform.parent = Root.transform.root;
		
        //Container
        Container.transform.parent = refContainer.transform;
		
        Debug.Log("Ragdoll has been binded");
        
        this.Close();
    }
	
    void OnDisable()
    {
        instance = null;
    }
}