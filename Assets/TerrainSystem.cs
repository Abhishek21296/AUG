﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Creates the terrain of planets using voxels and marching cubes. instanced by the planet class
//NOTE: a chunk is the same thing as a terrainobject

//DEPRECATED, REPLACED BY LODSYSTEM
public class TerrainSystem
{
	private Planet planet;//a reference to its planet

	//noise scale and height, these are tests and will be replaces with multiple scales and heights
	float scale = 20f;
	float height = 5f;
	float radius;//the planet radius
	int chunkSize = TerrainObject.chunkSize;//voxels per chunk side

	//the list of the terrain chunks thaat have been loaded and their cooresponding positions
	//maybe not make static? so each planet can retain its own list of terrain chunks? 
	public static Dictionary<WorldPos, TerrainObject> chunks = new Dictionary<WorldPos, TerrainObject>();


	public TerrainSystem(Planet p, float r)
	{
		radius = r;
		planet = p;
	}

	//creates and instantiates a terrain chunk (but does not render it yet)
	//NOTE: instantiating a prefab might be faster but i will use this for now
	public void CreateChunk(WorldPos pos) 
	{
		//build the terrainobject and add its gameobject to the chunks list(may remove this last thing later)
		TerrainObject chunk = Build.buildObject<TerrainObject>(pos.toVector3(), Quaternion.identity);
		chunks.Add(pos, chunk);

		//loops through every voxel in the chunk (make own funtion later)
		for (int x = 0; x<=chunkSize; x++) 
		{
			for (int y = 0; y<=chunkSize; y++) 
			{
				for (int z = 0; z<=chunkSize; z++) 
				{

					//the world position of the current voxel
					Vector3 voxPos = new Vector3(pos.x+x, pos.y+y, pos.z+z);//position of chunk+position of voxel within chunk

					/*
					//finds the distance from the center of the planet to the current voxel using 3d distance formula
					float distxyS = posX*posX+posY*posY; //first part of distace formula
					float distxyz = Mathf.Sqrt(distxyS + posZ*posZ); //the distance from the center of the world to the current voxel
					*/

					float distxyz = Vector3.Distance(Vector3.zero, voxPos);


					//need this surface variable latr to prevent floating terrain
					//Vector3 surface = new Vector3(x,y,z).normalized * planetRadius; //gives the point on face of unaltered planet sphere, directly below current voxel


					//NOISE!!!!!!!!!!!!!!!!!!!!!

					//float noise = 0.0f;//Noise.GetNoise(voxPos.x/scale,voxPos.y/scale,voxPos.z/scale)*height;
					float altitude = planet.noise.getAltitude(voxPos);

					//float mts = (Noise.GetNoise(posX/50, surface.y/mtnScale, surface.z/mtnScale)-(1f-mtnFrequency)) * mtnHeight; //moountain noise 1.0f decrases frequency
					/*float mts = (Noise.GetNoise(posX/50.0f, posY/50.0f, posZ/50.0f)-0.5f) * 20;
					if(mts<1f)//negatives are bad for pow, factions are bad because they result in very low numbers
						mts = 0f;
					else
					{
						mts = Mathf.Pow(mts, 5f);
						//fine = Noise.Noise.GetNoise(x/mtnfineScale, y/mtnfineScale, z/mtnfineScale) * mtnfineHeight;
						//gen = 0;
					}*/

					chunk.voxVals[x,y,z] = distxyz/altitude;//Noise.GetNoise((x+pos.x)/scale,(y+pos.y)/scale,(z+pos.z)/scale);

					//puts a hole in the planet(just for fun
					//if(voxPos.x<10 && voxPos.x>-10 && voxPos.z<10 && voxPos.z>-10)
						//chunk.voxVals[x,y,z] = 2;
						
					
				}

			}

		}

		//TerrainLoader.addToRender(chunk);
		//Loader.addToRender(chunk);
		//chunk.Render();//renders the chunk (be sure to remove later)

	}

	//destroys a specified chunk and removes it from the dictionary NOTE: may also have to remove it from the render list
	public void DestroyChunk(WorldPos pos)
	{
		TerrainObject chunk;
		if (chunks.TryGetValue (pos, out chunk)) //will probalby always be true
		{
			//destroy the terrain object
			Build.destroyObject(chunk);
			//Object.Destroy(chunk.gameObject);//destroy the gameobject, unity says not to do this, oh well, i do what i want
			chunks.Remove(pos);//remove the reference from the dictionary
		}

	
	}

	/*public static TerrainObject getChunk(Vector3 pos)
	{
		return chunks.TryGetValue
	}*/


}
