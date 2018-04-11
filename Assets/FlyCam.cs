using UnityEngine;

public class FlyCam : MonoBehaviour {
    public float cameraSensitivity = 90;
    public float climbSpeed = 4;
    public float normalMoveSpeed = 10;
    public float slowMoveFactor = 0.25f;
    public float fastMoveFactor = 3;

    private float rotationX = 0.0f;
    private float rotationY = 0.0f;

    void Start()
    {
        GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
    }

    void Update() {
        if (Input.GetMouseButton(0))
        {
            rotationX += Input.GetAxis("Mouse X") * cameraSensitivity * Time.deltaTime;
            rotationY += Input.GetAxis("Mouse Y") * cameraSensitivity * Time.deltaTime;
            rotationY = Mathf.Clamp(rotationY, -90, 90);

            transform.localRotation = Quaternion.AngleAxis(rotationX, Vector3.up);
            transform.localRotation *= Quaternion.AngleAxis(rotationY, Vector3.left);
        }

        Vector3 forwardLocked = new Vector3(transform.forward.x, 0f, transform.forward.z);

        float speed = normalMoveSpeed;

        // Faster flying on shift
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
            speed *= fastMoveFactor;
        } else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) {
            speed *= slowMoveFactor;
        }

        Vector3 velocity = forwardLocked * speed * Input.GetAxis("Vertical") * Time.deltaTime +
                         transform.right * speed * Input.GetAxis("Horizontal") * Time.deltaTime;

        // Elevation
        if (Input.GetKey(KeyCode.Q)) { velocity.y = -climbSpeed * Time.deltaTime; }
        if (Input.GetKey(KeyCode.E)) { velocity.y = climbSpeed * Time.deltaTime; }

        transform.position += velocity;
    }
}
