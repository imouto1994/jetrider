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
	
	// the list of probabilities or sections, depending on the type
	public InterpolatedValueList sectionList;

	private InterpolatedValueList platformSectionList;
	private InterpolatedValueList sceneSectionList;
	private int activePlatformSection;
	private int activeSceneSection;

	public void Awake()
	{
		instance = this;
	}

	// Initialize function
	public void Start()
	{
		platformSectionList = new InterpolatedValueList(sectionList.hasLoop, sectionList.loopBackToIndex);
		platformSectionList.values = sectionList.values;
		platformSectionList.Init();
		
		sceneSectionList = new InterpolatedValueList(sectionList.hasLoop, sectionList.loopBackToIndex);
		sceneSectionList.values = sectionList.values;
		sceneSectionList.Init();
	}
	
	// Returns the section based off of the distance.
	public int GetSection(float distance, bool isSceneObject)
	{
		int activeSection = (isSceneObject ? activeSceneSection : activePlatformSection);

		if (isSceneObject) {
			activeSection = (int)sceneSectionList.GetValue(distance);
		} else {
			activeSection = (int)platformSectionList.GetValue(distance);
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
