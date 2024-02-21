using System.Collections;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;

public class TheStack : MonoBehaviour
{
    public Color32[] gameColors = new Color32[4];
    public Material stackMat;

    private const float BOUNDS_SIZE = 3.5f;
    private const float STACK_MOVING_SPEED = 5.0f;
    private const float ERROR_MARGIN = 0.1f;
    private const float STACK_BOUNDS_GAIN = 0.25f;
    private const float COMBO_START_GAIN = 3;

    private GameObject[] theStack;
    private Vector2 stackBounds = new Vector2(BOUNDS_SIZE, BOUNDS_SIZE);

    private int stackIndex;
    private int scoreCount = 0;
    private int combo = 0;

    private float tileTransition = 0.0f;
    private float tileSpeed = 2.5f;
    private float secondaryPosition;

    private bool isMovingOnX = true;
    private bool gameOver = false;

    private Vector3 desirePosition;
    private Vector3 lastTilePosition;

    // Start is called before the first frame update
    void Start()
    {
        theStack = new GameObject[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            theStack[i] = transform.GetChild(i).gameObject;
            ColerMesh(theStack[i].GetComponent<MeshFilter>().mesh);
        }

        stackIndex = transform.childCount - 1;
    }

    private void CreateRubble(Vector3 pos, Vector3 scale)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.AddComponent<Rigidbody>();

        go.GetComponent<MeshRenderer>().material = stackMat;
        ColerMesh(go.GetComponent<MeshFilter>().mesh);
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (PlaceTile())
            {
                SpawnTile();
                scoreCount++;
            }
            else
            {
                EndGame();
            }
        }
        MoveTile();

        transform.position = Vector3.Lerp(transform.position, desirePosition, STACK_MOVING_SPEED * Time.deltaTime);
        //카메라무빙
    }

    private void MoveTile()
    {
        if (gameOver)
            return;


        tileTransition += Time.deltaTime * tileSpeed;
        if (isMovingOnX)
        {
            theStack[stackIndex].transform.localPosition = new Vector3(Mathf.Sin(tileTransition) * BOUNDS_SIZE, scoreCount, secondaryPosition);
        }
        else
            theStack[stackIndex].transform.localPosition = new Vector3(secondaryPosition, scoreCount, Mathf.Sin(tileTransition) * BOUNDS_SIZE);
    }

    private void SpawnTile()
    {
        lastTilePosition = theStack[stackIndex].transform.localPosition;
        stackIndex--;
        if (stackIndex < 0)
            stackIndex = transform.childCount - 1;

        desirePosition = (Vector3.down) * scoreCount;
        theStack[stackIndex].transform.localPosition = new Vector3(0, scoreCount, 0);
        theStack[stackIndex].transform.localScale = new Vector3(stackBounds.x, 1, stackBounds.y);

        //ColorMesh(theStack[Stack].GetComponent<MeshFilter>().mesh);
    }

    private void ColerMesh(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Color32[] color = new Color32[vertices.Length];
        float f = Mathf.Sin(scoreCount * 0.25f);

        for (int i = 0; i < vertices.Length; i++)
            color[i] = Lerp4(gameColors[0], gameColors[1], gameColors[2], gameColors[3], f);

        mesh.colors32 = color;
    }

    private bool PlaceTile()
    {
        Transform moveT = theStack[stackIndex].transform;

        if (isMovingOnX) //타일 자르기
        {
            float deltaX = lastTilePosition.x - moveT.position.x;//자른거 

            if (Mathf.Abs(deltaX) > ERROR_MARGIN)
            {
                combo = 0;
                stackBounds.x -= Mathf.Abs(deltaX);//잘라낸부분이 없어졌으니 그부분 제외하고 기억
                if (stackBounds.x <= 0)
                    return false;

                float middle = (lastTilePosition.x + moveT.localPosition.x) / 2;//새로운 중심점 계산
                moveT.localScale = (new Vector3(stackBounds.x, 1, stackBounds.y));

                moveT.localPosition = new Vector3(middle, scoreCount, lastTilePosition.z);//
                float halfScale = deltaX / 2;

                CreateRubble
                    (
                    new Vector3((deltaX < 0)
                        ? moveT.position.x + stackBounds.x / 2 + halfScale
                        : moveT.position.x - (stackBounds.x / 2 + halfScale)
                        , moveT.position.y
                        , moveT.position.z),
                    new Vector3(Mathf.Abs(deltaX), 1, moveT.localScale.z)
                    );

            }
            else
            {
                if (combo > COMBO_START_GAIN)
                {
                    //콤보보너스
                    stackBounds.x += STACK_BOUNDS_GAIN;
                    if (stackBounds.x > BOUNDS_SIZE)
                        stackBounds.x = BOUNDS_SIZE;

                    float middle = lastTilePosition.x + moveT.localPosition.x / 2;
                    moveT.localScale = (new Vector3(stackBounds.x, 1, stackBounds.y));
                    moveT.localPosition = new Vector3(middle - (lastTilePosition.x / 2), scoreCount, lastTilePosition.z);
                }
                combo++;
                moveT.localPosition = new Vector3(lastTilePosition.x, scoreCount, lastTilePosition.z);
            }

        }
        else
        {
            float deltaZ = lastTilePosition.z - moveT.position.z;
            if (Mathf.Abs(deltaZ) > ERROR_MARGIN)
            {
                combo = 0;
                stackBounds.y -= Mathf.Abs(deltaZ);
                if (stackBounds.y <= 0)
                    return false;

                float middle = (lastTilePosition.z + moveT.localPosition.z) / 2; //새로운 중심점
                moveT.localScale = (new Vector3(stackBounds.x, 1, stackBounds.y));

                moveT.localPosition = new Vector3(lastTilePosition.x, scoreCount, middle);
                float halfScale = deltaZ / 2;

                CreateRubble
                   (
                   new Vector3(moveT.position.x, moveT.position.y
                       , (deltaZ < 0)
                       ? moveT.position.z + stackBounds.y / 2 + halfScale
                       : moveT.position.z - (stackBounds.y / 2 + halfScale)
 ),  //
                   new Vector3(moveT.localScale.x, 1, Mathf.Abs(deltaZ))
                   );
            }
            else
            {
                if (combo > COMBO_START_GAIN)
                {
                    if (stackBounds.y > BOUNDS_SIZE)
                        stackBounds.y = BOUNDS_SIZE;

                    stackBounds.y += STACK_BOUNDS_GAIN;
                    float middle = lastTilePosition.z + moveT.localPosition.z / 2;
                    moveT.localScale = (new Vector3(stackBounds.x, 1, stackBounds.y));
                    moveT.localPosition = new Vector3(middle - (lastTilePosition.x), scoreCount, middle - (lastTilePosition.z / 2));
                }
                combo++;
                moveT.localPosition = new Vector3(lastTilePosition.x, scoreCount, lastTilePosition.z);
            }
        }
        secondaryPosition = (isMovingOnX)
            ? moveT.localPosition.x
            : moveT.localPosition.z;
        isMovingOnX = !isMovingOnX;

        return true;
    }

    private Color32 Lerp4(Color32 a, Color32 b, Color32 c, Color32 d, float t)
    {
        if (t < 0.33f)
            return Color.Lerp(a, b, t / 0.33f);
        else if (t < 0.66f)
            return Color.Lerp(b, c, (t - 0.33f) / 0.33f);
        else
            return Color.Lerp(c, d, (t - 0.66f) / 0.66f);
    }

    private void EndGame()
    {
        Debug.Log("lose");
        gameOver = true;
        theStack[stackIndex].AddComponent<Rigidbody>();
    }
}
