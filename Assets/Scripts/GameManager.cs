using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    #region Declarations

    [Header("Components")]

    [SerializeField]
    private MapManager mapManager;

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
    private Image imageUnitPortrait;
    [SerializeField]
    private TMP_Text textUnitName;
    [SerializeField]
    private TMP_Text textUnitHealth;
    [SerializeField]
    private TMP_Text textUnitAttackDamage;
    [SerializeField]
    private TMP_Text textUnitAttackRange;
    [SerializeField]
    private TMP_Text textUnitMoveSpeed;

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
        currentTeam = 0;

        displayingUnitInfo = false;

        currentPathToCursor = new List<Node>();
        currentPathExists = false;

        playerTurnAnim = playerTurnMessage.GetComponent<Animator>();
        playerTurnText = playerTurnMessage.GetComponentInChildren<TextMeshProUGUI>();

        PrintCurrentTeam();
        UpdateUITeamHealthBarColour();
    }

    private void Update()
    {
        //Always keep track of the cursor's position.
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if(Physics.Raycast(ray, out hit))
        {
            UpdateUICursor();
            UpdateUIUnit();

            if (mapManager.selectedUnit != null &&
                mapManager.selectedUnit.GetComponent<Unit>().movementState == MovementState.Selected &&
                mapManager.selectedUnitMoveRange.Contains(mapManager.graph[cursorX, cursorZ]))
            {
                if (cursorX != mapManager.selectedUnit.GetComponent<Unit>().tileX ||
                    cursorZ != mapManager.selectedUnit.GetComponent<Unit>().tileZ)
                {
                    if (!currentPathExists && mapManager.selectedUnit.GetComponent<Unit>().movementQueue.Count == 0)
                    {
                        currentPathToCursor = mapManager.GeneratePathTo(cursorX, cursorZ);

                        pathToX = cursorX;
                        pathToZ = cursorZ;

                        if (currentPathToCursor.Count != 0)
                        {
                            for (int i = 0; i < currentPathToCursor.Count; i++)
                            {
                                int nodeX = currentPathToCursor[i].x;
                                int nodeZ = currentPathToCursor[i].z;

                                if (i == 0)
                                {
                                    GameObject quad = mapManager.quadUIUnitPath[nodeX, nodeZ];
                                    quad.GetComponent<Renderer>().material = uICursor;
                                }
                                else if (i != 0 && (i + 1) != currentPathToCursor.Count)
                                    DrawUnitPath(nodeX, nodeZ, i);
                                else if (i == currentPathToCursor.Count - 1)
                                    DrawUnitPathArrow(nodeX, nodeZ, i);

                                mapManager.quadUIUnitPath[nodeX, nodeZ].GetComponent<Renderer>().enabled = true;
                            }
                        }

                        currentPathExists = true;
                    }
                    else if (pathToX != cursorX || pathToZ != cursorZ)
                    {
                        if (currentPathToCursor.Count != 0)
                        {
                            for (int i = 0; i < currentPathToCursor.Count; i++)
                            {
                                int nodeX = currentPathToCursor[i].x;
                                int nodeZ = currentPathToCursor[i].z;

                                mapManager.quadUIUnitPath[nodeX, nodeZ].GetComponent<Renderer>().enabled = false;
                            }
                        }

                        currentPathExists = false;
                    }
                }
                else if (cursorX == mapManager.selectedUnit.GetComponent<Unit>().tileX &&
                    cursorZ == mapManager.selectedUnit.GetComponent<Unit>().tileZ)
                {
                    mapManager.DisableQuadUIUnitMovement();
                    currentPathExists = false;
                }
            }
        }
    }

    #endregion


    #region Custom Functions

    public void EndTurn()
    {
        if (mapManager.selectedUnit == null)
        {
            SwitchCurrentTeam();

            if (currentTeam == 1)
            {
                playerTurnAnim.SetTrigger("Slide Left");
                playerTurnText.SetText("Player Two's Turn");
            }
            else if (currentTeam == 0)
            {
                playerTurnAnim.SetTrigger("Slide Right");
                playerTurnText.SetText("Player One's Turn");
            }

            UpdateUITeamHealthBarColour();
            PrintCurrentTeam();
        }
    }

    private GameObject GetCurrentTeam(int teamNumber)
    {
        GameObject team = null;

        if (teamNumber == 0)
            team = team1;
        else if (teamNumber == 1)
            team = team2;

        return team;
    }

    private void SwitchCurrentTeam()
    {
        ResetTeam(GetCurrentTeam(currentTeam));
        currentTeam++;

        if (currentTeam == numberOfTeams)
            currentTeam = 0;
    }

    private void ResetTeam(GameObject team)
    {
        foreach (Transform unit in team.transform)
        {
            unit.GetComponent<Unit>().path = null;
            unit.GetComponent<Unit>().movementState = MovementState.Unselected;
            unit.GetComponent<Unit>().moveCompleted = false;
            //unit.gameObject.GetComponentInChildren<Renderer>().material = unit.GetComponent<Unit>().unitMat;
            //unit.GetComponent<Unit>().PlayIdleAnim();
        }
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

    private void UpdateUIUnit()
    {
        if (!displayingUnitInfo)
        {
            if (hit.transform.CompareTag("Unit"))
            {
                unitDisplayed = hit.transform.parent.gameObject;
                Unit unit = hit.transform.parent.gameObject.GetComponent<Unit>();

                PrintUnitInfo(unit);
            }
            else if (hit.transform.CompareTag("Tile")
                && hit.transform.GetComponent<ClickableTile>().unitOccupyingTile != null)
            {
                unitDisplayed = hit.transform.GetComponent<ClickableTile>().unitOccupyingTile;
                Unit unit = unitDisplayed.GetComponent<Unit>();

                PrintUnitInfo(unit);
            }
        }
        else if (hit.transform.gameObject.CompareTag("Unit")
            && hit.transform.parent.gameObject != unitDisplayed)
        {
            canvasUnitInfo.enabled = false;
            displayingUnitInfo = false;
        }
        else if (hit.transform.gameObject.CompareTag("Tile"))
        {
            if (hit.transform.GetComponent<ClickableTile>().unitOccupyingTile == null)
            {
                canvasUnitInfo.enabled = false;
                displayingUnitInfo = false;
            }
            else if (hit.transform.GetComponent<ClickableTile>().unitOccupyingTile != unitDisplayed)
            {
                canvasUnitInfo.enabled = false;
                displayingUnitInfo = false;
            }
        }
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

    private void DrawUnitPath(int nodeX, int nodeZ, int i)
    {
        Vector2 prevTile = new Vector2(currentPathToCursor[i - 1].x + 1, currentPathToCursor[i - 1].z + 1);
        Vector2 currTile = new Vector2(currentPathToCursor[i].x + 1, currentPathToCursor[i].z + 1);
        Vector2 nextTile = new Vector2(currentPathToCursor[i + 1].x + 1, currentPathToCursor[i + 1].z + 1);

        Vector2 prevToCurrVector = VectorDirection(prevTile, currTile);
        Vector2 currToNextVector = VectorDirection(currTile, nextTile);

        if (prevToCurrVector == Vector2.right && currToNextVector == Vector2.right)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 270, uIUnitPath);
        else if (prevToCurrVector == Vector2.right && currToNextVector == Vector2.up)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 180, uIUnitPathCurve);
        else if (prevToCurrVector == Vector2.right && currToNextVector == Vector2.down)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 270, uIUnitPathCurve);
        else if (prevToCurrVector == Vector2.left && currToNextVector == Vector2.left)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 90, uIUnitPath);
        else if (prevToCurrVector == Vector2.left && currToNextVector == Vector2.up)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 90, uIUnitPathCurve);
        else if (prevToCurrVector == Vector2.left && currToNextVector == Vector2.down)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 0, uIUnitPathCurve);
        else if (prevToCurrVector == Vector2.up && currToNextVector == Vector2.up)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 0, uIUnitPath);
        else if (prevToCurrVector == Vector2.up && currToNextVector == Vector2.right)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 0, uIUnitPathCurve);
        else if (prevToCurrVector == Vector2.up && currToNextVector == Vector2.left)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 270, uIUnitPathCurve);
        else if (prevToCurrVector == Vector2.down && currToNextVector == Vector2.down)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 0, uIUnitPath);
        else if (prevToCurrVector == Vector2.down && currToNextVector == Vector2.right)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 90, uIUnitPathCurve);
        else if (prevToCurrVector == Vector2.down && currToNextVector == Vector2.left)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 180, uIUnitPathCurve);
    }

    private void DrawUnitPathArrow(int nodeX, int nodeZ, int i)
    {
        Vector2 prevTile = new Vector2(currentPathToCursor[i - 1].x + 1, currentPathToCursor[i - 1].z + 1);
        Vector2 currTile = new Vector2(currentPathToCursor[i].x + 1, currentPathToCursor[i].z + 1);

        Vector2 prevToCurrVector = VectorDirection(prevTile, currTile);

        if (prevToCurrVector == Vector2.right)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 270, uIUnitPathArrow);
        else if (prevToCurrVector == Vector2.left)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 90, uIUnitPathArrow);
        else if (prevToCurrVector == Vector2.up)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 0, uIUnitPathArrow);
        else if (prevToCurrVector == Vector2.down)
            DrawUnitPathQuad(nodeX, nodeZ, 90, 180, uIUnitPathArrow);
    }

    private void DrawUnitPathQuad(int nodeX, int nodeZ, int rotX, int rotZ, Material mat)
    {
        GameObject quad = mapManager.quadUIUnitPath[nodeX, nodeZ];
        quad.GetComponent<Transform>().rotation = Quaternion.Euler(rotX, 0, rotZ);
        quad.GetComponent<Renderer>().material = mat;
        quad.GetComponent<Renderer>().enabled = true;
    }

    private Vector2 VectorDirection(Vector2 currVector, Vector2 nextVector)
    {
        Vector2 vectorDirection = (nextVector - currVector).normalized;

        if (vectorDirection == Vector2.right)
            return Vector2.right;
        else if (vectorDirection == Vector2.left)
            return Vector2.left;
        else if (vectorDirection == Vector2.up)
            return Vector2.up;
        else if (vectorDirection == Vector2.down)
            return Vector2.down;
        else
        {
            return new Vector2();
        }
    }

    private void PrintCurrentTeam()
    {
        textCurrentPlayer.SetText("Current Player's Turn: Player " + (currentTeam + 1).ToString());
    }

    private void PrintUnitInfo(Unit unit)
    {
        canvasUnitInfo.enabled = true;
        displayingUnitInfo = true;

        imageUnitPortrait.sprite = unit.portrait;
        textUnitName.SetText(unit.name);
        textUnitHealth.SetText(unit.currentHealth.ToString());
        textUnitAttackDamage.SetText(unit.attackDamage.ToString());
        textUnitAttackRange.SetText(unit.attackRange.ToString());
        textUnitMoveSpeed.SetText(unit.moveSpeed.ToString());
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
