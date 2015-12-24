﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//class dedicated to filling a mid transport unit with base units
//should have gone in the TransportSystem class, but it is way to long and i am somewhat organized......
public class TUMidFiller
{

	//a reference to the transport system that contains this instance
	private TransportSystem tran;
	private int midTUWidth;//how many base transport units are on one side of a mid street unit
		
	//a list of base transport units that is returned and nullified after every fill call
	//private TransportUnit[,] baseList;
	private Dictionary<SurfaceUnit, TUBase> baseList;//a list of all the base units in the current base unit
	private TransportUnit midU;//the mid unit being filled
	private int baseIndexI;//the position of the mid unit in base units
	private int baseIndexJ;

	public TUMidFiller(TransportSystem t, int m)
	{
		tran = t;
		midTUWidth = m;
	}


	public Dictionary<SurfaceUnit, TUBase> populate(TransportUnit mu, SurfaceUnit su)
	{
		//nullify baseList so it can be reused for this mid unit
		baseList = null;
		midU = mu;

		//an array of base transport units that will eventually be returned
		//baseList = new TransportUnit[midTUWidth, midTUWidth];
		baseList = new Dictionary<SurfaceUnit, TUBase>();

		midU = mu;
		baseIndexI = midU.indexI*midTUWidth;
		baseIndexJ = midU.indexJ*midTUWidth;
		fill(mu, su);

		return baseList;

	}

	//fills a mid unit with base units 
	//NOTE: target refers to the intersection while build direction refers to an unmodified straight street path
	public void fill(TransportUnit mu, SurfaceUnit su)
	{

		//set the conPoint of the base unit that contains the mid unit's conPoint
		int powIndexX = Mathf.FloorToInt(mu.conPoint.x);
		int powIndexY = Mathf.FloorToInt(mu.conPoint.y);
		TransportUnit powUnit = getBase(powIndexX, powIndexY);
		powUnit.conPoint = mu.conPoint;

		MyDebug.placeMarker(UnitConverter.getWP(new SurfacePos(su.side, powUnit.conPoint.x, powUnit.conPoint.y), 
		                     WorldManager.curPlanet.radius, 64*16));
		                    
		//Debug.Log(mu.conPoint + " " + powUnit.conPoint);
		//Debug.Log(powIndexX + " " + powIndexY);

		//the mid transport units to the left and bottom of the current mid unit
		TransportUnit leftTU = tran.getMid(new SurfaceUnit(su.side,su.u - 1, su.v));
		TransportUnit downTU = tran.getMid(new SurfaceUnit(su.side,su.u, su.v - 1));

		//the conPoint of the mid unit above, below, to the right and left of this one
		Vector2 conPointRight = tran.getMid(new SurfaceUnit(su.side,su.u + 1, su.v)).conPoint;
		Vector2 conPointLeft = leftTU.conPoint;
		Vector2 conPointUp = tran.getMid(new SurfaceUnit(su.side,su.u, su.v + 1)).conPoint;
		Vector2 conPointDown = downTU.conPoint;

		//Debug.Log(conPointUp + " " + conPointRight + " " + conPointLeft + " " + conPointDown);

		//the directions a street will connect to from the center
		bool conRight = mu.conRight;
		bool conLeft = leftTU.conRight;//if the unit to the left connects to the right, then this unit will connect to the left
		bool conUp = mu.conUp;
		bool conDown = downTU.conUp;

		//the slope that all streets aim for when they converge in the middle
		float targetSlopeRight = 0;
		float targetSlopeLeft = 0;
		float targetSlopeUp = 0;
		float targetSlopeDown = 0;

		//sets all the target slopes based on what sides connect
		if(conUp && conDown && conRight && conLeft)
		{ //form a 4 way perpindicular intersection
			//vector representing the direction from the bottom to top street point
			Vector2 DownUpVec = conPointUp - conPointDown; 
			
			//vector representing the direction from the left to right street point
			Vector2 LeftRightVec = conPointRight - conPointLeft; 
			
			//the vector perpindicular to the the left right vector used to find the target inter line
			Vector2 LeftRightPerp = new Vector2(-LeftRightVec.y, LeftRightVec.x);//opposite reciprocal
			
			//the target vector that the street coming from above will aim for at the intersection
			Vector2 targetVecUpDown = (DownUpVec.normalized + LeftRightPerp.normalized) / 2;
			
			//the slope the roads going from up to down should have at the intersection, average of up down slope and slope perpindicular to left right slope
			//float targetSlopeUpDown = (slopeUpDown + slopeLeftRightR)/2;
			//float targetSlopeLeftRight = (slopeLeftRight + slopeUpDownR)/2;
			float targetSlopeUpDown = targetVecUpDown.y / targetVecUpDown.x;
			float targetSlopeLeftRight = -1 / targetSlopeUpDown;//perpindicular to vertical target slope
			
			targetSlopeRight = targetSlopeLeft = targetSlopeLeftRight;
			targetSlopeUp = targetSlopeDown = targetSlopeUpDown;
			//Debug.Log(targetSlopeUp + " " + targetSlopeRight);
		} 
		else if(conUp && conDown)//if the top and bottom are connected but all four sides are not
		{
			targetSlopeUp = targetSlopeDown = findSlope(conPointUp, conPointDown);
			
			if(conRight)
				targetSlopeRight = perp(targetSlopeUp);//this street will come in aand connect perpindicular to the up and down street
			else if(conLeft)
				targetSlopeLeft = perp(targetSlopeUp);
		} 
		else if(conRight && conLeft)//if the left and right are connected but all four sides are not
		{
			targetSlopeRight = targetSlopeLeft = findSlope(conPointRight, conPointLeft);
			
			if(conUp)
				targetSlopeUp = perp(targetSlopeRight);//this street will come in aand connect perpindicular to the right and left street(has no influence on the slope)
			else if(conDown)
				targetSlopeDown = perp(targetSlopeRight);
		} 
		else if(conUp && conRight)//if the street connects up and right but nowhere else
		{
			targetSlopeUp = targetSlopeRight = findSlope(conPointUp, conPointRight);
		} 
		else if(conUp && conLeft)
		{
			targetSlopeUp = targetSlopeLeft = findSlope(conPointUp, conPointLeft);
		} 
		else if(conDown && conRight)
		{
			targetSlopeDown = targetSlopeRight = findSlope(conPointDown, conPointRight);
		} 
		else if(conDown && conLeft)//if the street connects down and left but nowhere else
		{
			targetSlopeDown = targetSlopeLeft = findSlope(conPointDown, conPointLeft);
		} 
		else if(conUp)//if it only connects to the top mid unit, make slope between the top bl point and this bl point
		{
			targetSlopeUp = findSlope(conPointUp, mu.conPoint);
		} 
		else if(conDown)
		{
			targetSlopeDown = findSlope(conPointDown, mu.conPoint);
		} 
		else if(conRight)
		{
			targetSlopeRight = findSlope(conPointRight, mu.conPoint);
		} 
		else if(conLeft)
		{
			targetSlopeLeft = findSlope(conPointLeft, mu.conPoint);
		}

		//Debug.Log(targetSlopeUp + " " + targetSlopeRight + " " + targetSlopeLeft + " " + targetSlopeDown);
		/*MyDebug.placeMarker(UnitConverter.getWP(new SurfacePos(PSide.TOP, conPointUp.x, conPointUp.y), 
		                                        WorldManager.curPlanet.radius, 64*16));
		MyDebug.placeMarker(UnitConverter.getWP(new SurfacePos(PSide.TOP, conPointDown.x, conPointDown.y), 
		                                        WorldManager.curPlanet.radius, 64*16));
		MyDebug.placeMarker(UnitConverter.getWP(new SurfacePos(PSide.TOP, conPointRight.x, conPointRight.y), 
		                                        WorldManager.curPlanet.radius, 64*16));
		MyDebug.placeMarker(UnitConverter.getWP(new SurfacePos(PSide.TOP, conPointLeft.x, conPointLeft.y), 
		                                        WorldManager.curPlanet.radius, 64*16));
			*/

		//actually build the streets
		if(conUp)
			buildStreetCurve(conPointUp, targetSlopeUp, Dir.UP, powIndexX, powIndexY, mu);
		
		if(conDown)//if the mid unit below connects up, make a street down
			buildStreetCurve(conPointDown, targetSlopeDown, Dir.DOWN, powIndexX, powIndexY, mu);
		
		if(conRight)
			buildStreetCurve(conPointRight, targetSlopeRight, Dir.RIGHT, powIndexX, powIndexY, mu);
		
		if(conLeft)//if the mid unit below connects up, make a street down
			buildStreetCurve(conPointLeft, targetSlopeLeft, Dir.LEFT, powIndexX, powIndexY, mu);


	}

	//builds one of four possible streets in a mid unit (up down left right);
	//outsideConPoint is the conPoint of the streetpoint that is outside of the mid unit (left, right up down), targetSlope is the slope the road tries to have at the mid street point
	//buildToSide is the side the street will be built and connected to
	//startIndex x and y is the index of the base unit that contains the mid unit conPoint and the roads will be build from
	public void buildStreetCurve(Vector2 outsideConPoint, float targetSlope, Dir buildToSide, int startIndexX, int startIndexY, TransportUnit mu)
	{
		//the direction to build the road, adjacent street point - cur street point normalized, might need to change distance for a higher sample rate
		Vector2 buildDir = (outsideConPoint - mu.conPoint).normalized * 1f;//1 is the lenght of the build vector(distance between checked street points
		

		Debug.Log(outsideConPoint+ " " +  targetSlope + " " +  buildToSide+ " " + startIndexX + " " + startIndexY + " " + mu.conPoint);
		//goal line slope 
		float gps = findSlope(mu.conPoint, outsideConPoint);
		float gpx, gpy;//the x and y coordinate of the goal point
		int gix, giy;//the goal base unit that is touching the edge and the goal point

		//this block will find the goal point and goal base unit
		if(buildToSide == Dir.UP)//if a street is being build to connect to the up side
		{
			gpy = (mu.indexJ + 1) * midTUWidth;//IF FACING UP!!! goal point y value , touches the top of the mid unit
			gpx = (gpy - mu.conPoint.y + gps * mu.conPoint.x) / gps;//finds x point from y point
			
			findBaseIndexfromConPoint(new Vector2(gpx, gpy - 0.5f), out gix, out giy);//finds index of base unit right below the goal point, 0.5 gets the point inside the base unit for sure
			
			getBase(gix, giy).conUp = true;//this top unit will connect to the one above it in the topp mid unit

			TUBase goalBU = getBase(gix,giy);
			if(!goalBU.conSet)
				goalBU.conPoint = new Vector2(gpx, gpy - 0.0001f);//sets the bl point of this goal base unit in case it is not later set
			
		} else if(buildToSide == Dir.DOWN)
		{
			gpy = (mu.indexJ) * midTUWidth;//IF FACING DOWN!!! goal point y value , touches the bottom of the mid unit
			gpx = (gpy - mu.conPoint.y + gps * mu.conPoint.x) / gps;//finds x point from y point
			
			findBaseIndexfromConPoint(new Vector2(gpx, gpy + 0.5f), out gix, out giy);//finds index of base unit right above the goal point

			TUBase goalBU = getBase(gix,giy);
			if(!goalBU.conSet)
				goalBU.conPoint = new Vector2(gpx, gpy + 0.0001f);//sets the bl point of this goal base unit in case it is not later set
			
		} else if(buildToSide == Dir.RIGHT)
		{
			gpx = (mu.indexI + 1) * midTUWidth; //+1 includes the width of the current unit to find the right goal point
			
			//need to change this probably!!
			gpy = gps * (gpx - mu.conPoint.x) + mu.conPoint.y;//finds y point from x point using point slope
			
			//y-y1=m(x-x1)
			//y=m(x-x1)+y1
			
			findBaseIndexfromConPoint( new Vector2(gpx - 0.5f, gpy), out gix, out giy);//finds index of base unit right to the left the goal point
			
			getBase(gix, giy).conRight = true;//this right unit will connect to the one next to it in the right mid unit

			TUBase goalBU = getBase(gix,giy);
			if(!goalBU.conSet)
				goalBU.conPoint = new Vector2(gpx - 0.0001f, gpy);//sets the bl point of this goal base unit in case it is not later set
			
		} else if(buildToSide == Dir.LEFT)
		{
			gpx = (mu.indexI) * midTUWidth; //+1 includes the width of the current unit to find the right goal point
			
			//need to change this probably!!
			gpy = gps * (gpx - mu.conPoint.x) + mu.conPoint.y;//finds y point from x point using point slope
			
			//y-y1=m(x-x1)
			//y=m(x-x1)+y1
			
			findBaseIndexfromConPoint(new Vector2(gpx + 0.5f, gpy), out gix, out giy);//finds index of base unit right to the right the goal point

			TUBase goalBU = getBase(gix,giy);
			if(!goalBU.conSet)
				goalBU.conPoint = new Vector2(gpx + 0.0001f, gpy);//sets the bl point of this goal base unit in case it is not later set
			
			//makeMarker(blToWorldUnits(baseList[gix, giy].streetPointBL));
		} else //will never happen
		{
			gpx = 0;
			gpy = 0;
			gix = 0;
			giy = 0;
		}

		//Debug.Log(gpx + " " + gpy + " " + getBase(gix, giy).conPoint);
		MyDebug.placeMarker(UnitConverter.getWP(new SurfacePos(PSide.TOP, gpx, gpy), 
		                                        WorldManager.curPlanet.radius, 64*16));
		//the point on the buildDir line that is on the edge of the mid unit, used to calculate interpolation percents
		Vector2 goalPoint = new Vector2(gpx, gpy);
		

		//total distance between the mid street point and point on the edge(goal point)
		float totalDist = Vector2.Distance(mu.conPoint, goalPoint);
		
		TUBase lastBaseUnit = getBase(startIndexX, startIndexY);//the base unit used as a refernce for the loop after it

		//this loop builds the road out from the center(mid conPoint) to the edge(goal point)
		//20 or whatever is the limit 
		for(int i=1; i<20; i++)
		{
			//a point in the buildDir Direction (x2,y2)
			Vector2 buildDirPoint = mu.conPoint + buildDir * i;
			
			//finds the point on the target slope line that forms a perpindicular line with the buildDirPoint so they can be interpolated between
			Vector2 pointOnTarget = findPoint(mu.conPoint.x, mu.conPoint.y, targetSlope, buildDirPoint.x, buildDirPoint.y, -1 / targetSlope);//targetSlopeUpDown
			
			//distance from the build dir point and goal point on edge (less than total dist)
			float curDist = Vector2.Distance(buildDirPoint, goalPoint);
			
			//the percentage curDist is of total dist used to interpolate between buildDirPoint and pointOnTarget to get the final point
			float percentDist = curDist / totalDist;
			
			//the distance between the buildDirPoint and the pointOnTarget
			Vector2 distTargets = buildDirPoint - pointOnTarget;
			
			//the partial distance that the final point is from buildDirPoint to pointOnTarget
			Vector2 partDistTargets = distTargets * percentDist;
			
			Vector2 finalPoint = buildDirPoint - partDistTargets;//final point is partDist from build dir point to pointOnTarget
			
			int fIndexX, fIndexY;//index of the base unit that contains the final point

			findBaseIndexfromConPoint(finalPoint, out fIndexX, out fIndexY);//finds index of base unit right below the goal point

			//MyDebug.placeMarker(UnitConverter.getWP(new SurfacePos(PSide.TOP, finalPoint.x, finalPoint.y), 
			  //                                      WorldManager.curPlanet.radius, 64*16));

			//if the final point is out of range in the current mid unit, stop the loop
			//also, this means that the goal base unit was never reached, so the last base unit needs to be connected to it
			if(fIndexX >= baseIndexI+midTUWidth || fIndexY >= baseIndexJ+midTUWidth || fIndexX < baseIndexI || fIndexY < baseIndexJ)
			{
				connectBases(getBase(gix, giy), lastBaseUnit);//connect the last used base unit to the goal base unit because they were not connected automaticaly
				break;
			}
			
			TUBase curBaseUnit = getBase(fIndexX, fIndexY);
			//the current base unit to modify
			
			
			if(!curBaseUnit.conSet)//if the bl point of the current base unit has not already been set, set it. can change this to use a property in the subase class(probably should)
			{
				
				curBaseUnit.conPoint = finalPoint;//make bl point of the base that contains the final point the final point
				curBaseUnit.conSet = true; //can no longer set the street point of this base unit
				
				connectBases(curBaseUnit, lastBaseUnit);//connect the current base unit to the last one
				
			}

			if(fIndexX == gix && fIndexY == giy)//end the loop if the current point is in the base unit on the edge
				break;

			lastBaseUnit = curBaseUnit;//set the new last base unit for reference in the next loop
		
		}

	}

	//checks where two bases are in relation to each other and sets their connection variables as necesarry
	//will not connect at all if they are too far apart
	public void connectBases(TUBase base1, TUBase base2)//the two units to set connectivity
	{
		if(base1.indexI + 1 == base2.indexI && base1.indexJ == base2.indexJ)//if the second base unit is directly to the right of the first one
		{
			base1.conRight = true;//connect the first base to the right because the second base is on the right
		} else if(base1.indexI - 1 == base2.indexI && base1.indexJ == base2.indexJ)//if the second base unit is directly to the left of the first one
		{
			base2.conRight = true;
		} else if(base1.indexI == base2.indexI && base1.indexJ + 1 == base2.indexJ)//if the second base unit is directly to the top of the first one
		{
			base1.conUp = true;
		} else if(base1.indexI == base2.indexI && base1.indexJ - 1 == base2.indexJ)//if the second base unit is directly to the bottom of the first one
		{
			base2.conUp = true;
		} else if(base1.indexI + 1 == base2.indexI && base1.indexJ + 1 == base2.indexJ)//if the second base unit is to the top right of the first one
		{
			base1.conUpRight = true;
		} else if(base1.indexI - 1 == base2.indexI && base1.indexJ + 1 == base2.indexJ)//if the second base unit is to the top left of the first one
		{
			base1.conUpLeft = true;
		} else if(base1.indexI + 1 == base2.indexI && base1.indexJ - 1 == base2.indexJ)//if the second base unit is to the bottom right of the first one
		{
			base2.conUpLeft = true;
		} else if(base1.indexI - 1 == base2.indexI && base1.indexJ - 1 == base2.indexJ)//if the second base unit is to the bottom left of the first one
		{
			base2.conUpRight = true;
		}
	}

	//returns the base unit from baseList at index u,v and creates one if necessary
	private TUBase getBase(int i, int j)
	{
		/*if(baseList[i,j] == null)
			baseList[i,j] = new TransportUnit();

		return baseList[i,j];*/
		TUBase bu = null;
		SurfaceUnit su = new SurfaceUnit(PSide.NONE, i, j);
		//Debug.Log(su);
		if(!baseList.TryGetValue(su, out bu))
		{
			bu = new TUBase();
			bu.indexI = i;
			bu.indexJ = j;
			baseList.Add(su, bu);
		}
		return bu;
	}



	//finds the base unit that a bl point falls in given the current mid unit
	public void findBaseIndexfromConPoint(Vector2 conPoint, out int indexX, out int indexY)
	{
		//rounds down to an integer to find the index
		indexX = Mathf.FloorToInt(conPoint.x);
		indexY = Mathf.FloorToInt(conPoint.y);
		//indexX = Mathf.FloorToInt(conPoint.x - midTUWidth * midU.indexI);
		//indexY = Mathf.FloorToInt(conPoint.y - midTUWidth * midU.indexJ);

	}

	public float findSlope(Vector2 v1, Vector2 v2)
	{
		if(v1.x - v2.x == 0)
		{
			Debug.Log("infinite slope");
			return Mathf.Infinity;//i don't know what this will do but hopefully it will work
		}
		return (v1.y - v2.y) / (v1.x - v2.x);
		
	}
	
	//returns the slope perpindicular to the given slope
	public float perp(float slope)
	{
		if(slope == 0f || slope == 0)
		{
			Debug.Log("infinite slope from perp function");
			return Mathf.Infinity;//again i don't know what this will do but hopefully it will work
		}
		return -1 / slope;
	}
	
	//finds a point given 2 points and 2 slopes (uses a modifies point slope form)
	public Vector2 findPoint(float x1, float y1, float s1, float x2, float y2, float s2)
	{
		float x3 = (s1 * x1 - s2 * x2 - y1 + y2) / (s1 - s2);//two equations in point slope form solved for y set equal to each other and solved for x
		
		float y3 = s1 * (x3 - x1) + y1; //point slope of y1 set equal to y
		
		return new Vector2(x3, y3);
	}

	/*
	
	//NOTE: target refers to the intersection while build direction refers to an unmodified straight street path
	public void fillMidWithBase()
	{
		//the indx of the base unit that contains the mid street point
		int powIndexX;
		int powIndexY;
		findBaseIndexfromBLpoint(streetPointBL, out powIndexX, out powIndexY);
		
		
		//print (powIndexX + " " + powIndexY);
		
		//set the bl point of the base unit that contains the mid unit bl point to the mid unit bl point
		baseList [powIndexX, powIndexY].streetPointBL = streetPointBL;
		baseList [powIndexX, powIndexY].blSet = true;//bl point can no longer be set
		//baseList [powIndexX, powIndexY].streetPointBL = streetPointBL;//bl point is the same
		//makeMarker(blToWorldUnits(baseList [powIndexX, powIndexY].streetPointBL));
		
		//the bl point of the mid unit above, below, to the right and left of this one
		Vector2 blStreetPointRight = containerUnit.midList [indexI + 1, indexJ].streetPointBL;
		Vector2 blStreetPointLeft = containerUnit.midList [indexI - 1, indexJ].streetPointBL;
		Vector2 blStreetPointUp = containerUnit.midList [indexI, indexJ + 1].streetPointBL;
		Vector2 blStreetPointDown = containerUnit.midList [indexI, indexJ - 1].streetPointBL;
		
		//be sure to never divide by zero
		
	*/
	//finds the slope of the left and right and of the above and below street points and their opposite reciprocals
	/*float slopeLeftRight = findSlope (blStreetPointRight, blStreetPointLeft);
		float slopeLeftRightR = -1/slopeLeftRight;//opposite inverse slope (perpendicular)


		float slopeUpDown =  findSlope (blStreetPointUp, blStreetPointDown);
		float slopeUpDownR = -1/slopeUpDown;*/
		
	/*
		//the directions a street will connect to from the center
		//bool conRight = conRight;
		bool conLeft = containerUnit.midList [indexI - 1, indexJ].conRight;//if the unit to the left connects to the right, then this unit will connect to the left
		//bool conUp = conUp;
		bool conDown = containerUnit.midList [indexI, indexJ - 1].conUp;
		
		//the slope that all streets aim for when they converge in the middle
		float targetSlopeRight = 0;
		float targetSlopeLeft = 0;
		float targetSlopeUp = 0;
		float targetSlopeDown = 0;
		
		//sets all the target slopes based on what sides connect
		if(conUp && conDown && conRight && conLeft)
		{ //form a 4 way perpindicular intersection
			//vector representing the direction from the bottom to top street point
			Vector2 DownUpVec = blStreetPointUp - blStreetPointDown; 
			
			//vector representing the direction from the left to right street point
			Vector2 LeftRightVec = blStreetPointRight - blStreetPointLeft; 
			
			//the vector perpindicular to the the left right vector used to find the target inter line
			Vector2 LeftRightPerp = new Vector2(-LeftRightVec.y, LeftRightVec.x);//opposite reciprocal
			
			//the target vector that the street coming from above will aim for at the intersection
			Vector2 targetVecUpDown = (DownUpVec.normalized + LeftRightPerp.normalized) / 2;
			
			//the slope the roads going from up to down should have at the intersection, average of up down slope and slope perpindicular to left right slope
			//float targetSlopeUpDown = (slopeUpDown + slopeLeftRightR)/2;
			//float targetSlopeLeftRight = (slopeLeftRight + slopeUpDownR)/2;
			float targetSlopeUpDown = targetVecUpDown.y / targetVecUpDown.x;
			float targetSlopeLeftRight = -1 / targetSlopeUpDown;//perpindicular to vertical target slope
			
			targetSlopeRight = targetSlopeLeft = targetSlopeLeftRight;
			targetSlopeUp = targetSlopeDown = targetSlopeUpDown;
			
		} else if(conUp && conDown)//if the top and bottom are connected but all four sides are not
		{
			targetSlopeUp = targetSlopeDown = findSlope(blStreetPointUp, blStreetPointDown);
			
			if(conRight)
				targetSlopeRight = perp(targetSlopeUp);//this street will come in aand connect perpindicular to the up and down street
			else if(conLeft)
				targetSlopeLeft = perp(targetSlopeUp);
		} else if(conRight && conLeft)//if the left and right are connected but all four sides are not
		{
			targetSlopeRight = targetSlopeLeft = findSlope(blStreetPointRight, blStreetPointLeft);
			
			if(conUp)
				targetSlopeUp = perp(targetSlopeRight);//this street will come in aand connect perpindicular to the right and left street(has no influence on the slope)
			else if(conDown)
				targetSlopeDown = perp(targetSlopeRight);
		} else if(conUp && conRight)//if the street connects up and right but nowhere else
		{
			targetSlopeUp = targetSlopeRight = findSlope(blStreetPointUp, blStreetPointRight);
		} else if(conUp && conLeft)
		{
			targetSlopeUp = targetSlopeLeft = findSlope(blStreetPointUp, blStreetPointLeft);
		} else if(conDown && conRight)
		{
			targetSlopeDown = targetSlopeRight = findSlope(blStreetPointDown, blStreetPointRight);
		} else if(conDown && conLeft)//if the street connects down and left but nowhere else
		{
			targetSlopeDown = targetSlopeLeft = findSlope(blStreetPointDown, blStreetPointLeft);
		} 
		else if(conUp)//if it only connects to the top mid unit, make slope between the top bl point and this bl point
		{
			targetSlopeUp = findSlope(blStreetPointUp, streetPointBL);
		}
		else if(conDown)
		{
			targetSlopeDown = findSlope(blStreetPointDown, streetPointBL);
		}
		else if(conRight)
		{
			targetSlopeRight = findSlope(blStreetPointRight, streetPointBL);
		}
		else if(conLeft)
		{
			targetSlopeLeft = findSlope(blStreetPointLeft, streetPointBL);
		}
		
		//Debug.DrawRay(blToWorldUnits(streetPointBL), new Vector3(1f,0f,slopeLeftRight).normalized*5, Color.white, Mathf.Infinity);
		//Debug.DrawRay(blToWorldUnits(streetPointBL), new Vector3(1f,0f,slopeLeftRightR).normalized*5, Color.cyan, Mathf.Infinity);
		//Debug.DrawRay(blToWorldUnits(streetPointBL), new Vector3(1f,0f,slopeUpDown).normalized*5, Color.white, Mathf.Infinity);
		//Debug.DrawRay(blToWorldUnits(streetPointBL), new Vector3(1f,0f,slopeUpDownR).normalized*5, Color.cyan, Mathf.Infinity);
		//Debug.DrawRay(blToWorldUnits(streetPointBL), new Vector3(1f, 0f, targetSlopeUpDown).normalized * 5, Color.green, Mathf.Infinity);
		///Debug.DrawRay(blToWorldUnits(streetPointBL), new Vector3(1f, 0f, targetSlopeLeftRight).normalized * 5, Color.green, Mathf.Infinity);
		
		
		//actually build the streets
		if(conUp)
			buildStreetCurve(blStreetPointUp, targetSlopeUp, Dir.UP, powIndexX, powIndexY);
		
		if(conDown)//if the mid unit below connects up, make a street down
			buildStreetCurve(blStreetPointDown, targetSlopeDown, Dir.DOWN, powIndexX, powIndexY);
		
		if(conRight)
			buildStreetCurve(blStreetPointRight, targetSlopeRight, Dir.RIGHT, powIndexX, powIndexY);
		
		if(conLeft)//if the mid unit below connects up, make a street down
			buildStreetCurve(blStreetPointLeft, targetSlopeLeft, Dir.LEFT, powIndexX, powIndexY);
		//print(buildDir);
		
		
	}
	
	
	
	//builds one of four possible streets in a mid unit (up down left right);
	//outsideStreetPoint is the bl position of the streetpoint that is outside of the mid unit (left, right up down), targetSlope is the slope the road tries to have at the mid street point
	//buildToSide is the side the street will be built and connected to
	//startIndex x and y is the index of the base unit that contains the mid unit bl point and the roads will be build from
	public void buildStreetCurve(Vector2 outsideStreetPoint, float targetSlope, Dir buildToSide, int startIndexX, int startIndexY)
	{
		
		//the direction to build the road, adjacent street point - cur street point normalized, might need to change distance for a higher sample rate
		Vector2 buildDir = (outsideStreetPoint - streetPointBL).normalized * 1f;//1 is the lenght of the build vector(distance between checked street points
		
		
		//goal line slope 
		float gps = findSlope(streetPointBL, outsideStreetPoint);
		
		float gpx, gpy;//the x and y coordinate of the goal point
		
		int gix, giy;//the goal base unit that is touching the edge and the goal point
		
		//this block will find the goal point and goal base unit
		if(buildToSide == Dir.UP)//if a street is being build to connect to the up side
		{
			gpy = (indexJ + 1) * containerUnit.mWidth;//IF FACING UP!!! goal point y value , touches the top of the mid unit
			gpx = (gpy - streetPointBL.y + gps * streetPointBL.x) / gps;//finds x point from y point
			
			findBaseIndexfromBLpoint(new Vector2(gpx, gpy - 0.5f), out gix, out giy);//finds index of base unit right below the goal point, 0.5 gets the point inside the base unit for sure
			
			baseList [gix, giy].conUp = true;//this top unit will connect to the one above it in the topp mid unit
			
			if(!baseList [gix, giy].blSet)
				baseList [gix, giy].streetPointBL = new Vector2(gpx, gpy - 0.0001f);//sets the bl point of this goal base unit in case it is not later set
			
		} else if(buildToSide == Dir.DOWN)
		{
			gpy = (indexJ) * containerUnit.mWidth;//IF FACING DOWN!!! goal point y value , touches the bottom of the mid unit
			gpx = (gpy - streetPointBL.y + gps * streetPointBL.x) / gps;//finds x point from y point
			
			findBaseIndexfromBLpoint(new Vector2(gpx, gpy + 0.5f), out gix, out giy);//finds index of base unit right above the goal point
			
			if(!baseList [gix, giy].blSet)
				baseList [gix, giy].streetPointBL = new Vector2(gpx, gpy + 0.0001f);//sets the bl point of this goal base unit in case it is not later set
			
		} else if(buildToSide == Dir.RIGHT)
		{
			gpx = (indexI + 1) * containerUnit.mWidth; //+1 includes the width of the current unit to find the right goal point
			
			//need to change this probably!!
			gpy = gps * (gpx - streetPointBL.x) + streetPointBL.y;//finds y point from x point using point slope
			
			//y-y1=m(x-x1)
			//y=m(x-x1)+y1
			
			findBaseIndexfromBLpoint( new Vector2(gpx - 0.5f, gpy), out gix, out giy);//finds index of base unit right to the left the goal point
			
			baseList [gix, giy].conRight = true;//this right unit will connect to the one next to it in the right mid unit
			
			if(!baseList [gix, giy].blSet)
				baseList [gix, giy].streetPointBL = new Vector2(gpx - 0.0001f, gpy);//sets the bl point of this goal base unit in case it is not later set
			
		} else if(buildToSide == Dir.LEFT)
		{
			gpx = (indexI) * containerUnit.mWidth; //+1 includes the width of the current unit to find the right goal point
			
			//need to change this probably!!
			gpy = gps * (gpx - streetPointBL.x) + streetPointBL.y;//finds y point from x point using point slope
			
			//y-y1=m(x-x1)
			//y=m(x-x1)+y1
			
			findBaseIndexfromBLpoint(new Vector2(gpx + 0.5f, gpy), out gix, out giy);//finds index of base unit right to the right the goal point
			
			if(!baseList [gix, giy].blSet)
				baseList [gix, giy].streetPointBL = new Vector2(gpx + 0.0001f, gpy);//sets the bl point of this goal base unit in case it is not later set
			
			//makeMarker(blToWorldUnits(baseList[gix, giy].streetPointBL));
		} else //will never happen
		{
			gpx = 0;
			gpy = 0;
			gix = 0;
			giy = 0;
		}
		
		
		//the point on the buildDir line that is on the edge of the mid unit, used to calculate interpolation percents
		Vector2 goalPoint = new Vector2(gpx, gpy);
		
		
		
		//GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		//marker.transform.position = blToWorldUnits(goalPoint);
		
		//total distance between the mid street point and point on the edge(goal point)
		float totalDist = Vector2.Distance(streetPointBL, goalPoint);
		
		//	int lastIndexX = startIndexX;//the x index of the last base unit to have a point added to it
		//	int lastIndexY = startIndexY;
		
		StreetUnitBase lastBaseUnit = baseList [startIndexX, startIndexY];//the base unit used as a refernce for the loop after it
		
		//this loop builds the road out from the center(mid bl point) to the edge(goal point)
		for(int i=1; i<20; i++)
		{
			//a point in the buildDir Direction (x2,y2)
			Vector2 buildDirPoint = streetPointBL + buildDir * i;
			
			//finds the point on the target slope line that forms a perpindicular line with the buildDirPoint so they can be interpolated between
			Vector2 pointOnTarget = findPoint(streetPointBL.x, streetPointBL.y, targetSlope, buildDirPoint.x, buildDirPoint.y, -1 / targetSlope);//targetSlopeUpDown
			
			//distance from the build dir point and goal point on edge (less than total dist)
			float curDist = Vector2.Distance(buildDirPoint, goalPoint);
			
			//the percentage curDist is of total dist used to interpolate between buildDirPoint and pointOnTarget to get the final point
			float percentDist = curDist / totalDist;
			
			//the distance between the buildDirPoint and the pointOnTarget
			Vector2 distTargets = buildDirPoint - pointOnTarget;
			
			//the partial distance that the final point is from buildDirPoint to pointOnTarget
			Vector2 partDistTargets = distTargets * percentDist;
			
			Vector2 finalPoint = buildDirPoint - partDistTargets;//final point is partDist from build dir point to pointOnTarget
			
			int fIndexX, fIndexY;//index of the base unit that contains the final point
			
			findBaseIndexfromBLpoint(finalPoint, out fIndexX, out fIndexY);//finds index of base unit right below the goal point
			
			//if the final point is out of range in the current mid unit, stop the loop
			//also, this means that the goal base unit was never reached, so the last base unit needs to be connected to it
			if(fIndexX >= containerUnit.mWidth || fIndexY >= containerUnit.mWidth || fIndexX < 0 || fIndexY < 0)
			{
				connectBases(baseList [gix, giy], lastBaseUnit);//connect the last used base unit to the goal base unit because they were not connected automaticaly
				break;
			}
			
			StreetUnitBase curBaseUnit = baseList [fIndexX, fIndexY];
			;//the current base unit to modify
			
			
			if(!curBaseUnit.blSet)//if the bl point of the current base unit has not already been set, set it. can change this to use a property in the subase class(probably should)
			{
				
				curBaseUnit.streetPointBL = finalPoint;//make bl point of the base that contains the final point the final point
				curBaseUnit.blSet = true; //can no longer set the street point of this base unit
				
				connectBases(curBaseUnit, lastBaseUnit);//connect the current base unit to the last one
				
			}
			//makeMarker(blToWorldUnits(finalPoint));
			
			//if(fIndexX == gix && fIndexY == giy && buildToSide == Dir.LEFT)
			//if(finalPoint.x<goalPoint.x && buildToSide == Dir.LEFT)
			//	print ("How?");
			
			if(fIndexX == gix && fIndexY == giy)//end the loop if the current point is in the base unit on the edge
				break;
			
			//if(fIndexX>=containerUnit.mWidth || fIndexX<0 || fIndexY>=containerUnit.mWidth || fIndexY<0)//if the final point is out of range in the current mid unit, end the loop
			
			//lastIndexX = fIndexX;//set the new last index for reference in the next loop
			//lastIndexY = fIndexY;
			
			lastBaseUnit = curBaseUnit;//set the new last base unit for reference in the next loop
			
		}
		
		
	}
	
	
	//checks where two bases are in relation to each other and sets their connection variables as necesarry
	//will not connect at all if they are too far apart
	public void connectBases(StreetUnitBase base1, StreetUnitBase base2)//the two units to set connectivity
	{
		if(base1.indexI + 1 == base2.indexI && base1.indexJ == base2.indexJ)//if the second base unit is directly to the right of the first one
		{
			base1.conRight = true;//connect the first base to the right because the second base is on the right
		} else if(base1.indexI - 1 == base2.indexI && base1.indexJ == base2.indexJ)//if the second base unit is directly to the left of the first one
		{
			base2.conRight = true;
		} else if(base1.indexI == base2.indexI && base1.indexJ + 1 == base2.indexJ)//if the second base unit is directly to the top of the first one
		{
			base1.conUp = true;
		} else if(base1.indexI == base2.indexI && base1.indexJ - 1 == base2.indexJ)//if the second base unit is directly to the bottom of the first one
		{
			base2.conUp = true;
		} else if(base1.indexI + 1 == base2.indexI && base1.indexJ + 1 == base2.indexJ)//if the second base unit is to the top right of the first one
		{
			base1.conUpRight = true;
		} else if(base1.indexI - 1 == base2.indexI && base1.indexJ + 1 == base2.indexJ)//if the second base unit is to the top left of the first one
		{
			base1.conUpLeft = true;
		} else if(base1.indexI + 1 == base2.indexI && base1.indexJ - 1 == base2.indexJ)//if the second base unit is to the bottom right of the first one
		{
			base2.conUpLeft = true;
		} else if(base1.indexI - 1 == base2.indexI && base1.indexJ - 1 == base2.indexJ)//if the second base unit is to the bottom left of the first one
		{
			base2.conUpRight = true;
		}
	}
	
	public void findBaseIndexfromBLpoint(Vector2 blpoint, out int indexX, out int indexY)//finds the base unit that a bl point falls in given the current mid unit
	{
		//multiplies the width of the mid unit(in base units) by its index and subtracts it from the x point
		//then rounds down to an integer to find the index
		indexX = Mathf.FloorToInt(blpoint.x - containerUnit.mWidth * indexI);
		indexY = Mathf.FloorToInt(blpoint.y - containerUnit.mWidth * indexJ);
		
		
	}
	//places a sphere marker on a point
	public void makeMarker(Vector3 position)
	{
		GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		marker.transform.position = position;
	}
	
	//finds the slope given two points
	public float findSlope(Vector2 v1, Vector2 v2)
	{
		if(v1.x - v2.x == 0)
		{
			Debug.Log("infinite slope");
			return Mathf.Infinity;//i don't know what this will do but hopefully it will work
		}
		return (v1.y - v2.y) / (v1.x - v2.x);
		
	}
	
	//returns the slope perpindicular to the given slope
	public float perp(float slope)
	{
		if(slope == 0f || slope == 0)
		{
			Debug.Log("infinite slope from perp function");
			return Mathf.Infinity;//again i don't know what this will do but hopefully it will work
		}
		return -1 / slope;
	}
	
	//finds a point given 2 points and 2 slopes (uses a modifies point slope form)
	public Vector2 findPoint(float x1, float y1, float s1, float x2, float y2, float s2)
	{
		float x3 = (s1 * x1 - s2 * x2 - y1 + y2) / (s1 - s2);//two equations in point slope form solved for y set equal to each other and solved for x
		
		float y3 = s1 * (x3 - x1) + y1; //point slope of y1 set equal to y
		
		return new Vector2(x3, y3);
	}*/
}