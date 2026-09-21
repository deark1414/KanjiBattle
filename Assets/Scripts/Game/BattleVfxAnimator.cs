using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// WebGLのUI上で、1枚のImageを使ってスプライト連番を再生する軽量アニメーター。
/// 複数Imageの有効・無効を切り替えないため、バッチ順と残像の影響を受けにくい。
/// </summary>
public sealed class BattleVfxAnimator : MonoBehaviour
{
    private Image image;
    private Sprite[] frames;
    private float elapsed;
    private float duration;
    private int frameCount;
    private bool loop;

    public void Play(Sprite[] sourceFrames, float playbackDuration, int visibleFrameCount, bool shouldLoop)
    {
        image = GetComponent<Image>();
        frames = sourceFrames;
        duration = Mathf.Max(0f, playbackDuration);
        frameCount = Mathf.Clamp(visibleFrameCount, 1, frames != null ? frames.Length : 1);
        loop = shouldLoop;
        elapsed = 0f;

        if (image != null && frames != null && frames.Length > 0)
        {
            image.sprite = frames[0];
        }
    }

    private void Update()
    {
        if (image == null || frames == null || frames.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        elapsed += Time.unscaledDeltaTime;
        if (!loop && duration > 0f && elapsed >= duration)
        {
            Destroy(gameObject);
            return;
        }

        float frameDuration = duration > 0f
            ? duration / frameCount
            : 1f / 12f;
        int frameIndex = Mathf.FloorToInt(elapsed / frameDuration);
        frameIndex = loop
            ? frameIndex % frameCount
            : Mathf.Min(frameIndex, frameCount - 1);

        if (image.sprite != frames[frameIndex])
        {
            image.sprite = frames[frameIndex];
        }
    }
}

/// <summary>
/// 盤面全体のVFXレイヤーに置いた演出を、対象コマの移動と寿命に追従させる。
/// </summary>
public sealed class BattleVfxFollowTarget : MonoBehaviour
{
    private RectTransform target;

    public void Initialize(RectTransform source)
    {
        target = source;
        FollowTarget();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        FollowTarget();
    }

    private void FollowTarget()
    {
        if (target != null)
        {
            transform.position = target.position;
        }
    }
}
