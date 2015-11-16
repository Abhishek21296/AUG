﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//used to generation things on the surface of a planet
//an instance of this will belong to a planet instance
public class SurfaceSystem
{

	private float radius;//the planet radius
	public int sideLength;//how many surface units are on one side of a planet face
	private float halfSide;//half the side length used more than once
	
	private static List<SurfaceUnit> surfList = new List<SurfaceUnit>();//surface units that have already been loaded
	//public GameObject tree;//used for instantiation testing


	public SurfaceSystem(float r, int side)
	{
		radius = r;
		sideLength = side;
		halfSide = sideLength/2;
	}


	//a working name
	//builds all the objects in a certain surface unit
	public void CreateSurfaceObjects(SurfaceUnit su)
	{
		//only make the objects in this unit if it has not already been generated
		//and add it to the list so it is not generated again
		if(!surfList.Contains(su))
		{
			//instance a random number generator with seed based on the su position sent through a hash function
			//NOTE: the last 1 parameter is used as a kind of planet identifier, but this may not be needed
			System.Random rand = new System.Random((int)WorldManager.hash.GetHash(su.u, su.v, (int)su.side, 1));

			surfList.Add(su);
			for(int i = 0; i<rand.Next(30); i++)
			{
				//surfacepos of the tree (middle of unit)
				SurfacePos treeSurf = new SurfacePos(su.side, su.u + (float)rand.NextDouble(), su.v + (float)rand.NextDouble());
				//convert to world unit
				Vector3 treeWorld = UnitConverter.getWP(treeSurf, radius, sideLength);
				//GameObject.Instantiate(tree, treeWorld, Quaternion.identity);
				//build the tree object(adds it to builtobjects list and maybe eventually add it to the render list
				Build.buildObject<TestTree>(treeWorld).init();
				//WorldHelper.buildObject<TestTree>(new Vector3(5,5,210));
			
			}
		}

	}
}
