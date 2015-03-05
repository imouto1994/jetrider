using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* This class represents a sequence of InterpolatedValue objects
 *	It is required to not have any overlaps in the sequence
 *	The list of distances has to be in increasing order
 */
[System.Serializable]
public class InterpolatedValueList
{
	public List<InterpolatedValue> values;
	private int[] endDistances;

	// The sequence will loop back when reaching the end
	public bool hasLoop;
	public int loopBackToIndex = 0;	
	private List<InterpolatedValue> loopedValues;
	private int[] loopedEndDistances;

	// The index for end distance which is called last time
	private int previousEndDistanceIndex;
	
	public InterpolatedValueList(bool hasLoop, int loopBackToIndex)
	{
		this.hasLoop = hasLoop;
		this.loopBackToIndex = loopBackToIndex;
	}
	
	public InterpolatedValueList(InterpolatedValue v)
	{
		values = new List<InterpolatedValue>();
		values.Add(v);
	}

	// Initialize function
	public void Init()
	{
		endDistances = new int[values.Count];
		previousEndDistanceIndex = 0;
		for (int i = 0; i < endDistances.Length; ++i) {
			endDistances[i] = values[i].endDistance;
		}
		// Setup if the sequence has loop
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

	// Reset the values when you want to remove the cache of which index of end distances was used last time
	public void ResetValues()
	{
		previousEndDistanceIndex = 0;
	}

	// Get the number of intervals in the sequence
	public int Count()
	{
		if (values == null)
			return 0;
		return values.Count;
	}

	// Get the value according to the distance in the sequence
	public float GetValue(float distance)
	{
		if (values.Count == 0)
			return 0;
		
		bool isLooped = false;
		if (hasLoop) {
			float prevDistance = distance;
			isLooped = (int)(distance / values[endDistances.Length - 1].endDistance) > 0;
			if (isLooped) {
				distance = (distance - values[loopBackToIndex].startDistance) % loopedValues[loopedEndDistances.Length - 1].endDistance;
			} else {
				distance = distance % values[endDistances.Length - 1].endDistance;
			}
			// reset lastEndDistanceIndex if the distance looped around
			if (distance <= prevDistance) {
				previousEndDistanceIndex = 0;
			}
		}
		
		if (isLooped) {
			return loopedValues[GetIndexFromDistance(distance, true)].GetInterpolatedValue(distance);
		} else {
			return values[GetIndexFromDistance(distance, false)].GetInterpolatedValue(distance);
		}
	}

	// Get the minimum and maximum values in the sequence
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

	// Get the corresponding index of interval from the given distance
	// Don't loop from the start of the list each time. Keep a cache of the latest index.
	private int GetIndexFromDistance(float distance, bool isLooped)
	{
		int[] distances = isLooped ? loopedEndDistances : endDistances;
		for (int i = previousEndDistanceIndex; i < distances.Length; ++i) {
			if (distance <= distances[i]) {
				previousEndDistanceIndex = i;
				return i;
			}
		}
		
		return distances.Length - 1;
	}
}