using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarGrid {

    private int width;
    private int height;
    private float cellSize;

    //to show or hide the debug print
    private bool DEBUG = false;

    private GridCell[,] gridArray;

    private TextMesh[,] debugTextArray;

    public AStarGrid(int width, int height, float cellSize){
        this.width=width;
        this.height=height;
        this.cellSize = cellSize;

        gridArray= new GridCell[width,height];

        if (DEBUG){
            debugTextArray = new TextMesh[width,height];

            Debug.Log(width + " , " + height);
        }

        //Shows the grid
        for (int x = 0; x<gridArray.GetLength(0);x++){
            for (int y = 0; y<gridArray.GetLength(1);y++){
                GridCell cell = new GridCell(x,y);

                //each element of the Matrix is a GridCell
                gridArray[x,y] = cell;

                
                //text object
                if (DEBUG) {
                    Debug.Log("Grid element:"+gridArray[x,y].ToString());
                    // debugTextArray[x,y] = UtilsClass.CreateWorldText(gridArray[x,y].ToString(),Color.white, null, GetWorldPosition(x,y) + new Vector3(cellSize,0,cellSize)*.5f,5);
                    
                    Debug.DrawLine(GetWorldPosition(x,y),GetWorldPosition(x,y+1),Color.white,100f);
                    Debug.DrawLine(GetWorldPosition(x,y),GetWorldPosition(x+1,y),Color.white,100f);
                }
            }     
        }

        // Debug.DrawLine(GetWorldPosition(0,height),GetWorldPosition(width,height),Color.white,100f);
        // Debug.DrawLine(GetWorldPosition(width,0),GetWorldPosition(width,height),Color.white,100f);

        //SetValue(1,1,56);
    }

    public Vector3 GetWorldPosition(int x, int y){
        return new Vector3(x,0,y)*cellSize;
    }

    public void GetXY(Vector3 worldPosition, out int x, out int y){
        x = Mathf.FloorToInt(worldPosition.x/cellSize);
        y = Mathf.FloorToInt(worldPosition.y/cellSize);
    }

    // public void SetValue(int x,int y, int value){
    //     if (x>=0 && y>=0 && x<width && y<height){
    //        gridArray[x,y] = value;
    //        debugTextArray[x,y].text=gridArray[x,y].ToString();
    //     }

    // }

    // public void SetValue(Vector3 worldPosition, int value){
    //     int x, y;
    //     GetXY(worldPosition,out x,out y);
    //     SetValue(x,y,value);
    // }

    public GridCell GetCell(int x, int y){
        return gridArray[x,y];
    }

    public void SetCell(int x, int y, GridCell cell){
        gridArray[x,y] = cell;
    }

    public void SetCellGCost(int x, int y, int gCost){
        gridArray[x,y].SetGCost(gCost);
        if (DEBUG) 
            debugTextArray[x,y].text=gridArray[x,y].ToString();
    }

    public void SetCellHCost(int x, int y, int hCost){
        gridArray[x,y].SetHCost(hCost);
        if (DEBUG) 
            debugTextArray[x,y].text=gridArray[x,y].ToString();
    }

    public void ComputeCellFCost(int x, int y){
        gridArray[x,y].CalculateFCost();
        if (DEBUG) 
            debugTextArray[x,y].text=gridArray[x,y].ToString();
    }

    public void SetCameFromNode(int x, int y, GridCell cell){
        gridArray[x,y].cameFromCell=cell;
    }

    //returns a list of all the neighbour cells
    public List<GridCell> GetNeigbours(int x, int y){
        List<GridCell> neighbourList = new List<GridCell>();

        //Check also if is not occupied
        //left
        if (x>0 && !gridArray[x-1,y].isOccupied)
            neighbourList.Add(gridArray[x-1,y]);

        //right
        if (x<width && !gridArray[x+1,y].isOccupied)
            neighbourList.Add(gridArray[x+1,y]);

        //top
        if (y>0 && !gridArray[x,y-1].isOccupied)
            neighbourList.Add(gridArray[x,y-1]);

        //bottom
        if (y<height && !gridArray[x,y+1].isOccupied)
            neighbourList.Add(gridArray[x,y+1]);

        //top right 
        if (y>0 && x < width && !gridArray[x+1,y-1].isOccupied)
            neighbourList.Add(gridArray[x+1,y-1]);

        //top left 
        if (y>0 && x>0 && !gridArray[x-1,y-1].isOccupied)
            neighbourList.Add(gridArray[x-1,y-1]);

        //bottom left 
        if (y<height && x>0 && !gridArray[x-1,y+1].isOccupied)
            neighbourList.Add(gridArray[x-1,y+1]);

        //bottom right 
        if (y<height && x<width && !gridArray[x+1,y+1].isOccupied)
            neighbourList.Add(gridArray[x+1,y+1]);
        
        //debug
        Debug.Log("Neigbours of:"+x+"-"+y);
        foreach (GridCell node in neighbourList){
            Debug.Log(node.ToString());
        }

        return neighbourList;
    }
}