using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// List of sections that this basic object can be in
public class SectionList
{
	public HashSet<int> sections;
	
	public SectionList(List<int> sectionArray)
	{
		sections = new HashSet<int>();
		for (int i = 0; i < sectionArray.Count; ++i) {
			sections.Add(sectionArray[i]);
		}
	}
	
	public bool ContainsSection(int section)
	{
		return sections.Contains(section);
	}
	
	public int Count()
	{
		return sections.Count;
	}
}

/* Object Type */
public enum ObjectType { Platform, Environment, Obstacle, PowerUp, Donut, Fuel, Count }

/* Basic Object that every object in the game will inherit */
public abstract class BasicObject : MonoBehaviour
{	
	// List of sections that this object belongs to
	public List<int> sections;
	private SectionList sectionList;

	// The object type 
	protected ObjectType objectType;

	// The intial position, rotation and parent Game Object
	private Vector3 startPosition;
	private Quaternion startRotation;
	// Initial transform of parent object
	private Transform startParent;

	// The local index
	private int localIndex;
	// The parent object
	private BasicObject objectParent;

	// Component References
	protected GameObject thisGameObject;
	protected Transform thisTransform;
	
	public virtual void Init()
	{
		sectionList = new SectionList(sections);
		startPosition = transform.position;
		startRotation = transform.rotation;
	}
	
	public virtual void Awake()
	{
		thisGameObject = gameObject;
		thisTransform = transform;

		startPosition = transform.position;
		startRotation = transform.rotation;
	}
	
	public ObjectType GetObjectType()
	{
		return objectType;
	}
	
	public Transform GetTransform()
	{
		return thisTransform;
	}
	
	public void SetLocalIndex(int index)
	{
		localIndex = index;
	}
	
	public int GetLocalIndex()
	{
		return localIndex;
	}
	
	public Vector3 GetStartPosition()
	{
		return startPosition;
	}
	
	public virtual void SetParent(Transform parent)
	{
		thisTransform.parent = parent;
		
		SetStartParent(parent);
	}
	
	public void SetStartParent(Transform parent)
	{
		startParent = parent;
	}
	
	public void SetObjectParent(BasicObject parentObject)
	{
		objectParent = parentObject;
		thisTransform.parent = parentObject.GetTransform();
	}
	
	public BasicObject GetObjectParent()
	{
		return objectParent;
	}
	
	// orient for platform and scene objects
	public virtual void Orient(Vector3 position, Quaternion rotation)
	{
		Vector3 pos = Vector3.zero;
		float yAngle = rotation.eulerAngles.y;
		pos.Set(startPosition.x * Mathf.Cos(yAngle * Mathf.Deg2Rad) + startPosition.z * Mathf.Sin(yAngle * Mathf.Deg2Rad), startPosition.y,
		        -startPosition.x * Mathf.Sin(yAngle * Mathf.Deg2Rad) + startPosition.z * Mathf.Cos(yAngle * Mathf.Deg2Rad));
		pos += position;
		thisTransform.position = pos;
		thisTransform.rotation = startRotation;
		thisTransform.Rotate(0, yAngle, 0, Space.World);
	}
	
	// orient for collidables which have a platform as a parent
	public virtual void Orient(BasicObject parent, Vector3 position, Quaternion rotation)
	{
		thisTransform.parent = parent.GetTransform();
		Vector3 pos = Vector3.zero;
		float yAngle = rotation.eulerAngles.y;
		pos.Set(startPosition.x * Mathf.Cos(yAngle * Mathf.Deg2Rad) + startPosition.z * Mathf.Sin(yAngle * Mathf.Deg2Rad), startPosition.y,
		        -startPosition.x * Mathf.Sin(yAngle * Mathf.Deg2Rad) + startPosition.z * Mathf.Cos(yAngle * Mathf.Deg2Rad));
		pos += position;
		thisTransform.localPosition = parent.GetTransform().InverseTransformPoint(pos);
		thisTransform.rotation = startRotation;
		thisTransform.Rotate(0, rotation.eulerAngles.y, 0, Space.World);
	}
	
	public virtual void Activate()
	{
		Utility.SetActive(thisTransform, true);
	}
	
	public virtual void Deactivate()
	{
		thisTransform.parent = startParent;
		objectParent = null;
		Utility.SetActive(thisTransform, false);
	}
	
	public bool IsActive()
	{
		return thisGameObject.activeSelf;
	}
	
	// the obejct can spawn if it contains the section or there are no sections
	public bool CanSpawnInSection(int section)
	{
		return sectionList.Count() == 0 || sectionList.ContainsSection(section);
	}
}
