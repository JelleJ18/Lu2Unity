using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    float scrollSpeed = 0.2f;
    void Update()
    {
        if(Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            transform.position += Vector3.left * Time.deltaTime * 5;
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            transform.position += Vector3.down * Time.deltaTime * 5;
        }
        else if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            transform.position += Vector3.up * Time.deltaTime * 5;
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            transform.position += Vector3.right * Time.deltaTime * 5;
        }

        if(Input.GetAxis("Mouse ScrollWheel") > 0 && Camera.main.orthographicSize > 1)
        {
            Camera.main.orthographicSize -= scrollSpeed;
        }
        else if(Input.GetAxis("Mouse ScrollWheel") < 0 && Camera.main.orthographicSize < 30)
        {
            Camera.main.orthographicSize += scrollSpeed;
        }
    }
}
