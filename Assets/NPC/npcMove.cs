using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIPatrol : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float reachDistance = 0.5f;

    [Header("Idle")]
    public float idleTime = 2f;

    [Header("Waypoints")]
    public List<Transform> waypoints = new List<Transform>();

    int currentIndex;
    bool isWaiting;

    AIBrain brain;

    void Awake()
    {
        brain = GetComponent<AIBrain>();
    }

    void Start()
    {
        if (brain != null)
            brain.moveSpeed = moveSpeed;
    }

    void Update()
    {
        if (waypoints.Count == 0 || isWaiting)
            return;

        MoveToWaypoint();
    }

    void MoveToWaypoint()
    {
        Transform target = waypoints[currentIndex];
        Vector3 dir = (target.position - transform.position).normalized;

        transform.position += dir * moveSpeed * Time.deltaTime;

        if (dir.sqrMagnitude > 0.0001f)
        {
            transform.forward = Vector3.Lerp(
                transform.forward,
                dir,
                10f * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, target.position) < reachDistance)
            StartCoroutine(IdleThenNext());
    }

    IEnumerator IdleThenNext()
    {
        isWaiting = true;

        if (brain != null)
            brain.SetSpeed(0f);

        yield return new WaitForSeconds(idleTime);

        currentIndex++;
        if (currentIndex >= waypoints.Count)
            currentIndex = 0;

        isWaiting = false;
    }
}
