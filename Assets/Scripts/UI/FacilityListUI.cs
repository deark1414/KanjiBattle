using UnityEngine;

[DefaultExecutionOrder(200)]
public class FacilityListUI : MonoBehaviour
{
    [SerializeField] private Transform contentParent;
    [SerializeField] private FacilityUI facilityPrefab;
    private void Start()
    {
        Transform host=transform.parent;
        if(host.Find("CastleTree")==null)
        {
            var go=new GameObject("CastleTree",typeof(RectTransform),typeof(CastleTreeView));
            go.transform.SetParent(host,false);
        }
        Transform oldInfo=host.Find("FacilityInformationRegion");
        if(oldInfo!=null)oldInfo.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }
    public void RefreshList() { }
    public bool OwnsContent(Transform target) => contentParent!=null && target!=null
        && (target==contentParent||target.IsChildOf(contentParent)||contentParent.IsChildOf(target));
}
