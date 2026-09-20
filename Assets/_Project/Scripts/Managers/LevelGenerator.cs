using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public GameObject wallPrefab;
    public GameObject pelletPrefab;
    public GameObject powerFoodPrefab;
    public GameObject wormPrefab;

    public Transform wallParent;
    public Transform pelletParent;

    public Vector2 startPos = new Vector2(-9f, 10f); //start of the maze
    
    // Key for the Maze array
    // #  is a  Wall
    // .  is a Pellet
    // P  is PowerFood
    // W  is a Worm
    // T  is the PacStudent Spawn
    // ' ' is Empty

    private string[] mazeLayout = new string[] // every string is 1 row of the maze and every character is 1 tile
    {
        "###################",
        "#........#........#",
        "#.##.###.#.###.##.#",
        "#P##.###.#.###.##P#",
        "#.................#",
        "#.##.#.#####.#.##.#",
        "#....#...#...#....#",
        "####.### # ###.####",
        "   #.#       #.#   ",
        "####.# ##=## #.####",
        "    .  #   #  .    ",
        "####.# ##### #.####",
        "   #.#       #.#   ",
        "####.# ##### #.####",
        "#........#........#",
        "#.##.###.#.###.##.#",
        "#P.#.....T.....#.P#",
        "##.#.#.#####.#.#.##",
        "#....#...#...#....#",
        "#.########.########",
        "#.................#",
        "###################"
    };



    void Start()
    {
       GenerateLvl(); 
    }

    void GenerateLvl()
    {
        for (int y = 0; y < mazeLayout.Length; y++) // total rows
        {
            for (int x = 0; x < mazeLayout[y].Length; x++) //total columns
            {
                char tile = mazeLayout[y][x]; // gives the row and coumn number
                Vector2 spawnPos = new Vector2(startPos.x + x, startPos.y - y);

                switch (tile) // will create a new copy of the prefab at the given pos. Quaternion.identity means no rotation
                {
                    case '#':
                    Instantiate(wallPrefab, spawnPos, Quaternion.identity, wallParent);
                    break;

                    case '.':
                    Instantiate(pelletPrefab, spawnPos, Quaternion.identity, pelletParent);
                    break;

                    case 'P':
                    Instantiate(powerFoodPrefab, spawnPos, Quaternion.identity, pelletParent);
                    break;

                    case 'W':
                    Instantiate(wormPrefab, spawnPos, Quaternion.identity, pelletParent);
                    break;
                    
                    case 'T':
                    GameObject.FindGameObjectWithTag("PacStudent").transform.position = spawnPos; // move exisiting pacstudent to this world pos
                    break;

                    case ' ':
                    break;
                }

            } 
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
