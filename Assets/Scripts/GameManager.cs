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
    public int currentTeam;

    [SerializeField]
    private GameObject unitsOnMap;
    [SerializeField]
    private GameObject team1;
    [SerializeField]
    private GameObject team2;

    [NonSerialized]
    private GameObject unitDisplayed;
    [HideInInspector]
    public GameObject highlightedTile;

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
    private int highlightedTileX;
    /// <summary>
    /// The current tile that the mouse is hovering over.
    /// </summary>
    [NonSerialized]
    private int highlightedTileZ;

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
    private Canvas canvasGameOver;

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
    private GameObject playerTurnMessage;

    [NonSerialized]
    private Animator playerTurnAnim;
    [NonSerialized]
    private TMP_Text playerTurnText;

    #endregion


    #region Unity Functions

    private void Start()
    {
        mapManager = GetComponent<MapManager>();

        currentTeam = 0;

        displayingUnitInfo = false;

        currentPathToCursor = new List<Node>();
        currentPathExists = false;

        playerTurnAnim = playerTurnMessage.GetComponent<Animator>();
        playerTurnText = playerTurnMessage.GetComponentInChildren<TextMeshProUGUI>();

        UpdateUICurrentPlayer();
        UpdateUITeamHealthBarColour();
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

    private GameObject GetCurrentTeam(int teamNumber)
    {
        GameObject team = null;

        if (teamNumber == 0)
            team = team1;
        else if (teamNumber == 1)
            team = team2;

        return team;
    }

    private void UpdateUICurrentPlayer()
    {
        textCurrentPlayer.SetText("Current Player's Turn: Player " + (currentTeam + 1).ToString());
    }

    /// <summary>
    /// Updates the cursor's position on the UI.
    /// </summary>
    private void UpdateUICursor()
    {
        //If the cursor is hovering over a tile, highlight it.
        if (hit.transform.CompareTag("Tile"))
        {
            if (highlightedTile == null)
                HighlightTile(hit.transform.gameObject);
            else if (highlightedTile != hit.transform.gameObject)
            {
                highlightedTileX = highlightedTile.GetComponent<ClickableTile>().tileX;
                highlightedTileZ = highlightedTile.GetComponent<ClickableTile>().tileZ;

                mapManager.quadUICursor[highlightedTileX, highlightedTileZ].GetComponent<MeshRenderer>().enabled = false;

                HighlightTile(hit.transform.gameObject);

            }
        }
        else if (hit.transform.CompareTag("Unit"))
        {
            if (highlightedTile == null)
                HighlightTile(hit.transform.parent.gameObject);
            else if (highlightedTile != hit.transform.gameObject)
            {
                if (hit.transform.parent.gameObject.GetComponent<Unit>().movementQueue.Count == 0)
                {
                    highlightedTileX = highlightedTile.GetComponent<ClickableTile>().tileX;
                    highlightedTileZ = highlightedTile.GetComponent<ClickableTile>().tileZ;

                    mapManager.quadUICursor[highlightedTileX, highlightedTileZ].GetComponent<MeshRenderer>().enabled = false;

                    HighlightTile(hit.transform.parent.gameObject);
                }
            }
        }
        else
            mapManager.quadUICursor[highlightedTileX, highlightedTileZ].GetComponent<MeshRenderer>().enabled = false;
    }

    private void UpdateUITeamHealthBarColour()
    {
        for (int i = 0; i < numberOfTeams; i++)
        {
            GameObject team = GetCurrentTeam(i);

            if (team == GetCurrentTeam(currentTeam))
            {
                foreach (Transform unit in team.transform)
                    unit.GetComponent<Unit>().healthBar.color = Color.blue;
            }
            else
            {
                foreach (Transform unit in team.transform)
                    unit.GetComponent<Unit>().healthBar.color = Color.red;
            }
        }
    }

    private void HighlightTile(GameObject tile)
    {
        if (hit.transform.CompareTag("Tile"))
        {
            highlightedTileX = tile.GetComponent<ClickableTile>().tileX;
            highlightedTileZ = tile.GetComponent<ClickableTile>().tileZ;
        }
        else if (hit.transform.CompareTag("Unit"))
        {
            highlightedTileX = tile.GetComponent<Unit>().tileX;
            highlightedTileZ = tile.GetComponent<Unit>().tileZ;
        }

        cursorX = highlightedTileX;
        cursorZ = highlightedTileZ;

        mapManager.quadUICursor[highlightedTileX, highlightedTileZ].GetComponent<MeshRenderer>().enabled = true;

        if (hit.transform.CompareTag("Tile"))
            highlightedTile = tile;
        else if (hit.transform.CompareTag("Unit"))
            highlightedTile = tile.GetComponent<Unit>().occupiedTile;
    }

    private void PrintVictor(string winner)
    {
        canvasGameOver.enabled = true;
        canvasGameOver.GetComponentInChildren<TextMeshProUGUI>().SetText(winner);
    }

    #endregion


    #region Coroutines

    public IEnumerator CheckVictor(GameObject attacker, GameObject defender)
    {
        while (attacker.GetComponent<Unit>().combatQueue.Count != 0)
            yield return new WaitForEndOfFrame();

        while (defender.GetComponent<Unit>().combatQueue.Count != 0)
            yield return new WaitForEndOfFrame();

        if (team1.transform.childCount == 0)
            PrintVictor("Victor: Team Two!");
        else if (team2.transform.childCount == 0)
            PrintVictor("Victor: Team One!");
    }

    #endregion
}
