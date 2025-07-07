using UnityEngine;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    public Text fpsText;
    private float timer;
    private int frames;
    private float fps;

    void Update()
    {
        frames++;
        timer += Time.unscaledDeltaTime;
        if (timer >= 1f)
        {
            fps = frames / timer;
            if (fpsText != null)
                fpsText.text = "FPS: " + Mathf.RoundToInt(fps);
            frames = 0;
            timer = 0f;
        }
    }
    public void QuitTheGame()
    {
        Application.Quit();
    }
}
