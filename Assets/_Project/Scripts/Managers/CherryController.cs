using System.Collections;
using UnityEngine;

public class CherryController : MonoBehaviour
{
    [SerializeField] private GameObject cherryPrefab;
    [SerializeField] private LevelGenerator levelGenerator;
    [SerializeField] private float speed = 3f;     
    [SerializeField] private float spawnDelay = 5f;  
    [SerializeField] private float edgeMargin = 2f;   // tiles outside the level edge
    [SerializeField] private int sortingOrder = 100;  // draw over everything

    private Vector3 centre;
    private float halfWidth, halfHeight;
    private int lastSide = -1;
    private GameObject maskObject;

    private void Start()
    {
        if (levelGenerator == null)
        {
            levelGenerator = FindAnyObjectByType<LevelGenerator>();
        }
        if (cherryPrefab == null || levelGenerator == null)
        {
            Debug.LogError("assign the cherry prefab and make sure a LevelGenerator exists.");
            enabled = false;
            return;
        }

        int rows = levelGenerator.FullRows;
        int cols = levelGenerator.FullCols;

        centre = new Vector3((cols - 1) / 2f, -(rows - 1) / 2f, 0f);
        halfWidth = cols / 2f;
        halfHeight = rows / 2f;

        CreateLevelMask(cols, rows);
        StartCoroutine(SpawnLoop());
    }

    private void OnDestroy()
    {
        if (maskObject != null) Destroy(maskObject);
    }

    private void CreateLevelMask(int cols, int rows)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        // 1 pixel at 1 pixel per unit = a 1 x 1 unit sprite
        Sprite square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        maskObject = new GameObject("CherryLevelMask");
        maskObject.transform.position = centre;
        maskObject.transform.localScale = new Vector3(cols, rows, 1f);

        SpriteMask mask = maskObject.AddComponent<SpriteMask>();
        mask.sprite = square;
    }

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(spawnDelay);

        while (true)
        {
            yield return StartCoroutine(RunCherry());

            yield return new WaitForSeconds(spawnDelay);
        }
    }

    private IEnumerator RunCherry()
    {
        PickPath(out Vector3 start, out Vector3 end);

        GameObject cherry = Instantiate(cherryPrefab, start, Quaternion.identity);
        PrepareRenderers(cherry);

        float duration = Vector3.Distance(start, end) / speed;
        float startTime = Time.time;

        // cherry becomes null if something else destroys it
        while (cherry != null)
        {
            float t = Mathf.Clamp01((Time.time - startTime) / duration);
            cherry.transform.position = Vector3.Lerp(start, end, t);

            if (t >= 1f)
            {
                Destroy(cherry); // outside the level
                break;
            }
            yield return null;
        }
    }


    private void PickPath(out Vector3 start, out Vector3 end)
    {
        int side;
        do { side = Random.Range(0, 4); } while (side == lastSide); // different edge each time
        lastSide = side;

        float along = Random.Range(-0.4f, 0.4f); // random spot along that edge

        switch (side)
        {
            case 0: // left
                start = new Vector3(centre.x - halfWidth - edgeMargin, centre.y + along * halfHeight * 2f, 0f);
                break;
            case 1: // right
                start = new Vector3(centre.x + halfWidth + edgeMargin, centre.y + along * halfHeight * 2f, 0f);
                break;
            case 2: // top
                start = new Vector3(centre.x + along * halfWidth * 2f, centre.y + halfHeight + edgeMargin, 0f);
                break;
            default: // bottom
                start = new Vector3(centre.x + along * halfWidth * 2f, centre.y - halfHeight - edgeMargin, 0f);
                break;
        }

        end = 2f * centre - start;
    }

    private void PrepareRenderers(GameObject cherry)
    {
        foreach (SpriteRenderer sr in cherry.GetComponentsInChildren<SpriteRenderer>())
        {
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            sr.sortingOrder = sortingOrder;
        }
    }
}