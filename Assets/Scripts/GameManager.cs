using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;   // <-- needed for IEnumerator

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    public struct Level
    {
        public int rows;
        public int cols;
        public int colors;
        public int targetScore;
        public int moves;
    }

    [Header("Levels (edit in Inspector)")]
    public Level[] levels;

    private const string LevelKey = "bbm3_level_index";
    public int CurrentLevel { get; private set; }

    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text movesText;
    public TMP_Text levelText;      // shows “Level X/Y” during play
    public TMP_Text levelUpText;    // disabled by default; shows “Level X!” between levels
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("Scoring")]
    public int basePerTile = 10;
    public int cascadeBonusPerStep = 50;

    [Header("Testing")]
    public bool resetProgressOnPlay = false;   // tick in Inspector to always start at level 1

    public int Score { get; private set; }
    public int MovesLeft { get; private set; }
    public bool IsOver { get; private set; }

    private Grid grid;

    void Awake()
    {
        if (resetProgressOnPlay)
        {
            PlayerPrefs.DeleteKey(LevelKey);
            PlayerPrefs.Save();
        }

        CurrentLevel = Mathf.Clamp(PlayerPrefs.GetInt(LevelKey, 0), 0, Mathf.Max(0, levels.Length - 1));

        grid = FindObjectOfType<Grid>();
        if (!grid)
        {
            Debug.LogError("[GameManager] No Grid found.");
            return;
        }

        // Apply level config to grid
        if (levels != null && levels.Length > 0)
        {
            var L = levels[CurrentLevel];
            grid.xDim = L.cols;
            grid.yDim = L.rows;
            grid.customColorsNum = Mathf.Max(3, L.colors);
        }

        grid.gameManager = this;
    }

    void Start()
    {
        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (levelUpText) levelUpText.gameObject.SetActive(false); // ensure hidden at start

        if (levels != null && levels.Length > 0)
        {
            var L = levels[CurrentLevel];
            Score = 0;
            MovesLeft = Mathf.Max(0, L.moves);
        }
        else
        {
            Score = 0;
            MovesLeft = 20; // fallback
        }

        IsOver = false;
        UpdateUI();
    }

    // ---- Called by Grid after a VALID swap resolves ----
    public void OnValidSwapResolved(int tilesCleared, int cascades)
    {
        if (IsOver) return;

        Score += tilesCleared * basePerTile
               + Mathf.Max(0, cascades - 1) * cascadeBonusPerStep;

        MovesLeft = Mathf.Max(0, MovesLeft - 1);
        CheckWinLose();
        UpdateUI();
    }

    // Optional if you want invalid attempts to cost a move
    public void OnInvalidSwap()
    {
        // if (IsOver) return;
        // MovesLeft = Mathf.Max(0, MovesLeft - 1);
        // CheckWinLose();
        // UpdateUI();
    }

    void CheckWinLose()
    {
        if (levels == null || levels.Length == 0) return;

        var L = levels[CurrentLevel];

        if (Score >= L.targetScore)
        {
            IsOver = true;
            if (winPanel) winPanel.SetActive(true);
            StartCoroutine(NextLevelAfterFlash());
        }
        else if (MovesLeft == 0)
        {
            IsOver = true;
            if (losePanel) losePanel.SetActive(true);
        }
    }

    void UpdateUI()
    {
        if (scoreText) scoreText.text = $"Score: {Score}";
        if (movesText) movesText.text = $"Moves: {MovesLeft}";
        if (levelText) levelText.text = $"Level {CurrentLevel + 1}/{Mathf.Max(1, levels.Length)}";
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Show “Level X!” briefly, then advance and reload scene
    IEnumerator NextLevelAfterFlash()
    {
        if (levels == null || levels.Length == 0)
            yield break;

        int next = Mathf.Min(CurrentLevel + 1, levels.Length - 1);

        if (levelUpText)
        {
            levelUpText.text = $"Level {next + 1}!";
            levelUpText.gameObject.SetActive(true);
            yield return new WaitForSeconds(1.2f);
        }

        PlayerPrefs.SetInt(LevelKey, next);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Direct button hook if you ever want to skip immediately
    public void NextLevel()
    {
        StartCoroutine(NextLevelAfterFlash());
    }

    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(LevelKey);
        PlayerPrefs.Save();
    }
}
