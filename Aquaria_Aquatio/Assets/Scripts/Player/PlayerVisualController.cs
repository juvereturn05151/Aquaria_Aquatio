using UnityEngine;

public class PlayerVisualController : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private Transform playerRoot;

    [SerializeField]
    private float maxMovementSpeed = 3f;

    [SerializeField]
    private float movementThreshold = 0.05f;

    private Vector3 previousPosition;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private void Start()
    {
        previousPosition = playerRoot.position;
    }

    private void Update()
    {
        Vector3 currentPosition = playerRoot.position;

        Vector3 displacement = currentPosition - previousPosition;

        float movementSpeed = displacement.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

        float normalizedSpeed = Mathf.Clamp01(movementSpeed / maxMovementSpeed);

        if (movementSpeed < movementThreshold)
        {
            normalizedSpeed = 0f;
        }

        animator.SetFloat(SpeedHash, normalizedSpeed);

        previousPosition = currentPosition;
    }
}