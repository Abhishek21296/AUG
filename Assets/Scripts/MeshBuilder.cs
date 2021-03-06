﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class MeshBuilder 
{
	//the vertices of the mesh
	private List<Vector3> verts = new List<Vector3>();
	public List<Vector3> Verts{ get { return verts;} }

	//the coordinates for uv mapping
	private List<Vector2> uvs = new List<Vector2>();
	public List<Vector2> UVs{ get { return uvs;} }

	//the triangle indexes
	private List<int> triIndexes = new List<int>();
	public List<int> TriIndexes{ get { return triIndexes;} }


	//adds a vertex and sets up its cooresponding uv point
	public void addVertex(Vector3 pos, Sub sub)
	{
		verts.Add(pos);
		uvs.Add(Substance.subs[sub].colorPoint);
	}

	//adds a triangle to triIndexes
	public void addTriangle(int index0, int index1, int index2)
	{
		triIndexes.Add(index0);
		triIndexes.Add(index1);
		triIndexes.Add(index2);
	}



	public Mesh getMesh()
	{
		Mesh mesh = new Mesh();

		mesh.vertices = verts.ToArray();
		mesh.uv = uvs.ToArray();
		mesh.triangles = triIndexes.ToArray();

		mesh.RecalculateBounds();
		mesh.RecalculateNormals();

		return mesh;
	}


	//adds another meshbuilder's data to this one
	public void addMesh(MeshBuilder mb, Vector3 pos, Quaternion rot)
	{

		int numVerts = verts.Count;

		//add the verts
		foreach(Vector3 vert in mb.verts)
		{
			
			//rotate and traslate the vert
			Vector3 newVert = rot * vert;
			newVert += pos;
			verts.Add(newVert);
		}

		//add the uvs
		uvs.AddRange(mb.uvs);

		foreach(int tri in mb.triIndexes)
		{
			int newTri = tri + numVerts;
			triIndexes.Add(newTri);
		}
	}
}
