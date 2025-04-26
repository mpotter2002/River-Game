using UnityEngine;

public class AutoScrollCamera : MonoBehaviour
{
    public float scrollSpeed = 2f;

    void Update()
    {
        transform.position += new Vector3(0, scrollSpeed * Time.deltaTime, 0);
    }
}


