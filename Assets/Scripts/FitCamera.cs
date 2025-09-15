using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FitCamera2D : MonoBehaviour
{
    public Transform boardRoot;     // drag Grid here
    public float padding = 0.5f;    // world units around edges
    public bool runEveryFrame = false; // TODO: enable if  board sze changes at runtime

    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
    }

    void Start()
    {
        Fit();
    }

    void LateUpdate()
    {
        if (runEveryFrame) Fit();
    }

    public void Fit()
    {
        if (!boardRoot) return;

        var renderers = boardRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

        // size so both width & height fit on this device
        float sizeY = b.extents.y + padding;
        float sizeX = (b.extents.x + padding) / cam.aspect;
        cam.orthographicSize = Mathf.Max(sizeX, sizeY);

        // center on board (keep current Z; if 0, use -10)
        var p = cam.transform.position;
        cam.transform.position = new Vector3(b.center.x, b.center.y, p.z == 0 ? -10f : p.z);
    }
}
