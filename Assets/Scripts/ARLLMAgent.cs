using UnityEngine;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using System.Collections;
using System;

public class ARLLMAgent : MonoBehaviour
{
    public ARTrackedImageManager trackedImageManager;
    public Transform cameraTransform;
    public GameObject turbineModel;
    public TextMeshProUGUI llmText;
    public TMP_InputField sizeInputField;
    public Button sizeSubmitButton;

    private bool isImageTracked = false;
    private string defectSize;
    private string defectType = "SCRATCH";
    private bool defectSizeSubmitted = false;

    void Start()
    {
        sizeInputField.gameObject.SetActive(false);
        sizeSubmitButton.onClick.AddListener(OnSizeSubmitted);
        sizeSubmitButton.gameObject.SetActive(false);
        sizeInputField.onEndEdit.AddListener(delegate { DeactivateKeyboard(); });

        StartCoroutine(ActivateDetectionAfterDelay(4f));
    }

    // Coroutine for activating detection after a delay
    IEnumerator ActivateDetectionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay); // Wait for the specified delay
        llmText.text = "Reference image is missing. Align the camera with the reference image.";
    }

    void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnImageChanged;
    }

    void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnImageChanged;
    }

    void OnImageChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImage in eventArgs.added)
        {
            HandleTrackedImage(trackedImage);
        }

        foreach (var trackedImage in eventArgs.updated)
        {
            HandleTrackedImage(trackedImage);
        }

        foreach (var trackedImage in eventArgs.removed)
        {
            if (trackedImage.referenceImage.name == "JetEngineTracker" && !isImageTracked)
            {
                isImageTracked = false;
                turbineModel.SetActive(false);
                llmText.text = "Reference image is missing. Align the camera with the reference image.";
            }
        }
    }

    void HandleTrackedImage(ARTrackedImage trackedImage)
    {
        if (trackedImage.referenceImage.name == "JetEngineTracker" && !isImageTracked)
        {
            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                isImageTracked = true;
                turbineModel.SetActive(true);
                llmText.text = "Move the camera to your right.";
            }
            else
            {
                isImageTracked = false;
                turbineModel.SetActive(false);
                llmText.text = "Reference image is missing. Align the camera with the reference image.";
            }
        }
    }

    void Update()
    {
        if (!defectSizeSubmitted && isImageTracked) // Check if defect size has not been submitted
        {
            RaycastHit hit;
            Vector3 rayDirection = cameraTransform.forward;
            float rayLength = 7f; // Adjust as needed

            // Debug Ray (visible in Scene view)
            Debug.DrawRay(cameraTransform.position, rayDirection * rayLength, Color.red);

            if (Physics.Raycast(cameraTransform.position, rayDirection, out hit, rayLength))
            {
                if (hit.collider.tag == "Defect")
                {
                    llmText.text = "Defect present. Please enter the size of the defect in the textbox.";
                    StartCoroutine(ActivateInputFieldsAfterDelay(1f));
                }
            }
        }
    }

    // Coroutine for activating input fields after a delay
    IEnumerator ActivateInputFieldsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay); // Wait for the specified delay
        sizeInputField.gameObject.SetActive(true);
        sizeSubmitButton.gameObject.SetActive(true);
    }

    private void OnSizeSubmitted()
    {
        defectSizeSubmitted = true;
        defectSize = sizeInputField.text;
        sizeInputField.gameObject.SetActive(false);
        sizeSubmitButton.gameObject.SetActive(false);

        // Check if the button is indeed deactivated
        if (!sizeSubmitButton.gameObject.activeSelf)
        {
            defectSizeSubmitted = true;
            llmText.text = $"I think you should FAIL this indication. Size of defect: <color=#FFA500>{defectSize}mm</color> and defect is <color=#FFA500>{defectType}</color>.";
        }
    }

    private void DeactivateKeyboard()
    {
        if (TouchScreenKeyboard.isSupported)
        {
            TouchScreenKeyboard.hideInput = true;
        }
    }
}
