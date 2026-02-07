using UnityEngine;

[ExecuteInEditMode] // Optional: allows sorting to update in the editor without running the game
public class SetMeshRendererSortingLayer : MonoBehaviour
{
    // These fields will appear in the Inspector for easy editing
    [SerializeField]
    private string sortingLayerName;
    [SerializeField]
    private int sortingOrder;

    private Renderer meshRenderer;

    void Awake()
    {
        meshRenderer = GetComponent<Renderer>();
        ApplySortingOrder();
    }

    void OnValidate() // Called in the editor when a value is changed
    {
        meshRenderer = GetComponent<Renderer>();
        ApplySortingOrder();
    }

    void ApplySortingOrder()
    {
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = sortingLayerName;
            meshRenderer.sortingOrder = sortingOrder;
        }
    }
}
