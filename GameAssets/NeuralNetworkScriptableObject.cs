using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Network scriptable object", menuName = "New network scriptable object")]
public class NeuralNetworkScriptableObject : ScriptableObject  // i don't use this
{
    public new string name;

    public int inputLayerSize;
    public int secondLayerSize;
    public int thirdLayerSize;
    public int outputLayerSize;

    public float[,] inputLayerWeights;
    public float[,] secondLayerWeights;
    public float[,] thirdLayerWeights;

    public float[,] secondLayerBiasses;
    public float[,] thirdLayerBiasses;
    public float[,] outputLayerBiasses;

    [System.Serializable]
    public struct Column
    {
        public float[] inputLayerWeightrows;
    }

    public NeuralNetworkScriptableObject(float[,] inputLayerWeights)
    {
        this.inputLayerWeights = inputLayerWeights;
    }
}
