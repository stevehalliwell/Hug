﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace AID
{
    public class DevConsole : MonoBehaviour
    {
        public bool firstLineOnly = true;
        public InputField inputField;
        public int logsToShow = 5;
        public System.Action<string> OnConsoleCommandInput;
        public System.Func<string, string[]> OnConsoleCompleteRequested;
        public ScrollRect outputScrollRect;
        public RectTransform outputTextContainer;
        public GameObject outputTextLocalRoot;
        public Text outputTextPrefab;
        public AnimationCurve previewPanelFade;
        public Text previewText;
        public CanvasGroup previewTextCanvasGroup;

        [Tooltip("Negative is infinite")]
        public float showLogFor = -1;

        public KeyCode toggleConsole = KeyCode.Tilde,
                       confirmCommandEntered = KeyCode.Return,
                       suggestionKey = KeyCode.Tab,
                       historyIndexUp = KeyCode.UpArrow,
                       historyIndexDown = KeyCode.DownArrow;

        private const int MAX_HISTORY = 50;

        private static readonly Dictionary<LogType, string> logTypeColors = new Dictionary<LogType, string>
        {
            { LogType.Assert, "<color=#ffffffff>" },
            { LogType.Error, "<color=#ff0000ff>" },
            { LogType.Exception, "<color=#ff0000ff>" },
            { LogType.Warning, "<color=#ffff00ff>" },
        };

        private List<string> enteredHistory = new List<string>();
        private bool needsScrollUpdate;
        private int prevCompleteIndex;
        private string[] prevCompleteResults;
        private int prevHistoryIndex;
        private float previewPanelCounter;
        private int previousCaretPos;
        private string previousInputLine;

        public static int FirstCommonCharacters(string[] strs, int startingIndex = 0)
        {
            var shortest = strs[0].Length;

            //find the actual shortest
            for (int i = 1; i < strs.Length; i++)
            {
                if (strs[i].Length < shortest)
                    shortest = strs[i].Length;
            }

            //find where they stop matching
            for (; startingIndex < shortest; startingIndex++)
            {
                var targetChar = strs[0][startingIndex];
                for (int i = 1; i < strs.Length; i++)
                {
                    if (targetChar != strs[i][startingIndex])
                        return startingIndex;
                }
            }

            //never stopped matching so return what will be the shortest index
            return startingIndex;
        }

        public void AddPreviewText(string toLog)
        {
            if (toLog.Last() != '\n')
                toLog += "\n";

            if (showLogFor > 0)
            {
                previewText.text += toLog;

                StartCoroutine(DelayRemovePreviewText(toLog.Length));

                previewPanelCounter = 0;
            }
        }

        public void AddToMainOutput(string toLog)
        {
            var newElement = Instantiate(outputTextPrefab, outputTextContainer);
            newElement.text = toLog;

            needsScrollUpdate = true;
        }

        public void Clear()
        {
            for (int i = outputTextContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(outputTextContainer.GetChild(i).gameObject);
            }
        }

        public void CompleteConsoleInput()
        {
            if (OnConsoleCompleteRequested == null)
                return;

            //first time trying to complete
            if (prevCompleteResults == null || prevCompleteResults.Length == 0)
            {
                prevCompleteResults = OnConsoleCompleteRequested.Invoke(inputField.text.Substring(0, inputField.selectionAnchorPosition));
                prevCompleteIndex = 0;

                if (prevCompleteResults.Length == 1)
                {
                    inputField.text = prevCompleteResults[0];
                    inputField.MoveTextEnd(false);

                    //lets assume we just autocompleted and this might be a holder object so try to complete again
                    //  won't run endlessly as if it is the same as the current input then we don't recurse
                    prevCompleteResults = null;
                    CompleteConsoleInput();
                }
                else if (prevCompleteResults.Length > 0)
                {
                    int firstCommonChars = FirstCommonCharacters(prevCompleteResults, inputField.selectionAnchorPosition);

                    inputField.text = prevCompleteResults[0].Substring(0, firstCommonChars);
                    inputField.MoveTextEnd(false);

                    AddToMainOutput(string.Join("\n", prevCompleteResults));
                    AutoCompletePreviewText();
                }
            }
            else
            {
                //trying to tab thro commands
                prevCompleteIndex++;
                prevCompleteIndex %= prevCompleteResults.Length;
                AutoCompletePreviewText();
            }
            previousInputLine = inputField.text;
        }

        public void DoConsoleInput(string input)
        {
            OnConsoleCommandInput?.Invoke(input);
            inputField.text = string.Empty;
            inputField.Select();
            inputField.ActivateInputField();
            enteredHistory.Add(input);
            if (enteredHistory.Count > MAX_HISTORY)
            {
                enteredHistory.RemoveAt(0);
            }
            prevHistoryIndex = enteredHistory.Count;
        }

        public void ToggleConsole()
        {
            outputTextLocalRoot.SetActive(!outputTextLocalRoot.activeInHierarchy);
            previewText.gameObject.SetActive(!outputTextLocalRoot.activeInHierarchy);

            if (outputTextLocalRoot.activeInHierarchy)
            {
                inputField.text = string.Empty;
                inputField.Select();
                inputField.ActivateInputField();
            }
        }

        protected IEnumerator DelayRemovePreviewText(int thisLogLen)
        {
            yield return new WaitForSeconds(showLogFor);
            previewText.text = previewText.text.Substring(thisLogLen);
        }

        protected void SafeShowHistory()
        {
            prevHistoryIndex = Mathf.Clamp(prevHistoryIndex, 0, enteredHistory.Count - 1);
            inputField.text = enteredHistory[prevHistoryIndex];
            inputField.caretPosition = 0;
            inputField.selectionFocusPosition = inputField.text.Length;
        }

        private void AutoCompletePreviewText()
        {
            inputField.text = prevCompleteResults[prevCompleteIndex];
            inputField.selectionFocusPosition = inputField.text.Length;
        }

        private void LateUpdate()
        {
            if (outputTextLocalRoot.activeInHierarchy && needsScrollUpdate)
            {
                Canvas.ForceUpdateCanvases();
                outputScrollRect.verticalNormalizedPosition = 0;
                needsScrollUpdate = false;
            }
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= UnityConsoleHandler;
        }

        private void OnEnable()
        {
            Application.logMessageReceived += UnityConsoleHandler;
        }

        private void UnityConsoleHandler(string msg, string stackTrace, LogType type)
        {
            if (!enabled)
                return;

            if (firstLineOnly)
            {
                var p = msg.IndexOf("\n");
                if (p != -1)
                {
                    msg = msg.Substring(0, p);
                }
            }

            string toLog = string.Empty;

            if (logTypeColors.TryGetValue(type, out string colPrefix))
            {
                toLog = colPrefix + msg + "</color>";
            }
            else
            {
                toLog = msg;
            }

            AddPreviewText(toLog);
            AddToMainOutput(toLog);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleConsole))
                ToggleConsole();

            if (inputField.gameObject.activeInHierarchy)
            {
                if (Input.GetKeyDown(confirmCommandEntered))
                {
                    DoConsoleInput(inputField.text);
                }

                if (Input.GetKeyDown(suggestionKey))
                {
                    CompleteConsoleInput();
                }
            }

            if (previousCaretPos != inputField.selectionAnchorPosition)
            {
                prevCompleteResults = null;
                prevCompleteIndex = 0;
            }

            if (previousInputLine != inputField.text)
            {
                previousInputLine = inputField.text;

                prevCompleteResults = null;
                prevCompleteIndex = 0;
            }

            if (enteredHistory.Count > 0)
            {
                if (Input.GetKeyDown(historyIndexDown))
                {
                    prevHistoryIndex++;
                    SafeShowHistory();
                }

                if (Input.GetKeyDown(historyIndexUp))
                {
                    prevHistoryIndex--;
                    SafeShowHistory();
                }
            }

            previousCaretPos = inputField.selectionAnchorPosition;

            previewPanelCounter += Time.deltaTime;
            previewTextCanvasGroup.alpha = previewPanelFade.Evaluate(previewPanelCounter);
        }
    }
}