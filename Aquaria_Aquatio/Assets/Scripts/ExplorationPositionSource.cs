using System;
using UnityEngine;

public abstract class ExplorationPositionSource : MonoBehaviour
{
    public event Action<ExplorationPositionSource> PositionAccepted;

    [SerializeField] protected bool isReady;
    [SerializeField] protected float eastMeters;
    [SerializeField] protected float northMeters;
    [SerializeField] protected float totalDistanceMeters;
    [SerializeField] protected int acceptedSamples;
    [SerializeField] protected int rejectedSamples;
    [SerializeField] protected string lastSampleResult = "Waiting";

    public bool IsReady => isReady;
    public float EastMeters => eastMeters;
    public float NorthMeters => northMeters;
    public float TotalDistanceMeters => totalDistanceMeters;
    public Vector3 DisplacementMeters => new Vector3(eastMeters, 0f, northMeters);
    public int AcceptedSamples => acceptedSamples;
    public int RejectedSamples => rejectedSamples;
    public string LastSampleResult => lastSampleResult;

    public virtual double OriginLatitude => 0.0;
    public virtual double OriginLongitude => 0.0;
    public virtual double CurrentLatitude => 0.0;
    public virtual double CurrentLongitude => 0.0;
    public virtual float HorizontalAccuracy => 0f;
    public virtual LocationServiceStatus GPSStatus => LocationServiceStatus.Stopped;

    protected void AcceptPosition(float east, float north, string result)
    {
        UpdatePosition(east, north, result);
        acceptedSamples++;
        PositionAccepted?.Invoke(this);
    }

    protected void UpdatePosition(float east, float north, string result)
    {
        eastMeters = east;
        northMeters = north;
        totalDistanceMeters = new Vector2(eastMeters, northMeters).magnitude;
        isReady = true;
        lastSampleResult = result;
    }

    protected void RejectPosition(string reason)
    {
        rejectedSamples++;
        lastSampleResult = reason;
    }
}
