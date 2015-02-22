using UnityEngine;
using System.Collections;

// The sequence of propabilities that an object will appear or not appear
public class AppearProbs : MonoBehaviour
{
	// Sequences of propabilities
	public InterpolatedValueList appearProbabilities;
	public InterpolatedValueList nonappearProbabilities;

	// Initialize function
	public void Init()
	{
		appearProbabilities.Init();
		nonappearProbabilities.Init();
	}
	
	// Return the probability that this object should appear based off of the current distance
	public float GetProbability(float distance)
	{
		// Chance of no probability of no occur says so
		if (nonappearProbabilities.Count() > 0) {
			float prob = nonappearProbabilities.GetValue(distance);
			if (Random.value < prob) {
				return 0;
			}
		}
		
		return nonappearProbabilities.GetValue(distance);
	}
}