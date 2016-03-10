using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using PDollarGestureRecognizer;
using Random = UnityEngine.Random;

namespace Assets.Scripts
{
    public class GameManager : MonoBehaviour
    {
        public Transform GoalOnScreenPrefab;

        private Gesture _currentGoal;

        private int _score;
        private float _firstTimer = 20;
        private float _timer = 20;

        private readonly List<Point> _currentGesture = new List<Point>();
        private readonly List<LineRenderer> _gestureLinesRenderer = new List<LineRenderer>();

        private readonly List<Gesture> _currentSet = new List<Gesture>();

        private GameObject _gestureTrail;

        private Rect _drawArea;
        private Rect _figureNameArea;
        private Rect _figureArea;
        private Rect _timeArea;
        private Rect _scoreArea;

        private string _resultMessage;

        private GUIStyle _guiStyle = new GUIStyle();

        private void Start()
        {
            SetAreaPositions();

            //Load gestures set
            var gesturesXml = Resources.LoadAll<TextAsset>("GestureSet/");
            foreach (var gestureXml in gesturesXml)
                _currentSet.Add(GestureIO.ReadGestureFromXML(gestureXml.text));

            //Load user custom gestures
            string[] filePaths = Directory.GetFiles(Application.persistentDataPath, "*.xml");
            foreach (string filePath in filePaths)
                _currentSet.Add(GestureIO.ReadGestureFromFile(filePath));

            _currentGoal = _currentSet[Random.Range(0, _currentSet.Count)];
            DisplayGoal(_figureArea);
        }

        private void Update()
        {
            var menuScript = transform.GetComponent<MenuScript>();

            if (menuScript.Show) return;

            if (Input.GetButtonDown("Cancel"))
            {
                Time.timeScale = 0;
                menuScript.Show = true;
                menuScript.Mode = MenuMode.Resume;
                DeletePreviousGoal();
                return;
            }

            if (_timer <= 0)
            {
                menuScript.Show = true;
                menuScript.Mode = MenuMode.Retry;
                DeletePreviousGoal();
                return;
            }
            _timer -= Time.deltaTime;

            if (_gestureLinesRenderer.Count <= 0) DisplayGoal(_figureArea);

            var phase = TouchPhase.Stationary;
            var screenPoint = Vector3.zero;

#if UNITY_STANDALONE || UNITY_WEBPLAYER

            if (Input.GetMouseButtonDown(0))
            {
                phase = TouchPhase.Began;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                phase = TouchPhase.Ended;
            }
            else if (Input.GetMouseButton(0))
            {
                if (!Mathf.Approximately(Input.GetAxisRaw("Mouse X"), 0) ||
                    !Mathf.Approximately(Input.GetAxisRaw("Mouse Y"), 0))
                    phase = TouchPhase.Moved;
            }

            if (phase != TouchPhase.Stationary)
                screenPoint = Input.mousePosition;

#elif UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE

        if (Input.touchCount > 0)
        {
            var touch = Input.touches[0];

            phase = touch.phase;
            screenPoint = touch.position;
        }
#endif

            if (phase != TouchPhase.Stationary && phase != TouchPhase.Canceled &&
                _drawArea.Contains(screenPoint))
            {
                HandleInput(Camera.main.ScreenToWorldPoint(screenPoint), phase);
            }

        }

        private void HandleInput(Vector2 worldPoint, TouchPhase phase)
        {
            if (phase == TouchPhase.Began)
            {
                _resultMessage = null;
                _currentGesture.Clear();

                if (_gestureTrail != null) Destroy(_gestureTrail, _gestureTrail.GetComponent<TrailRenderer>().time);
                _gestureTrail = SpecialEffectsScript.MakeTrail(worldPoint);
            }

            _currentGesture.Add(new Point(worldPoint.x, worldPoint.y, 0));

            if (phase == TouchPhase.Moved)
            {
                if (_gestureTrail == null) _gestureTrail = SpecialEffectsScript.MakeTrail(worldPoint);
                _gestureTrail.transform.position = worldPoint;
            }

            if (phase == TouchPhase.Ended)
            {
                if (_gestureTrail != null) Destroy(_gestureTrail, _gestureTrail.GetComponent<TrailRenderer>().time);
                Recognize();
            }
        }

        private void DisplayGoal(Rect goalArea)
        {
            var tmpGesture = Instantiate(GoalOnScreenPrefab, transform.position, Quaternion.identity) as Transform;
            var currentGestureLineRenderer = tmpGesture.GetComponent<LineRenderer>();

            var center = Camera.main.ScreenToWorldPoint(goalArea.center);

            var vertexCount = 0;

            foreach (var point in _currentGoal.Points)
            {
                currentGestureLineRenderer.SetVertexCount(++vertexCount);
                currentGestureLineRenderer.SetPosition(vertexCount - 1, new Vector3((point.X + center.x), (point.Y - center.y)));

                _gestureLinesRenderer.Add(currentGestureLineRenderer);
            }
        }

        private void DeletePreviousGoal()
        {
            if (_gestureLinesRenderer.Count > 0)
            {
                foreach (var lineRenderer in _gestureLinesRenderer)
                {

                    lineRenderer.SetVertexCount(0);
                    Destroy(lineRenderer.gameObject);
                }

                _gestureLinesRenderer.Clear();
            }
        }

        private void Recognize()
        {
            if (_currentGesture.Count <= 5) return;
            var candidate = new Gesture(_currentGesture.ToArray());
            var gestureResult = PointCloudRecognizer.Classify(candidate, _currentSet.ToArray());

            if (gestureResult.GestureClass == _currentGoal.Name)
            {
                _currentGoal = _currentSet[Random.Range(0, _currentSet.Count)];
                DeletePreviousGoal();
                DisplayGoal(_figureArea);

                _resultMessage = "Right!";

                _score++;
                if (_score < _firstTimer)
                    _timer = _firstTimer - _score*0.5f;
            }
            else
            {
                _resultMessage = "Wrong!";
            }
        }

        private void OnGUI()
        {
            if (transform.GetComponent<MenuScript>().Show)
            {
                GUI.Box(new Rect(
                Screen.width * 0.375f,
                Screen.height * 0.2f,
                Screen.width * 0.25f,
                Screen.height * 0.05f),
                "Score: " + _score);
                return;
            }

            SetAreaPositions();
            SetBoxGuiStyle();

            GUI.Box(_drawArea, "Draw Area");
            GUI.Box(_figureNameArea, "Name:\n" + _currentGoal.Name);
            GUI.Box(_figureArea, "Figure:");
            GUI.Box(_timeArea, "Time:\n" + TimeSpan.FromMinutes(_timer).Minutes + ":" +TimeSpan.FromMinutes(_timer).Seconds);
            GUI.Box(_scoreArea, "Score:\n" + _score);

            if (_resultMessage == null) return;

            var guiStyle = GetResultLabelGuiStyle();

            if (_resultMessage == "Wrong!") guiStyle.normal.textColor = Color.red;
            if (_resultMessage == "Right!") guiStyle.normal.textColor = Color.green;

            GUI.Label(new Rect(Screen.width * 0.15f, Screen.height * 0.9f, Screen.width * 0.4f, Screen.height * 0.1f), _resultMessage, guiStyle);
        }

        private GUIStyle GetResultLabelGuiStyle()
        {
            var guiStyle = new GUIStyle(GUI.skin.label);
            guiStyle.fontSize = Mathf.CeilToInt(Screen.height * 0.03f);
            guiStyle.alignment = TextAnchor.UpperCenter;
            return guiStyle;
        } 

        private void SetBoxGuiStyle()
        {
            _guiStyle = GUI.skin.box;

            _guiStyle.fontSize = Mathf.CeilToInt(Screen.height * 0.03f);
            _guiStyle.normal.textColor = Color.white;
            _guiStyle.alignment = TextAnchor.UpperCenter;
        }

        private void SetAreaPositions()
        {
            _drawArea = new Rect(Screen.width * 0.05f, Screen.height * 0.1f, Screen.width * 0.6f, Screen.height * 0.8f);
            _figureNameArea = new Rect(Screen.width * 0.7f, Screen.height * 0.1f, Screen.width * 0.25f, Screen.height * 0.1f);
            _figureArea = new Rect(Screen.width * 0.7f, Screen.height * 0.25f, Screen.width * 0.25f, Screen.height * 0.35f);
            _timeArea = new Rect(Screen.width * 0.7f, Screen.height * 0.65f, Screen.width * 0.25f, Screen.height * 0.1f);
            _scoreArea = new Rect(Screen.width * 0.7f, Screen.height * 0.8f, Screen.width * 0.25f, Screen.height * 0.1f);
        }
    }

}

