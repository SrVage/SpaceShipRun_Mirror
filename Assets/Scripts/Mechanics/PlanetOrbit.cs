using Network;
using UnityEngine;
using Mirror;

public class PlanetOrbit : NetworkMovableObject
{
    protected override float speed => smoothTime;

    [SerializeField] protected Vector3 aroundPoint;
    [SerializeField] protected float smoothTime = .3f;
    [SerializeField] protected float circleInSecond = 1f;

    [SerializeField] protected float offsetSin = 1;
    [SerializeField] protected float offsetCos = 1;
    [SerializeField] protected float rotationSpeed;

    [SerializeField] protected float radius;
    protected float currentAng;
    protected Vector3 currentPositionSmoothVelocity;
    protected float currentRotationAngle;

    protected const float circleRadians = Mathf.PI * 2;

    private void Start()
    {
        Initiate(UpdatePhase.FixedUpdate);
    }

    protected override void HasAuthorityMovement()
    {
        if (!isServer)
            return;

        Vector3 p = aroundPoint;
        p.x += Mathf.Sin(currentAng) * radius * offsetSin;
        p.z += Mathf.Cos(currentAng) * radius * offsetCos;
        transform.position = p;
        currentRotationAngle += Time.deltaTime * rotationSpeed;
        currentRotationAngle = Mathf.Clamp(currentRotationAngle, 0, 361);
        if (currentRotationAngle >= 360)
            currentRotationAngle = 0;

        transform.rotation = Quaternion.AngleAxis(currentRotationAngle, transform.up);
        currentAng += circleRadians * circleInSecond * Time.deltaTime;

        SendToClients();
    }

    protected override void SendToClients()
    {
        serverPosition = transform.position;
        serverEulers = transform.eulerAngles;
    }

    protected override void FromOwnerUpdate()
    {
        if (!isClient)
            return;

        transform.position = Vector3.SmoothDamp(transform.position, serverPosition, ref currentPositionSmoothVelocity, speed);
        transform.rotation = Quaternion.Euler(serverEulers);
    }
}

