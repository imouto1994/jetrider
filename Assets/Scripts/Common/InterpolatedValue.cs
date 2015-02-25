using UnityEngine;
using System.Collections;

/* Interpolate value given start and end distances + values */
[System.Serializable]
public class InterpolatedValue {	
	// Start data
	public int startDistance;
	public float startValue;
	// End data
	public int endDistance;
	public float endValue;
	// If this is false, we will not use the end data
	public bool hasLimit;
	
	public InterpolatedValue(int startDistance, float startValue, int endDistance, float endValue, bool hasLimit)
	{
		this.startDistance = startDistance;
		this.startValue = startValue;
		this.endDistance = endDistance;
		this.endValue = endValue;
		this.hasLimit = hasLimit;
	}

	/* Get the interpolated value */
	public float GetInterpolatedValue(float distance)
	{
		if (distance < startDistance || (hasLimit && distance > endDistance))
			return 0;
		
		if (startDistance == endDistance || !hasLimit) {
			return startValue;
		}
		float distancePercent = ((distance - startDistance) / (endDistance - startDistance));

		return startValue + (distancePercent * (endValue - startValue));
	}

	/* Check if the distance is within the start and end distance */
	public bool IsWithinRange(float distance)
	{
		return distance >= startDistance && (!hasLimit || (hasLimit && distance <= endDistance));
	}

}
