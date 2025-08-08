using UnityEngine;

[System.Serializable]
public class SimpleVFX : MonoBehaviour
{
    [Header("��������")]
    public Sprite[] sprites;
    public float fps = 30f;
    public bool loop = false;


    [Tooltip("-1��ʾ���ݶ��������Զ����㣬0��ʾ������ʾ��������ʾ��������")]
    public float lifeTime = -1; // -1��ʾ���ݶ��������Զ����㣬0��ʾ���ã�������ʾ����ʱ��

    [Header("ƫ����������")]
    public Vector3 offset = Vector3.zero;
    public Vector3 scale = Vector3.one;

    private SpriteRenderer sr;
    private float timer;
    private int currentFrame;
    private bool isPlaying;
    private float actualLifeTime; // ʵ��ʹ�õ���������

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        // Ӧ������
        transform.localScale = scale;
    }

    void OnEnable()
    {
        PlayAnimation();
    }

    // ������Ч����
    public void SetupVFX(bool facingRight = true)
    {
        if (sr != null)
        {
            sr.flipX = !facingRight;
        }

        // Ӧ��ƫ��
        transform.localPosition = offset;
    }


    void PlayAnimation()
    {
        if (sprites == null || sprites.Length == 0) return;

        isPlaying = true;
        currentFrame = 0;
        timer = 0;

        // ����ʵ����������
        if (lifeTime < 0) // �Զ�����
        {
            if (sprites.Length == 1)
            {
                // ����ͼƬĬ����ʾ���������Ը�����Ҫ������
                actualLifeTime = 0.2f;
            }
            else
            {
                // ����ͼƬ���ݶ������ȼ���
                actualLifeTime = sprites.Length / fps;
                if (loop) actualLifeTime = 0; // ѭ������������ʾ
            }
        }
        else if (lifeTime == 0) // ������ʾ
        {
            actualLifeTime = 0;
        }
        else // ʹ��ָ��ʱ��
        {
            actualLifeTime = lifeTime;
        }

        sr.sprite = sprites[0];
    }

    void Update()
    {
        if (!isPlaying) return;

        // �������ڹ���0��ʾ������ʾ��
        if (actualLifeTime > 0)
        {
            actualLifeTime -= Time.deltaTime;
            if (actualLifeTime <= 0)
            {
                VFXManager.Instance.ReturnToPool(gameObject);
                return;
            }
        }

        // ���ֻ��һ��ͼƬ������Ҫ���¶���
        if (sprites.Length == 1) return;

        // ���¶���֡������ͼƬʱ��
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
                    // �����������
                    if (actualLifeTime <= 0) // ���û��������������
                    {
                        VFXManager.Instance.ReturnToPool(gameObject);
                        return;
                    }
                    // ���򱣳����һֱ֡���������ڽ���
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

    // ������Ч״̬�����ڶ���ظ��ã�
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