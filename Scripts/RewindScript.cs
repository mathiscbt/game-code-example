using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Records the character's positions over time, then replays them on a copy.
/// </summary>

[RequireComponent(typeof(Rigidbody2D))]
public class RewindScript : MonoBehaviour
{
    private Rigidbody2D rb;
    private List<Vector3> positions = new List<Vector3>();
    private List<float> timestamps = new List<float>();
    private Vector3 initialPosition;
    private float startTime;
    private bool registering = false;
    private float startReplayTime;
    private int replayIndex;
    private bool replaying;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        initialPosition = transform.position;
    }

    /// <summary>
    /// Starts recording the character's trajectory from the current frame.
    /// Clears any previous recording. Call Save() to stop recording.
    /// </summary>
    public void StartRecording()
    {
        positions.Clear();
        timestamps.Clear();
        startTime = Time.time;
        initialPosition = transform.position;
        rb.bodyType = RigidbodyType2D.Dynamic;
        registering = true;
        replaying = false;
    }

    /// <summary>
    /// While recording, stores the current position and elapsed time each frame.
    /// While replaying, advances through the recorded timestamps and places the copy
    /// at the matching position.
    /// </summary>
    void Update()
    {
        if (registering)
        {
            positions.Add(transform.position);
            timestamps.Add(Time.time - startTime);
        }
        else if (replaying)
        {
            if (replayIndex < timestamps.Count - 1)
            {
                while (Time.time - startReplayTime >= timestamps[replayIndex] && replayIndex < timestamps.Count - 1)
                {
                    replayIndex += 1;
                }
                transform.position = positions[replayIndex];
            }
            else
            {
                transform.position = positions[replayIndex - 1];
            }
        }
    }

    /// <summary>
    /// Stops recording. The recorded trajectory is kept for replay.
    /// </summary>
    public void Save() => registering = false;

    /// <summary>
    /// Switches to replay: the body becomes kinematic, so it is not affected by player physics,
    /// and the copy restarts from the initial position.
    /// </summary>
    public void Play()
    {
        if (positions.Count == 0) return;

        replaying = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
        startReplayTime = Time.time;
        replayIndex = 0;

        transform.position = initialPosition;
    }

}
