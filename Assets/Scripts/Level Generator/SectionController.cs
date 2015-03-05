using UnityEngine;
using System.Collections;

/**
 * SINGLETON CLASS for controlling sections
 * Sections define groups of objects that can spawn at certain times. 
 * For example you may want a set of objects to spawn when indoors and a different set to spawn when outdoors
 */
public class SectionController : MonoBehaviour
{
	static public SectionController instance;
	
	// If true, you must provide transitions from one section to another for every combination of sections for both platforms and scenes
	public bool useSectionTransitions;
	
	// the start section, used by the probability type
	public int startSection;
	
	// the end section (inclusive), used by the probability type
	public int endSection;
	
	// the list of probabilities or sections, depending on the type
	public InterpolatedValueList sectionList;

	private int activePlatformSection;
	private int activeSceneSection;

	public void Awake()
	{
		instance = this;
	}

	// Initialize function
	public void Start()
	{
		sectionList.Init();
	}
	
	// Returns the section based off of the distance.
	public int GetSection(float distance, bool isSceneObject)
	{
		int activeSection = (isSceneObject ? activeSceneSection : activePlatformSection);

		if (isSceneObject) { // scene objects need to have the same section as the platform below it
			activeSection = ObjectHistory.instance.GetFirstPlatformSection();
		} else {
			if (Random.value < sectionList.GetValue(distance)) {
				activeSection = Random.Range(startSection, endSection + 1);
			}
		}
		
		if (isSceneObject) {
			activeSceneSection = activeSection;
		} else {
			activePlatformSection = activeSection;
		}
		
		return activeSection;
	}

	// Reset values
	public void ResetValues()
	{
		activePlatformSection = activeSceneSection = 0;
		sectionList.ResetValues();

	}
}
