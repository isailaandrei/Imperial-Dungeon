﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/* 
 * All credits for this algorithm are given to 
 * http://www.gamasutra.com/blogs/AAdonaac/20150903/252889/Procedural_Dungeon_Generation_Algorithm.php
*/
public class DungeonGenerator : MonoBehaviour {

	public Vector2 ellipseSize;
	public int maxRoomWidth;
	public int maxRoomHeight;
	public int minRoomWidth;
	public int minRoomHeight;
	public int numberOfInitialGeneratedRooms;
	public GameObject roomPrefab;
	public GameObject linePrefab;
	public Text progressText;
	public Camera mainCamera;
	public GameObject loadingScreen;

	private List<Room> rooms = new List<Room> ();
	private List<Room> mainRooms = new List<Room> ();
	private bool withAnimation;

	public void BeginGeneration (bool withAnimation) {
		this.withAnimation = withAnimation;
		loadingScreen.SetActive (!withAnimation);
		GameObject.FindGameObjectWithTag ("Canvas").SetActive (!withAnimation);
		StartCoroutine (GenerateInitialRooms ());
	}

	private IEnumerator GenerateInitialRooms () {
		progressText.text = "Random point sampling in ellipse";
		// startup time
		if (withAnimation) {
			yield return new WaitForSeconds (0.5f);
		}
		for (int i = 0; i < numberOfInitialGeneratedRooms; i++) {
			rooms.Add (GenerateRoom ());
			if (withAnimation) {
				yield return new WaitForSeconds (0.0005f);
			}
		}

		// wait for collision to finnish
		// important not to override animation
		yield return new WaitForSeconds (5f);

		foreach (var room in rooms) {
			room.RemovePhys ();
		}

		progressText.text = "Rounding position to nearest int";

		foreach (var room in rooms) {
			room.RoundPositionToNearestInt ();
			if (withAnimation) {
				yield return new WaitForSeconds (0.002f);
			}
		}

		StartCoroutine (SelectMainRooms ());
	}

	private IEnumerator SelectMainRooms () {
		progressText.text = "Selecting main rooms";
		Vector2 averageSize = new Vector2 (0f, 0f);

		foreach (var room in rooms) {
			averageSize.x += room.GetRect ().size.x;
			averageSize.y += room.GetRect ().size.y;
		}

		averageSize.x /= rooms.Count;
		averageSize.y /= rooms.Count;

		foreach (var room in rooms) {
			if (room.GetRect ().size.x > averageSize.x * 1.10f && room.GetRect ().size.y > averageSize.y * 1.10f) {
				mainRooms.Add (room);
			}
		}

		foreach (var room in mainRooms) {
			room.SetColor (Color.red);
			if (withAnimation) {
				yield return new WaitForSeconds (0.02f);
			}
		}

		if (withAnimation) {
			yield return new WaitForSeconds (1f);
		}
		
		StartCoroutine (DoDelaunayTriangulation (mainRooms));
	}

	private IEnumerator DoDelaunayTriangulation (List<Room> rooms) {
		progressText.text = "Applying Delaunay Triangulation";

		foreach (var room in mainRooms) {
			room.SetNode (true);
			if (withAnimation) {
				yield return new WaitForSeconds (0.02f);
			}
		}

		List<Vertex> nodes = new List<Vertex> ();

		foreach (var room in mainRooms) {
			nodes.Add (new Vertex (room.GetPosition ()));
		}

		DelauneyTriangulation dt = new DelauneyTriangulation (nodes);
		Graph g = dt.Apply ();

		List<GameObject> lines = new List<GameObject> ();
		g.ForEachEdge ((e) => {
			GameObject line = CreateLine (e.p1.point, e.p2.point);
			lines.Add (line);
			line.SetActive (false);
		});

		foreach (var line in lines) {
			line.SetActive (true);
			if (withAnimation) {
				yield return new WaitForSeconds (0.02f);
			}
		}

		if (withAnimation) {
			yield return new WaitForSeconds (1.5f);
		}

		foreach (var line in lines) {
			DestroyImmediate (line);
		}

		StartCoroutine (DoMST (mainRooms, g));
	}

	private IEnumerator DoMST (List<Room> rooms, Graph graph) {
		progressText.text = "Applying Minimum Spanning Tree";
		var mst = graph.ApplyMST ();

		List<GameObject> lines = new List<GameObject> ();
		mst.ForEachEdge ((e) => {
			GameObject line = CreateLine (e.p1.point, e.p2.point);
			lines.Add (line);
			line.SetActive (false);
		});

		foreach (var line in lines) {
			line.SetActive (true);
			if (withAnimation) {
				yield return new WaitForSeconds (0.02f);
			}
		}

		if (withAnimation) {
			yield return new WaitForSeconds (1.5f);
		}

		foreach (var line in lines) {
			DestroyImmediate (line);
		}

		progressText.text = "Adding more edges";

		int nrOfExtraEdges =  (int) (graph.GetNrOfEdges () * 0.15f);

		for (int i = 0; i < nrOfExtraEdges; i++) {
			mst.AddEdge (graph.GetRandomEdge ());
		}

		lines.Clear ();

		mst.ForEachEdge ((e) => {
			GameObject line = CreateLine (e.p1.point, e.p2.point);
			lines.Add (line);
			line.SetActive (false);
		});

		foreach (var line in lines) {
			line.SetActive (true);
			if (withAnimation) {
				yield return new WaitForSeconds (0.02f);
			}
		}

		if (withAnimation) {
			yield return new WaitForSeconds (1.5f);
		}

		foreach (var line in lines) {
			DestroyImmediate (line);
		}

		StartCoroutine (CreateHallways (mst));
	}

	private IEnumerator CreateHallways (Graph graph) {
		progressText.text = "Creating hallways";

		foreach (var room in rooms) {
			room.SetNode (false);
			room.gameObject.SetActive (false);
		}

		Dictionary<Vector2, Room> map = new Dictionary<Vector2, Room> ();

		foreach (var room in mainRooms) {
			room.gameObject.SetActive (true);
			room.SetColor (Color.green);
			map.Add (room.GetPosition (), room);
		}

		List<GameObject> hallways = new List<GameObject> ();

		graph.ForEachEdge ((Edge e) => {
			GameObject hall = map[e.p1.point].CreateHallway (map[e.p2.point]);
			if (hall == null) {
				return;
			}
			hallways.Add (hall);
			hall.SetActive (false);
		});

		foreach (var hall in hallways) {
			hall.SetActive (true);
			if (withAnimation) {
				yield return new WaitForSeconds (0.01f);
			}
		}

		if (withAnimation) {
			yield return new WaitForSeconds (1.5f);
		}

		Vector3 spawnPoint;

		int random = Random.Range (0, mainRooms.Count - 1);

		spawnPoint = mainRooms [random].GetPosition ();
		mainCamera.gameObject.SetActive (false);
		progressText.gameObject.SetActive (false);

		foreach (var room in mainRooms) {
			room.SpawnObjects ();
		}
			
		GameObject.FindGameObjectWithTag ("EndlessScript").GetComponent <EndlessController> ().OnFinishedDungeon (spawnPoint);
	}

	public GameObject CreateLine (Vector3 position1, Vector3 position2) {
		GameObject line = Instantiate (linePrefab, Vector3.zero, Quaternion.identity);
		line.GetComponent<LineRenderer> ().SetPosition (0, position1);
		line.GetComponent<LineRenderer> ().SetPosition (1, position2);
		line.GetComponent<LineRenderer> ().sortingOrder = 10;
		line.GetComponent<LineRenderer> ().startWidth = 0.10f;
		line.GetComponent<LineRenderer> ().endWidth = 0.10f;
		line.transform.SetParent (transform, false);
		return line;
	}

	public Vector2 GetRandomPointInEllipse () {
		float t = 2 * Mathf.PI * Random.value;
		float u = Random.value + Random.value;
		float r = u > 1 ? 2 - u : u;

		return new Vector2 (ellipseSize.x * r * Mathf.Cos (t) / 2, ellipseSize.y * r * Mathf.Sin (t) / 2);
	}

	public Room GenerateRoom () {
		Vector2 position = GetRandomPointInEllipse ();
		GameObject room = Instantiate (roomPrefab, Vector3.zero, Quaternion.identity);
		room.GetComponent <Room> ().Init (position, Random.Range(minRoomWidth, maxRoomWidth), Random.Range (minRoomHeight, maxRoomHeight), transform);
		return room.GetComponent <Room> ();
	}
}
