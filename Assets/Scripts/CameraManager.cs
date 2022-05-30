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
    [Tooltip("How fast the texture will be faded out.")]
    [SerializeField]
    private float fadeTime = 5.0f;
    [Tooltip("The color that will fill the screen during a fade.")]
    [SerializeField]
    private Color fadeColor = new Color(255.0f, 255.0f, 255.0f, 1.0f);
    [Tooltip("How long the screen will remain filled during a fade in.")]
    [SerializeField]
    private float timeBeforeFade;

    [NonSerialized]
    private float alpha = 1.0f;
    [NonSerialized]
    private Texture2D texture;
    [NonSerialized]
    private bool isFadingIn = false;
    [NonSerialized]
    private bool isFadingOut = false;
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

        // Set up the camera fade in.
        texture = new Texture2D(1, 1);
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

    private void StartFading(bool isFadingIn, bool isFadingOut, Options options = null)
    {
        currentTime = 0;

        if (options != null)
        {
            fadeTime = (float)options.fadeTime;

            timeBeforeFade = (float)options.timeBeforeFade;

            if (options.fadeColor != null) fadeColor = options.fadeColor;
        }

        this.isFadingIn = isFadingIn;
        this.isFadingOut = isFadingOut;
    }

    public void FadeIn(Options options = null)
    {
        alpha = 1.0f;
        StartFading(true, false, options);
    }

    public void FadeOut(Options options = null)
    {
        alpha = 0.0f;
        StartFading(false, true, options);
    }

    public void OnGUI()
    {
        if (isFadingIn || isFadingOut)
        {
            ShowBlackScreen();
        }
    }

    private void ShowBlackScreen()
    {
        if (isFadingIn && alpha <= 0.0f)
        {
            isFadingIn = false;

            return;
        }

        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texture);

        if (isFadingIn && timeBeforeFade > 0)
        {
            timeBeforeFade -= Time.deltaTime;

            return;
        }

        if (isFadingOut && alpha >= 1.0f) return;

        CalculateTexture();
    }

    private void CalculateTexture()
    {
        currentTime += Time.deltaTime;

        if (isFadingIn) alpha = 1.0f - currentTime / fadeTime;
        else alpha = currentTime / fadeTime;

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
    public float fadeTime;
    public Color fadeColor;
    public float timeBeforeFade;
}
