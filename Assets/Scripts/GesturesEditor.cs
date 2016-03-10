using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

using PDollarGestureRecognizer;

public class GesturesEditor : MonoBehaviour
{

	public Transform GestureOnScreenPrefab;

	private readonly List<Gesture> _trainingSet = new List<Gesture>();

	private readonly List<Point> _points = new List<Point>();
	private int _strokeId = -1;

	private Vector3 _virtualKeyPosition = Vector2.zero;
	private Rect _drawArea;

	private RuntimePlatform _platform;
	private int _vertexCount = 0;

	private List<LineRenderer> gestureLinesRenderer = new List<LineRenderer>();
	private LineRenderer currentGestureLineRenderer;

	//GUI
	private string message;
	private string newGestureName = "";

	void Start () {

		_platform = Application.platform;
		_drawArea = new Rect(0, 0, Screen.width - Screen.width / 3, Screen.height);

		//Load pre-made gestures
		TextAsset[] gesturesXml = Resources.LoadAll<TextAsset>("GestureSet/");
		foreach (TextAsset gestureXml in gesturesXml)
			_trainingSet.Add(GestureIO.ReadGestureFromXML(gestureXml.text));

		//Load user custom gestures
		string[] filePaths = Directory.GetFiles(Application.persistentDataPath, "*.xml");
		foreach (string filePath in filePaths)
			_trainingSet.Add(GestureIO.ReadGestureFromFile(filePath));
	}

	void Update () {

		if (_platform == RuntimePlatform.Android || _platform == RuntimePlatform.IPhonePlayer) {
			if (Input.touchCount > 0) {
				_virtualKeyPosition = new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y);
			}
		} else {
			if (Input.GetMouseButton(0)) {
				_virtualKeyPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y);
			}
		}

		if (_drawArea.Contains(_virtualKeyPosition)) {

			if (Input.GetMouseButtonDown(0)) {

				Refresh();

				++_strokeId;
				
				Transform tmpGesture = Instantiate(GestureOnScreenPrefab, transform.position, transform.rotation) as Transform;
				currentGestureLineRenderer = tmpGesture.GetComponent<LineRenderer>();
				
				gestureLinesRenderer.Add(currentGestureLineRenderer);
				
				_vertexCount = 0;
			}
			
			if (Input.GetMouseButton(0)) {
				_points.Add(new Point(_virtualKeyPosition.x, _virtualKeyPosition.y, _strokeId));

				currentGestureLineRenderer.SetVertexCount(++_vertexCount);
				currentGestureLineRenderer.SetPosition(_vertexCount - 1, Camera.main.ScreenToWorldPoint(new Vector3(_virtualKeyPosition.x, _virtualKeyPosition.y, 10)));
			}
		}
	}

    private void Refresh()
    {
        _strokeId = -1;

        _points.Clear();

        foreach (var lineRenderer in gestureLinesRenderer)
        {

            lineRenderer.SetVertexCount(0);
            Destroy(lineRenderer.gameObject);
        }

        gestureLinesRenderer.Clear();
    }

	void OnGUI() {

		GUI.Box(_drawArea, "Draw Area");

		GUI.Label(new Rect(10, Screen.height - 40, 500, 50), message);

		if (GUI.Button(new Rect(Screen.width - 150, 10, 100, 30), "Recognize")) {

			var candidate = new Gesture(_points.ToArray());
			var gestureResult = PointCloudRecognizer.Classify(candidate, _trainingSet.ToArray());
			
			message = gestureResult.GestureClass + " " + gestureResult.Score;
		}

		GUI.Label(new Rect(Screen.width - 200, 150, 70, 30), "Add as: ");
		newGestureName = GUI.TextField(new Rect(Screen.width - 150, 150, 100, 30), newGestureName);

		if (GUI.Button(new Rect(Screen.width - 150, 190, 100, 30), "Add") && _points.Count > 0 && newGestureName != "") {

			var fileName = string.Format("{0}/{1}-{2}.xml", Application.persistentDataPath, newGestureName, DateTime.Now.ToFileTime());

			#if !UNITY_WEBPLAYER
				GestureIO.WriteGesture(_points.ToArray(), newGestureName, fileName);
			#endif

			_trainingSet.Add(new Gesture(_points.ToArray(), newGestureName));

			newGestureName = "";
            Refresh();
		}

        if (GUI.Button(new Rect(Screen.width - 150, Screen.height - 50, 100, 30), "Back"))
            Application.LoadLevel("MainMenu");
	}
}
