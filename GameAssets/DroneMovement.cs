using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneMovement : MonoBehaviour
{
    [SerializeField] float maxThrusterProcent;
    [SerializeField] float maxTurningAngle;
    [SerializeField] float maxTurningPerTick;

    [Header("Debugging")]
    [SerializeField] Vector2 leftThursterForce;
    [SerializeField] Vector2 leftForcePosition;
    [SerializeField] [Range(0f, 1f)] float leftThrusterProsent;
    [SerializeField] [Range(-180, 180)] float leftThrusterAngle;

    [SerializeField] Vector2 rightThursterForce;
    [SerializeField] Vector2 rightForcePosition;
    [SerializeField] [Range(0f, 1f)] float rightThrusterProsent;
    [SerializeField] [Range(-180, 180)] float rightThrusterAngle;

    [SerializeField] bool playerCanControl;
    [SerializeField] Vector3 spawnLocationThis;
    static Vector3 spawnLocation;

    [SerializeField] Vector2 thrustDirectionVectorLeft;
    [SerializeField] Vector2 thrustDirectionVectorRight;

    Vector3 gizmoPositionleft;
    Vector3 gizmoPositionright;
    Vector2 pos;
    Vector2 power;

    Rigidbody2D rb;

    void Awake() 
    {
        spawnLocation = spawnLocationThis;  // fix spawnLocation
        rb = gameObject.GetComponent<Rigidbody2D>();
    }

    /*void FixedUpdate()
    {
        if (playerCanControl)
        {
            ProcessPlayerMoving();
        }
    }*/

    private void ProcessPlayerMoving()
    {
        if (Input.GetKey(KeyCode.F))
        {
            pos = RotateAroundOrigin(transform.position + ConvertToV3(leftForcePosition), transform.position, transform.rotation.eulerAngles.z);
            power = RotateAroundOrigin(leftThursterForce, pos, transform.rotation.eulerAngles.z + leftThrusterAngle);
            rb.AddForceAtPosition(power * Time.deltaTime * leftThrusterProsent, pos);
        }
        if (Input.GetKey(KeyCode.G))
        {
            pos = RotateAroundOrigin(transform.position + ConvertToV3(rightForcePosition), transform.position, transform.rotation.eulerAngles.z);
            power = RotateAroundOrigin(leftThursterForce, pos, transform.rotation.eulerAngles.z + rightThrusterAngle);
            rb.AddForceAtPosition(power * Time.deltaTime * rightThrusterProsent, pos);
        }

        if (Input.GetKey(KeyCode.Y))
        {
            ResetDronePosition();
        }
    }

    public void ResetDronePosition()
    {
        transform.position = spawnLocation;
        transform.rotation = Quaternion.Euler(0, 0, 0);
        rb.velocity = Vector2.zero;
    }

    public void ProcessAiMoving(float[,] inputs)  // should be a 4 x 1 matrix
    {                                             // input from ai are something from - 10 to 10
        rightThrusterProsent = Mathf.Clamp(inputs[0, 0], 0f, maxThrusterProcent);
        leftThrusterProsent = Mathf.Clamp(inputs[1, 0], 0f, maxThrusterProcent);

        float desiredRightThrusterAngle = Mathf.Clamp(inputs[2, 0] * Mathf.Rad2Deg, -maxTurningAngle, maxTurningAngle);
        float desiredLeftThrusterAngle = Mathf.Clamp(inputs[3, 0] * Mathf.Rad2Deg, -maxTurningAngle, maxTurningAngle);

        rightThrusterAngle += Mathf.Clamp(desiredRightThrusterAngle - rightThrusterAngle, -maxTurningPerTick, maxTurningPerTick);
        leftThrusterAngle += Mathf.Clamp(desiredLeftThrusterAngle - leftThrusterAngle, -maxTurningPerTick, maxTurningPerTick);

        pos = RotateAroundOrigin(transform.position + ConvertToV3(leftForcePosition), transform.position, transform.rotation.eulerAngles.z);
        power = RotateAroundOrigin(leftThursterForce, pos, transform.rotation.eulerAngles.z + leftThrusterAngle);
        rb.AddForceAtPosition(power * Time.deltaTime * leftThrusterProsent, pos);

        pos = RotateAroundOrigin(transform.position + ConvertToV3(rightForcePosition), transform.position, transform.rotation.eulerAngles.z);
        power = RotateAroundOrigin(leftThursterForce, pos, transform.rotation.eulerAngles.z + rightThrusterAngle);
        rb.AddForceAtPosition(power * Time.deltaTime * rightThrusterProsent, pos);
    }

    private Vector2 RadianToVector2(float radian)
    {
        return new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
    }
  
    private Vector2 DegreeToVector2(float degree)
    {
        return RadianToVector2((degree) * Mathf.Deg2Rad);
    }

    private Vector3 ConvertToV3(Vector2 vector)
    {
        return vector;
    }

    public Vector2 RotateAroundOrigin(Vector2 point, Vector2 origin, float rotation)
    {
        float y = point.y - origin.y;
        float x = point.x - origin.x;
        float distance = Mathf.Sqrt(x * x + y * y);
        rotation *= Mathf.Deg2Rad;
        rotation += Mathf.Atan2(y, x);
        return new Vector2(distance * Mathf.Cos(rotation) + origin.x, distance * Mathf.Sin(rotation) + origin.y);
    }

    public void ResetAngle()
    {
        rightThrusterAngle = 0;
        leftThrusterAngle = 0;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        gizmoPositionleft = RotateAroundOrigin(transform.position + ConvertToV3(leftForcePosition), transform.position, transform.rotation.eulerAngles.z);
        gizmoPositionright = RotateAroundOrigin(transform.position + ConvertToV3(rightForcePosition), transform.position, transform.rotation.eulerAngles.z);

        Gizmos.DrawWireSphere(gizmoPositionleft, 0.5f);
        Gizmos.DrawWireSphere(gizmoPositionright, 0.5f);

        Gizmos.DrawLine(gizmoPositionleft, RotateAroundOrigin(transform.position + ConvertToV3(leftThursterForce) * -1, gizmoPositionleft, transform.rotation.eulerAngles.z + leftThrusterAngle));
        Gizmos.DrawLine(gizmoPositionright, RotateAroundOrigin(transform.position + ConvertToV3(rightThursterForce) * -1, gizmoPositionleft, transform.rotation.eulerAngles.z + rightThrusterAngle));

        Gizmos.DrawWireSphere(spawnLocation, 1f);
    }
}
