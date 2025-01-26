using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[System.Serializable]
public class NeuralNetwork
{
    public int inputLayerSize;
    public int secondLayerSize;
    public int thirdLayerSize;
    public int outputLayerSize;
    public Vector2 desiredPoint;

    public float[,] inputLayerWeights;
    public float[,] secondLayerWeights;
    public float[,] thirdLayerWeights;

    public float[,] secondLayerBiasses;
    public float[,] thirdLayerBiasses;
    public float[,] outputLayerBiasses;

    public NeuralNetwork(int inputLayerSize, int secondLayerSize, int thirdLayerSize, int outputLayerSize, Vector2 desiredPoint)
    {
        this.inputLayerSize = inputLayerSize;
        this.secondLayerSize = secondLayerSize;
        this.thirdLayerSize = thirdLayerSize;
        this.outputLayerSize = outputLayerSize;
        this.desiredPoint = desiredPoint;

        this.inputLayerWeights = new float[secondLayerSize, inputLayerSize];
        this.secondLayerWeights = new float[thirdLayerSize, secondLayerSize];
        this.thirdLayerWeights = new float[outputLayerSize, thirdLayerSize];

        this.secondLayerBiasses = new float[secondLayerSize, 1];
        this.thirdLayerBiasses = new float[thirdLayerSize, 1];
        this.outputLayerBiasses = new float[outputLayerSize, 1];
    }

    public NeuralNetwork(int inputLayerSize, int secondLayerSize, int thirdLayerSize, int outputLayerSize, float[,] iW, float[,] sW, float[,] tW, float[,] sB, float[,] tB, float[,] oB, Vector2 desiredPoint)
    {
        this.inputLayerSize = inputLayerSize;
        this.secondLayerSize = secondLayerSize;
        this.thirdLayerSize = thirdLayerSize;
        this.outputLayerSize = outputLayerSize;
        this.desiredPoint = desiredPoint;

        this.inputLayerWeights = iW;
        this.secondLayerWeights = sW;
        this.thirdLayerWeights = tW;

        this.secondLayerBiasses = sB;
        this.thirdLayerBiasses = tB;
        this.outputLayerBiasses = oB;
    }

    /*private static Vector2 SetNewGoal(Vector2 desiredPoint1, Vector2 desiredPoint2, Vector2 oldPoint, float goalMinimiumDistance)
    {
        Vector2 newPoint = Vector2.zero;  // need to initialize at zero or will throw error
        float distance = 0;
        for (int i = 0; i < 1000; i++)  // just to remove while loop
        {
            newPoint = new Vector2(Random.Range(desiredPoint1.x, desiredPoint2.x), Random.Range(desiredPoint1.y, desiredPoint2.y));
            distance = Vector2.Distance(newPoint, oldPoint);
            if (distance > goalMinimiumDistance)
            {
                return newPoint;
            }
        }
        return newPoint;
    }*/
}
