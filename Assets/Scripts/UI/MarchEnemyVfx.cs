using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 行軍マップ上で、選択局の敵軍が待機し、固有の代表VFXを短く見せる演出。
/// 戦闘の判定には関与しない。
/// </summary>
public sealed class MarchEnemyVfx : MonoBehaviour
{
    private readonly List<RectTransform> enemyPieces = new();
    private RectTransform overlay;
    private RectTransform targetNode;
    private List<CharacterData> enemies = new();
    private Coroutine routine;

    public void Configure(Transform mapContent, RectTransform stageNode, IEnumerable<CharacterData> enemyRoster)
    {
        Clear();
        if (mapContent == null || stageNode == null)
        {
            return;
        }

        targetNode = stageNode;
        enemies = enemyRoster?.Where(character => character != null).Distinct().Take(3).ToList() ?? new List<CharacterData>();
        if (enemies.Count == 0)
        {
            return;
        }

        EnsureOverlay(mapContent);
        for (int index = 0; index < enemies.Count; index++)
        {
            float x = enemies.Count == 1 ? 0f : Mathf.Lerp(-44f, 44f, index / (float)(enemies.Count - 1));
            RectTransform piece = CreateEnemyPiece(enemies[index], new Vector2(x, 58f));
            if (piece != null)
            {
                enemyPieces.Add(piece);
            }
        }
        routine = StartCoroutine(PatrolRoutine());
    }

    public void Clear()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        enemyPieces.Clear();
        enemies.Clear();
        if (overlay != null)
        {
            Destroy(overlay.gameObject);
            overlay = null;
        }
        targetNode = null;
    }

    private void OnDisable() => Clear();

    private void EnsureOverlay(Transform mapContent)
    {
        var overlayObject = new GameObject("MarchEnemyVfxOverlay", typeof(RectTransform));
        overlayObject.transform.SetParent(mapContent, false);
        overlay = overlayObject.GetComponent<RectTransform>();
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = Vector2.zero;
        overlay.offsetMax = Vector2.zero;
        overlay.SetAsLastSibling();
    }

    private RectTransform CreateEnemyPiece(CharacterData character, Vector2 offset)
    {
        if (overlay == null || targetNode == null || character == null || character.icon == null)
        {
            return null;
        }

        var objectPiece = new GameObject("MarchEnemy_" + character.characterName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        objectPiece.transform.SetParent(overlay, false);
        var image = objectPiece.GetComponent<Image>();
        image.sprite = character.icon;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = new Color(1f, 0.91f, 0.82f, 1f);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.one * 56f;
        rect.position = targetNode.position + (Vector3)offset;
        // 行軍では敵軍も進行方向と同じ上向き。盤上だけ敵軍を反転して対峙させる。
        rect.localRotation = Quaternion.identity;
        return rect;
    }

    private IEnumerator PatrolRoutine()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        while (overlay != null && targetNode != null && enemies.Count > 0)
        {
            int index = Random.Range(0, Mathf.Min(enemies.Count, enemyPieces.Count));
            RectTransform source = enemyPieces[index];
            CharacterData enemy = enemies[index];
            if (source != null && enemy != null)
            {
                Vector3 target = targetNode.position + Vector3.down * 16f;
                Vector3 travel = target - source.position;
                Vector2 visualDirection = travel.sqrMagnitude > 0.001f
                    ? new Vector2(travel.x, travel.y).normalized
                    : Vector2.down;

                if (enemy.skillType == SkillType.NumberPassive)
                {
                    Sprite sprite = SkillVfxMotion.GetRepresentativeSprite(SkillVfxMotion.GetVfxFolder(enemy));
                    yield return SkillVfxMotion.PlayNumberAura(overlay, sprite, source.position, 76f, 0.9f, 180f, false);
                }
                else if (enemy.skillType == SkillType.Slash)
                {
                    yield return SkillVfxMotion.PlaySwordMotion(
                        overlay,
                        source.position,
                        visualDirection,
                        108f,
                        0.82f,
                        0.20f,
                        0.86f);
                }
                else if (enemy.skillType == SkillType.StunBlow)
                {
                    yield return SkillVfxMotion.PlayHammerMotion(
                        overlay,
                        source.position,
                        visualDirection,
                        112f,
                        0.78f);
                }
                else
                {
                    Sprite sprite = SkillVfxMotion.GetRepresentativeSprite(SkillVfxMotion.GetVfxFolder(enemy));
                    if (sprite != null)
                    {
                        yield return SkillVfxMotion.PlayRepresentativeSprite(overlay, sprite, source.position, target, 78f, 0.8f, 0f);
                    }
                }
            }
            yield return new WaitForSecondsRealtime(Random.Range(3.8f, 6.0f));
        }
    }
}
