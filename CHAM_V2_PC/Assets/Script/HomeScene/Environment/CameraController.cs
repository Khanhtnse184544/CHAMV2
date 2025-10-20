using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraController : MonoBehaviour
{
    public float zoomSpeed = 2f;        // tốc độ zoom
    public float minZoom = 3f;          // zoom nhỏ nhất
    public float maxZoom = 10f;         // zoom lớn nhất
    public Tilemap backgroundTilemap;   // tilemap nền

    private Camera cam;
    private Bounds mapBounds;
    private Vector3 dragOrigin;

    void Start()
    {
        cam = Camera.main;

        if (backgroundTilemap == null)
            backgroundTilemap = Object.FindFirstObjectByType<Tilemap>();


        // Nén tilemap để loại bỏ khoảng trống không có tile
        backgroundTilemap.CompressBounds();

        // Lấy bounds đúng từ tilemap
        mapBounds = backgroundTilemap.localBounds;
        mapBounds.min = backgroundTilemap.transform.TransformPoint(mapBounds.min);
        mapBounds.max = backgroundTilemap.transform.TransformPoint(mapBounds.max);

        Debug.Log("Tilemap Bounds (World): " + mapBounds);
    }

    void LateUpdate()
    {
        HandleZoom();
        HandleMovement();
        ClampCameraPosition();
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

#if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 t0Prev = t0.position - t0.deltaPosition;
            Vector2 t1Prev = t1.position - t1.deltaPosition;

            float prevMag = (t0Prev - t1Prev).magnitude;
            float currMag = (t0.position - t1.position).magnitude;

            float diff = currMag - prevMag;
            scroll = -diff * 0.01f;
        }
#endif

        if (scroll != 0)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    void HandleMovement()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
        {
            Vector2 delta = Input.GetTouch(0).deltaPosition;
            cam.transform.Translate(-delta.x * Time.deltaTime, -delta.y * Time.deltaTime, 0);
        }
#else
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            dragOrigin = Input.mousePosition;

        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            Vector3 diff = cam.ScreenToWorldPoint(dragOrigin) - cam.ScreenToWorldPoint(Input.mousePosition);
            cam.transform.position += diff;
            dragOrigin = Input.mousePosition;
        }
#endif
    }

    void ClampCameraPosition()
    {
        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;

        float minX = mapBounds.min.x + camWidth / 2f;
        float maxX = mapBounds.max.x - camWidth / 2f;
        float minY = mapBounds.min.y + camHeight / 2f;
        float maxY = mapBounds.max.y - camHeight / 2f;

        Vector3 pos = cam.transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        cam.transform.position = pos;
    }

    // Vẽ gizmo để debug tilemap bounds
    void OnDrawGizmos()
    {
        if (backgroundTilemap != null)
        {
            backgroundTilemap.CompressBounds();
            Bounds b = backgroundTilemap.localBounds;
            b.min = backgroundTilemap.transform.TransformPoint(b.min);
            b.max = backgroundTilemap.transform.TransformPoint(b.max);

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}
