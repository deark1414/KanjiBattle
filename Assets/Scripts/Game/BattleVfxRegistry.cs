using System.Collections.Generic;
using UnityEngine;

// Owns transient battle VFX so scene changes, deaths, and battle resets share one cleanup path.
public sealed class BattleVfxRegistry
{
    private readonly List<GameObject> activeObjects = new();

    public void Register(GameObject value)
    {
        if (value != null && !activeObjects.Contains(value)) activeObjects.Add(value);
    }

    public void Destroy(GameObject value)
    {
        if (value == null) return;
        activeObjects.Remove(value);
        Object.Destroy(value);
    }

    public void Cleanup()
    {
        for (int index = activeObjects.Count - 1; index >= 0; index--)
        {
            if (activeObjects[index] != null) Object.Destroy(activeObjects[index]);
        }
        activeObjects.Clear();
    }
}
