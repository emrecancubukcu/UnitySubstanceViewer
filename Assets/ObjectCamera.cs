using UnityEngine;

public class ObjectCamera : MonoBehaviour
{
    public int deadGUIZone = 340;
    [SerializeField] private Vector3 _target = Vector3.zero;
    [SerializeField]
    private float
        _distance = 10f,
        _xSpeed = 250f,
        _ySpeed = 120f,
        _zoomSpeed = 90f,
        _yMinLimit = -20f,
        _yMaxLimit = 80f;

    private float _x, _y;
    private Vector3 _startPos;

    private void Reset()
    {
        _y = 89.9f;
        _x = 180;
        _distance = 2.3f;
        _target = new Vector3(0.0f, 0f, 0.0f);
    }

    void OnGUI()
    {
        //          GUI.backgroundColor = Color.red;
        GUILayout.BeginArea(new Rect(Screen.width - 40, 10, 30, 30));
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("R"))
        {
            Reset();

        }

        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }



    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f)
            angle += 360f;
        if (angle > 360f)
            angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }

    private void Start()
    {
        _startPos = _target;
        var eulerAngles = transform.eulerAngles;
        _x = eulerAngles.y; //TODO fix maybe
        _y = eulerAngles.x;

        if (GetComponent<Rigidbody>())
            GetComponent<Rigidbody>().freezeRotation = true;

        Reset();
    }

    private void LateUpdate()
    {
        if (Input.mousePosition.x < deadGUIZone)
            return;

        //   if (Input.GetKey(KeyCode.LeftAlt))
        {
            var axis = Input.GetAxis("Mouse X");
            var axis2 = Input.GetAxis("Mouse Y");
            if (Input.GetMouseButton(0))
            {
                _x += axis * _xSpeed * 0.03f;
                _y += axis2 * _ySpeed * 0.03f;
                _y = ClampAngle(_y, _yMinLimit, _yMaxLimit);
            }
            else if (Input.GetMouseButton(1))
            {
                var num = (Mathf.Abs(axis) <= Mathf.Abs(axis2)) ? axis2 : axis;
                num = -num * _zoomSpeed * 0.03f;
                _distance += num * (Mathf.Max(_distance, 0.02f) * 0.03f);
            }
            else if (Input.GetMouseButton(2))
            {
                var a = transform.rotation * Vector3.right;
                var a2 = transform.rotation * Vector3.down;
                var a3 = -a * axis * _xSpeed * 0.02f;
                var b = -a2 * axis2 * _ySpeed * 0.02f;
                _target += (a3 + b) * (Mathf.Max(_distance, 0.04f) * 0.01f);
            }
        }
        if (Input.GetKey(KeyCode.F))
        {
            _target = _startPos;
        }
        _distance += -Input.GetAxis("Mouse ScrollWheel") * _zoomSpeed * 0.03f;
        //  ZoomAmount = Mathf.Clamp(ZoomAmount, -MaxToClamp, MaxToClamp);
        // _distance = Mathf.Min(Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")), MaxToClamp - Mathf.Abs(ZoomAmount));

        var rotation = Quaternion.Euler(_y, _x, 0f);
        var position = rotation * new Vector3(-0.75f, 0f, -_distance) + _target;
        transform.rotation = rotation;
        transform.position = position+new Vector3(0.0f,0,0);
    }

    public void SetTarget(Vector3 target)
    {
        _target = target;
    }
}
