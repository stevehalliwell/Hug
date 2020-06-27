using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace AID
{
    /// <summary>
    /// UI display and user input for use with Console class. Handles showing all output via elements and a preview via text.
    /// Completion will fill in all common characters and user can cycle between all possible commands that match. Keeps a
    /// history of the commands entered which can be moved through to repeat commands.
    ///
    /// Also shows unity's Debug log outputs with color coding.
    /// </summary>
    public class DevConsoleUI : MonoBehaviour
    {
        [Tooltip("If true, only show the first line of a log from the Unity.Debug")]
        [SerializeField] protected bool firstLineOnly = true;

        [SerializeField] protected InputField inputField;

        /// <summary>
        /// Invoked with content of the inputfield when the confirmCommandEntered is hit. String is entire contents of input.
        /// </summary>
        public System.Action<string> OnConsoleCommandInput;

        /// <summary>
        /// Invoked with content from inputfield when the suggestionKey is hit. String is entire contents of input.
        ///
        /// Must return array of all possible commands that are the suggested matches based on the existing input,
        /// needs to return these in a stable way to allow cycling through results.
        /// </summary>
        public System.Func<string, string[]> OnConsoleCompleteRequested;

        [SerializeField] protected ScrollRect outputScrollRect;
        [SerializeField] protected RectTransform outputTextContainer;

        [Tooltip("Holder of all the console objects when it is active and shown to user")]
        [SerializeField] protected GameObject outputTextLocalRoot;

        [Tooltip("Prefab used for each element shown in the output")]
        [SerializeField] protected Text outputTextPrefab;

        [SerializeField] protected AnimationCurve previewPanelFade;
        [SerializeField] protected Text previewText;
        [SerializeField] protected CanvasGroup previewTextCanvasGroup;

        [Tooltip("Negative is infinite")]
        [SerializeField] protected float showLogFor = -1;

        [SerializeField]
        protected KeyCode toggleConsole = KeyCode.Tilde,
                      confirmCommandEntered = KeyCode.Return,
                      suggestionKey = KeyCode.Tab,
                      historyIndexUp = KeyCode.UpArrow,
                      historyIndexDown = KeyCode.DownArrow;

        private const int MAX_HISTORY = 50;

        protected static readonly Dictionary<LogType, string> logTypeColors = new Dictionary<LogType, string>
        {
            { LogType.Assert    , "<color=#ffffffff>" },
            { LogType.Error     , "<color=#ff0000ff>" },
            { LogType.Exception , "<color=#ff0000ff>" },
            { LogType.Warning   , "<color=#ffff00ff>" },
        };

        protected bool needsScrollUpdate;
        protected List<string> enteredHistory = new List<string>();
        protected int prevCompleteIndex;
        protected string[] prevCompleteResults;
        protected int prevHistoryIndex;
        protected float previewPanelCounter;
        protected int previousCaretPos;
        protected string previousInputLine;

        /// <summary>
        /// Find the number of shared common characters among an array of strings. Used during autocomplete to fill in
        /// the number of characters that we know must be typed without the user having to type them.
        /// </summary>
        /// <returns>number of characters that match</returns>
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

        protected virtual void AddPreviewText(string toLog)
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

        public void ClearMainOutput()
        {
            for (int i = outputTextContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(outputTextContainer.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Completion can take a number of actions, either finding multiple partial matches, moving through the existing collection
        /// of partial matches, filling out a complete line as there is only 1 partial match, finding zero matches.
        /// </summary>
        protected virtual void CompleteConsoleInput()
        {
            if (OnConsoleCompleteRequested == null)
                return;

            //first time trying to complete
            if (prevCompleteResults == null || prevCompleteResults.Length == 0)
            {
                //reset the autocomplete history
                var searchString = inputField.text.Substring(0, inputField.selectionAnchorPosition);
                prevCompleteResults = OnConsoleCompleteRequested.Invoke(searchString);
                prevCompleteIndex = 0;

                if (prevCompleteResults.Length == 1)
                {
                    //its not just the same existing matching
                    if (searchString != prevCompleteResults[0])
                    {
                        inputField.text = prevCompleteResults[0];
                        inputField.MoveTextEnd(false);

                        //lets assume we just autocompleted and this might be a holder object so try to complete again
                        //  won't run endlessly as if it is the same as the current input then we don't recurse
                        prevCompleteResults = null;
                        CompleteConsoleInput();
                    }
                }
                else if (prevCompleteResults.Length > 0)
                {
                    //mutliple possiblilties do the characters.
                    int firstCommonChars = FirstCommonCharacters(prevCompleteResults, inputField.selectionAnchorPosition);

                    inputField.text = prevCompleteResults[0].Substring(0, firstCommonChars);
                    inputField.MoveTextEnd(false);

                    AddToMainOutput(string.Join("\n", prevCompleteResults));
                    AutoCompletePreviewText();
                }
                else
                {
                    AddToMainOutput("No partial matches found. Try a find or a search command.");
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

        protected virtual void DoConsoleInput(string input)
        {
            if (OnConsoleCommandInput != null)
                OnConsoleCommandInput.Invoke(input);

            //cleanup
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

        protected virtual void ToggleConsole()
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

        protected virtual IEnumerator DelayRemovePreviewText(int thisLogLen)
        {
            yield return new WaitForSeconds(showLogFor);
            previewText.text = previewText.text.Substring(thisLogLen);
        }

        protected virtual void SafeShowHistory()
        {
            prevHistoryIndex = Mathf.Clamp(prevHistoryIndex, 0, enteredHistory.Count - 1);
            inputField.text = enteredHistory[prevHistoryIndex];
            inputField.caretPosition = 0;
            inputField.selectionFocusPosition = inputField.text.Length;
        }

        protected virtual void AutoCompletePreviewText()
        {
            inputField.text = prevCompleteResults[prevCompleteIndex];
            inputField.selectionFocusPosition = inputField.text.Length;
        }

        //required so we can autoscroll to the bottom safely only once perframe if it is required
        protected virtual void LateUpdate()
        {
            if (outputTextLocalRoot.activeInHierarchy && needsScrollUpdate)
            {
                Canvas.ForceUpdateCanvases();
                outputScrollRect.verticalNormalizedPosition = 0;
                needsScrollUpdate = false;
            }

            if (Input.GetKeyDown(toggleConsole))
                ToggleConsole();
        }

        protected virtual void OnDisable()
        {
            Application.logMessageReceived -= UnityConsoleHandler;
        }

        protected virtual void OnEnable()
        {
            Application.logMessageReceived += UnityConsoleHandler;
        }

        protected virtual void UnityConsoleHandler(string msg, string stackTrace, LogType type)
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

        protected virtual void Update()
        {
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
            previewTextCanvasGroup.alpha = previewPanelFade.Evaluate(previewPanelCounter / showLogFor);
        }
    }
}