using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Random=UnityEngine.Random;

public class NeuralNetworkScript : MonoBehaviour
{
    [SerializeField] Camera mainCamera;

    [SerializeField] bool readNetworksFromFile;
    [SerializeField] int saveNetworkEveryXGenerations;
    [SerializeField] bool mouseControl;

    [SerializeField] float dividePosBy;
    [SerializeField] float divideAngularMomentumBy;

    [Header("Population")]
    [SerializeField] public int populationSize;
    [SerializeField] int eliteAmount;
    [SerializeField] [Range(1, 5)]int tournamentSelectionAmount;

    [Header("New network generation")]
    [SerializeField] int inputLayerSize;
    [SerializeField] int secondLayerSize;
    [SerializeField] int thirdLayerSize;
    [SerializeField] int outputLayerSize;
    [SerializeField] float randomNumberWeights;
    [SerializeField] float randomNumberBiasses;

    [Header("Drone variables")]
    [SerializeField] Vector2 leftUpDesiredPoint;
    [SerializeField] Vector2 rightDownDesiredPoint;
    [SerializeField] Vector2 leftUpDeathBoxCorner;
    [SerializeField] Vector2 rightDownDeathBoxCorner;
    [SerializeField] float minimiumGoalDistance;
    [SerializeField] float maximumGoalDistance;
    [SerializeField] Vector2 startingPoint;
    [SerializeField] GameObject dronePrefab;

    [Header("Between generations")]
    [SerializeField] float maxGenerationTime;
    IEnumerator generationTimeCoroutine;
    [SerializeField] float weightsMutationAmount;
    [SerializeField] float biasesMutationAmount;
    [SerializeField] [Range(0f, 100f)] float bigMutationChance; 
    [SerializeField] float bigMutationAmount;
    [SerializeField] float parentFlipChanceAmountToPopulation;

    [Header("Debugging")]
    [SerializeField] int debugCounter;
    [SerializeField] DroneNetwork[] droneNetworkList;
    [SerializeField] float timeHour;
    [SerializeField] float timeMinute;
    [SerializeField] float timeSecond;
    [SerializeField] List<NeuralNetwork> neuralNetworkList;
    [SerializeField] List<NeuralNetwork> finalNetworkList;
    [SerializeField] Dictionary<float, NeuralNetwork> scoreDictionary = new Dictionary<float, NeuralNetwork>();

    [SerializeField] int amountFinished;
    [SerializeField] int generation;

    NeuralNetworkManager neuralNetworkManager;
    SQLiteNetworkCommunicator sQLiteNetworkCommunicator;

    string[] fileNameArray = new string[5] {"/1NeuralNetwork.txt", "/2NeuralNetwork.txt", "/3NeuralNetwork.txt", "/4NeuralNetwork.txt", "/5NeuralNetwork.txt"};
    string bigFileName = "/NeuralNetworkList.txt";
    string debugFileName = "/debugFile.txt";
    static Vector2 mousePosition = new Vector2(0f, 10f);

    private void Awake()
    {
        if (minimiumGoalDistance > maximumGoalDistance)
        {
            Debug.LogError("minimiumGoalDistance is smaller than maximiumGoalDistance");
        }
        DroneNetwork.SetDroneVariables(leftUpDesiredPoint, rightDownDesiredPoint, leftUpDeathBoxCorner, rightDownDeathBoxCorner, minimiumGoalDistance, startingPoint, mouseControl);
        NeuralNetworkManager.SetNetworkManagerVariables(leftUpDesiredPoint, rightDownDesiredPoint, minimiumGoalDistance, maximumGoalDistance, startingPoint, mouseControl);
        neuralNetworkManager = gameObject.GetComponent<NeuralNetworkManager>();
        sQLiteNetworkCommunicator = gameObject.GetComponent<SQLiteNetworkCommunicator>();
        Physics2D.IgnoreLayerCollision(7, 7, true);
        sQLiteNetworkCommunicator.SetNeuralNetworkLenght(inputLayerSize, secondLayerSize, thirdLayerSize, outputLayerSize);
    }

    private void Start()
    {
        if (readNetworksFromFile)
        {
            float[,] neuralNetworkArray = sQLiteNetworkCommunicator.LoadFromNeuralNetwork();
            if (((inputLayerSize + 1) * secondLayerSize + (secondLayerSize + 1) * thirdLayerSize + (thirdLayerSize + 1) * outputLayerSize) != neuralNetworkArray.GetLength(1))
            {
                Debug.Log("you using different lenght networks");
                return;
            }
            neuralNetworkList = neuralNetworkManager.NetworkListFromArray(neuralNetworkArray, inputLayerSize, secondLayerSize, thirdLayerSize, outputLayerSize);
        }
        else
        {
            neuralNetworkList = neuralNetworkManager.CreateNetworks(inputLayerSize, secondLayerSize, thirdLayerSize, outputLayerSize, randomNumberWeights, randomNumberBiasses, populationSize);
        }

        amountFinished = populationSize;

        for (int i = 0; i < populationSize; i++)
        {
            GameObject instantiateDrone = Instantiate(dronePrefab, startingPoint, Quaternion.identity);
            instantiateDrone.GetComponent<DroneNetwork>().network = neuralNetworkList[i];
        }
        droneNetworkList = FindObjectsOfType<DroneNetwork>();

        generationTimeCoroutine = TriggerNewGenerationInTime(maxGenerationTime);
        StartCoroutine(generationTimeCoroutine);
    }

    private void FixedUpdate()  // to identify when a chash happens
    {
        timeHour = DateTime.Now.Hour;
        timeMinute = DateTime.Now.Minute;
        timeSecond = DateTime.Now.Second;
    }

    private void Update() 
    {
        if (Input.GetButton("Fire1"))
        {
            mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        }
    }

    public float[,] CalculateNetworkAnswer(float xDistance, float yDistance, float xVelocity, float yVelocity, float cosAngle, float sinAngle, float angularMomentum, NeuralNetwork network)
    {
        float[,] inputAnswers = new float[,] {{xDistance / dividePosBy}, {yDistance / dividePosBy}, {xVelocity}, {yVelocity}, {cosAngle}, {sinAngle}, {angularMomentum / divideAngularMomentumBy}};
        float[,] secondLayerAnswers = new float[network.secondLayerSize, 0];
        float[,] thirdLayerAnswers = new float[network.thirdLayerSize, 0];
        float[,] outputLayerAnswers = new float[network.outputLayerSize, 0];

        secondLayerAnswers = neuralNetworkManager.MultiplyMatrices(network.inputLayerWeights, inputAnswers);
        secondLayerAnswers = neuralNetworkManager.MatrixAddition(secondLayerAnswers, network.secondLayerBiasses);
        for (int i = 0; i < secondLayerAnswers.GetLength(0); i++)
        {
            secondLayerAnswers[i, 0] = neuralNetworkManager.ActivationFunction(secondLayerAnswers[i, 0]);
        }

        thirdLayerAnswers = neuralNetworkManager.MultiplyMatrices(network.secondLayerWeights, secondLayerAnswers);
        thirdLayerAnswers = neuralNetworkManager.MatrixAddition(thirdLayerAnswers, network.thirdLayerBiasses);
        for (int i = 0; i < thirdLayerAnswers.GetLength(0); i++)
        {
            thirdLayerAnswers[i, 0] = neuralNetworkManager.ActivationFunction(thirdLayerAnswers[i, 0]);
        }

        outputLayerAnswers = neuralNetworkManager.MultiplyMatrices(network.thirdLayerWeights, thirdLayerAnswers);
        outputLayerAnswers = neuralNetworkManager.MatrixAddition(outputLayerAnswers, network.outputLayerBiasses);
        for (int i = 0; i < outputLayerAnswers.GetLength(0); i++)
        {
            //Debug.Log(outputLayerAnswers[i, 0]);
        }

        return outputLayerAnswers;
    }

    private IEnumerator TriggerNewGenerationInTime(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        for (int i = 0; i < droneNetworkList.Length; i++)
        {
            droneNetworkList[i].FinishCall();
        }
        ProcessNewGeneration();
    }

    public void DroneFinished()
    {
        debugCounter++;
        amountFinished--;
        if (amountFinished <= 0)
        {
            ProcessNewGeneration();
        }
    }

    public void ReturnScore(float scoreValue, NeuralNetwork network)
    {
        if (!(scoreValue >= 0 || scoreValue <= 5000))
        {
            Debug.Log("uwegybrnvjuhenbrtghurtegbnhurnbtghrtunbghbutrng error 155 line");
            Debug.Log("the type is " + scoreValue.GetType());
            Debug.Log(scoreValue);
            Debug.Log(DateTime.Now.Second);
            Debug.Log(network);
            Debug.Log(network.inputLayerWeights[0, 0]);
            scoreValue = 0f;
        }
        for (int i = 0; i < 10000; i++)
        {
            if (!scoreDictionary.ContainsKey(scoreValue))
            {
                scoreDictionary.Add(scoreValue, neuralNetworkManager.DeepCopyNetwork(network));
                return;
            }
            scoreValue += Random.Range(-0.001f, 0.001f);
        }
    }

    private void ProcessNewGeneration()
    {                                       // make a output list here so you dont have to deep copy and can pass it into crossover function
        //Debug.Log("1");
        if (eliteAmount > populationSize) { Debug.Log("eliteAmount too big"); return; }

        amountFinished = populationSize;
        StopCoroutine(generationTimeCoroutine);
        //Debug.Log("2");

        Debug.Log("scoreDictionary lenght is " + scoreDictionary.Count);
        neuralNetworkList = FitnessSelection(scoreDictionary);
        Debug.Log("after fitness selection the lenght is " + neuralNetworkList.Count);
        //Debug.Log("3");

        NetworkMutation(neuralNetworkList);
        Debug.Log("after mutation the lenght is " + neuralNetworkList.Count);
        //Debug.Log("4");

        finalNetworkList = NetworkCrossoverFunction(neuralNetworkList, finalNetworkList);
        Debug.Log("after crossover the lenght is " + finalNetworkList.Count);
        //Debug.Log("5");

        // de stuff to reset the generation

        for (int i = 0; i < droneNetworkList.Length; i++)
        {
            droneNetworkList[i].gameObject.SetActive(true);
            droneNetworkList[i].GetComponent<DroneMovement>().ResetDronePosition();
            //Debug.Log(i);
            droneNetworkList[i].network = finalNetworkList[i];
        }
        Debug.Log("at the end of the generation the lenght was " + finalNetworkList.Count);
        if (generation % saveNetworkEveryXGenerations == saveNetworkEveryXGenerations - 1 && !mouseControl)
        {
            sQLiteNetworkCommunicator.SaveNeuralNetworkList(finalNetworkList);
        }
        //Debug.Log(generation);

        scoreDictionary.Clear();
        finalNetworkList.Clear();
        neuralNetworkList.Clear();
        generationTimeCoroutine = TriggerNewGenerationInTime(maxGenerationTime);
        StartCoroutine(generationTimeCoroutine);
        DroneNetwork.SetDroneVariables(leftUpDesiredPoint, rightDownDesiredPoint, leftUpDeathBoxCorner, rightDownDeathBoxCorner, minimiumGoalDistance, startingPoint, mouseControl);
        NeuralNetworkManager.SetNetworkManagerVariables(leftUpDesiredPoint, rightDownDesiredPoint, minimiumGoalDistance, maximumGoalDistance, startingPoint, mouseControl);
        generation++;
    }

    private List<NeuralNetwork> FitnessSelection(Dictionary<float, NeuralNetwork> inputDict)
    {
        List<NeuralNetwork> outputList = new List<NeuralNetwork>();
        List<float> keyList = new List<float>(inputDict.Keys);
        //Debug.Log("start here");

        if (tournamentSelectionAmount == 0)
        {
            Debug.Log("lol the tournament selection is == 0 get guud");
            return null;
        }
        Debug.Log("keylist count = " + keyList.Count + ". elites count are " + eliteAmount);
        if (inputDict.Count > populationSize)
        {
            Debug.Log("the input dictionary is too big " + inputDict.Count);
        }

        for (int i = 0; i < populationSize; i++)
        {
            if (tournamentSelectionAmount == 1) {
                outputList.Add(neuralNetworkManager.DeepCopyNetwork(inputDict[Mathf.Max(keyList[Random.Range(0, keyList.Count)] )]));
            }
            else if (tournamentSelectionAmount == 2) {
                outputList.Add(neuralNetworkManager.DeepCopyNetwork(inputDict[Mathf.Max(keyList[Random.Range(0, keyList.Count)], keyList[Random.Range(0, keyList.Count)] )]));
            }
            else if (tournamentSelectionAmount == 3) {
                outputList.Add(neuralNetworkManager.DeepCopyNetwork(inputDict[Mathf.Max(keyList[Random.Range(0, keyList.Count)], keyList[Random.Range(0, keyList.Count)], keyList[Random.Range(0, keyList.Count)] )]));
            }
            else if (tournamentSelectionAmount == 4) {
                outputList.Add(neuralNetworkManager.DeepCopyNetwork(inputDict[Mathf.Max(keyList[Random.Range(0, keyList.Count)], keyList[Random.Range(0, keyList.Count)], keyList[Random.Range(0, keyList.Count)], keyList[Random.Range(0, keyList.Count)] )]));
            }
            else if (tournamentSelectionAmount == 5) {
                outputList.Add(neuralNetworkManager.DeepCopyNetwork(inputDict[Mathf.Max(keyList[Random.Range(0, keyList.Count)], keyList[Random.Range(0, keyList.Count)], keyList[Random.Range(0, keyList.Count)], keyList[Random.Range(0, keyList.Count)], keyList[Random.Range(0, keyList.Count)] )]));
            }
        }
        keyList.Sort();

        Debug.Log("amount of elites chosen " + Mathf.Min(eliteAmount, keyList.Count));
        for (int i = 0; i < Mathf.Min(eliteAmount, keyList.Count); i++)
        {
            finalNetworkList.Add((inputDict[keyList[i]]));
        }

        Debug.Log("amount chosen " + outputList.Count);
        return outputList;
    }

    private void NetworkMutation(List<NeuralNetwork> networkList)
    {
        for ( int i = 0; i < networkList.Count; i++)
        {
            networkList[i].inputLayerWeights = neuralNetworkManager.ChangeNetworkWeights(networkList[i].inputLayerWeights, weightsMutationAmount, bigMutationChance, bigMutationAmount);
            networkList[i].secondLayerWeights = neuralNetworkManager.ChangeNetworkWeights(networkList[i].secondLayerWeights, weightsMutationAmount, bigMutationChance, bigMutationAmount);
            networkList[i].thirdLayerWeights = neuralNetworkManager.ChangeNetworkWeights(networkList[i].thirdLayerWeights, weightsMutationAmount, bigMutationChance, bigMutationAmount);

            networkList[i].secondLayerBiasses = neuralNetworkManager.ChangeNetworkBiases(networkList[i].secondLayerBiasses, biasesMutationAmount, bigMutationChance, bigMutationAmount);
            networkList[i].thirdLayerBiasses = neuralNetworkManager.ChangeNetworkBiases(networkList[i].thirdLayerBiasses, biasesMutationAmount, bigMutationChance, bigMutationAmount);
            networkList[i].outputLayerBiasses = neuralNetworkManager.ChangeNetworkBiases(networkList[i].outputLayerBiasses, biasesMutationAmount, bigMutationChance, bigMutationAmount);
        }
    }

    private List<NeuralNetwork> NetworkCrossoverFunction(List<NeuralNetwork> networkList, List<NeuralNetwork> finalList)
    {
        List<NeuralNetwork> outputList = new List<NeuralNetwork>();
        for (int i = 0; i < eliteAmount; i++)
        {
            outputList.Add(finalList[i]);
        }
        for (int i = 0; i < networkList.Count - finalList.Count; i++)
        {
            outputList.Add(TwoNetworkCrossover(networkList[Random.Range(0, networkList.Count - 1)], networkList[Random.Range(0, networkList.Count - 1)], parentFlipChanceAmountToPopulation));
        }

        return outputList;
    }

    private NeuralNetwork TwoNetworkCrossover(NeuralNetwork network1, NeuralNetwork network2, float parentFlipChance)
    {
        bool first = false; // alternate between networks
        NeuralNetwork output = new NeuralNetwork(network1.inputLayerSize, network1.secondLayerSize, network1.thirdLayerSize, network1.outputLayerSize, 
                                                neuralNetworkManager.GetNewGoal(startingPoint));

        // mayby make into arrays
        List<float[,]> outputValues = new List<float[,]> {output.inputLayerWeights, output.secondLayerWeights, output.thirdLayerWeights, output.secondLayerBiasses, output.thirdLayerBiasses, output.outputLayerBiasses};
        List<float[,]> network1Values = new List<float[,]> {network1.inputLayerWeights, network1.secondLayerWeights, network1.thirdLayerWeights, network1.secondLayerBiasses, network1.thirdLayerBiasses, network1.outputLayerBiasses};
        List<float[,]> network2Values = new List<float[,]> {network2.inputLayerWeights, network2.secondLayerWeights, network2.thirdLayerWeights, network2.secondLayerBiasses, network2.thirdLayerBiasses, network2.outputLayerBiasses};

        for (int i = 0; i < outputValues.Count; i++)
        {
            var values = crossoverFunctionArrayLoop(outputValues[i], network1Values[i], network2Values[i], parentFlipChance, first);
            outputValues[i] = values.Item1;
            first = values.Item2;
        }
        return output;
    }

    private (float[,], bool) crossoverFunctionArrayLoop(float[,] output, float[,] array1, float[,] array2, float flipChance, bool first)
    {
        for (int i = 0; i < output.GetLength(0); i++){
            for (int j = 0; j < output.GetLength(1); j++) {
                if (first) {
                    output[i, j] = array1[i, j];
                }
                else {
                    output[i, j] = array2[i, j];
                }
                if (flipChance > Random.Range(0f, 100f))
                {
                    first = !first;
                }
            }
        }
        return (output, first);
    }

    private void SaveNetworkToFile(NeuralNetwork network, String fileName)  // don't use
    {
        // clear the file
        File.WriteAllText(Application.dataPath + fileName, "empty");

        List<float> list = new List<float>();
        List<float[,]> A = new List<float[,]>() {network.inputLayerWeights, network.secondLayerWeights, network.thirdLayerWeights, network.secondLayerBiasses, network.thirdLayerBiasses, network.outputLayerBiasses};

        foreach (float[,] arr in A)
        {
            for (int i = 0; i < arr.GetLength(0); i++)
            {
                for ( int j = 0; j < arr.GetLength(1); j++)
                {
                    list.Add(arr[i, j]);
                }
            }
            list.Add(1234567890);
        }
        //Debug.Log("array = " + String.Join(" | ", new List<float>(list).ConvertAll(i => i.ToString()).ToArray()));

        File.WriteAllLines(Application.dataPath + fileName, new List<float>(list).ConvertAll(i => i.ToString()).ToArray());
    }

    private void SaveEveryNetworkToFile(List<NeuralNetwork> list)  // don't use
    {
        File.WriteAllText(Application.dataPath + bigFileName, "empty");

        List<float> writeList = new List<float>();

        foreach (NeuralNetwork network in list)
        {
            List<float[,]> A = new List<float[,]>() {network.inputLayerWeights, network.secondLayerWeights, network.thirdLayerWeights, network.secondLayerBiasses, network.thirdLayerBiasses, network.outputLayerBiasses};
            foreach (float[,] arr in A)
            {
                for (int i = 0; i < arr.GetLength(0); i++)
                {
                    for ( int j = 0; j < arr.GetLength(1); j++)
                    {
                        writeList.Add(arr[i, j]);
                    }
                }
                writeList.Add(1234567890);
            }
            writeList.Add(123456789);
        }

        File.WriteAllLines(Application.dataPath + bigFileName, new List<float>(writeList).ConvertAll(i => i.ToString()).ToArray());
    }

    public static Vector2 GetMousePosition()
    {
        return mousePosition;
    }
}  // |
