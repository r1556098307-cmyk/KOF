using UnityEngine;

[System.Serializable]
public class SimpleVFX : MonoBehaviour
{
    [Header("动画设置")]
    public Sprite[] sprites;
    public float fps = 30f;
    public bool loop = false;


    [Tooltip("-1表示根据动画长度自动计算，0表示永久显示，正数表示具体秒数")]
    public float lifeTime = -1; // -1表示根据动画长度自动计算，0表示永久，正数表示具体时间

    [Header("偏移量和缩放")]
    public Vector3 offset = Vector3.zero;
    public Vector3 scale = Vector3.one;

    private SpriteRenderer sr;
    private float timer;
    private int currentFrame;
    private bool isPlaying;
    private float actualLifeTime; // 实际使用的生命周期

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        // 应用缩放
        transform.localScale = scale;
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

        // 应用偏移
        transform.localPosition = offset;
    }


    void PlayAnimation()
    {
        if (sprites == null || sprites.Length == 0) return;

        isPlaying = true;
        currentFrame = 0;
        timer = 0;

        // 计算实际生命周期
        if (lifeTime < 0) // 自动计算
        {
            if (sprites.Length == 1)
            {
                // 单张图片默认显示秒数（可以根据需要调整）
                actualLifeTime = 0.2f;
            }
            else
            {
                // 多张图片根据动画长度计算
                actualLifeTime = sprites.Length / fps;
                if (loop) actualLifeTime = 0; // 循环动画永久显示
            }
        }
        else if (lifeTime == 0) // 永久显示
        {
            actualLifeTime = 0;
        }
        else // 使用指定时间
        {
            actualLifeTime = lifeTime;
        }

        sr.sprite = sprites[0];
    }

    void Update()
    {
        if (!isPlaying) return;

        // 生命周期管理（0表示永久显示）
        if (actualLifeTime > 0)
        {
            actualLifeTime -= Time.deltaTime;
            if (actualLifeTime <= 0)
            {
                VFXManager.Instance.ReturnToPool(gameObject);
                return;
            }
        }

        // 如果只有一张图片，不需要更新动画
        if (sprites.Length == 1) return;

        // 更新动画帧（多张图片时）
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
                    // 动画播放完毕
                    if (actualLifeTime <= 0) // 如果没有设置生命周期
                    {
                        VFXManager.Instance.ReturnToPool(gameObject);
                        return;
                    }
                    // 否则保持最后一帧直到生命周期结束
                    currentFrame = sprites.Length - 1;
                    isPlaying = false;
                }
            }

            if (currentFrame < sprites.Length)
            {
                sr.sprite = sprites[currentFrame];
            }
        }
    }

    // 重置特效状态（用于对象池复用）
    public void ResetVFX()
    {
        isPlaying = false;
        currentFrame = 0;
        timer = 0;
        actualLifeTime = lifeTime;

        if (sr != null && sprites != null && sprites.Length > 0)
        {
            sr.sprite = sprites[0];
        }
    }

    void OnDisable()
    {
        ResetVFX();
    }

}