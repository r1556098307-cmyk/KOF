using UnityEngine;

[System.Serializable]
public class SimpleVFX : MonoBehaviour
{
    [Header("动画设置")]
    public Sprite[] sprites;
    public float fps = 30f;
    public bool loop = false;

    [Header("视觉效果")]
    public bool additive = true;
    public float lifeTime = -1; // -1表示根据动画长度自动计算

    [Header("偏移量和缩放")]
    public Vector3 offset = Vector3.zero;
    public Vector3 scale = Vector3.one;

    private SpriteRenderer sr;
    private float timer;
    private int currentFrame;
    private bool isPlaying;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        // 设置发光材质
        if (additive)
        {
            sr.material = new Material(Shader.Find("Sprites/Default"));
            sr.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            sr.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        }
    }

    void OnEnable()
    {
        PlayAnimation();
    }


    // 设置特效参数

    public void SetupVFX(bool facingRight = true)
    {
        if (sr != null)
        {
            sr.flipX = !facingRight;
        }
    }

    void PlayAnimation()
    {
        if (sprites == null || sprites.Length == 0) return;

        isPlaying = true;
        currentFrame = 0;
        timer = 0;

        // 自动计算生命周期
        if (lifeTime < 0)
        {
            lifeTime = sprites.Length / fps;
            if (loop) lifeTime = 999f;
        }

        sr.sprite = sprites[0];
    }

    void Update()
    {
        if (!isPlaying) return;

        // 生命周期管理
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0)
        {
            VFXManager.Instance.ReturnToPool(gameObject);
            return;
        }

        // 更新动画帧
        timer += Time.deltaTime;
        if (timer >= 1f / fps)
        {
            timer = 0;
            currentFrame++;

            if (currentFrame >= sprites.Length)
            {
                if (loop)
                {
                    currentFrame = 0;
                }
                else
                {
                    VFXManager.Instance.ReturnToPool(gameObject);
                    return;
                }
            }

            if (currentFrame < sprites.Length)
            {
                sr.sprite = sprites[currentFrame];
            }
        }
    }
}