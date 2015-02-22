using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* Object Type */
public enum ObjectType { Platform, Scene, Obstacle, PowerUp, Donut, Fuel, Count }

/* Basic Object that every object in the game will inherit */
public abstract class BasicObject : MonoBehaviour
{	
	// List of sections that this object belongs to
	public List<int> sections;
	private SectionList sectionList;

	// The object type 
	protected ObjectType objectType;

	// The initial position, rotation and parent Game Object
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

	// Initialize function
	public virtual void Init()
	{
		sectionList = new SectionList(sections);
		startPosition = transform.position;
		startRotation = transform.rotation;
	}

	// Awake Function
	public virtual void Awake()
	{
		thisGameObject = gameObject;
		thisTransform = transform;

		startPosition = transform.position;
		startRotation = transform.rotation;
	}

	/* Get the type of object */
	public ObjectType GetObjectType()
	{
		return objectType;
	}

	/* Get the transform of object */
	public Transform GetTransform()
	{
		return thisTransform;
	}

	/* Set the local index of the object with its similar object type */
	public void SetLocalIndex(int index)
	{
		localIndex = index;
	}

	/* Return the local index of this object */
	public int GetLocalIndex()
	{
		return localIndex;
	}

	/* Return the initial position of the object */
	public Vector3 GetStartPosition()
	{
		return startPosition;
	}

	/* Set the parent transform of this object */
	public virtual void SetParent(Transform parent)
	{
		thisTransform.parent = parent;
		
		SetStartParent(parent);
	}

	/* Set the initial parent transform of this object */
	public void SetStartParent(Transform parent)
	{
		startParent = parent;
	}

	/* Set parent object */
	public void SetObjectParent(BasicObject parentObject)
	{
		objectParent = parentObject;
		thisTransform.parent = parentObject.GetTransform();
	}

	/* Return the parent object */
	public BasicObject GetObjectParent()
	{
		return objectParent;
	}
	
	// Orient objects
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
	
	// Orient for objects relative to parent
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

	// Activate the object and its children
	public virtual void Activate()
	{
		Utility.SetActive(thisTransform, true);
	}

	// Deactivate the object
	public virtual void Deactivate()
	{
		thisTransform.parent = startParent;
		objectParent = null;
		Utility.SetActive(thisTransform, false);
	}

	// Check if the object is active or not
	public bool IsActive()
	{
		return thisGameObject.activeSelf;
	}
	
	// Check if the object can spawn in section if it contains the indicated section or there are no sections
	public bool CanSpawnInSection(int section)
	{
		return sectionList.Count() == 0 || sectionList.ContainsSection(section);
	}
}
