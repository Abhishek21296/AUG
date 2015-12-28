﻿using UnityEngine;
using System.Collections;

public class TransportUnit 
{
	public Vector2 conPoint; //the point in the unit where the segments connect
	public Vector3 conPointWorld;//the connecting point in its world position

	public bool conRight = false;//is the streetPoint connected to the street above it(forming a street) ?
	public bool conUp = false;//is the streetPoint connected to the street to the right of it

	public bool populated = false;//for mid and large tus,has teh transport unit been populated with smaller transport units?

	public int indexI;//the I index of the street unit in its container transport unit
	public int indexJ;

	//the level of transport segment that is built(level 1 is highest, nondependent, ex. highways would be 1, smaller streets 2)
	//0 cooresponds to not yet set
	public int rightLev = 0;
	public int upLev = 0;

	//has the conPoint been officially set yet?
	public bool conSet = false;
}

public enum Dir //enum of absolute directions
{
	UP,
	DOWN,
	RIGHT,
	LEFT
};