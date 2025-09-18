using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class home : MonoBehaviour
{

    [Header("Play Button")]
    public Button playButton; // drag from Inspector
    
    [Header("Button Animation")]
    public float moveAmount = 10f;
    public float duration = 1f;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Example: pulse the Play button
        RectTransform playRect = playButton.GetComponent<RectTransform>();
        playRect.DOScale(1.1f, 0.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        // fit the screen view to match to android screen
        // TODO: where this should be
        Camera.main.GetComponent<FitCamera2D>()?.Fit();

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnButtonPress(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
