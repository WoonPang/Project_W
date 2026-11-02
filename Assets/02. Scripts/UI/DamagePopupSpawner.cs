using UnityEngine;

public sealed class DamagePopupSpawner : MonoBehaviour
{
    [SerializeField] private DamagePopupUI popupPrefab;

    private RectTransform canvasRect;
    private Camera worldCamera;

    public void Configure(DamagePopupUI prefab)
    {
        popupPrefab = prefab;
    }

    private void Awake()
    {
        canvasRect = GetComponent<RectTransform>();
        worldCamera = Camera.main;
    }

    private void OnEnable()
    {
        CombatTarget2D.Damaged += OnTargetDamaged;
    }

    private void OnDisable()
    {
        CombatTarget2D.Damaged -= OnTargetDamaged;
    }

    private void OnTargetDamaged(float damage, Vector3 worldPosition)
    {
        if (popupPrefab == null || worldCamera == null || damage <= 0f)
            return;

        Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
        if (screenPosition.z <= 0f)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPosition, null, out Vector2 canvasPosition);
        DamagePopupUI popup = Instantiate(popupPrefab, canvasRect);
        popup.Show(damage, canvasPosition);
    }
}
