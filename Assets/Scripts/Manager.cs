using UnityEngine;

public class Manager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // Lock the cursor to the center of the screen
        Cursor.visible = false; // Hide the cursor

        Application.targetFrameRate = 144; // 🔁 Ändra till din önskade FPS
        QualitySettings.vSyncCount = 0;   // Viktigt: stäng av VSync annars styr den FPS
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}
