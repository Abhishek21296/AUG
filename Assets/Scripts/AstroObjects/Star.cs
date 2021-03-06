﻿ using UnityEngine;
using System.Collections;

public class Star : CelestialBody
{

	public Star(int _seed, float r, LongPos pos):base(_seed, r, pos)
	{
		
		createRep();
	}

	protected override void createRep()
	{
		scaledRep =  new GameObject("Star " + seed + ", radius: " + radius);



		//the gameobject that holds the scaledRep's mesh data so it can be scaled
		GameObject meshobj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		meshobj.transform.SetParent(scaledRep.transform);
		GameObject.Destroy(meshobj.GetComponent<SphereCollider>());//remove this pesky component
		meshobj.transform.localScale = new Vector3(scaledRadius*2, scaledRadius*2, scaledRadius*2);

		//TODO: move some of these things to a function in celestialbody
		scaledRep.transform.position = CoordinateHandler.stellarSpace.getFloatingPos(scaledPos);

		scaledRep.layer = (int)spaces.Stellar;//add to stellar space
		meshobj.layer = (int)spaces.Stellar;

		meshobj.GetComponent<MeshRenderer>().material = Resources.Load("Star") as Material;
		meshobj.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
	}


}
