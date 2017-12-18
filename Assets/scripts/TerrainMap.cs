using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TerrainMap : MonoBehaviour
{
    public int mapWidthInTiles = 5;
    public int mapHeightInTiles = 5;

    static List<string> terrainTypes = new List<string> { // contains the list of possible terrains
        "PolarDesert", "Tundra", "Taiga", "MixedForest", "BroadleafForest", "Rainforest", "AridDesert",
        "ShrubDesert", "Steppe", "GrassSavanna", "TreeSavanna", "Shrubland", "Wetland" };
    static Dictionary<string, List<double>> distribution;
    static Dictionary<string, int> counts = new Dictionary<string, int>(); // for testing probabilities
    static public System.Random rng = new System.Random();
    static int numTerrainTypes;
    static Dictionary<String, TerrainTile> terrainTiles = new Dictionary<String, TerrainTile>();
    static public float tileWidth = 9f * Mathf.Sqrt(3) / 2; // width of each tile

    void Start()
    {
         string folderPath = Directory.GetCurrentDirectory().Replace("\\", "/") + "/Assets/ProjectFiles/";

        // store errors in a string to be writen to output file
        string errorDump = "ERRORS:\r\n";

        // dictionary which stores the conditional probabilities of seeing each terrain type next to another (given) type
        distribution = new Dictionary<string, List<double>>();

        // read probabilities from file
        using (StreamReader reader = new StreamReader(folderPath + "TerrainConditionalProbabilities.csv"))
        {
            // read first line into terrain types list
            List<string> terrainTypesFile = reader.ReadLine().Split(',').ToList();

            // make sure the list read from file is equal to the given list
            foreach (string ts in terrainTypesFile) // all terrain types from file
            {
                if (ts != "" && !terrainTypes.Contains(ts))
                {
                    errorDump += "file has extra terrain type, " + ts + "\r\n";
                }
            }
            foreach (string ts in terrainTypes) // all given terrain types
            {
                if (!terrainTypesFile.Contains(ts))
                {
                    errorDump += "file missing terrain type, " + ts + "\r\n";
                }
            }

            // remove the first (blank) and last (if newline) elements of the list
            terrainTypes.RemoveAt(0);
            if (terrainTypes[terrainTypes.Count - 1].Contains("\r") || terrainTypes[terrainTypes.Count - 1].Contains("\n")) // newlines contain either or both depending on OS
            {
                terrainTypes.RemoveAt(terrainTypes.Count - 1);
            }

            // assign number of terrains
            numTerrainTypes = terrainTypes.Count;

            while (!reader.EndOfStream) // while not end of file
            {
                // read each line into a list of strings
                List<string> values = reader.ReadLine().Split(',').ToList();
                string currKey = values[0]; // key is first value in the line
                values.RemoveAt(0); // remove first value (key) from line list
                if (values[values.Count - 1].Contains("\r") || values[values.Count - 1].Contains("\n"))
                {
                    values.RemoveAt(values.Count - 1); // remove ending newline char
                }

                List<double> numValues = new List<double>(); // list of probabilities

                foreach (string value in values) // loop through each element remaining in the line list
                {
                    double numValue = 0; // sets probability to 0 (if the string cannot be convered)
                    try // converting string to double
                    {
                        numValue = Convert.ToDouble(value);
                    }
                    catch (FormatException) // if value cannot be converted to double
                    {
                        errorDump += "failed to convert to double on " + value + " in " + currKey + "\r\n";
                    }
                    numValues.Add(numValue); // add to current list of probabilities
                }

                // add terrain type (key) and list of tuples (value) to the dictionary
                distribution.Add(currKey, numValues);
            }
        }

        // check if distribution is exhaustive (has all terrain types in both directions)
        if (!DictionarySquare(distribution))
        {
            errorDump += "dictionary not square\r\n";
        }

        TerrainTile currTile; // empty terrain tile to hold current tile in loop
        string currType; // will hold type of current tile
        Coordinate currCoord;
        string aboveCoordstr, leftCoordstr;

        // populate 2D array terrainMap 
        int widthStart = mapWidthInTiles / 2 - mapWidthInTiles,
            heightStart = mapHeightInTiles / 2 - mapHeightInTiles;
        int widthEnd = widthStart + mapWidthInTiles,
            heightEnd = heightStart + mapHeightInTiles;
        for (int i = widthStart; i < widthEnd; i++)
        {
            for (int j = heightStart; j < heightEnd; j++)
            {
                currCoord = new Coordinate(i, j); // current spot on the map
                leftCoordstr = (new Coordinate(i, j - 1)).AsString();
                aboveCoordstr = (new Coordinate(i - 1, j)).AsString();

                if (i == widthStart && j == heightStart) // first tile in map
                {
                    currTile = new TerrainTile(i, j);
                }
                else if (i == widthStart && j != heightStart)
                {
                    currTile = new TerrainTile(terrainTiles[leftCoordstr].type, currCoord);
                }
                else if (i != widthStart && j == heightStart)
                {
                    currTile = new TerrainTile(terrainTiles[aboveCoordstr].type, currCoord);
                }
                else
                {
                    currTile = new TerrainTile(terrainTiles[aboveCoordstr].type, terrainTiles[leftCoordstr].type, currCoord);
                }
                currType = currTile.type;
                
                terrainTiles.Add(currCoord.AsString(), currTile); // add current tile to dictionary
                currTile.PlaceTerrainTile();

                // incriment the count recording number of each terrain tile 
                // for testing and balancing purposes
                if (counts.ContainsKey(currType))
                {
                    counts[currType]++;
                }
                else
                {
                    counts[currType] = 1;
                }
            }
        }

        foreach (string terrain in terrainTypes)
        {
            string line = terrain + " not found";
            if (counts.ContainsKey(terrain))
            {
               line = terrain + " : " + counts[terrain].ToString();
            }
            File.AppendAllText(folderPath + "testfile.txt", line + "\r\n");
        }

        File.WriteAllText(folderPath + "testfile.txt", errorDump);
    }

    // tests if a dictionaries keys matchs a certain list of strings
    static bool KeysEqualList<T>(Dictionary<string, T> dict, List<string> strings)
    {
        if (dict.Count != strings.Count)
        {
            return false;
        }
        for (int i = 0; i < dict.Count; i++)
        {
            if (dict.Keys.ElementAt(i) != strings[i])
            {
                return false;
            }
        }
        return true;
    }

    // tests whether a dictionary with list values is square, ie that each list is the same length as the number of lists
    static bool DictionarySquare<T1, T2>(Dictionary<T1, List<T2>> dict)
    {
        int numKeys = dict.Keys.Count;
        foreach (T1 key in dict.Keys)
        {
            if (dict[key].Count != numKeys)
            {
                return false;
            }
        }
        return true;
    }

    // randomly selects a starting terrain where all types have equal probability 
    public static string RandomStartingTerrain()
    {
        return terrainTypes[rng.Next(numTerrainTypes)];
    }

    // randomly selects a next terrain given the previous one given the conditional probability distribtution
    public static string GetNextTerrain(string currentTerrain)
    {
        List<double> probabilities = distribution[currentTerrain];

        double currProb = 0;
        double randProb = rng.NextDouble(); // gives a random double from 0 to 1
        for (int i = 0; i < probabilities.Count; i++)
        {
            if (probabilities[i] > 0) // only check if the probability is non 0
            {
                currProb += probabilities[i]; // increases threshhold probability
                if (randProb <= currProb)
                {
                    return terrainTypes[i];
                }
            }
        }
        return "";
    }

    public static string GetNextTerrainFromList(List<string> existingTerrains)
    {
        List<double>[] probabilitiesAll = new List<double>[4];

        // initialize probabilities lists to all 1s unless a TerrainTile exists in that spot
        for (int i = 0; i < 4; i++) // loop 4 times
        {
            if (existingTerrains.Count > i)
            {
                probabilitiesAll[i] = distribution[existingTerrains[i]];
            }
            else
            {
                probabilitiesAll[i] = Enumerable.Repeat(1.0, numTerrainTypes).ToList();
            }
        }

        // combine probabilites by multiplying
        List<double> probabilities = new List<double>();
        double total = 0;
        for (int i = 0; i < numTerrainTypes; i++)
        {
            double prob = probabilitiesAll[0][i] * probabilitiesAll[1][i] * probabilitiesAll[2][i] * probabilitiesAll[3][i];
            probabilities.Add(prob);
            total += prob;
        }

        // divid each relative probability by the total to make them add to 1
        for (int i = 0; i < probabilities.Count; i++)
        {
            probabilities[i] = probabilities[i] / total;
        }

        double currProb = 0;
        double randProb = rng.NextDouble(); // gives a random double from 0 to 1
        for (int i = 0; i < probabilities.Count; i++)
        {
            if (probabilities[i] > 0) // only check if the probability is non 0
            {
                currProb += probabilities[i]; // increases threshhold probability
                if (randProb <= currProb)
                {
                    return terrainTypes[i];
                }
            }
        }
        return "";
    }
}


public class TerrainTile // individual tile within a TerrainMap
{
    public string type;
    public Coordinate location;

    public TerrainTile(int x = 0, int y = 0) // for a TerrainTile with no surrounding TerrainTiles
    {
        type = TerrainMap.RandomStartingTerrain();
        location = new Coordinate(x, y); // sets to (0,0)
    }

    public TerrainTile(string terrain1, Coordinate c)
    {
        type = TerrainMap.GetNextTerrainFromList
            (new List<string> { terrain1 });
        location = c;

    }

    public TerrainTile(string terrain1, string terrain2, Coordinate c)
    {
        type = TerrainMap.GetNextTerrainFromList
            (new List<string> { terrain1, terrain2 });
        location = c;
    }

    public TerrainTile(string terrain1, string terrain2, string terrain3, Coordinate c)
    {
        type = TerrainMap.GetNextTerrainFromList
            (new List<string> { terrain1, terrain2, terrain3 });
        location = c;
    }

    public TerrainTile(string terrain1, string terrain2, string terrain3, string terrain4, Coordinate c)
    {
        type = TerrainMap.GetNextTerrainFromList
            (new List<string> { terrain1, terrain2, terrain3, terrain4 });
        location = c;
    }

    // get file name based on properties of terrain tile
    public string GetPrefabName()
    {
        // get file name based on properties of terrain tile
        string name = this.type;
        return name;
    }

    public void SetLocation(Coordinate c)
    {
        this.location = c;
    }

    public Coordinate GetLocation()
    {
        // Debug.Log(this.location.AsString());
        return this.location;
    }


    public void PlaceTerrainTile()
    {
        GameObject tilePrefab = Resources.Load<GameObject>(this.GetPrefabName());
        Vector3 pos = GetLocation().ToUnityPosition();
        int rotation = TerrainMap.rng.Next(6) * 60 + 30; // 60 * random int between 0 and 5 for rotation in degrees
        GameObject.Instantiate(tilePrefab, pos, Quaternion.Euler(0, 0, rotation)); // PUT THE TILE ON THE MAP =) =) =)
    }
}

public class Coordinate
{
    int x;
    int y;

    public Coordinate() // Coordinate at origin
    {
        x = 0;
        y = 0;
    }

    public Coordinate(Vector3 position) // from unity location, will not be int type in final
    {
        List<int> xy = CoordFromLocation(position);
        x = xy[0];
        y = xy[1];
    }

    public Coordinate(int x_value, int y_value)
    {
        x = x_value;
        y = y_value;
    }

    // will return unity location
    public Vector3 ToUnityPosition()
    {
        float yDist = this.y * 27f / 4;
        float xDist = 0;
        if(this.y % 2 == 0)
        {
            xDist = this.x * TerrainMap.tileWidth;
        }
        else
        {
            xDist = (this.x + 0.5f) * TerrainMap.tileWidth;
        }
        return new Vector3(xDist, yDist, 0);
    }

    public List<int> CoordFromLocation(Vector3 pos)
    {
        int x_pos = (int)(pos.x / TerrainMap.tileWidth + 0.5);
        int y_pos = (int)(pos.y / TerrainMap.tileWidth + 0.5);
        return new List<int> { x_pos, y_pos };
    }

    public String AsString()
    {
        return "(" + x + "," + y + ")";
    }
}