using System;
using UnityEngine;

public class PurePursuitControl : MonoBehaviour
{
    
    private Collision collision;
    public WheelCollider leftWheel, rightWheel;
    public Transform leftT, rightT;

    //b is the distance of the point in front of the robot
    private double b = 1f;

    // for debuggin print on holo lens
    // public TextMesh robotText;
    bool robotRunning = false, destinationReached = false;
    public float [,] path;
    int next_index = 0;
    public double min_distance = 0.2;
    public float scale = 1f;
    

    void Start(){

        this.b = 0.8 * scale;     
        
    }

    void FixedUpdate() {

        //check if the user decided to start the motion of robot        
        if (!robotRunning ) {
            // robotText.text = String.Format("Robot stopped");
            return;
        }
                
        //Find the nearest point between the ones contained in the path       
        next_index = FindNextPoint(next_index, min_distance);
        if (next_index == -1) return;        

        //Using the look-haed distance, set the target for the robot
        Vector3 target_position  = ComandedPoint(next_index);        
       
        //Distance between the robot and the target
        float distance = Vector3.Distance(transform.position, target_position);        
       
        //Get geometric dimensions of the robot
        double wheel_radius = leftWheel.radius * scale;
        double wheel_distance = 1.2f *scale;        
       
        //Calculate the toruqe that will be applied to the robot
        float[] torques = GetTorques(target_position,distance,(float)wheel_radius, (float)wheel_distance);

        //apply torques to the weels and update their position in unity
        leftWheel.motorTorque = torques[0];
        rightWheel.motorTorque = torques[1];    
         
        UpdateWheelPose(rightWheel, rightT);
        UpdateWheelPose(leftWheel, leftT);     

        
    }

    public float[] GetTorques(Vector3 target_position, float distance, float wheel_radius, float wheelsDistance){
    
        //angle between the target and the robot wiht respect to z axis        
        Vector3 direction = target_position - transform.position;
        float [] torques = new float[] {0f,0f};        
        float angle = Vector3.SignedAngle(direction, transform.forward, Vector3.up) * Mathf.Deg2Rad;       
        
        //  translational velocity
        float speed = 1.5f * distance; 

        //Empiric correction paramenter for angular velocity of the robot
        float angular_dynamic = 0.3f;

        //computes the desired torques
        float omegaR=(float)(speed + angular_dynamic*0.5*speed*Mathf.Sin(angle))/wheel_radius;        
        float omegaL=(float)(speed - angular_dynamic*0.5*speed*Mathf.Sin(angle))/wheel_radius;
        
        //Proportional controller, that takes in input the error on angular velocity of wheels
        float current_torqueL= leftWheel.motorTorque + 50f * (omegaL - leftWheel.rpm * Mathf.PI / 30 );        
        float current_torqueR= rightWheel.motorTorque + 50f * (omegaR - rightWheel.rpm * Mathf.PI / 30);
        
        //Return the values just computed
        torques[0] = current_torqueL; 
        torques[1] = current_torqueR; 
       
        return torques; 
    }

    public void UpdateWheelPose(WheelCollider _collider, Transform _transform){

        // The function GetWorldPose updates the position and velocity of wheel colliders
        // with respect to the torques applied     
        Vector3 _pos = _transform.position;
		Quaternion _quat = _transform.rotation;           
       
        _collider.GetWorldPose(out _pos, out _quat);
        
        _transform.position = _pos;  
        _transform.rotation = _quat;
    }


    public int FindNextPoint(int curr_index, double min_distance){

        //The variable "path" contains a list of coordinates of points that build the path
        //This function returns the point of the path not yet reached by the robot that is nearer to it
        //In order not to let the robot go back on itself, the function gets as input
        // "curr_index" which is the point of the path that the robot should reach
        //(for example, if the robot just started, curr_index is equal to 1)   
        float curr_distance = Vector3.Distance(transform.position, new Vector3(path[0, curr_index],transform.position.y,path[1, curr_index]));
        float distance_from_point = 0; 
        int next_index = curr_index; 

        //Since it is possible that there are point on the list that are nearer to the robot than the one of 
        //index "curr_index", the function calculates the distance from the robot of the all point from 
        //"curr_index" till the end of the list
        for(int i = curr_index+1; i < path.GetLength(1); i++){
            
            distance_from_point = Vector3.Distance(transform.position, new Vector3(path[0, i],transform.position.y,path[1,i]));
            
            //if point i is nearer, set point i as the new target
            if(distance_from_point < curr_distance){

                curr_distance = distance_from_point;
                next_index = i;
            }
        }      

        //if the distance from the point is less than the minimum acceptable distance from target ,
        // command immediately the next point to the robot
        if (curr_distance < min_distance && curr_index < (path.GetLength(1)-1)){
              
            next_index++;
        }
        
        // If we reach the End Marker, simply stop commanding the robot and let it stop by itself        
        if (curr_distance < min_distance && curr_index == (path.GetUpperBound(1)))
        {
            // Collision with the End Marker detected
            StopRobot();
            // robotText.text += String.Format("Robot Reached the end marker");
            return -1;
        }
       
        return next_index;
    }


    public Vector3 ComandedPoint(int next_point){
        
        //Position of the robot
        float xr =(float)transform.position.x;
        float zr =(float)transform.position.z;

        //Next point on the path 
        float x1 =(float)path[0, next_point];
        float z1 =(float)path[1, next_point];

        //previous point on the path
        float x0 =(float)path[0, next_point-1];
        float z0 =(float)path[1, next_point-1];

        //New target commanded to the robot, which is on the line connecting (x0,z0) and (x1,y1)
        Vector3 target_position;

        //radius of the circumference centred in the robot, corresponing to the look ahead
        float r = 0.4f;

        //If the point (x1,z1) is the last one of the path, and corresponds to the final goal,
        // directly command it.
        //In the other cases calculate the intersections between the line and the circumference.
        //3 different cases are taken in account

        //Check if the line between (x0,z0) and (x1,y1) as equation x = k and is parallel to z axis
        //In other case the equation will be in form of z= m*x + q
        if(next_point == (path.GetLength(1) -1)){
            target_position = new Vector3((float) x1, transform.position.y,(float) z1);
        }
        else if(x0 == x1){
            //The line has equation x = x0, so the two point of intersection are
            float za =(float) (zr + Mathf.Sqrt(Mathf.Pow(r,2) - Mathf.Pow(x0 - xr, 2)));
            float zb =(float) (zr - Mathf.Sqrt(Mathf.Pow(r,2) - Mathf.Pow(x0 - xr, 2)));

            //Check which is nearer to the point (x1,z1) of the path
            if(Mathf.Abs(z1 - za) < Mathf.Abs(z1 -zb)){
                target_position = new Vector3((float) x0, transform.position.y,(float) za);
            }
            else{
                target_position = new Vector3((float) x0, transform.position.y,(float) zb);
            }
        }
        else{

            //equation of the straight line between the two points
            double m = (z1 - z0)/(x1 - x0);
            double q = z0 - m*x0;                 
            
            // calculate the coefficients of the quadratic equation
            double a = 1.0 + m*m;
            double b = -2.0*xr + 2.0*m*q - 2.0*m*zr;
            double c = xr * xr + q*q + zr * zr - r*r - 2.0*q*zr;

            // solve the quadratic equation
            double delta = b*b - 4.0*a*c;
        
            if (delta < 0.0) {

                //Since there are no points of intersection, I simply comand the goal point (x1,z1)
                target_position = new Vector3((float) x1, transform.position.y,(float) z1);
            } 
            else if (delta == 0.0) {

                //The line is tangent to the circumference. Only a point of intersection
                double x = -b/(2.0*a);
                double z = m*x + q;
                
                target_position = new Vector3((float) x, transform.position.y,(float) z);
            } 
            else {

                double xa = (-b + Math.Sqrt(delta))/(2.0*a);
                double xb = (-b - Math.Sqrt(delta))/(2.0*a);
                double za = m*xa + q;
                double zb = m*xb + q;                

                //calcualte the distance of this two points from the goal point (x1,z1)
                double distance_a = Math.Sqrt(Math.Pow(x1 - xa, 2) + Math.Pow(z1 - za, 2));
                double distance_b = Math.Sqrt(Math.Pow(x1 - xb, 2) + Math.Pow(z1 - zb, 2));

                //Select the one which is nearer to (x1,z1)
                if(distance_a < distance_b){
                    target_position = new Vector3((float) xa, transform.position.y,(float) za);
                }
                else{
                    target_position = new Vector3((float) xb, transform.position.y,(float) zb);
                }
            }
        }
        return target_position;   
    } 


    private void UpdateWheelPoses(){
        UpdateWheelPose(rightWheel, rightT);
        UpdateWheelPose(leftWheel, leftT);        
    } 

    public void SetRobotRunning(bool robotRunning){
        this.robotRunning = robotRunning;
    }

    public void SetRobotPath(float[,] path){
        this.path = path;
        next_index = 0;

    }

    public void StopRobot(){
        leftWheel.motorTorque = 0;
        rightWheel.motorTorque = 0; 
        UpdateWheelPose(rightWheel, rightT);
        UpdateWheelPose(leftWheel, leftT);     
        SetRobotRunning(false);
        
    }

}