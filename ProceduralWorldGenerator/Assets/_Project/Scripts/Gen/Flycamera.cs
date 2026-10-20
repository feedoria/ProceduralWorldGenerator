using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// free camera for walking around a generated terrain in the 3D view
// hold right mouse = look around, WASD = move, Q/E = down/up, Shift = 3x faster,
// mouse wheel = slower/faster, Esc = close
public class FlyCamera : MonoBehaviour
{
    public float speed = 150f;            // meters per second
    public float lookSensitivity = 0.15f; // degrees per pixel of mouse movement
    public string title = "";             // shown in the corner (preset, seed)

    float yaw, pitch;

    // puts the camera where oblique.png is taken from, a bit closer, and turns the fly controls on
    public static FlyCamera SetUp(Terrain terrain, string title)
    {
        var cam = Camera.main;
        if (cam == null)
        {
            cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
        }

        Vector3 size = terrain.terrainData.size;
        Vector3 center = terrain.transform.position + new Vector3(size.x / 2f, 0f, size.z / 2f);
        cam.nearClipPlane = 0.5f;
        cam.farClipPlane = (size.x + size.y + size.z) * 4f;   // the whole terrain has to fit in view
        cam.fieldOfView = 50f;

        Vector3 target = center + Vector3.up * size.y * 0.3f;
        float diag = Mathf.Sqrt(size.x * size.x + size.z * size.z);
        Vector3 dir = Quaternion.Euler(30f, 45f, 0f) * Vector3.forward;   // from the south-west, looking down
        cam.transform.position = target - dir * diag * 0.6f;
        cam.transform.LookAt(target);

        var fly = cam.gameObject.GetComponent<FlyCamera>();
        if (fly == null) fly = cam.gameObject.AddComponent<FlyCamera>();   // ?? doesn't work well with unity objects
        fly.speed = Mathf.Max(50f, size.x / 15f);   // 1 km terrain ~67 m/s, 3 km ~200 m/s
        fly.title = title;
        return fly;
    }

    void Start()
    {
        var e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x > 180f ? e.x - 360f : e.x;   // euler angles come as 0..360, we want -89..89
    }

    void Update()
    {
        // look only while the right button is held, so the mouse stays free otherwise
        bool look = RightMouse();
        Cursor.lockState = look ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !look;
        if (look)
        {
            Vector2 d = MouseDelta();
            yaw += d.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - d.y * lookSensitivity, -89f, 89f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        float scroll = Scroll();
        if (scroll > 0f) speed = Mathf.Min(speed * 1.2f, 3000f);
        if (scroll < 0f) speed = Mathf.Max(speed / 1.2f, 5f);

        // WASD in the direction the camera looks, Q/E straight down/up
        Vector3 move = transform.forward * Axis(KeyW(), KeyS()) + transform.right * Axis(KeyD(), KeyA());
        move += Vector3.up * Axis(KeyE(), KeyQ());
        float s = speed * (Shift() ? 3f : 1f);
        transform.position += move * s * Time.deltaTime;

        if (Esc()) Application.Quit();
    }

    void OnGUI()
    {
        GUI.Label(new Rect(12, 10, 900, 22), title);
        GUI.Label(new Rect(12, 30, 900, 22),
            $"right mouse: look   WASD: move   Q/E: down/up   Shift: faster   wheel: speed ({speed:0} m/s)   Esc: close");
    }

    static float Axis(bool positive, bool negative) => (positive ? 1f : 0f) - (negative ? 1f : 0f);

    // input works with both the new Input System and the old Input Manager,
    // whichever one is turned on in Player Settings
#if ENABLE_INPUT_SYSTEM
    static bool K(Key k) => Keyboard.current != null && Keyboard.current[k].isPressed;
    static bool KeyW() => K(Key.W);
    static bool KeyS() => K(Key.S);
    static bool KeyA() => K(Key.A);
    static bool KeyD() => K(Key.D);
    static bool KeyQ() => K(Key.Q);
    static bool KeyE() => K(Key.E);
    static bool Shift() => K(Key.LeftShift) || K(Key.RightShift);
    static bool Esc() => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    static bool RightMouse() => Mouse.current != null && Mouse.current.rightButton.isPressed;
    static Vector2 MouseDelta() => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
    static float Scroll() => Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
#else
    static bool KeyW() => Input.GetKey(KeyCode.W);
    static bool KeyS() => Input.GetKey(KeyCode.S);
    static bool KeyA() => Input.GetKey(KeyCode.A);
    static bool KeyD() => Input.GetKey(KeyCode.D);
    static bool KeyQ() => Input.GetKey(KeyCode.Q);
    static bool KeyE() => Input.GetKey(KeyCode.E);
    static bool Shift() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    static bool Esc() => Input.GetKeyDown(KeyCode.Escape);
    static bool RightMouse() => Input.GetMouseButton(1);
    static Vector2 MouseDelta() => new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10f;
    static float Scroll() => Input.mouseScrollDelta.y;
#endif
}