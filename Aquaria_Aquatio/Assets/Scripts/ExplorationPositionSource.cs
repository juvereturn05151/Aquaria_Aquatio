/*
ExplorationPositionSource.cs

Purpose:
Defines a common base class for systems that provide the player's exploration position.

Responsibilities:

Store the player's local East/North displacement in meters.
Track whether a valid position is available.
Track accepted and rejected position samples.
Store the latest sample result for debugging.
Expose displacement data to other gameplay systems.
Notify listeners when a new position sample is accepted.

Architecture:
This class does not determine where position data comes from.

Concrete classes such as GPSPositionSource or simulated/editor position sources
should inherit from this class and provide their own position data.

Gameplay systems should depend on ExplorationPositionSource rather than a
specific implementation. This allows the same gameplay code to work with
real GPS data, simulated GPS data, or other position sources.

Position Mapping:
East -> Unity X axis
North -> Unity Z axis
Up -> Unity Y axis

Events:
PositionAccepted is invoked whenever a new position sample is successfully accepted.

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
