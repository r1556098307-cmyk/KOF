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
        GUILayout.Label("快速创建VFX预制体", EditorStyles.boldLabel);

        vfxName = EditorGUILayout.TextField("特效名称", vfxName);

        EditorGUILayout.Space();

        // Sprite列表管理
        EditorGUILayout.LabelField("Sprite序列帧", EditorStyles.boldLabel);

        // 拖拽区域
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "拖拽Sprites到这里");

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
                            // 尝试加载为Sprite
                            string path = AssetDatabase.GetAssetPath(dragged);
                            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
                            foreach (var sprite in sprites)
                            {
                                if (!spritesList.Contains(sprite))
                                    spritesList.Add(sprite);
                            }
                        }
                    }

                    // 排序
                    spritesList.Sort((a, b) => a.name.CompareTo(b.name));
                    evt.Use();
                }
                break;
        }

        // 显示Sprite列表
        if (spritesList.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"已添加 {spritesList.Count} 个Sprites");
            if (GUILayout.Button("清空", GUILayout.Width(50)))
            {
                spritesList.Clear();
            }
            if (GUILayout.Button("排序", GUILayout.Width(50)))
            {
                spritesList.Sort((a, b) => a.name.CompareTo(b.name));
            }
            EditorGUILayout.EndHorizontal();

            // 预览列表
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(100));
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < spritesList.Count; i++)
            {
                if (spritesList[i] != null)
                {
                    // 显示缩略图
                    GUILayout.Label(AssetPreview.GetAssetPreview(spritesList[i]),
                        GUILayout.Width(50), GUILayout.Height(50));
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space();

        fps = EditorGUILayout.FloatField("FPS", fps);
        additive = EditorGUILayout.Toggle("发光效果", additive);
        loop = EditorGUILayout.Toggle("循环", loop);

        EditorGUILayout.Space();

        GUI.enabled = spritesList.Count > 0;
        if (GUILayout.Button("创建预制体", GUILayout.Height(30)))
        {
            CreateVFXPrefab();
        }
        GUI.enabled = true;

        EditorGUILayout.Space();

        if (GUILayout.Button("自动设置Manager", GUILayout.Height(25)))
        {
            SetupManager();
        }
    }

    void CreateVFXPrefab()
    {
        if (spritesList.Count == 0)
        {
            EditorUtility.DisplayDialog("错误", "请先拖入Sprite序列", "OK");
            return;
        }

        // 创建GameObject
        GameObject go = new GameObject(vfxName);

        // 添加组件
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        SimpleVFX vfx = go.AddComponent<SimpleVFX>();

        // 设置参数
        vfx.sprites = spritesList.ToArray();
        vfx.fps = fps;
        vfx.additive = additive;
        vfx.loop = loop;

        // 保存为预制体
        string path = $"Assets/VFX/Prefabs/{vfxName}.prefab";

        // 确保目录存在
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/VFX"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "VFX");

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        // 自动添加到Manager
        AddToManager(vfxName, prefab);

        EditorUtility.DisplayDialog("成功", $"预制体已创建: {path}", "OK");

        // 选中新创建的预制体
        Selection.activeObject = prefab;
    }

    void AddToManager(string name, GameObject prefab)
    {
        // 查找场景中的Manager
        VFXManager manager = FindObjectOfType<VFXManager>();

        if (manager == null)
        {
            // 创建Manager
            GameObject mgrObj = new GameObject("VFXManager");
            manager = mgrObj.AddComponent<VFXManager>();
        }

        // 添加到列表
        if (manager.vfxPresets == null)
            manager.vfxPresets = new List<VFXManager.VFXPreset>();

        // 检查是否已存在
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
        EditorUtility.DisplayDialog("完成", "Manager已创建/选中", "OK");
    }
}

// 右键菜单 - 快速创建VFX
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

        // 排序
        sprites.Sort((a, b) => a.name.CompareTo(b.name));

        // 获取名称（去掉数字后缀）
        string baseName = sprites[0].name;
        baseName = System.Text.RegularExpressions.Regex.Replace(baseName, @"_?\d+$", "");

        // 创建预制体
        GameObject go = new GameObject(baseName);
        SimpleVFX vfx = go.AddComponent<SimpleVFX>();
        vfx.sprites = sprites.ToArray();
        vfx.fps = 30;
        vfx.additive = true;

        // 保存
        string path = $"Assets/{baseName}_VFX.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        Debug.Log($"VFX预制体已创建: {path}");
    }
}
#endif