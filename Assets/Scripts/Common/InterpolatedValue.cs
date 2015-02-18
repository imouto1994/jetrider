using UnityEngine;
using System.Collections;

public class InterpolatedValue : MonoBehaviour {	

	public int startDistance;
	public float startValue;
	public int endDistance;
	public float endValue;
	public bool hasLimit;
	
	public InterpolatedValue(int sd, float sv, int ed, float ev, bool ued)
	{
		startDistance = sd;
		startValue = sv;
		endDistance = ed;
		endValue = ev;
		hasLimit = ued;
	}
	
	public float GetInterpolatedValue(float distance)
	{
		// if the distance is before the start distance or after the end distance, return 0
		if (distance < startDistance || (hasLimit && distance > endDistance))
			return 0;
		
		if (startDistance == endDistance || !hasLimit) {
			return startValue;
		}
		
		float distancePercent = ((distance - startDistance) / (endDistance - startDistance));
		
		// distancePercent can be greater than 1 if distance > endDistance
		if (distancePercent > 1)
			distancePercent = 1;
		
		// linear interpolation
		return startValue + (distancePercent * (endValue - startValue));
	}
	
	public bool IsWithinRange(float distance)
	{
		return distance >= startDistance && (!hasLimit || (hasLimit && distance <= endDistance));
	}

}
