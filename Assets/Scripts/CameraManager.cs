using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls camera operations, including shake and fading in and out.
/// </summary>
public class CameraManager : MonoBehaviour
{
    #region Declarations

    [Header("Components")]
    [Tooltip("The UIManager script.")]
    [SerializeField]
    private UIManager uIManager;

    [Header("Lerp Settings")]
    [Tooltip("The position of the camera on the main menu.")]
    [SerializeField]
    private Vector3 mainMenuPos;
    [Tooltip("The position of the camera during gameplay.")]
    [SerializeField]
    private Vector3 gamePos;
    [Tooltip("The speed at which the camera lerps from its position on the main menu to its position during gameplay.")]
    [SerializeField]
    private float lerpSpeed;
    [Tooltip("Checks whether the camera should lerp its transform position.")]
    [HideInInspector]
    public bool lerpCam;

    [Header("Fade Settings")]
    [Tooltip("The duration that a texture will be faded in, or out.")]
    [SerializeField]
    private float fadeTime = 5.0f;
    [Tooltip("The color that will be faded to, or from.")]
    [SerializeField]
    private Color fadeColor = new Color(255.0f, 255.0f, 255.0f, 1.0f);
    [Tooltip("How long the screen will remain filled during a fade.")]
    [SerializeField]
    private float timeBeforeFade;
    [Tooltip("The image that the camera fades to, or from.")]
    [NonSerialized]
    private Texture2D texture;
    [Tooltip("The transparency property of the image that is faded to, or from.")]
    [NonSerialized]
    private float alpha = 1.0f;
    [Tooltip("Checks whether the camera should fade in.")]
    [NonSerialized]
    private bool isFadingIn = false;
    [Tooltip("Checks whether the camera should fade out.")]
    [NonSerialized]
    private bool isFadingOut = false;
    [Tooltip("The time that has passed since a fade was called.")]
    [NonSerialized]
    private float currentTime = 0;
    [NonSerialized]
    static private CameraManager instance;
    static public CameraManager Instance
    {
        get
        {
            if (instance == null)
                instance = GameObject.FindObjectOfType<CameraManager>();

            return instance;
        }
    }

    #endregion


    #region Unity Functions

    private void Start()
    {
        // Set the camera's position to its position on the main menu.
        transform.position = mainMenuPos;
        lerpCam = false;

        // Create an image to fade to, or from.
        texture = new Texture2D(1, 1);
        // Set the fade image's colour.
        texture.SetPixel(0, 0, new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha));
        texture.Apply();

        FadeIn();
    }

    private void Update()
    {
        // Lerp the camera's transform to its gameplay position.
        if (lerpCam)
        {
            transform.position = Vector3.Lerp(transform.position, gamePos, lerpSpeed * Time.deltaTime);

            // When the camera is close enough to its gameplay position, stop lerping and start the game.
            if ((transform.position - gamePos).sqrMagnitude < 0.001)
            {
                lerpCam = false;
                uIManager.StartGame();
            }
        }
    }

    #endregion


    #region Custom Functions

    /// <summary>
    /// Fade the camera in from a fully opaque colour.
    /// </summary>
    /// <param name="options">The fade time, fade colour and time before fading.</param>
    public void FadeIn(Options options = null)
    {
        // Set the fade image's transparency as fully opaque and start fading in.
        alpha = 1.0f;
        StartFading(true, false, options);
    }

    /// <summary>
    /// Fade the camera out from a fully transparent colour.
    /// </summary>
    /// <param name="options">The fade time, fade colour and time before fading.</param>
    public void FadeOut(Options options = null)
    {
        // Set the fade image's transparency as fully transparent and start fading out.
        alpha = 0.0f;
        StartFading(false, true, options);
    }

    /// <summary>
    /// Gets the settings for fading and resets the timer, before starting to fade.
    /// </summary>
    /// <param name="isFadingIn"></param>
    /// <param name="isFadingOut"></param>
    /// <param name="options"></param>
    private void StartFading(bool isFadingIn, bool isFadingOut, Options options = null)
    {
        // Reset the timer.
        currentTime = 0;

        // If the options are not null, get their properties.
        if (options != null)
        {
            fadeTime = options.fadeTime;
            fadeColor = options.fadeColor;
            timeBeforeFade = options.timeBeforeFade;
        }

        // Start fading in, or out.
        this.isFadingIn = isFadingIn;
        this.isFadingOut = isFadingOut;
    }

    /// <summary>
    /// The fade event is called from OnGUI.
    /// </summary>
    public void OnGUI()
    {
        if (isFadingIn || isFadingOut)
        {
            ShowBlackScreen();
        }
    }

    /// <summary>
    /// Calculates when and for how long to display the fade image over the camera.
    /// </summary>
    private void ShowBlackScreen()
    {
        // If the camera is fading in and the fade image is fully transparent, the camera is no longer fading in.
        if (isFadingIn && alpha <= 0.0f)
        {
            isFadingIn = false;

            return;
        }

        // Draw the fade image over the entire screen.
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texture);

        // If the camera is fading in and the time before fading is greater than zero, keep displaying the fade image until the timer ends.
        if (isFadingIn && timeBeforeFade > 0)
        {
            timeBeforeFade -= Time.deltaTime;

            return;
        }

        // If the camera is fading out and the fade image is fully opaque, the camera is no longer fading out.
        if (isFadingOut && alpha >= 1.0f)
            return;

        CalculateTexture();
    }

    /// <summary>
    /// This function changes the alpha value of the fade image over time.
    /// </summary>
    private void CalculateTexture()
    {
        currentTime += Time.deltaTime;

        // If the camera is fading in, the fade image's alpha is reduced over the specified fade time.
        if (isFadingIn) 
            alpha = 1.0f - currentTime / fadeTime;
        // If the camera is fading in, the fade image's alpha is increased over the specified fade time.
        else 
            alpha = currentTime / fadeTime;

        // Set the fade image's colour as its alpha value is changed.
        texture.SetPixel(0, 0, new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha));
        texture.Apply();
    }

    #endregion


    #region Coroutines

    /// <summary>
    /// Shakes the screen when a unit attacks another unit.
    /// </summary>
    /// <param name="duration"></param>
    /// <param name="strength"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    public IEnumerator ShakeCamera(float duration, float strength, Vector3 direction)
    {
        float tempStrength = strength;

        if (strength > 10)
            strength = 10;

        Vector3 startPos = transform.position;
        //Vector3 endPos = new Vector3(direction.x, 0, direction.z) * (strength / 2);

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float xPos = UnityEngine.Random.Range(-0.1f, 0.1f) * strength;
            float zPos = UnityEngine.Random.Range(-0.1f, 0.1f) * strength;

            Vector3 newPos = new Vector3(transform.position.x + xPos, transform.position.y, transform.position.z + zPos);

            transform.position = Vector3.Lerp(transform.position, newPos, 0.15f);

            elapsedTime += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }

        transform.position = startPos;
    }

    #endregion
}

public class Options
{
    [Tooltip("The duration that a texture will be faded in, or out.")]
    public float fadeTime;
    [Tooltip("The color that will be faded to, or from.")]
    public Color fadeColor;
    [Tooltip("How long the screen will remain filled during a fade.")]
    public float timeBeforeFade;
}
