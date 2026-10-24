using System.Collections.Generic;
using UnityEngine;

public static class CharacterArtCatalog
{
    private static PieceMotifCatalog catalog;
    private static readonly Dictionary<string, Sprite> pieces = new();

    public static IReadOnlyList<PieceMotifEntry> Entries
    {
        get
        {
            if (catalog == null)
            {
                var json = Resources.Load<TextAsset>("UI/PieceMotifs/catalog");
                catalog = json == null ? new PieceMotifCatalog() : JsonUtility.FromJson<PieceMotifCatalog>(json.text);
                catalog.entries ??= System.Array.Empty<PieceMotifEntry>();
            }
            return catalog.entries;
        }
    }

    public static int ArtId(CharacterData character) => character.characterId == -20000
        && character.skillType == SkillType.Grass ? -1014 : character.characterId;

    public static PieceMotifEntry Find(CharacterData character)
    {
        if (character == null) return null;
        int id = ArtId(character);
        foreach (var entry in Entries) if (entry.id == id) return entry;
        return null;
    }

    public static void Apply(CharacterData character)
    {
        var entry = Find(character);
        if (entry == null) return;
        character.icon = LoadPiece(entry.piece);
        character.enemyIcon = LoadPiece(entry.enemyPiece);
    }

    private static Sprite LoadPiece(string resource)
    {
        if (string.IsNullOrEmpty(resource)) return null;
        if (!pieces.TryGetValue(resource, out var sprite))
            pieces[resource] = sprite = Resources.Load<Sprite>(resource);
        return sprite;
    }
}
