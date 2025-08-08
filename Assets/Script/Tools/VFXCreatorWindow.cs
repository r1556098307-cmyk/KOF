#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;


public class VFXCreatorWindow : EditorWindow
{
    private string vfxName = "NewVFX";
    private List<Sprite> spritesList = new List<Sprite>();
    private float fps = 30;
    private bool additive = true;
    private bool loop = false;
    private Vector2 scrollPos;

    [MenuItem("Tools/Quick VFX Creator")]
    static void ShowWindow()
    {
        GetWindow<VFXCreatorWindow>("VFX Creator");
    }

    void OnGUI()
    {
        GUILayout.Label("���ٴ���VFXԤ����", EditorStyles.boldLabel);

        vfxName = EditorGUILayout.TextField("��Ч����", vfxName);

        EditorGUILayout.Space();

        // Sprite�б����
        EditorGUILayout.LabelField("Sprite����֡", EditorStyles.boldLabel);

        // ��ק����
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "��קSprites������");

        Event evt = Event.current;
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    break;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (Object dragged in DragAndDrop.objectReferences)
                    {
                        if (dragged is Sprite)
                        {
                            if (!spritesList.Contains((Sprite)dragged))
                                spritesList.Add((Sprite)dragged);
                        }
                        else if (dragged is Texture2D)
                        {
                            // ���Լ���ΪSprite
                            string path = AssetDatabase.GetAssetPath(dragged);
                            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
                            foreach (var sprite in sprites)
                            {
                                if (!spritesList.Contains(sprite))
                                    spritesList.Add(sprite);
                            }
                        }
                    }

                    // ����
                    spritesList.Sort((a, b) => a.name.CompareTo(b.name));
                    evt.Use();
                }
                break;
        }

        // ��ʾSprite�б�
        if (spritesList.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"����� {spritesList.Count} ��Sprites");
            if (GUILayout.Button("���", GUILayout.Width(50)))
            {
                spritesList.Clear();
            }
            if (GUILayout.Button("����", GUILayout.Width(50)))
            {
                spritesList.Sort((a, b) => a.name.CompareTo(b.name));
            }
            EditorGUILayout.EndHorizontal();

            // Ԥ���б�
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(100));
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < spritesList.Count; i++)
            {
                if (spritesList[i] != null)
                {
                    // ��ʾ����ͼ
                    GUILayout.Label(AssetPreview.GetAssetPreview(spritesList[i]),
                        GUILayout.Width(50), GUILayout.Height(50));
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space();

        fps = EditorGUILayout.FloatField("FPS", fps);
        additive = EditorGUILayout.Toggle("����Ч��", additive);
        loop = EditorGUILayout.Toggle("ѭ��", loop);

        EditorGUILayout.Space();

        GUI.enabled = spritesList.Count > 0;
        if (GUILayout.Button("����Ԥ����", GUILayout.Height(30)))
        {
            CreateVFXPrefab();
        }
        GUI.enabled = true;

        EditorGUILayout.Space();

        if (GUILayout.Button("�Զ�����Manager", GUILayout.Height(25)))
        {
            SetupManager();
        }
    }

    void CreateVFXPrefab()
    {
        if (spritesList.Count == 0)
        {
            EditorUtility.DisplayDialog("����", "��������Sprite����", "OK");
            return;
        }

        // ����GameObject
        GameObject go = new GameObject(vfxName);

        // ������
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        SimpleVFX vfx = go.AddComponent<SimpleVFX>();

        // ���ò���
        vfx.sprites = spritesList.ToArray();
        vfx.fps = fps;
        vfx.additive = additive;
        vfx.loop = loop;

        // ����ΪԤ����
        string path = $"Assets/VFX/Prefabs/{vfxName}.prefab";

        // ȷ��Ŀ¼����
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/VFX"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "VFX");

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        // �Զ���ӵ�Manager
        AddToManager(vfxName, prefab);

        EditorUtility.DisplayDialog("�ɹ�", $"Ԥ�����Ѵ���: {path}", "OK");

        // ѡ���´�����Ԥ����
        Selection.activeObject = prefab;
    }

    void AddToManager(string name, GameObject prefab)
    {
        // ���ҳ����е�Manager
        VFXManager manager = FindObjectOfType<VFXManager>();

        if (manager == null)
        {
            // ����Manager
            GameObject mgrObj = new GameObject("VFXManager");
            manager = mgrObj.AddComponent<VFXManager>();
        }

        // ��ӵ��б�
        if (manager.vfxPresets == null)
            manager.vfxPresets = new List<VFXManager.VFXPreset>();

        // ����Ƿ��Ѵ���
        if (!manager.vfxPresets.Exists(p => p.name == name))
        {
            manager.vfxPresets.Add(new VFXManager.VFXPreset(name, prefab));
            EditorUtility.SetDirty(manager);
        }
    }

    void SetupManager()
    {
        VFXManager manager = FindObjectOfType<VFXManager>();
        if (manager == null)
        {
            GameObject mgrObj = new GameObject("VFXManager");
            manager = mgrObj.AddComponent<VFXManager>();
        }

        Selection.activeObject = manager.gameObject;
        EditorUtility.DisplayDialog("���", "Manager�Ѵ���/ѡ��", "OK");
    }
}

// �Ҽ��˵� - ���ٴ���VFX
public class QuickVFXContextMenu
{
    [MenuItem("Assets/Create/Quick VFX from Sprites", true)]
    static bool ValidateCreateVFX()
    {
        return Selection.objects.Length > 0 && Selection.objects[0] is Sprite;
    }

    [MenuItem("Assets/Create/Quick VFX from Sprites")]
    static void CreateVFXFromSelection()
    {
        List<Sprite> sprites = new List<Sprite>();

        foreach (var obj in Selection.objects)
        {
            if (obj is Sprite)
                sprites.Add(obj as Sprite);
        }

        if (sprites.Count == 0) return;

        // ����
        sprites.Sort((a, b) => a.name.CompareTo(b.name));

        // ��ȡ���ƣ�ȥ�����ֺ�׺��
        string baseName = sprites[0].name;
        baseName = System.Text.RegularExpressions.Regex.Replace(baseName, @"_?\d+$", "");

        // ����Ԥ����
        GameObject go = new GameObject(baseName);
        SimpleVFX vfx = go.AddComponent<SimpleVFX>();
        vfx.sprites = sprites.ToArray();
        vfx.fps = 30;
        vfx.additive = true;

        // ����
        string path = $"Assets/{baseName}_VFX.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        Debug.Log($"VFXԤ�����Ѵ���: {path}");
    }
}
#endif