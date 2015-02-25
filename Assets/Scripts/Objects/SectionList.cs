using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// List of sections that an object can be in
public class SectionList
{
	public HashSet<int> sections;
	
	public SectionList(List<int> sections)
	{
		this.sections = new HashSet<int>();
		for (int i = 0; i < sections.Count; ++i) {
			this.sections.Add(sections[i]);
		}
	}
	
	// Check whether this object can be in this section
	public bool ContainsSection(int section)
	{
		return this.sections.Contains(section);
	}
	
	// Get the number of sections that this object can be in
	public int Count()
	{
		return this.sections.Count;
	}
}