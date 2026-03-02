using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FacilityDatabase", menuName = "Game/Facility Database")]
public class FacilityDatabase : ScriptableObject
{
    public List<FacilityData> facilities = new();

    public FacilityData GetById(int id)
    {
        return facilities.Find(f => f != null && f.facilityId == id);
    }
}
