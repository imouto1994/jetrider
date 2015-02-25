using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Maps the platform distance/section to a local platform index. 
 * Used by the scenes and sections to be able to determine which platform they are spawning near
 */
[System.Serializable]
public class PlatformDistanceDataMap
{
	public List<float> distances;
	public List<int> localIndexes;
	public List<int> sections;
	
	public PlatformDistanceDataMap()
	{
		distances = new List<float>();
		localIndexes = new List<int>();
		sections = new List<int>();
	}
	
	// A new platform has been spawned, add the distance and section
	public void AddIndex(float distance, int index, int section)
	{
		distances.Add(distance);
		localIndexes.Add(index);
		sections.Add(section);
	}
	
	// Remove the reference if the scene distance is greater than the earliest platform distance
	public void CheckForRemoval(float distance)
	{
		if (distances.Count > 0) {
			// add 0.1f to prevent rounding errors
			if (distances[0] <= distance + 0.1f) {
				distances.RemoveAt(0);
				localIndexes.RemoveAt(0);
				sections.RemoveAt(0);
			}
		}
	}
	
	// Retrieve the first platform local index who does not have a scene spawned near it
	public int GetFirstIndex()
	{
		if (localIndexes.Count > 0) {
			return localIndexes[0];
		}
		return -1;
	}

	// Retrieve the first section which does not have a scene spawned near it
	public int GetFirstSection()
	{
		if (sections.Count > 0) {
			return sections[0];
		}
		return -1;
	}

	// Reset all values
	public void ResetValues()
	{
		distances.Clear();
		localIndexes.Clear();
		sections.Clear();
	}

	// Copy from another PlatformDistanceDataMap object
	public void CopyFrom(PlatformDistanceDataMap other)
	{
		distances = other.distances.GetRange(0, other.distances.Count);
		localIndexes = other.localIndexes.GetRange(0, other.localIndexes.Count);
		sections = other.sections.GetRange(0, other.sections.Count);
	}
}