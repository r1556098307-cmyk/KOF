using System.Collections.Generic;
using UnityEngine;

public class VFXManager : Singleton<VFXManager>
{
    [Header("��ЧԤ�����б�")]
    public List<VFXPreset> vfxPresets = new List<VFXPreset>();

    [Header("��������")]
    public int poolSize = 5;

    private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
    private Transform poolContainer;

    [System.Serializable]
    public class VFXPreset
    {
        public string name;
        public GameObject prefab;

        public VFXPreset(string n, GameObject p)
        {
            name = n;
            prefab = p;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        InitializePools();
    }

    void InitializePools()
    {
        poolContainer = new GameObject("VFXPool").transform;
        poolContainer.SetParent(transform);

        foreach (var preset in vfxPresets)
        {
            if (preset.prefab == null) continue;

            Queue<GameObject> pool = new Queue<GameObject>();

            for (int i = 0; i < poolSize; i++)
            {
                GameObject obj = Instantiate(preset.prefab, poolContainer);
                obj.SetActive(false);
                pool.Enqueue(obj);
            }

            pools[preset.name] = pool;
        }
    }


    // ������Ч
    public GameObject PlayVFX(string vfxName, Vector3 position, Vector3 scale = default, Transform target = null,bool facingRight = true)
    {
        if (scale == default) scale = Vector3.one;

        GameObject vfx = GetFromPool(vfxName);
        if (vfx == null)
        {
            Debug.LogWarning($"VFX '{vfxName}' not found!");
            return null;
        }

        // ���ñ任
        vfx.transform.SetParent(target);

        vfx.transform.position = position;
        vfx.transform.localScale = scale;


        // ������
        if (!facingRight)
        {
            Vector3 currentScale = vfx.transform.localScale;
            currentScale.x = -Mathf.Abs(currentScale.x);
            vfx.transform.localScale = currentScale;
        }


        // ������Ч���
        SimpleVFX simpleVFX = vfx.GetComponent<SimpleVFX>();
        if (simpleVFX != null)
        {
            simpleVFX.SetupVFX(facingRight);
        }

        vfx.SetActive(true);
        return vfx;
    }


    // ��Ŀ������λ�ò�����Ч����ƫ�ƣ�
    public GameObject PlayVFXAt(string vfxName, Transform target, bool facingRight = true)
    {
        if (target == null) return null;
        GameObject vfx = GetFromPool(vfxName);

        if (vfx == null)
        {
            Debug.LogWarning($"VFX '{vfxName}' not found!");
            return null;
        }

        // ��ȡ��Ч����
        SimpleVFX simpleVFX = vfx.GetComponent<SimpleVFX>();
        Vector3 offset = Vector3.zero;
        Vector3 scale = Vector3.one;

      

        if (simpleVFX != null)
        {
            offset = simpleVFX.offset;
            scale = simpleVFX.scale == Vector3.zero ? Vector3.one : simpleVFX.scale;

            // ���������ߣ�x����ƫ����ȡ��
            if (!facingRight)
            {
                offset.x *= -1;
            }
        }
        Debug.Log($"offset:{offset},scale:{scale}");

        Vector3 finalPosition = target.position + offset;
        Debug.Log($"target.position :{target.position},finalPosition :{finalPosition}");

        // ���յ����У�Ȼ�������úõĲ������²���
        ReturnToPool(vfx);
        return PlayVFX(vfxName, finalPosition, scale, target,facingRight);
    }

    GameObject GetFromPool(string vfxName)
    {
        if (!pools.ContainsKey(vfxName))
        {
            var preset = vfxPresets.Find(p => p.name == vfxName);
            if (preset != null && preset.prefab != null)
            {
                pools[vfxName] = new Queue<GameObject>();
                return Instantiate(preset.prefab);
            }
            return null;
        }

        var pool = pools[vfxName];

        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }
        else
        {
            var preset = vfxPresets.Find(p => p.name == vfxName);
            if (preset != null && preset.prefab != null)
            {
                return Instantiate(preset.prefab);
            }
        }

        return null;
    }

    public void ReturnToPool(GameObject vfx)
    {
        if (vfx == null) return;

        vfx.SetActive(false);
        vfx.transform.SetParent(poolContainer);

        foreach (var preset in vfxPresets)
        {
            if (vfx.name.Contains(preset.prefab.name))
            {
                if (!pools.ContainsKey(preset.name))
                {
                    pools[preset.name] = new Queue<GameObject>();
                }
                pools[preset.name].Enqueue(vfx);
                return;
            }
        }

        Destroy(vfx);
    }
}