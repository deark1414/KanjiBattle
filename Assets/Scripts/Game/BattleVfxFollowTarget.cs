using UnityEngine;
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
