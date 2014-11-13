﻿using UnityEngine;
using System.Collections.Generic;

/**
 * City generator
 * 
 * TileSet definitions:
 * 0-grass
 * 1-road
 * 2-block (for houses proabably, or parks)
 * 3-townCenter
 * 
 */
public class CityGenerator : MapGenerator {

	private static int WIDTH = 800;//both width and height of the area
	private static float BRANCH_CHANCE = 0.5f;
	private static int BLOCK_RADIUS = 20;
	private static int MAX_DEPTH = 5;

	private Vector2 townCenterPlot; // tile identified by its top left corner
	private List<Vector2> blocks; //tiles (by top left corner) on top of which houses or parks can go
	private List<Vector2> roads; // roads (by top left corner)
	private float minX, minY, maxX, maxY; //extrema for city roads

	public CityGenerator(Area a, TileSet tiles) : base(a,tiles) {
		blocks = new List<Vector2> ();
		roads = new List<Vector2> ();
	}

	/**
	 * Length determines city radius
	 */
	protected override void generateGround (int length)
	{
		//step 1: generate main entrance and exit area roads
		//TODO handle not having exits on all sides
		Vector2 bottomVertical = Vector2.zero, topVertical = Vector2.zero, leftHorizontal = Vector2.zero, rightHorizontal = Vector2.zero;//extrema of main roads in city limits
		int mainTileCount = WIDTH / (int)tileSet.tiles[1].size;
		for (int i = 0; i <= mainTileCount; i++) {
			SpawnTile(WIDTH/2,i*tileSet.tiles[1].size,1);
			SpawnTile(i*tileSet.tiles[1].size,WIDTH/2,1);
			if (mainTileCount/2 + BLOCK_RADIUS == i) {
				topVertical = new Vector2(WIDTH/2,i*tileSet.tiles[1].size);
				rightHorizontal = new Vector2(i*tileSet.tiles[1].size,WIDTH/2);
				maxX = i*tileSet.tiles[1].size;
			} else if (mainTileCount/2 - BLOCK_RADIUS == i) {
				bottomVertical = new Vector2(WIDTH/2,i*tileSet.tiles[1].size);
				leftHorizontal = new Vector2(i*tileSet.tiles[1].size,WIDTH/2);
				minX = i*tileSet.tiles[1].size;
			}
		}

		maxY = maxX;
		minY = minX;
		
		//step 2: generate city radius
		int radius = Random.Range (50, WIDTH - 50);

		//step 3: place town center area
		//town center will always be on one of the four corners of the intersection
		//of the two primary roads or at the end of either road that dead-ends at the center of town
		//TODO handle dead ends (after handling not having exits on all sides
		int corner = Random.Range (1, 4);
		//if (corner == 1) { // top right
			SpawnTile (((float)WIDTH) / 2.0f + ((float)(tileSet.tiles [3].size + tileSet.tiles [1].size)) * .5f, WIDTH / 2 + (tileSet.tiles [3].size + tileSet.tiles [1].size) * .5f, 3);
		/*} else if (corner == 2) {
			SpawnTile (((float)WIDTH) / 2.0f + ((float)(tileSet.tiles [3].size + tileSet.tiles [1].size)) * .5f, WIDTH / 2 + (tileSet.tiles [3].size + tileSet.tiles [1].size) * .5f, 3);		
		}*/

		//step 4: recursive city radius fill step
		//randomly place roads along the main roads in
		//the city radius, with a higher chance of placing
		//the closer to the center of the city you are or when a road was placed on the other side
		//then do this exact same thing on the placed road,
		//but with decreased chances.  Stop when chances are negligible
		//or a potential branch cannot have a length that is larger than a block

		branchFromRoad (leftHorizontal, rightHorizontal, 0);
		branchFromRoad (bottomVertical, topVertical, 0);


		//step 5: place base tiles for buildings
		//basically just go through the list of roads and place blockss where possible
		foreach (Vector2 vec in roads) {
			//try to place block on any side of the road
			Vector2 up = right (vec, Vector2.up);
			Vector2 rVec = right (vec, Vector2.right);
			Vector2 lVec = left (vec, Vector2.right);
			Vector2 down = left (vec, Vector2.up);

			tryPlaceBlock(up, Vector2.right);
			tryPlaceBlock(rVec, Vector2.right);
			tryPlaceBlock(lVec, Vector2.right);
			tryPlaceBlock(down, Vector2.right);

		}

	}

	/**
	 * Generate a road branch off the given vector-defined road path (the vectors mark the top left of a road tile)
	 */
	private void branchFromRoad(Vector2 end1, Vector2 end2, int depth) {
		if (depth > MAX_DEPTH) {
			return;
		} // limit branching

		Vector2 dir = (end2 - end1).normalized;
		int lestBranch = 0;//makes the loop skip branches so that roads don't just branch side by side

		//branch loop
		for (Vector2 branchPoint = end1; (branchPoint - end1).magnitude < (end2 - end1).magnitude; branchPoint += dir * tileSet.tiles[1].size ) {
			if(lestBranch != 0) {
				lestBranch--;
				continue;
			}

			if(Random.value < BRANCH_CHANCE) {

				//branch dirs
				Vector2 d1 = new Vector2(dir.y, -dir.x);
				Vector2 d2 = new Vector2(-dir.y, dir.x);

				//randomize a branch in both dirs
				Vector2 dest1 = findFirstTile(branchPoint + d1 * tileSet.tiles[1].size, d1, Random.Range(0,WIDTH/(int)(2*tileSet.tiles[1].size))); //because with main roads, width/2 is largest possible road
				Vector2 dest2 = findFirstTile(branchPoint + d2 * tileSet.tiles[1].size, d2, Random.Range(0,WIDTH/(int)(2*tileSet.tiles[1].size)));

				//spawn tiles for the branches
				for (Vector2 dVec = branchPoint; (dVec - branchPoint).magnitude < (dest1 - branchPoint).magnitude; dVec += d1 * tileSet.tiles[1].size) {
					placeRoad(dVec);
				}

				for (Vector2 dVec = branchPoint + d2 * tileSet.tiles[1].size; (dVec - branchPoint).magnitude < (dest2 - branchPoint).magnitude; dVec += d2*tileSet.tiles[1].size) {
					placeRoad(dVec);
				}

				if(dest1 != branchPoint + d1 * tileSet.tiles[1].size && dest1 != end2) {
					branchFromRoad(branchPoint, dest1, depth + 1);
				}

				if(dest2 != branchPoint + d2 * tileSet.tiles[1].size && dest2 != end2) {
					branchFromRoad(branchPoint, dest2, depth + 1);
				}

				lestBranch = 2;
			}
		}
	}

	/**
	 * Finds the upper left point of the first tile in the given direction starting at the given point or edge of road bounds
	 * within the given distance. Farthest tile location with the given distance if no tiles are in the way.
	 * 
	 * Note:  This method also may create new branches to facilitate "magic" glueing of branches together as the method
	 * finds the end of this branch
	 */
	private Vector2 findFirstTile(Vector2 start, Vector2 dir, int dist) {
		Vector2 init = start;
		Vector2 pDir = new Vector2 (-dir.y, dir.x);

		//first check to make sure this is even a valid branch
		Vector2 lVec = left (start, pDir);
		Vector2 leftLeft = left (lVec, pDir);
		Vector2 rVec = right (start, pDir);
		Vector2 rightRight = right (rVec, pDir);
		if (TileExists (lVec.x, lVec.y) || TileExists (leftLeft.x, leftLeft.y) || TileExists (rVec.x, rVec.y) || TileExists (rightRight.x, rightRight.y)) {
			return init;
		}

		bool returnFromGlue = false;// if true, then the current loop has glued this road to another road and thus should be returned

		for (int i = 0; i < dist; i++) {
			lVec = left (start, pDir);
			leftLeft = left (lVec, pDir);
			rVec = right (start, pDir);
			rightRight = right (rVec, pDir);
			if (TileExists(start.x, start.y) || TileExists(lVec.x, lVec.y) || TileExists (rVec.x, rVec.y) ||
			    start.y < minY || start.y > maxY || start.x < minX || start.x > maxX) { //okay, we hit a road, so now we need to return our target
				if (i < 2) {//if within 2, don't branch, because it is wierd if we have a road length 1 that was forced to length 1
					return init;
				}

				return start - dir * tileSet.tiles[1].size * 3;
			}

			if(i >= 2) { // don't glue until 2 away from the source road
				if (TileExists(leftLeft.x, leftLeft.y)) { //glue together roads that move close enough together, then return
					for (Vector2 dVec = start; (dVec - start).magnitude < (start - leftLeft).magnitude; dVec -= pDir * tileSet.tiles[1].size) {
						placeRoad(dVec);
					}
					returnFromGlue = true;
				}

				if(TileExists(rightRight.x, rightRight.y)) {
					for (Vector2 dVec = start; (dVec - start).magnitude < (start - rightRight).magnitude; dVec += pDir * tileSet.tiles[1].size) {
						placeRoad(dVec);
					}
					returnFromGlue = true;
				}
			}

			if(returnFromGlue) {
				return start;
			}

			start += dir * tileSet.tiles[1].size;
		}
		return start;
	}

	/**
	 * place a road at the given position
	 */
	private void placeRoad(Vector2 pos) {
		SpawnTile (pos.x, pos.y, 1);
		roads.Add (pos);
	}

	/**
	 * Place a block at the given position facing the given direction
	 * if possible
	 */
	private void tryPlaceBlock(Vector2 pos, Vector2 front) {
		if(!TileExists(pos.x, pos.y)) {
			SpawnTile (pos.x, pos.y, 2);
			blocks.Add (pos);
		}
	}

	/**
	 * Returns the point to the "left" (negative perpendicular)
	 * given a point and its positive perpundicular
	 */
	private Vector2 left(Vector2 point, Vector2 pDir) {
		return point - pDir * tileSet.tiles[1].size;
	}

	/**
	 * Returns the point to the "right" (negative perpendicular)
	 * given a point and its positive perpundicular
	 */
	private Vector2 right(Vector2 point, Vector2 pDir) {
		return point + pDir * tileSet.tiles[1].size;
	}
}