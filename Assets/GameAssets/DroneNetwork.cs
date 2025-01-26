using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class DroneNetwork : MonoBehaviour
{
    [SerializeField] public NeuralNetwork network { get; set; }
    [SerializeField] float gravity;

    [Header("Neural network inputs")]
    [SerializeField] float xDistance;
    [SerializeField] float yDistance;
    [SerializeField] float xVelocity;
    [SerializeField] float yVelocity;
    [SerializeField] float cosAngle;
    [SerializeField] float sinAngle;
    [SerializeField] float angularMomentum;

    [Header("Goal settings")]
    [SerializeField] int goalsNeededToReach;
    [SerializeField] float desiredPointRadius;
    [SerializeField] float goalDockTime;

    [Header("Score settings")]
    [SerializeField] float goalScoreReward;
    [SerializeField] float scorePerSecond;
    [SerializeField][Range(0f, 180f)] float flippingDegrees;
    [SerializeField] float scoreDivisionIfFlipped;

    [Header("Debugging")]
    [SerializeField] float score;
    [SerializeField] int goalsReached;
    [SerializeField] float scoreTimer;
    [SerializeField] bool hasFlipped;
    [SerializeField] float dockedTime;

    [SerializeField] float angleRadiansAcos;

    [SerializeField] int populationSize;
    [SerializeField] int inputLayerSize;
    [SerializeField] int secondLayerSize;
    [SerializeField] int thirdLayerSize;
    [SerializeField] int outputLayerSize;
    [SerializeField] float randomNumberWeights;
    [SerializeField] float randomNumberBiasses;

    NeuralNetworkScript neuralNetworkScript;
    NeuralNetworkManager neuralNetworkManager;
    Rigidbody2D rb;
    DroneMovement droneMovement;

    static Vector2 leftUpDesiredPoint;
    static Vector2 rightDownDesiredPoint;
    static Vector2 leftUpDeathBoxCorner;
    static Vector2 rightDownDeathBoxCorner;
    static float goalMinimiumDistance;
    static Vector2 startingPoint;
    static bool mouseControl;

    private void Awake() 
    {
        hasFlipped = false;
        scoreTimer = 0f;
        score = 0;
        neuralNetworkScript = FindObjectOfType<NeuralNetworkScript>();
        neuralNetworkManager = FindObjectOfType<NeuralNetworkManager>();
        rb = GetComponent<Rigidbody2D>();
        droneMovement = GetComponent<DroneMovement>();
        rb.gravityScale = gravity;
    }

    private void FixedUpdate()
    {
        CalculateDroneMovement();
        ProcessScore();
        ProcessDeath();
        if (mouseControl)
        {
            ProcessMouseGoal();
        }
    }

    private void ProcessMouseGoal()
    {
        network.desiredPoint = NeuralNetworkScript.GetMousePosition();
    }

    private void CalculateDroneMovement()
    {
        xDistance = network.desiredPoint.x - transform.position.x;
        yDistance = network.desiredPoint.y - transform.position.y;
        xVelocity = rb.velocity.x;
        yVelocity = rb.velocity.y;
        angleRadiansAcos = Mathf.Acos(xDistance / Vector2.Distance(transform.position, (Vector2)network.desiredPoint));
        if (yDistance < 0) {
            angleRadiansAcos *= -1;
        }

        //acos might cause null errors
        cosAngle = Mathf.Cos((angleRadiansAcos) * -1) + transform.rotation.z * Mathf.Deg2Rad;
        sinAngle = Mathf.Sin(angleRadiansAcos) + transform.rotation.z * Mathf.Deg2Rad;
        angularMomentum = rb.angularVelocity;

        if (angleRadiansAcos == float.NaN)  
        { 
            Debug.LogError("angleRadiansAcos is null");
            angleRadiansAcos = 0;
        }
        if (cosAngle == float.NaN)  
        { 
            Debug.LogError("cosAngle is null");
            cosAngle = 0;
        }
        if (sinAngle == float.NaN)  
        { 
            Debug.LogError("sinAngle is null");
            sinAngle = 0;
        }

        float[,] output = neuralNetworkScript.CalculateNetworkAnswer(xDistance, yDistance, xVelocity, yVelocity, cosAngle, sinAngle, angularMomentum, network);

        droneMovement.ProcessAiMoving(output);
    }

    private void ProcessScore()
    {
        scoreTimer += Time.deltaTime;
        score += scorePerSecond / 50f / (Mathf.Sqrt(xDistance * xDistance + yDistance * yDistance) + 1);

        if (TransformUtils.GetInspectorRotation(gameObject.transform).z < -flippingDegrees 
            || TransformUtils.GetInspectorRotation(gameObject.transform).z > flippingDegrees)
        {
            hasFlipped = true;
        }

        dockedTime = (Mathf.Sqrt(xDistance * xDistance + yDistance * yDistance) <= desiredPointRadius && !mouseControl ? 1 : 0) * (Time.deltaTime + dockedTime);
        
        if (dockedTime > goalDockTime) {
            ProcessGoal();
        }
    }

    private void ProcessGoal()
    {
        score += goalScoreReward;
        scoreTimer = 0f;
        goalsReached++;
        network.desiredPoint = neuralNetworkManager.GetNewGoal(network.desiredPoint);
        if (goalsReached >= 96)
        {
            Debug.Log(goalsReached);
        }

        if (goalsReached >= goalsNeededToReach)
        {
            Finished();
        }
    }

    private void ProcessDeath()  // move all into one if statement
    {
        if (transform.position.x < leftUpDeathBoxCorner.x || transform.position.x > rightDownDeathBoxCorner.x ||
            transform.position.y > leftUpDeathBoxCorner.y || transform.position.y < rightDownDeathBoxCorner.y)
        {
            Finished();
        }
    }

    private void Finished()
    {
        if (hasFlipped)
        {
            score = score / scoreDivisionIfFlipped;
        }

        gameObject.SetActive(false);
        neuralNetworkScript.ReturnScore(score, network);

        // calls to network that drone has finished. isn't in the other function because network already knows
        neuralNetworkScript.DroneFinished();

        droneMovement.ResetAngle();
        score = 0f;
        scoreTimer = 0f;
        goalsReached = 0;
        hasFlipped = false;
    }

    public void FinishCall()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }
        if (hasFlipped)
        {
            score = score / scoreDivisionIfFlipped;
        }

        gameObject.SetActive(false);
        neuralNetworkScript.ReturnScore(score, network);

        droneMovement.ResetAngle();
        score = 0f;
        scoreTimer = 0f;
        goalsReached = 0;
        hasFlipped = false;
    }

    static public void SetDroneVariables(Vector2 _leftUpDesiredPointBox, Vector2 _rightDownDesiredPointBox, Vector2 _leftUpDeathBoxCorner, Vector2 _rightDownDeathBoxCorner, float _goalMinimiumDistance, Vector2 _startingPoint, bool _mouseControl)
    {
        leftUpDesiredPoint = _leftUpDesiredPointBox;
        rightDownDesiredPoint = _rightDownDesiredPointBox;
        leftUpDeathBoxCorner = _leftUpDeathBoxCorner;
        rightDownDeathBoxCorner = _rightDownDeathBoxCorner;
        goalMinimiumDistance = _goalMinimiumDistance;
        startingPoint = _startingPoint;
        mouseControl = _mouseControl;
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(network.desiredPoint, desiredPointRadius);

        float desiredBoxHight = leftUpDesiredPoint.y - rightDownDesiredPoint.y;
        float desiredBoxlenght = rightDownDesiredPoint.x - leftUpDesiredPoint.x;
        float deathBoxHight = leftUpDeathBoxCorner.y - rightDownDeathBoxCorner.y;
        float deathBoxLenght = rightDownDeathBoxCorner.x - leftUpDeathBoxCorner.x;

        Gizmos.DrawRay(leftUpDesiredPoint, Vector3.down * desiredBoxHight);
        Gizmos.DrawRay(leftUpDesiredPoint, Vector3.right * desiredBoxlenght);
        Gizmos.DrawRay(rightDownDesiredPoint, Vector3.left * desiredBoxlenght);
        Gizmos.DrawRay(rightDownDesiredPoint, Vector3.up * desiredBoxHight);

        Gizmos.DrawRay(leftUpDeathBoxCorner, Vector3.down * deathBoxHight);
        Gizmos.DrawRay(leftUpDeathBoxCorner, Vector3.right * deathBoxLenght);
        Gizmos.DrawRay(rightDownDeathBoxCorner, Vector3.left * deathBoxLenght);
        Gizmos.DrawRay(rightDownDeathBoxCorner, Vector3.up * deathBoxHight);
    }
}
