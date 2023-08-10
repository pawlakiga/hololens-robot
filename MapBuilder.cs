/*

University of Trento, 2023 

Robotic Perception and Action project work 
Virtual Robot Interaction in AR

Team:       Guglielmo de Col, Simone Ciresa, Valeria Grotto, Iga Pawlak

Content:    A class to perform building a room map in 2D based on the scan form HoloLens.  
            The map has elements of 0 and 1 where 0 signifies the floor 
            and 1 an obstacle. 
            The size of the map is defined and then the room is divided into a respective number 
            of cells in each dimension. Working on the level of vertices, 1 is assigned to a 
            given cell if there is at least one vertex above the floor (with a given threshold) 
            in the location belonging to a corresponding cell.
            The map building algorithm also involves denoising of the map and expanding the obstacles by a given value of 10cm. 

*/
using System; 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using System.Linq;

public class MapBuilder
{
    public int[] mapSize; 
    private float[] roomExtents= {0f,0f,0f,0f}; 
    private int counter = 0; 
    private float xCellSize, zCellSize, floorLevel, ceilingLevel, neighbourMajority;  
    public float floorLevelThreshold;
    int [,] lastProcessedMap;
    int blockSize, coreSize;

    public MapBuilder(  int xMapSize = 50,
                        int zMapSize = 50,
                        float floorLevelThreshold = 0.2f, 
                        int blockSize = 4,
                        int coreSize = 2, 
                        float neighbourMajority = 0.8f
                      )
    {
        /*
        Public constructor of the class with arguments: 
        -   xMapSize and zMapSize   -   defining the map size in respecitve X and Y dimensions
        -   floorLevelThreshold     -   the threshold above which a found vertex is considered above the flooe
        -   blockSize               -   block size used in denoising of the map 
        -   coreSize                -   the size of a block's core used in denoising 
        -   neighbourMajority       -   
        */
        
        this.mapSize = new int[]{xMapSize,zMapSize};
        this.floorLevelThreshold = floorLevelThreshold;
        this.lastProcessedMap = null;
        this.blockSize = blockSize;
        this.coreSize = coreSize;
        this.neighbourMajority = neighbourMajority;

    }

    public int[,] BuildRoomMap(IReadOnlyDictionary<int,SpatialAwarenessMeshObject> meshes){
        /*
        A function to build the room map taking as argument the meshes collected during a room scan using HoloLens.
        */

        
        FindRoomBounds(meshes); 

        int[,] processedMap = new int[mapSize[0], mapSize[1]]; 
        // initialise map with zeros
        for (int x=0; x < mapSize[0]; x++){
            for (int z = 0; z < mapSize[1]; z++){
                processedMap[x,z] = 0; 
            }
        }
        // set cell sizes based on room extents and map size
            xCellSize = ((roomExtents[1]-roomExtents[0])/mapSize[0]);
            zCellSize = ((roomExtents[3]-roomExtents[2])/mapSize[1]); 

        // counter for all the points above the floor
            int pointCount; 

        // for loop to set the values of the map elements
            foreach (SpatialAwarenessMeshObject m in meshes.Values){
                pointCount = 0; 
                foreach( Vector3 meshVertex in m.Filter.mesh.vertices) {
                    Vector3 vertexWorldPosition = m.GameObject.transform.TransformPoint(meshVertex); 
                    // if a point is above the floor and below the ceiling 
                    if (vertexWorldPosition.y > floorLevel + floorLevelThreshold && vertexWorldPosition.y < ceilingLevel - floorLevelThreshold){
                        pointCount++; 
                        int[] cellIndex = FindCell(vertexWorldPosition); 
                        processedMap[cellIndex[0], cellIndex[1]] = 1;
                    } 
                }
            }
        lastProcessedMap = processedMap;
        // denoise the map
        MapCleanupBlocks();
        // expand the obstacles
        ExpandObstacles(0.1f);
        return processedMap; 
        

    }
    public int[] FindCell(Vector3 coordinates){
        /*
        A function to find the cell's indexes for X and Z given the world coordinates 
        */
        
        int[] cellIndex = new int[2]; 
        Vector3 roomRefCoords = WorldToRoomPosition(coordinates); 
        cellIndex[0] =  (int) Math.Ceiling(roomRefCoords.x/xCellSize)-1 ; 
        cellIndex[1] =  (int) Math.Ceiling(roomRefCoords.z/zCellSize)-1  ; 
        if (cellIndex[0]==-1){
            cellIndex[0] = 0; 
        }
        if (cellIndex[1]==-1){
            cellIndex[1] = 0; 
        }
        cellIndex[1] = (int)mapSize[1] - 1 - cellIndex[1]; 
        return cellIndex; 
    
    }

    public Vector3 FindWorldPosition(int xCellIndex, int zCellIndex){
        /*
        A fucntion to get the world position given cell indexes. Returns the 
        location of the center of the cell. 
        */

        float y;
        if (lastProcessedMap[xCellIndex,zCellIndex]== 0){ 
            y = floorLevel + 0.1f;
        }
        else{
            y = floorLevel + 0.8f;
        }
         
        return new Vector3(roomExtents[0] + xCellIndex * xCellSize + xCellSize/2,
                             y,
                             roomExtents[3] - zCellIndex*zCellSize- zCellSize/2);
    }

    public Vector3 WorldToRoomPosition(Vector3 coordinates){
        /*
        Fucntion to transform the world coordinates to room coordinates
        */
        return new Vector3(coordinates.x - roomExtents[0], coordinates.y, coordinates.z - roomExtents[2]); 
    }

    private void FindRoomBounds(IReadOnlyDictionary<int,SpatialAwarenessMeshObject> meshes){
        /*
        A function to find the room bounds based on the meshes from the spatial observer. 
        Sets the values to the local fields of the class: 
        -   roomExtents -   the minimum and maximum X and Z coordinates of mesh vertices 
                            that serve as the extents of the room - the map is built as 
                            a rectangle between those bounds
        -   minLevel    -   the center of the lowest mesh in the scan, used later as the base
                            for the floor level
        -   maxLevel    -   the center of the highest mesh in the scan, used later as the base
                            for the ceiling level
        */
        float minLevel = 0f, maxLevel = 0f;  

        foreach (SpatialAwarenessMeshObject m in meshes.Values){
                if (m.Filter.mesh.bounds.center.y < minLevel){
                    minLevel = m.Filter.mesh.bounds.center.y; 
                }
                if (m.Filter.mesh.bounds.center.y > maxLevel){
                    maxLevel = m.Filter.mesh.bounds.center.y; 
                }
                var mBounds = m.Filter.mesh.bounds; 
                if (mBounds.min.x < roomExtents[0]) roomExtents[0] = mBounds.min.x; 
                if (mBounds.min.z < roomExtents[2]) roomExtents[2] = mBounds.min.z; 
                if (mBounds.max.x > roomExtents[1]) roomExtents[1] = mBounds.max.x; 
                if (mBounds.max.z > roomExtents[3]) roomExtents[3] = mBounds.max.z; 
                }
            
        floorLevel = minLevel; ceilingLevel = maxLevel; 
        }

    public String PrintMap(){ 
        if (lastProcessedMap is null){
            return "";
        }
        String s= ""; 
        for (int xCellIndex=0; xCellIndex < mapSize[0]; xCellIndex++){
                for (int zCellIndex = 0; zCellIndex < mapSize[1]; zCellIndex++){
                    s += lastProcessedMap[xCellIndex,zCellIndex].ToString(); 
                }
                s+= "\n"; 
            }
        // debugText+= "Printing map\n"; 
        return s;  
        
    }

    public void Reset(){
        /*
        A function to reset the map builder. sets all the values to default, zeroes the counter and 
        nullifies the lastProcessedMap field. 
        */

        lastProcessedMap = null; 
        floorLevelThreshold = 0.2f; 
        roomExtents.SetValue(0f,0); roomExtents.SetValue(0f,1); roomExtents.SetValue(0f,2); roomExtents.SetValue(0f,3);
        mapText = "" ; 
        counter = 0; 
        mapSize = new int[]{50,50}; 

    }
        
    public int[,] GetMap(){
        return lastProcessedMap;
    }
    public void SetMapSize(int xSize, int zSize){
        this.mapSize[0] = xSize;
        this.mapSize[1] = zSize; 
    }

    public void SetFloorLevelThreshold(float threshold){
        this.floorLevelThreshold = threshold;
    }

    private List<int[]> FindNeighbours(int [] cell, int xDistance, int zDistance){
        List<int[]> neighbourList = new List<int[]>();
        
        for (int x = Math.Max(0,cell[0]-xDistance); x <= Math.Min(lastProcessedMap.GetUpperBound(0), cell[0] + xDistance); x++){
            for (int z = Math.Max(0,cell[1]-zDistance); z <= Math.Min(lastProcessedMap.GetUpperBound(1), cell[1] + zDistance); z++){
                
                if (x == cell[0] && z == cell[1]) continue;
                neighbourList.Add(new int[] {x,z}); 
            }
        }
        return neighbourList;
    }
    public void ExpandObstacles(float safetyDistance){
        if (lastProcessedMap is null) return;
        int xDistance = (int) Math.Ceiling(safetyDistance/xCellSize), zDistance = (int) Math.Ceiling(safetyDistance/zCellSize);
        List<int[]> cellsToChange = new List<int[]>();
        List<int[]> xNeighbours, zNeighbours; 
        // if there's a point and the ones around it have a different value 
        // debugTextMesh.text += String.Format("Entering loop");
        for (int xCellIndex=0; xCellIndex < mapSize[0]; xCellIndex++){
            for (int zCellIndex = 0; zCellIndex < mapSize[1]; zCellIndex ++){
                if (lastProcessedMap[xCellIndex,zCellIndex] == 1){
                    xNeighbours = FindNeighbours(new int[] {xCellIndex,zCellIndex}, xDistance,0); 
                    zNeighbours = FindNeighbours(new int[] {xCellIndex,zCellIndex}, 0, zDistance);
                    foreach (int[] xNeighbour in xNeighbours){
                        if (lastProcessedMap[xNeighbour[0], xNeighbour[1]]==0){
                            cellsToChange.Add(xNeighbour);
                        } 
                    }
                    foreach (int[] zNeighbour in zNeighbours){
                        if (lastProcessedMap[zNeighbour[0], zNeighbour[1]]==0){
                            cellsToChange.Add(zNeighbour);
                        } 
                    }


                }  
            }
        }
        foreach (int[] cell in cellsToChange){
            lastProcessedMap[cell[0],cell[1]] = 1 ;
        }
    }

    public float GetCellSize(String dimension){
        if (dimension == "x") return xCellSize; 
        if (dimension == "z") return zCellSize; 
        else return 0.0f;
    }

    private void MapCleanupBlocks(){
        if (lastProcessedMap is null) return;
        List<int[]> cellsToChange = new List<int[]>();
        int blockSum = 0;

        for (int ix = 0; ix < mapSize[0]; ix += (int) blockSize/2){
            for(int iz = 0; iz < mapSize[1]; iz += (int) blockSize/2){
                    // check the majority in the block
                    blockSum = BlockEdgeSum(ix,iz,blockSize, coreSize); 
                    if (blockSum > neighbourMajority * blockSize * blockSize){
                        ChangeCoreValues(ix, iz, blockSize, coreSize, 1);
                    }
                    if (blockSum < (1 - neighbourMajority) * blockSize * blockSize){
                        ChangeCoreValues(ix,iz,blockSize,coreSize,0);
                    }

            }
        } 
    }

    private int BlockSum(int ixStart, int izStart, int blockSize, int coreSize = 0){

        int sum = 0;
        for (int x = ixStart; x <= Math.Min(lastProcessedMap.GetUpperBound(0), ixStart + blockSize); x++){
            for (int z = izStart; z <= Math.Min(lastProcessedMap.GetUpperBound(1), izStart + blockSize); z++){
                sum += lastProcessedMap[x,z]; 
            }
        }
        return sum;
    }

    private int BlockEdgeSum(int ixStart, int izStart, int blockSize, int coreSize){

        int sum = 0;
        for (int x = ixStart; x <= Math.Min(lastProcessedMap.GetUpperBound(0), ixStart + blockSize); x++){
            for (int z = izStart; z <= Math.Min(lastProcessedMap.GetUpperBound(1), izStart + blockSize); z++){
                if (x >= ixStart + (blockSize - coreSize)/2 &&
                    x<=ixStart + blockSize - (blockSize - coreSize)/2 &&
                    z >= izStart + (blockSize - coreSize)/2 && 
                    z<=izStart + blockSize - (blockSize - coreSize)/2) {
                        continue;
                    }
                sum += lastProcessedMap[x,z]; 
            }
        }
        return sum;
    }

    private void ChangeCoreValues(int ixStart, int izStart, int blockSize, int coreSize, int newValue){
        for (int x = ixStart + (blockSize - coreSize)/2; x <= Math.Min(lastProcessedMap.GetUpperBound(0), ixStart + blockSize - (blockSize - coreSize)/2); x++){
            for (int z = izStart + (blockSize - coreSize)/2; z <= Math.Min(lastProcessedMap.GetUpperBound(1), izStart + blockSize - (blockSize - coreSize)/2); z++){
                lastProcessedMap[x,z] = newValue; 
            }
        }
    }

      public float[,] PositionPath(int[,] path){

        float [,] positionPath = new  float [2, path.GetUpperBound(1) + 1]; 
        for (int cellIndex = 0; cellIndex <= path.GetUpperBound(1); cellIndex++){
            positionPath[0,cellIndex] = FindWorldPosition(path[0,cellIndex],path[1,cellIndex]).x;
            positionPath[1,cellIndex] = FindWorldPosition(path[0,cellIndex],path[1,cellIndex]).z;
        }

        return positionPath;
    }

    public bool IsOnFloor(Vector3 position){

        return position.y <= floorLevel + floorLevelThreshold*2;

    }
  



}
