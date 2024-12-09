class GameOfLife {
    constructor(gridSize = 15, generationInterval = 250) {
        this.gridSize = gridSize; // 矩陣大小
        this.generationInterval = generationInterval; // 世代間隔（毫秒）
        this.cellPrefab = null; // 格子預製件 (需要在 HTML/CSS 中定義)
        this.currentGeneration = [];
        this.cellRenderers = [];
        this.lifeColors = ["blue", "green", "magenta", "yellow", "purple"];
        this.deadColor = "white";
        this.isGamePaused = false;

        this.initGrid();
        this.startGameLoop();

        // 新增存檔與讀檔按鈕
        this.addSaveLoadButtons();
    }

    // 初始化網格
    initGrid() {
        const grid = document.createElement("div");
        grid.id = "grid";
        grid.style.display = "grid";
        grid.style.gridTemplateColumns = `repeat(${this.gridSize}, 1fr)`;
        grid.style.gridTemplateRows = `repeat(${this.gridSize}, 1fr)`;
        document.body.appendChild(grid);

        for (let x = 0; x < this.gridSize; x++) {
            this.currentGeneration[x] = [];
            this.cellRenderers[x] = [];
            for (let y = 0; y < this.gridSize; y++) {
                const cell = document.createElement("div");
                cell.style.width = "20px";
                cell.style.height = "20px";
                cell.style.border = "1px solid #ccc";
                cell.style.backgroundColor = this.deadColor;
                cell.dataset.x = x;
                cell.dataset.y = y;
                cell.addEventListener("click", () => this.setCellAlive(x, y));
                grid.appendChild(cell);

                this.cellRenderers[x][y] = cell;
                this.currentGeneration[x][y] = this.deadColor;
            }
        }
    }

    // 開始遊戲循環
    startGameLoop() {
        setInterval(() => {
            if (!this.isGamePaused) {
                this.advanceGeneration();
            }
        }, this.generationInterval);
    }

    // 設定指定格子的狀態為存活 (隨機顏色)
    setCellAlive(x, y) {
        const randomColor = this.lifeColors[Math.floor(Math.random() * this.lifeColors.length)];
        this.currentGeneration[x][y] = randomColor;
        this.cellRenderers[x][y].style.backgroundColor = randomColor;
    }

    // 推進到下一世代
    advanceGeneration() {
        const nextGeneration = [];

        for (let x = 0; x < this.gridSize; x++) {
            nextGeneration[x] = [];
            for (let y = 0; y < this.gridSize; y++) {
                const { aliveNeighbors, neighborColors } = this.countAliveNeighbors(x, y);
                const isAlive = this.currentGeneration[x][y] !== this.deadColor;

                if (isAlive) {
                    nextGeneration[x][y] =
                        aliveNeighbors === 2 || aliveNeighbors === 3
                            ? this.currentGeneration[x][y]
                            : this.deadColor;
                } else {
                    nextGeneration[x][y] =
                        aliveNeighbors === 3 ? this.getDominantColor(neighborColors) : this.deadColor;
                }
            }
        }

        this.currentGeneration = nextGeneration;
        this.applyColorsToGrid();
    }

    // 計算存活鄰居數量與其顏色分佈
    countAliveNeighbors(x, y) {
        let count = 0;
        const neighborColors = {};

        for (let i = -1; i <= 1; i++) {
            for (let j = -1; j <= 1; j++) {
                if (i === 0 && j === 0) continue;

                const nx = x + i;
                const ny = y + j;

                if (nx >= 0 && nx < this.gridSize && ny >= 0 && ny < this.gridSize) {
                    const color = this.currentGeneration[nx][ny];
                    if (color !== this.deadColor) {
                        count++;
                        neighborColors[color] = (neighborColors[color] || 0) + 1;
                    }
                }
            }
        }

        return { aliveNeighbors: count, neighborColors };
    }

    // 獲取鄰居中出現最多次的顏色
    getDominantColor(colorCounts) {
        let dominantColor = this.deadColor;
        let maxCount = 0;

        for (const [color, count] of Object.entries(colorCounts)) {
            if (count > maxCount) {
                maxCount = count;
                dominantColor = color;
            }
        }

        return dominantColor === this.deadColor
            ? this.lifeColors[Math.floor(Math.random() * this.lifeColors.length)]
            : dominantColor;
    }

    // 更新網格顏色
    applyColorsToGrid() {
        for (let x = 0; x < this.gridSize; x++) {
            for (let y = 0; y < this.gridSize; y++) {
                this.cellRenderers[x][y].style.backgroundColor = this.currentGeneration[x][y];
            }
        }
    }

    // 新增存檔與讀檔按鈕
    addSaveLoadButtons() {
        const saveButton = document.createElement("button");
        saveButton.textContent = "Save Generation";
        saveButton.addEventListener("click", () => this.saveCurrentGeneration());
        document.body.appendChild(saveButton);

        const loadButton = document.createElement("button");
        loadButton.textContent = "Load Generation";
        loadButton.addEventListener("click", () => this.loadGenerationFromFile());
        document.body.appendChild(loadButton);
    }

    // 存檔功能：將世代儲存為 JSON 並下載
    saveCurrentGeneration() {
        const data = {
            gridSize: this.gridSize,
            generation: this.currentGeneration,
        };
        const json = JSON.stringify(data, null, 2);

        const blob = new Blob([json], { type: "application/json" });
        const url = URL.createObjectURL(blob);

        const a = document.createElement("a");
        a.href = url;
        a.download = `GameOfLife_${Date.now()}.json`;
        a.click();
        URL.revokeObjectURL(url);
    }

    // 讀檔功能：從 JSON 檔案載入世代
    loadGenerationFromFile() {
        const input = document.createElement("input");
        input.type = "file";
        input.accept = "application/json";
        input.addEventListener("change", (event) => {
            const file = event.target.files[0];
            const reader = new FileReader();
            reader.onload = (e) => {
                const data = JSON.parse(e.target.result);
                this.gridSize = data.gridSize;
                this.currentGeneration = data.generation;
                this.applyColorsToGrid();
            };
            reader.readAsText(file);
        });
        input.click();
    }
}

// 啟動遊戲
const game = new GameOfLife();