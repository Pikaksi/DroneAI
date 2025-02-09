using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Data;
using Mono.Data.Sqlite;

public class SQLiteNetworkCommunicator : MonoBehaviour
{
    string sQLiteConnectionString;
    [SerializeField] int tableLenght;
    int neuralNetworkLenght;

    string sqliteDebugFileName = "/NeuralNetworkList.txt";

    NeuralNetworkScript neuralNetworkScript;

    private void Awake()
    {
        //sQLiteConnectionString = Application.dataPath + "/NeuralNetworkSQLite.db";
        sQLiteConnectionString = "URI=file:NeuralNetworkSQLite.db";
        neuralNetworkScript = gameObject.GetComponent<NeuralNetworkScript>();
    }

    public void SaveNeuralNetworkList(List<NeuralNetwork> networkList)
    {
        CreateTablesIfNeeded();
        File.WriteAllText(Application.dataPath + sqliteDebugFileName, "created tables");
        VipeAllData();
        File.WriteAllText(Application.dataPath + sqliteDebugFileName, "viped data");
        int saveCounter = 0;
        foreach (NeuralNetwork network in networkList)
        {
            File.WriteAllText(Application.dataPath + sqliteDebugFileName, "saving file" + saveCounter);
            SaveNetworkToDatabase(network);
            saveCounter++;
        }
    }

    public float[,] LoadFromNeuralNetwork()
    {
        float[,] neuralNetworksArray = new float[neuralNetworkScript.populationSize, neuralNetworkLenght];

        using (var connection = new SqliteConnection(sQLiteConnectionString)) // it ufkign workri
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM NeuralNetworkSQLite;";
                int counter = 0;

                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < neuralNetworksArray.GetLength(1); i++)
                        {
                            neuralNetworksArray[counter, i] = Convert.ToSingle(reader["Field" + (i + 1)]);
                            /*if (i < 6)
                            {
                                Debug.Log(Convert.ToSingle(reader["Field" + (i + 1)]));
                            }*/
                        }
                        counter++;
                    }
                    reader.Close();
                }
            }
            connection.Close();
        }
        return neuralNetworksArray;
    }

    private void CreateTablesIfNeeded()
    {
        string tableCreationString = GetTableString();
        using (var connection = new SqliteConnection(sQLiteConnectionString))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = string.Format("CREATE TABLE IF NOT EXISTS {0}", tableCreationString);
                command.ExecuteNonQuery();
            }

            connection.Close();
        }
    }

    private void VipeAllData()
    {
        using (var connection = new SqliteConnection(sQLiteConnectionString))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM NeuralNetworkSQLite";
                command.ExecuteNonQuery();
            }

            connection.Close();
        }
    }

    private void SaveNetworkToDatabase(NeuralNetwork network)
    {
        using (SqliteConnection connection = new SqliteConnection(sQLiteConnectionString))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = GetInsertString(network);
                //Debug.Log(command.CommandText);
                command.ExecuteNonQuery();
            }

            connection.Close();
        }
    }

    private string GetTableString()
    {
        string tableContents = "NeuralNetworkSQLite (";
        for (int i = 1; i < tableLenght + 1; i++)  // need to add 1 to tableLenght becouse starts at 1
        {
            tableContents += string.Format(" Field{0} REAL,", i);
        }
        tableContents = tableContents.Remove(tableContents.Length - 1);
        tableContents += ");";
        return tableContents;
    }

    public string GetInsertString(NeuralNetwork network)
    {
        string insertString = "INSERT INTO NeuralNetworkSQLite (";
        float[] networkValues = GetNeuralNetworkNumbers(network);

        for (int i = 1; i < networkValues.Length + 1; i++)  // + 1 is added because Field names start at one not zero
        {
            insertString += string.Format(" Field{0}, ", i);
        }
        insertString = insertString.Remove(insertString.Length - 2);
        insertString += ") VALUES (";
        for (int i = 0; i < networkValues.Length; i++)
        {
            insertString += " '" + networkValues[i] + "',";
        }
        insertString = insertString.Remove(insertString.Length - 1);
        insertString += ");";
        return insertString;
    }

    private float[] GetNeuralNetworkNumbers(NeuralNetwork network)
    {
        // the ++ are there for the biasses
        float[] returnArray = new float[neuralNetworkLenght];
        if (returnArray.Length > tableLenght)
        {
            Debug.Log("NeuralNetwork is too big to be saved");
            return null;
        }
        /*Debug.Log(network.inputLayerSize);
        Debug.Log(network.secondLayerSize);
        Debug.Log(network.thirdLayerSize);
        Debug.Log(network.outputLayerSize);
        Debug.Log(returnArray.Length);*/

        int counter = 0;  // the SQLite IDs start at Field1 not Field0
        List<float[,]> networkParameters = new List<float[,]> { network.inputLayerWeights, network.secondLayerWeights, network.thirdLayerWeights, 
                                            network.secondLayerBiasses, network.thirdLayerBiasses, network.outputLayerBiasses };
        foreach (float[,] array in networkParameters)
        {
            for (int i = 0; i < array.GetLength(0); i++) {
                for (int j = 0; j < array.GetLength(1); j++) {
                    returnArray[counter] = array[i, j];
                    counter++;
                }
            }
        }
        return returnArray;
    }

    public void SetNeuralNetworkLenght(int inputLayerLenght, int secondLayerLenght, int thirdLayerLenght, int outputLayerLenght)
    {
        neuralNetworkLenght = (inputLayerLenght + 1) * secondLayerLenght + (secondLayerLenght + 1) * thirdLayerLenght + (thirdLayerLenght + 1) * outputLayerLenght;
    }
}
