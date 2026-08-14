using System.Collections.Generic;
using UnityEngine;

public static class SkillCatalog
{
    private const string ResourcePath = "skills";
    private static Dictionary<SkillType, SkillData> cached;

    [System.Serializable]
    private class SkillListWrapper
    {
        public List<SkillData> items = new();
    }

    public static SkillData Get(SkillType type)
    {
        EnsureLoaded();
        cached.TryGetValue(type, out SkillData data);
        return data;
    }

    private static void EnsureLoaded()
    {
        if (cached != null)
        {
            return;
        }

        cached = new Dictionary<SkillType, SkillData>();
        TextAsset jsonAsset = Resources.Load<TextAsset>(ResourcePath);
        if (jsonAsset == null)
        {
            Debug.LogWarning("[SkillCatalog] skills.json not found in Resources.");
            return;
        }

        string wrappedJson = $"{{\"items\":{jsonAsset.text}}}";
        SkillListWrapper wrapper = JsonUtility.FromJson<SkillListWrapper>(wrappedJson);
        if (wrapper?.items == null)
        {
            Debug.LogWarning("[SkillCatalog] skills.json could not be parsed.");
            return;
        }

        foreach (var skill in wrapper.items)
        {
            if (skill.skillType == SkillType.None && !string.IsNullOrEmpty(skill.skillTypeName))
            {
                if (System.Enum.TryParse(skill.skillTypeName, true, out SkillType parsed))
                {
                    skill.skillType = parsed;
                }
            }

            if (skill.effects != null)
            {
                foreach (var effect in skill.effects)
                {
                    if (!string.IsNullOrEmpty(effect.effectTypeName))
                    {
                        if (System.Enum.TryParse(effect.effectTypeName, true, out SkillEffectType parsedEffect))
                        {
                            effect.effectType = parsedEffect;
                        }
                    }
                }
            }

            if (!cached.ContainsKey(skill.skillType))
            {
                cached.Add(skill.skillType, skill);
            }
        }
    }
}
