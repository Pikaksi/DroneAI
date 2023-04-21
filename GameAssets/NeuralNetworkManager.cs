using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Random=UnityEngine.Random;

public class NeuralNetworkManager : MonoBehaviour
{
    [SerializeField] List<NeuralNetwork> neuralNetworkList = new List<NeuralNetwork>();

    string sQLiteConnectionString;
    int aR;
    int aC;
    int bR;
    int bC;
    float tempFloat;
    static bool mouseControl;

    // drone variables
    static Vector2 leftUpDesiredPoint;
    static Vector2 rightDownDesiredPoint;
    static float minimumGoalDistance;
    static float maximumGoalDistance;
    static Vector2 startingPoint;

    public List<NeuralNetwork> CreateNetworks(int inputLayerSize, int secondLayerSize, int thirdLayerSize, int outputLayerSize, 
                                              float randomNumberWeights, float randomNumberBiasses, int populationSize)
    {
        for (int i = 0; i < populationSize; i++)
        {
            neuralNetworkList.Add(new NeuralNetwork(inputLayerSize, secondLayerSize, thirdLayerSize, outputLayerSize, GetNewGoal(startingPoint)));
        }

        foreach(NeuralNetwork network in neuralNetworkList)
        {
            network.inputLayerWeights = SetNetworkWeightsRandom(network.inputLayerWeights, randomNumberWeights);
            network.secondLayerWeights = SetNetworkWeightsRandom(network.secondLayerWeights, randomNumberWeights);
            network.thirdLayerWeights = SetNetworkWeightsRandom(network.thirdLayerWeights, randomNumberWeights);

            network.secondLayerBiasses = SetNetworkBiasesRandom(network.secondLayerBiasses, randomNumberBiasses);
            network.thirdLayerBiasses = SetNetworkBiasesRandom(network.thirdLayerBiasses, randomNumberBiasses);
            network.outputLayerBiasses = SetNetworkBiasesRandom(network.outputLayerBiasses, randomNumberBiasses);
        }

        return neuralNetworkList;
    }

    public float ActivationFunction(float value)
    {
        //return Mathf.Max(value, 0f);
        float k = Mathf.Exp(-value);
        return 1.0f / (1.0f + k);
    }

    public Vector2 GetNewGoal(Vector2 oldPoint)
    {
        Vector2 newPoint = Vector2.zero;  // need to initialize at zero or will throw error
        float distance = 0;
        for (int i = 0; i < 10000; i++)  // just to remove while loop
        {
            newPoint = new Vector2(Random.Range(leftUpDesiredPoint.x, rightDownDesiredPoint.x), Random.Range(leftUpDesiredPoint.y, rightDownDesiredPoint.y));
            distance = Vector2.Distance(newPoint, oldPoint);
            if (distance > minimumGoalDistance && distance < maximumGoalDistance)
            {
                Debug.Log(i);
                return newPoint;
            }
        }
        Debug.Log("nooooooooooooooooooooooo goal");
        return newPoint;
    }

    public float[,] SetNetworkWeightsRandom(float[,] array, float randomRange)
    {
        for (int i = 0; i < array.GetLength(0); i++) {
            for (int j = 0; j < array.GetLength(1); j++) {
                array[i, j] = Random.Range(-randomRange, randomRange);
            }
        }
        return array;
    }

    public float[,] ChangeNetworkWeights(float[,] array, float amount, float bigMutationChance, float bigMutationAmount)
    {
        for (int i = 0; i < array.GetLength(0); i++) {
            for (int j = 0; j < array.GetLength(1); j++) {
                array[i, j] += Random.Range(-amount, amount);

                if (Random.Range(0f, 100f) < bigMutationChance)
                {
                    array[i, j] += Random.Range(-bigMutationAmount, bigMutationAmount);
                }
            }
        }
        return array;
    }

    public float[,] SetNetworkBiasesRandom(float[,] array, float randomRange)
    {
        for (int i = 0; i < array.GetLength(0); i++) {
            array[i, 0] = Random.Range(-randomRange, randomRange);
        }
        return array;
    }

    public float[,] ChangeNetworkBiases(float[,] array, float amount, float bigMutationChance, float bigMutationAmount)
    {
        for (int i = 0; i < array.GetLength(0); i++) {
            array[i, 0] += Random.Range(-amount, amount);
            
            if (Random.Range(0f, 100f) < bigMutationChance)
            {
                array[i, 0] += Random.Range(-bigMutationAmount, bigMutationAmount);
            }
        }
        return array;
    }

    public float[,] MultiplyMatrices(float[,] firstMatrix, float[,] secondMatrix)
    {
        aR = firstMatrix.GetLength(0);
        aC = firstMatrix.GetLength(1);
        bR = secondMatrix.GetLength(0);
        bC = secondMatrix.GetLength(1);

        if (aC != bR) // can't multiply matrix
        {
            Debug.Log(aR);
            Debug.Log(aC);
            Debug.Log(bR);
            Debug.Log(bC);
            Debug.Log("couldn't multiply matrices");
            return null;
        }

        float[,] returnMatrix = new float[aR, bC];

        for (int i = 0; i < aR; i++) {
            for (int j = 0; j < bC; j++) {
                tempFloat = 0;
                for (int k = 0; k < aC; k++)
                {
                    tempFloat += firstMatrix[i, k] * secondMatrix[k, j];
                }
                returnMatrix[i, j] = tempFloat;
            }
        }
        return returnMatrix;
    }

    public float[,] MatrixAddition(float[,] firstMatrix, float[,] secondMatrix)
    {
        aR = firstMatrix.GetLength(0);
        aC = firstMatrix.GetLength(1);
        bR = secondMatrix.GetLength(0);
        bC = secondMatrix.GetLength(1);

        if (aR != bR || aC != bC)
        {
            Debug.Log(aR);
            Debug.Log(aC);
            Debug.Log(bR);
            Debug.Log(bC);
            Debug.Log("couldn't sum matrices");
            return null;
        }

        float[,] returnMatrix = new float[aR, aC];

        for (int i = 0; i < aR; i++) {
            for (int j = 0; j < aC; j++) {
                returnMatrix[i, j] = firstMatrix[i, j] + secondMatrix[i, j];
            }
        }
        return returnMatrix;
    }

    public NeuralNetwork Clone(NeuralNetwork obj)
    {
        using (var stream = new MemoryStream())
        {
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
            stream.Position = 0;
            return (NeuralNetwork)formatter.Deserialize(stream);
        }
    }

    /*public List<NeuralNetwork> DeepCopyNetworkList(List<NeuralNetwork> list)
    {
        List<NeuralNetwork> outputList = new List<NeuralNetwork>();
        for (int i = 0; i < list.Count; i++)
        {
            NeuralNetwork net = list[i];
            outputList.Add(new NeuralNetwork(net.inputLayerSize, net.secondLayerSize, net.thirdLayerSize, net.outputLayerSize, 
                                              net.inputLayerWeights, net.secondLayerWeights, net.thirdLayerWeights, 
                                              net.secondLayerBiasses, net.thirdLayerBiasses, net.outputLayerBiasses,
                                              , , DroneMovement.GetSpawnLocation()));
        }
        return outputList;
    }*/

    public NeuralNetwork DeepCopyNetwork(NeuralNetwork net)
    {
        NeuralNetwork outputvariable = (new NeuralNetwork(net.inputLayerSize, net.secondLayerSize, net.thirdLayerSize, net.outputLayerSize, 
                                        ExtensionMethods.DeepClone(net.inputLayerWeights), ExtensionMethods.DeepClone(net.secondLayerWeights), ExtensionMethods.DeepClone(net.thirdLayerWeights), 
                                        ExtensionMethods.DeepClone(net.secondLayerBiasses), ExtensionMethods.DeepClone(net.thirdLayerBiasses), ExtensionMethods.DeepClone(net.outputLayerBiasses), 
                                        GetNewGoal(startingPoint)));
        return outputvariable;
    }

    public List<NeuralNetwork> NetworkListFromArray(float[,] networkValueArray, int inputLayerLenght, int secondLayerLenght, int thirdLayerLenght, int outputLayerLenght)
    {
        List<NeuralNetwork> returnList = new List<NeuralNetwork>();

        for (int i = 0; i < networkValueArray.GetLength(0); i++)
        {
            int counter = 0;
            float[,] iW = new float[secondLayerLenght, inputLayerLenght];
            float[,] sW = new float[thirdLayerLenght, secondLayerLenght];
            float[,] tW = new float[outputLayerLenght, thirdLayerLenght];
            float[,] sB = new float[secondLayerLenght, 1];
            float[,] tB = new float[thirdLayerLenght, 1];
            float[,] oB = new float[outputLayerLenght, 1];
            List<float[,]> networkCreationArrays = new List<float[,]>() {iW, sW, tW, sB, tB, oB};
            foreach (float[,] array in networkCreationArrays)
            {
                for (int j = 0; j < array.GetLength(0); j++) {
                    for (int k = 0; k < array.GetLength(1); k++) {
                        array[j, k] = networkValueArray[i, counter];
                        counter++;
                    }
                }
            }
            returnList.Add(new NeuralNetwork(inputLayerLenght, secondLayerLenght, thirdLayerLenght, outputLayerLenght, iW, sW, tW, sB, tB, oB, GetNewGoal(startingPoint)));//, 
                           //DroneNetwork.GetleftUpDesiredPointBox(), DroneNetwork.GetrightDownDesiredPointBox(), DroneMovement.GetSpawnLocation()));
        }
        return returnList;
    }

    static public void SetNetworkManagerVariables(Vector2 _leftUpDesiredPointBox, Vector2 _rightDownDesiredPointBox, float _minimumGoalDistance, float _maximumGoalDistance, Vector2 _startingPoint, bool _mouseControl)
    {
        leftUpDesiredPoint = _leftUpDesiredPointBox;
        rightDownDesiredPoint = _rightDownDesiredPointBox;
        minimumGoalDistance = _minimumGoalDistance;
        maximumGoalDistance = _maximumGoalDistance;
        startingPoint = _startingPoint;
        mouseControl = _mouseControl;
    }
}

public static class ExtensionMethods
{
    // Deep clone
    public static T DeepClone<T>(this T a)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, a);
            stream.Position = 0;
            return (T) formatter.Deserialize(stream);
        }
    }
}
