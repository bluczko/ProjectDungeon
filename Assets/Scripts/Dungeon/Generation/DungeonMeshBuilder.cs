﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Builds textured meshes for dungeons.</summary>
public class DungeonMeshBuilder {
	#region Quad Vertices
	private static readonly Vector3[][] QuadVertices = {
		new[] { // North
			new Vector3(-0.5f, -0.5f, 0.5f),
			new Vector3(-0.5f, 1f, 0.5f),
			new Vector3(0.5f, 1f, 0.5f),
			new Vector3(0.5f, -0.5f, 0.5f)
		},
		new[] { // East
			new Vector3(0.5f, -0.5f, 0.5f),
			new Vector3(0.5f, 1f, 0.5f),
			new Vector3(0.5f, 1f, -0.5f),
			new Vector3(0.5f, -0.5f, -0.5f)
		},
		new[] { // South
			new Vector3(0.5f, -0.5f, -0.5f),
			new Vector3(0.5f, 1f, -0.5f),
			new Vector3(-0.5f, 1f, -0.5f),
			new Vector3(-0.5f, -0.5f, -0.5f)
		},
		new[] { // West
			new Vector3(-0.5f, -0.5f, -0.5f),
			new Vector3(-0.5f, 1f, -0.5f),
			new Vector3(-0.5f, 1f, 0.5f),
			new Vector3(-0.5f, -0.5f, 0.5f)
		},
		new[] { // Floor
			new Vector3(-0.5f, -0.5f, -0.5f),
			new Vector3(-0.5f, -0.5f, 0.5f),
			new Vector3(0.5f, -0.5f, 0.5f),
			new Vector3(0.5f, -0.5f, -0.5f)
		},
		new[] { // Ceiling
			new Vector3(-0.5f, 1f, -0.5f),
			new Vector3(0.5f, 1f, -0.5f),
			new Vector3(0.5f, 1f, 0.5f),
			new Vector3(-0.5f, 1f, 0.5f)
		}
	};

	#endregion

	private const int ChunkSize = 8;

	private static Dungeon dungeon => Dungeon.Instance;
	private static Material wallMaterial;
	private static Transform staticMesh;

	private static Mesh CreateWallMesh(int wall, int x, int z) {
		var m = new Mesh();

		var w = QuadVertices[wall];

		m.SetVertices(new List<Vector3> {
			new Vector3(w[0].x + x, w[0].y, w[0].z + z),
			new Vector3(w[1].x + x, w[1].y, w[1].z + z),
			new Vector3(w[2].x + x, w[2].y, w[2].z + z),
			new Vector3(w[3].x + x, w[3].y, w[3].z + z)
		});

		m.SetUVs(0, new List<Vector2> {
			new Vector2(0, 0),
			new Vector2(0, 1),
			new Vector2(1, 1),
			new Vector2(1, 0)
		});

		m.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);

		return m;
	}

	private static void AddBlockMesh(IDictionary<int, List<Mesh>> meshGroup, Position pos) {
		var block = dungeon.getBlock(pos);
		if(block != null) {
			var textures = block.textures;

			for(var wall = 0; wall < textures.Length; wall++) {
				var texture = textures[wall];

				if(texture >= 0) {
					if(texture == 0) {
						if(wall >= 0 & wall < 4) {
							texture = dungeon.textures.defaultWall;
						} else if(wall == 4) {
							texture = dungeon.textures.defaultFloor;
						} else if(wall == 5) {
							texture = dungeon.textures.defaultCeiling;
						}
					}

					if(!meshGroup.ContainsKey(texture)) {
						meshGroup.Add(texture, new List<Mesh>());
					}

					var wallMesh = CreateWallMesh(wall, pos.x, pos.y);

					meshGroup[texture].Add(wallMesh);
				}
			}
		}
	}

	public static List<GameObject> BuildChunk(Position chPos) {
		var meshGroup = new Dictionary<int, List<Mesh>>();

		var p0 = new Position(chPos.x * ChunkSize, chPos.y * ChunkSize);
		var p1 = p0.copy().add(ChunkSize, ChunkSize);

		p1 = new Position(Mathf.Min(p1.x, dungeon.Width), Mathf.Min(p1.y, dungeon.Height));

		var p = p0.copy();

		for(p.x = p0.x; p.x < p1.x; p.x++) {
			for(p.y = p0.y; p.y < p1.y; p.y++) {
				AddBlockMesh(meshGroup, p);
			}
		}

		var meshes = new List<GameObject>();

		foreach(var texture in meshGroup.Keys) {
			var group = meshGroup[texture];
			var mgName = $"_meshGroup_{chPos.x}_{chPos.y}_{texture}";
			var meshGroupObject = GameObject.Find(mgName) ?? new GameObject(mgName);

			meshGroupObject.transform.parent = staticMesh.transform;

			var mf = meshGroupObject.AddComponent<MeshFilter>();
			var combines = new CombineInstance[group.Count];

			for(var i = 0; i < combines.Length; i++) {
				combines[i].mesh = group[i];
			}

			var shared = new Mesh();
			shared.CombineMeshes(combines, true, false);
			shared.RecalculateNormals();

			mf.sharedMesh = shared;

			if(wallMaterial == null) {
				wallMaterial = Resources.Load<Material>("Materials/WallEnv");
			}

			var mr = meshGroupObject.AddComponent<MeshRenderer>();
			mr.material = wallMaterial;
			mr.material.mainTexture = Resources.Load<Texture>($"Textures/Environment/{texture}");

			mr.shadowCastingMode = ShadowCastingMode.Off;
			mr.receiveShadows = false;
		}

		return meshes;
	}

	public static void BuildMesh() {
		var staticMeshObject = dungeon.gameObject;
		staticMesh = staticMeshObject.transform.Find("_staticMesh");

		if(staticMesh == null) {
			staticMesh = new GameObject("_staticMesh").transform;
			staticMesh.parent = dungeon.transform;
		}

		var chunkNum = new Position(
			Mathf.CeilToInt((float) dungeon.Width / ChunkSize),
			Mathf.CeilToInt((float) dungeon.Height / ChunkSize)
		);

		var meshes = new List<GameObject>();

		for(var x = 0; x < chunkNum.x; x++) {
			for(var y = 0; y < chunkNum.y; y++) {
				meshes.AddRange(BuildChunk(new Position(x, y)));
			}
		}

		StaticBatchingUtility.Combine(meshes.ToArray(), staticMesh.gameObject);
	}
}
