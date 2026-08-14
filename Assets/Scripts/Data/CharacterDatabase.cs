using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Game/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    public List<CharacterData> characters;

    /// <summary>
    /// IDでキャラクターを取得
    /// </summary>
    public CharacterData GetById(int id)
    {
        return characters.Find(c => c != null && c.characterId == id);
    }

    /// <summary>
    /// 名前でキャラクターを取得（デバッグ用）
    /// </summary>
    public CharacterData GetByName(string name)
    {
        return characters.Find(c => c != null && c.characterName == name);
    }
}