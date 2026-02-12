using UnityEngine;

public class editor_Dot : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetComponent<SpriteRenderer>().enabled = false; 
    }

}
