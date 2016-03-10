using UnityEngine;
using System.Collections.Generic;

public enum MenuMode { Start, Resume, Retry}

public class MenuScript : MonoBehaviour
{
    public bool Show;

    public MenuMode Mode;

    private readonly Rect[] _buttonsRect = 
    {
        new Rect(
          Screen.width * 0.375f,
          Screen.height * 0.325f,
          Screen.width * 0.25f,
          Screen.height * 0.1f),
        new Rect(
          Screen.width * 0.375f,
          Screen.height * 0.5f,
          Screen.width * 0.25f,
          Screen.height * 0.1f),
        new Rect(
          Screen.width * 0.375f,
          Screen.height * 0.675f,
          Screen.width * 0.25f,
          Screen.height * 0.1f)
    };

    private readonly Dictionary<MenuMode, string> _buttonName = new Dictionary<MenuMode, string>
    {
        { MenuMode.Start, "Start"},
        { MenuMode.Resume, "Resume" },
        { MenuMode.Retry, "Retry" }
    };

    void OnGUI()
    {
        if (Show == false) return;

        if (GUI.Button(_buttonsRect[0], _buttonName[Mode]))
        {
            if (Mode == MenuMode.Resume)
            {
                Show = false;
            }

            else
            {
                Application.LoadLevel("Stage_01");
            }
            Time.timeScale = 1;
        }


        if (Mode == MenuMode.Start)
        {
            if (GUI.Button(_buttonsRect[1], "Gesture Editor"))
                Application.LoadLevel("GesturesEditor");
            if (GUI.Button(_buttonsRect[2], "Exit"))
                Application.Quit();
        }
        else if (GUI.Button(_buttonsRect[1], "Back"))
        {
            Application.LoadLevel("MainMenu");
        }
    }

}
