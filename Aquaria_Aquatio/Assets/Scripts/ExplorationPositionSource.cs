/*
ExplorationPositionSource.cs

Purpose:
Defines the shared position-source abstraction for exploration movement.

Responsibilities:
- Store local East/North displacement in meters.
- Track readiness, accepted samples, rejected samples, and latest sample text.
- Expose GPS-like coordinate properties for real and simulated sources.
- Notify listeners when a new position sample is accepted.

Architecture:
Base MonoBehaviour for real GPS and simulated exploration position providers.
Downstream gameplay systems depend on this abstraction instead of a specific
GPS or editor implementation.

Dependencies:
- UnityEngine.LocationServiceStatus
- UnityEngine.Vector3

Events / Data Flow:
Concrete source accepts or rejects samples
    -> ExplorationPositionSource stores common state
    -> PositionAccepted event and polling properties feed gameplay systems

Position Mapping:
East -> Unity X axis
North -> Unity Z axis
Up -> Unity Y axis

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

using System;
using UnityEngine;

public abstract class ExplorationPositionSource : MonoBehaviour
{
    public event Action<ExplorationPositionSource> PositionAccepted;

    protected bool isReady;
    protected float eastMeters;
    protected float northMeters;
    protected float totalDistanceMeters;
    protected int acceptedSamples;
    protected int rejectedSamples;
    protected string lastSampleResult = "Waiting";

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
