using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

//todo scroll to bottom on write

namespace AID
{
    public class DevConsole : MonoBehaviour
    {
        private const int MAX_HISTORY = 50;

        private List<string> enteredHistory = new List<string>();

        public Text outputTextPrefab;
        public RectTransform outputTextContainer;
        public GameObject outputTextLocalRoot;
        public int logsToShow = 5;
        public bool firstLineOnly = true;

        [Tooltip("Negative is infinite")]
        public float showLogFor = -1;

        public KeyCode toggleConsole = KeyCode.Tilde,
                       confirmCommandEntered = KeyCode.Return,
                       suggestionKey = KeyCode.Tab,
                       historyIndexUp = KeyCode.UpArrow,
                       historyIndexDown = KeyCode.DownArrow;

        public Text previewText;
        public CanvasGroup previewTextCanvasGroup;
        public InputField inputField;

        public System.Action<string> OnConsoleCommandInput;
        public System.Func<string, string[]> OnConsoleCompleteRequested;

        private string[] prevCompleteResults;
        private int prevCompleteIndex;
        private int prevHistoryIndex;
        private string previousInputLine;
        private int previousCaretPos;
        private float previewPanelCounter;
        public AnimationCurve previewPanelFade;

        private static readonly Dictionary<LogType, string> logTypeColors = new Dictionary<LogType, string>
        {
            { LogType.Assert, "<color=#ffffffff>" },
            { LogType.Error, "<color=#ff0000ff>" },
            { LogType.Exception, "<color=#ff0000ff>" },
            { LogType.Warning, "<color=#ffff00ff>" },
        };

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

        protected IEnumerator DelayRemovePreviewText(int thisLogLen)
        {
            yield return new WaitForSeconds(showLogFor);
            previewText.text = previewText.text.Substring(thisLogLen);
        }

        public void AddToMainOutput(string toLog)
        {
            var newElement = Instantiate(outputTextPrefab, outputTextContainer);
            newElement.text = toLog;
        }

        private void OnEnable()
        {
            Application.logMessageReceived += UnityConsoleHandler;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= UnityConsoleHandler;
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

        protected void SafeShowHistory()
        {
            prevHistoryIndex = Mathf.Clamp(prevHistoryIndex, 0, enteredHistory.Count - 1);
            inputField.text = enteredHistory[prevHistoryIndex];
            inputField.caretPosition = 0;
            inputField.selectionFocusPosition = inputField.text.Length;
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

        private void AutoCompletePreviewText()
        {
            inputField.text = prevCompleteResults[prevCompleteIndex];
            inputField.selectionFocusPosition = inputField.text.Length;
        }

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

        //take something like Time.timeScale 1.5 and give back commandName {"Time", "timeScale"} and commandParams {"1.5"}
        public static void InputStringToCommandAndParams(string str, out string[] commandName, out string paramString)
        {
            str = str.Trim();
            var endOfCommand = str.IndexOf(' ');
            string commandStr = string.Empty;
            paramString = string.Empty;
            if (endOfCommand != -1)
            {
                commandStr = str.Substring(0, endOfCommand);
                paramString = str.Substring(endOfCommand).Trim();
            }
            else
            {
                commandStr = str;
            }

            commandName = commandStr.Split(ConsoleCommandTreeNode.NodeSeparatorChar);
        }

        public void Clear()
        {
            for (int i = outputTextContainer.childCount-1; i >= 0; i--)
            {
                Destroy(outputTextContainer.GetChild(i).gameObject);
            }
        }
    }
}