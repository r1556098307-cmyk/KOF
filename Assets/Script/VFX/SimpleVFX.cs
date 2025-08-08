using UnityEngine;

[System.Serializable]
public class SimpleVFX : MonoBehaviour
{
    [Header("��������")]
    public Sprite[] sprites;
    public float fps = 30f;
    public bool loop = false;

    [Header("�Ӿ�Ч��")]
    public bool additive = true;
    public float lifeTime = -1; // -1��ʾ���ݶ��������Զ�����

    [Header("ƫ����������")]
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

        // ���÷������
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


    // ������Ч����

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

        // �Զ�������������
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

        // �������ڹ���
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0)
        {
            VFXManager.Instance.ReturnToPool(gameObject);
            return;
        }

        // ���¶���֡
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