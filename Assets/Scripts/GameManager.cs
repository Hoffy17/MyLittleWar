using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region Declarations

    [Header("Units & Teams")]

    [SerializeField]
    private int numberOfTeams = 2;
    [SerializeField]
    private int currentTeam;

    [SerializeField]
    private GameObject unitsOnMap;
    [SerializeField]
    private GameObject team1;
    [SerializeField]
    private GameObject team2;

    [NonSerialized]
    private GameObject unitDisplayed;
    [NonSerialized]
    private GameObject tileDisplayed;

    [NonSerialized]
    private bool displayingUnitInfo;

    //[Header("Map")]

    [NonSerialized]
    private MapManager mapManager;

    [NonSerialized]
    private Ray ray;
    [NonSerialized]
    private RaycastHit hit;

    /// <summary>
    /// Cursor information to be passed to the <see cref="MapManager"/>.
    /// </summary>
    [NonSerialized]
    private int cursorX;
    /// <summary>
    /// Cursor information to be passed to the <see cref="MapManager"/>.
    /// </summary>
    [NonSerialized]
    private int cursorZ;
    /// <summary>
    /// The current tile that the mouse is hovering over.
    /// </summary>
    [NonSerialized]
    private int selectedTileX;
    /// <summary>
    /// The current tile that the mouse is hovering over.
    /// </summary>
    [NonSerialized]
    private int selectedTileZ;

    [NonSerialized]
    private List<Node> currentPath;
    [NonSerialized]
    private List<Node> currentPathToCursor;

    [NonSerialized]
    private bool currentPathExists;

    [NonSerialized]
    private int pathToX;
    [NonSerialized]
    private int pathToZ;

    [NonSerialized]
    private GameObject quadNeighbouringUnit;

    [Header("Map Materials")]

    [SerializeField]
    private Material uIUnitPath;
    [SerializeField]
    private Material uIUnitPathCurve;
    [SerializeField]
    private Material uIUnitPathArrow;
    [SerializeField]
    private Material uICursor;

    [Header("UI")]

    [SerializeField]
    private TMP_Text textCurrentPlayer;
    [SerializeField]
    private Canvas canvasDisplayWinner;

    [SerializeField]
    private Canvas canvasUnitInfo;
    [SerializeField]
    private TMP_Text textUnitName;
    [SerializeField]
    private TMP_Text textUnitHealth;
    [SerializeField]
    private TMP_Text textUnitAttackDamage;
    [SerializeField]
    private TMP_Text textUnitAttackRange;
    [SerializeField]
    private TMP_Text textUnitMovement;

    [SerializeField]
    private GameObject playerTurnBlock;

    [NonSerialized]
    private Animator playerTurnAnim;
    [NonSerialized]
    private TMP_Text playerTurnText;

    #endregion


    #region Unity Functions

    private void Start()
    {
        currentTeam = 0;

        displayingUnitInfo = false;

        currentPathToCursor = new List<Node>();
        currentPathExists = false;

        mapManager = GetComponent<MapManager>();
    }

    private void Update()
    {
        //Always keep track of the cursor's position.
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if(Physics.Raycast(ray, out hit))
        {
            UpdateUICursor();
        }
    }

    #endregion


    #region Custom Functions

    public void CheckRemainingUnits(GameObject attacker, GameObject defender)
    {

    }

    /// <summary>
    /// Updates the cursor's position on the UI.
    /// </summary>
    private void UpdateUICursor()
    {
        //If the cursor is hovering over a tile, highlight it.
        if (hit.transform.CompareTag("Tile"))
        {
            if (tileDisplayed == null)
            {
                selectedTileX = hit.transform.gameObject.GetComponent<ClickableTile>().tileX;
                selectedTileZ = hit.transform.gameObject.GetComponent<ClickableTile>().tileZ;

                cursorX = selectedTileX;
                cursorZ = selectedTileZ;

                //mapManager
            }
        }
    }

    #endregion
}
