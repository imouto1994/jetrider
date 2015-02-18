using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InterpolatedValueList
{
	// this array MUST be in order according to startDistance/EndDistance, with no overlap
	public List<InterpolatedValue> values;
	
	public bool hasLoop;
	public int loopBackToIndex = 0;	
	private List<InterpolatedValue> loopedValues;

	private int lastEndDistanceIndex;
	private int[] endDistances;
	private int[] loopedEndDistances;
	
	public InterpolatedValueList(bool l, int index)
	{
		hasLoop = l;
		loopBackToIndex = index;
	}
	
	public InterpolatedValueList(InterpolatedValue v)
	{
		values = new List<InterpolatedValue>();
		values.Add(v);
	}
	
	public void Init()
	{
		endDistances = new int[values.Count];
		lastEndDistanceIndex = 0;
		for (int i = 0; i < endDistances.Length; ++i) {
			endDistances[i] = values[i].endDistance;
		}
		if (hasLoop) {
			loopedValues = new List<InterpolatedValue>();
			int startDistance = values[loopBackToIndex].startDistance;
			loopedEndDistances = new int[values.Count - loopBackToIndex];
			for (int i = loopBackToIndex; i < values.Count; ++i) {
				loopedValues.Add(new InterpolatedValue(values[i].startDistance - startDistance, 
				                                       values[i].startValue, 
				                                       values[i].endDistance - startDistance, 
				                                       values[i].endValue, 
				                                       values[i].hasLimit));
				loopedEndDistances[loopedValues.Count - 1] = loopedValues[loopedValues.Count - 1].endDistance;
			}
		}
	}
	
	public void ResetValues()
	{
		lastEndDistanceIndex = 0;
	}
	
	public int Count()
	{
		if (values == null)
			return 0;
		return values.Count;
	}
	
	public float GetValue(float distance)
	{
		if (values.Count == 0)
			return 0;
		
		bool looped = false;
		if (hasLoop) {
			float prevDistance = distance;
			looped = (int)(distance / values[endDistances.Length - 1].endDistance) > 0;
			if (looped) {
				distance = (distance - values[loopBackToIndex].startDistance) % loopedValues[loopedEndDistances.Length - 1].endDistance;
			} else {
				distance = distance % values[endDistances.Length - 1].endDistance;
			}
			// reset lastEndDistanceIndex if the distance looped around
			if (distance <= prevDistance) {
				lastEndDistanceIndex = 0;
			}
		}
		
		if (looped) {
			return loopedValues[GetIndexFromDistance(distance, true)].GetInterpolatedValue(distance);
		} else {
			return values[GetIndexFromDistance(distance, false)].GetInterpolatedValue(distance);
		}
	}
	
	public void GetMinMaxValue(out float min, out float max)
	{
		max = float.MinValue;
		min = float.MaxValue;
		for (int i = 0; i < values.Count; ++i) {
			if (max < values[i].startValue) {
				max = values[i].startValue;
			}
			if (min > values[i].startValue) {
				min = values[i].startValue;
			}
			if (values[i].hasLimit) {
				if (max < values[i].endValue) {
					max = values[i].endValue;
				}
				if (min > values[i].endValue) {
					min = values[i].endValue;
				}
			}
		}
	}
	
	// Don't loop from the start of the list each time. Keep a cache of the latest index. This works because the 
	// list is in order from the start distance to the end distance.
	private int GetIndexFromDistance(float distance, bool useLoopedEndDistances)
	{
		int[] distances = useLoopedEndDistances ? loopedEndDistances : endDistances;
		for (int i = lastEndDistanceIndex; i < distances.Length; ++i) {
			if (distance <= distances[i]) {
				lastEndDistanceIndex = i;
				return i;
			}
		}
		
		return distances.Length - 1;
	}
}