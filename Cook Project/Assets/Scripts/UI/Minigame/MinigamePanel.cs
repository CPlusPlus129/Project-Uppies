using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using R3;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

public class MinigamePanel : MonoBehaviour
{
    // Hook this from other scripts, or swap for a UnityEvent<Key>
    public System.Action<Key> OnBeat;

    // Event fired when the minigame pattern is successfully completed
    public Subject<R3.Unit> OnMinigameCompleted = new Subject<R3.Unit>();

    // UI element for displaying the sigil pattern
    public UnityEngine.UI.LayoutGroup sigilDisplayArea;

    public Image currentSigilHighlighter;

    [Header("Visual Feedback")]
    [SerializeField] [UnityEngine.Range(0f, 1f)] private float completedSigilAlpha = 0.3f;

    // Holds all sigils in the scene
    private List<sigilType> sigilTypes;

    // Holds current sigil pattern
    private List<sigilType> currentSigilPattern;

    // Input system action for capturing beat keys
    private InputAction beatKey;

    // Holds previous sigil index to prevent immediate repeats
    private int prevSigilIndex = -1;

    private int currentSigilInd = 0;

    // Holds references to instantiated sigil UI objects
    private List<GameObject> instantiatedSigils = new List<GameObject>();

    // Setup key mappings and input action
    void OnEnable()
    {
        beatKey = new InputAction("BeatKey", InputActionType.Button);

        // Bind WASD + Arrow keys, all OR'd into one action
        string[] paths = {
            "<Keyboard>/w","<Keyboard>/a","<Keyboard>/s","<Keyboard>/d",
            "<Keyboard>/upArrow","<Keyboard>/leftArrow","<Keyboard>/downArrow","<Keyboard>/rightArrow"
        };
        foreach (var p in paths)
            beatKey.AddBinding(p).WithInteraction("press"); // edge-trigger on key down

        beatKey.performed += ctx =>
        {
            var kc = ctx.control as KeyControl;      // which key was pressed
            if (kc != null) HandleBeat(kc.keyCode);
        };

        beatKey.Enable();
    }

    // Clean up
    void OnDisable() => beatKey?.Disable();

    // Handles beat key presses
    void HandleBeat(Key key)
    {
        // Your function call here:
        // e.g., BeatHit(key);  or BeatHit(MapKeyToIndex(key));
        Debug.Log("Beat: " + key);
        OnBeat?.Invoke(key);

        var currentSigil = currentSigilPattern[currentSigilInd];

        var controlSigil = GetSigilByKey(key);

        // TODO Invert control sigil to show feedback

        if (key == currentSigil.sigilKey)
        {
            Debug.Log("Correct!");
            
            // Dim the current sigil before moving to the next
            DimSigil(currentSigilInd);
            
            currentSigilInd++;
            
            // Highlight next sigil
            if (currentSigilInd < currentSigilPattern.Count)
            {
                var nextSigil = sigilDisplayArea.transform.GetChild(currentSigilInd);
                currentSigilHighlighter.transform.position = nextSigil.position;
            }
            else
            {
                Debug.Log("Pattern complete!");
                
                // Notify the CookingSystem that the minigame is complete
                CookingSystem.Instance.CompleteCooking();
                
                // Fire completion event for any other systems that might need it
                OnMinigameCompleted.OnNext(R3.Unit.Default);
                
                // Reset for now
                currentSigilInd = 0;

                // Close the minigame UI
                UIRoot.Instance.GetUIComponent<MinigameUI>().Close();
            }
        }
        else
        {
            Debug.Log("Wrong key! Expected: " + currentSigil.sigilKey);
            // Reset progress on wrong key
            currentSigilInd = 0;
            currentSigilHighlighter.transform.position = sigilDisplayArea.transform.GetChild(0).position;
            
            // Reset all sigils to full opacity
            ResetAllSigilsAlpha();
        }
    }


    // Reads all available sigils and makes a random pattern for the player
    private void Awake()
    {
        sigilTypes = FindObjectsByType<sigilType>(FindObjectsSortMode.None).ToList();
        // Start capturing input for minigame
        Debug.Log(sigilTypes.Count());

        currentSigilPattern = GenerateNewSigilPattern(3, 5);

        DisplaySigils(currentSigilPattern);
    }

    // Gets a random sigil based on sigilTypes list
    private sigilType GetRandomSigil(bool preventRepeats=true)
    {
        int index = UnityEngine.Random.Range(0, sigilTypes.Count);

        // Prevent immediate repeats
        while (index == prevSigilIndex && preventRepeats)
        {
            index = UnityEngine.Random.Range(0, sigilTypes.Count);
        }

        prevSigilIndex = index;

        return sigilTypes[index];
    }

    private sigilType GetSigilByKey(Key key)
    {
        return sigilTypes.Find(s => s.sigilKey == key);
    }

    // Display the sigil pattern in the UI
    private void DisplaySigils(List<sigilType> pattern)
    {
        // Clear previous instantiated sigils list
        instantiatedSigils.Clear();

        foreach (var sigil in pattern)
        {
            Debug.Log(sigil.sigilKey);

            var child = Instantiate(sigil.gameObject, sigilDisplayArea.transform);
            child.transform.localScale = Vector3.one;

            // Store reference to instantiated sigil
            instantiatedSigils.Add(child);
        }
    }

    // Dim a specific sigil by index
    private void DimSigil(int index)
    {
        if (index < 0 || index >= instantiatedSigils.Count)
            return;

        var sigilObject = instantiatedSigils[index];
        if (sigilObject == null)
            return;

        // Get all Image components on the sigil and its children
        var images = sigilObject.GetComponentsInChildren<Image>();
        foreach (var image in images)
        {
            var color = image.color;
            color.a = completedSigilAlpha;
            image.color = color;
        }

        // Also check for raw images
        var rawImages = sigilObject.GetComponentsInChildren<RawImage>();
        foreach (var rawImage in rawImages)
        {
            var color = rawImage.color;
            color.a = completedSigilAlpha;
            rawImage.color = color;
        }
    }

    // Reset all sigils to full opacity (called on wrong key press)
    private void ResetAllSigilsAlpha()
    {
        for (int i = 0; i < instantiatedSigils.Count; i++)
        {
            var sigilObject = instantiatedSigils[i];
            if (sigilObject == null)
                continue;

            // Get all Image components and reset their alpha
            var images = sigilObject.GetComponentsInChildren<Image>();
            foreach (var image in images)
            {
                var color = image.color;
                color.a = 1f;
                image.color = color;
            }

            // Also check for raw images
            var rawImages = sigilObject.GetComponentsInChildren<RawImage>();
            foreach (var rawImage in rawImages)
            {
                var color = rawImage.color;
                color.a = 1f;
                rawImage.color = color;
            }
        }
    }

    // Create a new random pattern for the minigame
    private List<sigilType> GenerateNewSigilPattern(int sizeLow, int sizeHigh)
    {
        var pattern = new List<sigilType>();
        int patternSize = UnityEngine.Random.Range(sizeLow, sizeHigh + 1);

        for (int i = 0; i < patternSize; i++)
        {
            pattern.Add(GetRandomSigil());
        }

        return pattern;
    }
}
