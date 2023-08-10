using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCell {   
    private int gCost;
    private int hCost;
    private int fCost;

    //to trace back the path to take
    public GridCell cameFromCell;

    private int x;
    private int y;

    //public int value; //value assign to the cell for the Algorithm
    //public GameObject objectInThisGridSpace = null; //This could be usefull for obstacle avoidance
    public bool isOccupied = false; //Usefull to know if there's something on top

    public GridCell(int x, int y){
        this.x = x;
        this.y = y;
        gCost=0;
    }

    // Set the position of this grid cell on the grid
    public void SetPosition( int x, int y){
        this.x = x;
        this.y = y;
    }
    
    //get the position of this grid space on the grid
    public Vector2Int GetPosition(){
        return new Vector2Int(x, y);
    }

    public void SetGCost(int gCost){
        this.gCost = gCost;
    }

    public int GetGCost(){
        return gCost;
    }

    public void SetHCost(int hCost){
        this.hCost = hCost;
    }
    public void SetFCost(int fCost){
        this.fCost = fCost;
    }

    public int GetFCost(){
        return fCost;
    }

    public void CalculateFCost(){
        fCost = gCost+hCost;
    }

    //ToString Method
    public override string ToString()
    {
        return x +"," +y +"-"+gCost+"-"+fCost+"-"+hCost;
    }

    // override object.Equals
    public override bool Equals(object obj)
    {       
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }
        
        // TODO: write your implementation of Equals() here
        if (!(obj is GridCell))
        {
            return false;
        }
        
        return (this.x == ((GridCell)obj).x)
                && (this.y == ((GridCell)obj).y);
    }
    
    // override object.GetHashCode
    public override int GetHashCode()
    {
        // TODO: write your implementation of GetHashCode() here
        return 35^x+y;
    }
    
}
