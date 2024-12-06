using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameOfLife : MonoBehaviour
{
    // 矩陣大小 (例如: 15x15)
    public int gridSize = 15;

    // 世代間隔時間 (單位: 秒)
    public float generationInterval = 0.25f;

    // 格子預製件
    public GameObject cellPrefab;

    // 紀錄每個格子的顏色狀態
    private Color[,] currentGeneration;

    // 儲存所有格子的 MeshRenderer 以便快速更新顏色
    private MeshRenderer[,] cellRenderers;

    // 生存顏色: 藍、綠、粉、金、紫
    private Color[] lifeColors = { Color.blue, Color.green, Color.magenta, Color.yellow, new Color(0.5f, 0f, 0.5f) };

    // 死亡顏色: 白色
    private Color deadColor = Color.white;

    // 遊戲暫停狀態
    private bool isGamePaused = false;

    void Start()
    {
        // 初始化矩陣與渲染器陣列
        currentGeneration = new Color[gridSize, gridSize];
        cellRenderers = new MeshRenderer[gridSize, gridSize];

        InitializeGrid();
        StartCoroutine(GameLoop());
    }

    // 初始化格子網格
    void InitializeGrid()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                GameObject cell = Instantiate(cellPrefab, new Vector3(x, y, 0), Quaternion.identity);
                MeshRenderer renderer = cell.GetComponent<MeshRenderer>();
                renderer.material.color = deadColor; // 初始為死亡狀態 (白色)

                cellRenderers[x, y] = renderer;
                currentGeneration[x, y] = deadColor; // 紀錄當前顏色
            }
        }
    }

    IEnumerator GameLoop()
    {
        while (true)
        {
            if (!isGamePaused)
            {
                AdvanceGeneration();
            }
            yield return new WaitForSeconds(generationInterval);
        }
    }

    // 設定指定格子的狀態為存活 (隨機顏色)
    void SetCellAlive(int x, int y)
    {
        currentGeneration[x, y] = lifeColors[Random.Range(0, lifeColors.Length)];
        cellRenderers[x, y].material.color = currentGeneration[x, y];
    }

    // 推進到下一世代
    void AdvanceGeneration()
    {
        Color[,] nextGeneration = new Color[gridSize, gridSize];

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                int aliveNeighbors = CountAliveNeighbors(x, y, out Dictionary<Color, int> neighborColors);
                if (currentGeneration[x, y] != deadColor)
                {
                    // 如果存活，根據規則繼續存活或死亡
                    nextGeneration[x, y] = (aliveNeighbors == 2 || aliveNeighbors == 3) ? currentGeneration[x, y] : deadColor;
                }
                else
                {
                    // 如果死亡，檢查是否因鄰居復活
                    nextGeneration[x, y] = (aliveNeighbors == 3) ? GetDominantColor(neighborColors) : deadColor;
                }
            }
        }

        currentGeneration = nextGeneration;
        ApplyColorsToGrid();
    }

    // 計算存活的鄰居數量與其顏色分佈
    int CountAliveNeighbors(int x, int y, out Dictionary<Color, int> neighborColors)
    {
        int count = 0;
        neighborColors = new Dictionary<Color, int>();

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;

                int nx = x + i;
                int ny = y + j;

                if (nx >= 0 && nx < gridSize && ny >= 0 && ny < gridSize && currentGeneration[nx, ny] != deadColor)
                {
                    count++;
                    Color color = currentGeneration[nx, ny];
                    if (neighborColors.ContainsKey(color))
                        neighborColors[color]++;
                    else
                        neighborColors[color] = 1;
                }
            }
        }

        return count;
    }

    // 獲取鄰居中出現最多次的顏色
    Color GetDominantColor(Dictionary<Color, int> colorCounts)
    {
        Color dominantColor = deadColor;
        int maxCount = 0;

        foreach (var pair in colorCounts)
        {
            if (pair.Value > maxCount)
            {
                maxCount = pair.Value;
                dominantColor = pair.Key;
            }
        }

        return dominantColor == deadColor ? lifeColors[Random.Range(0, lifeColors.Length)] : dominantColor;
    }

    // 更新格子的顏色
    void ApplyColorsToGrid()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                cellRenderers[x, y].material.color = currentGeneration[x, y];
            }
        }
    }

    private void Update()
    {
        HandleInput();

        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveCurrentGenerationToJson();
        }
    }

    // 處理滑鼠輸入 (新增格子生命)
    void HandleInput()
    {
        if (Input.GetMouseButton(0))
        {
            isGamePaused = true;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                MeshRenderer renderer = hit.collider.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    Vector3 position = hit.transform.position;
                    int x = Mathf.RoundToInt(position.x);
                    int y = Mathf.RoundToInt(position.y);

                    if (currentGeneration[x, y] == deadColor)
                    {
                        SetCellAlive(x, y);
                    }
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isGamePaused = false;
        }
    }

    void SaveCurrentGenerationToJson()
    {
        // 定義顏色與名稱的對應關係
        Dictionary<Color, string> colorNameMap = new Dictionary<Color, string>
    {
        { Color.blue, "blue" },
        { Color.green, "green" },
        { Color.magenta, "magenta" },
        { Color.yellow, "yellow" },
        { new Color(0.5f, 0f, 0.5f), "purple" },
        { Color.white, "white" },
        { Color.black, "black" }
    };

        // 轉換 currentGeneration 為一維陣列
        List<string> stringGeneration = new List<string>();
        Dictionary<string, int> colorCount = new Dictionary<string, int>();

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                Color cellColor = currentGeneration[x, y];
                string colorName = colorNameMap.ContainsKey(cellColor) ? colorNameMap[cellColor] : "unknown";
                stringGeneration.Add(colorName);

                // 記錄顏色出現次數
                if (colorName != "white" && colorName != "black") // 忽略死亡顏色
                {
                    if (!colorCount.ContainsKey(colorName))
                        colorCount[colorName] = 0;
                    colorCount[colorName]++;
                }
            }
        }

        // 找出出現最多的顏色
        string mvpColor = "none";
        int maxCount = 0;
        foreach (var pair in colorCount)
        {
            if (pair.Value > maxCount)
            {
                maxCount = pair.Value;
                mvpColor = pair.Key;
            }
        }

        // 建構 JSON 資料
        GameData jsonData = new GameData
        {
            generation = stringGeneration.ToArray(),
            MVP = mvpColor
        };

        // 產生檔案名稱
        string fileName = $"GameOfLife_{System.DateTime.Now:yyyyMMddHHmmss}.json";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        // 寫入檔案
        File.WriteAllText(filePath, JsonUtility.ToJson(jsonData, true));
        Debug.Log($"Generation data saved to: {filePath}");
    }

    // 定義 GameData 類別來儲存世代資訊
    [System.Serializable]
    public class GameData
    {
        public string[] generation; // 轉換為一維陣列存儲
        public string MVP;
    }
}