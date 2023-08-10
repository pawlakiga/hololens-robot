using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// path planning 
// - add method to pass input matrix 
// - create grid with the size of input matrix 
// - modify visualisation


public class AStar{

    int width, height;

    //only for debug/visualization purposes
    private const float CELL_SIZE = 5f;

    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;  //length of the dyagonal

    private List<GridCell> openList; //nodes to search
    private List<GridCell> closedList;  //nodes already searched

    private AStarGrid grid;

    private int[,] inputMatrix;

    public AStar(int [,] inputMatrix){
        this.width = inputMatrix.GetLength(0);
        this.height = inputMatrix.GetLength(1);

        this.grid = new AStarGrid(width,height,CELL_SIZE);

        //the input is converted in to the AStarGrid
        for (int x = 0; x<inputMatrix.GetLength(0);x++){
            for(int y = 0; y< inputMatrix.GetLength(1);y++){
                if (inputMatrix[x,y] == 1){
                    grid.GetCell(x,y).isOccupied = true;
                    //cross on occupied cells
                    // Debug.DrawLine(new Vector3(x,0,y)*CELL_SIZE,new Vector3(x+1,0,y+1)*CELL_SIZE,Color.red,100f);
                    // Debug.DrawLine(new Vector3(x+1,0,y)*CELL_SIZE,new Vector3(x,0,y+1)*CELL_SIZE,Color.red,100f);
                }
            }
        }
    }


    //this method returns an array of coordinates (x,y) in the grid space
    public int[,] FindPath(int startX,int startY,int endX, int endY, TextMesh debugText){
        // debugText.text += String.Format("Finding Path...\n");
        GridCell startNode = new GridCell(startX,startY); 
        GridCell endNode = new GridCell(endX,endY); 

        //we start searching from the start node
        openList = new List<GridCell> { startNode };
        closedList = new List<GridCell>();

        // debugText.text += String.Format("Initializing costs...\n");
        //initialize costs
        for (int x = 0; x < width; x++){
            for (int y = 0; y < height; y++){
                grid.SetCellGCost(x,y,int.MaxValue);    //at the beginning the cost is set to infinite
                grid.ComputeCellFCost(x,y);             //f = g+h
                grid.SetCameFromNode(x,y,null);         //camefrom = null
            }
        }

        //set costs of the start node
        startNode.SetGCost(0);  //g cost
        startNode.SetGCost(ComputeDistanceCost(startNode,endNode)); //h cost
        startNode.CalculateFCost(); //f cost

        // debugText.text += String.Format("Start node costs:"+startNode.ToString()+"\n");

        while(openList.Count>0){
        //    debugText.text += String.Format("OpenList nodes:"+openList.Count + "\n");

            GridCell currentNode = GetLowestFCostNode(openList);

            //final node, return the calculated path to reach the end node
            if(currentNode.Equals(endNode)){
                // debugText.text += String.Format("Final Node:"+currentNode.ToString()+"=="+endNode.ToString() + "\n");
                List<GridCell> path = CalculatePath(endNode);


                // converts the output
                int [,]output = new int[path.Count,path.Count];
                if(path!=null){
                    for(int i = 0;i<=path.Count-1;i++){
                        // Debug.Log("Path Node:"+path[i].GetPosition().x+"-"+path[i].GetPosition().x);
                        // Debug.DrawLine(new Vector3(path[i].GetPosition().x,0,path[i].GetPosition().y)*CELL_SIZE+Vector3.one * CELL_SIZE/2,new Vector3(path[i+1].GetPosition().x,0,path[i+1].GetPosition().y)*CELL_SIZE+Vector3.one * CELL_SIZE/2,Color.green,100f);
                    
                        //convert in vector made by pairs of ints for the output
                        output[0,i]= path[i].GetPosition().x;
                        output[1,i]= path[i].GetPosition().y;
                    }
                }
                return output;
            }

            //the node has aready been searched
            openList.Remove(currentNode);
            closedList.Add(currentNode);

            // Debug.Log("Node:" +currentNode+"added to debug list");

            //loop trough the neighbours
            foreach (GridCell neigbourNode in grid.GetNeigbours(currentNode.GetPosition().x,currentNode.GetPosition().y)){
                if(closedList.Contains(neigbourNode)) continue;

                //see if the new cost is lower then the current one
                int tentativeGCost = currentNode.GetGCost()+ComputeDistanceCost(currentNode,neigbourNode);
                if(tentativeGCost<neigbourNode.GetGCost()){
                    neigbourNode.cameFromCell = currentNode;
                    neigbourNode.SetGCost(tentativeGCost);
                    neigbourNode.SetHCost(ComputeDistanceCost(neigbourNode,endNode));
                    neigbourNode.CalculateFCost();

                    int x = neigbourNode.GetPosition().x;
                    int y = neigbourNode.GetPosition().y;

                    grid.SetCellGCost(x,y,tentativeGCost);
                    grid.SetCellHCost(x,y,ComputeDistanceCost(neigbourNode,endNode));
                    grid.ComputeCellFCost(x,y);         
                    grid.SetCameFromNode(x,y,currentNode);

                    if(!openList.Contains(neigbourNode)){
                        openList.Add(neigbourNode);
                    }
                }
            }
        }
        // debugText.text += String.Format("Path could not be found.\n");
        //we have finished the nodes in the open list
        return null;
    }


    //trace back the steps frome the end to the start
    private List<GridCell> CalculatePath(GridCell endNode){
        List<GridCell> path = new List<GridCell>();

        GridCell currentNode = grid.GetCell(endNode.GetPosition().x,endNode.GetPosition().y);
        path.Add(currentNode);

        Debug.Log("current node "+currentNode.ToString()+" came from: "+currentNode.cameFromCell.ToString());
        while(currentNode.cameFromCell != null){
            path.Add(currentNode.cameFromCell);
            currentNode = currentNode.cameFromCell;
        }

        path.Reverse();

        Debug.Log("[CALCULATE PATH]The path is:");

        foreach(GridCell cell in path){
            Debug.Log(cell.ToString());
        }

        return path;
    }

    //h cost is the distance from the End (ignoring the obstacles)
    private int ComputeDistanceCost(GridCell start, GridCell end){
        Vector2Int a = start.GetPosition();
        Vector2Int b = end.GetPosition();

        int xDistance = Mathf.Abs(a.x-b.x);
        int yDistance = Mathf.Abs(a.y-b.y);
        int remaining = Mathf.Abs(xDistance-yDistance);

        //amount we can move diagonally + amount we can move going straight
        return MOVE_DIAGONAL_COST*Mathf.Min(xDistance,yDistance)+MOVE_STRAIGHT_COST*remaining;
    }

    //the current node is the one with lowest f cost
    private GridCell GetLowestFCostNode(List<GridCell> pathList) {
        GridCell lowestFCostNode = pathList[0];
        for (int i = 0;i<pathList.Count;i++){
            if(pathList[i].GetFCost() < lowestFCostNode.GetFCost()){
                lowestFCostNode= pathList[i];
            }
        }

        Debug.Log("Lowest f cost node:"+lowestFCostNode.ToString());
        return lowestFCostNode;
    }

    public AStarGrid GetGrid(){
        return grid;
    }

}