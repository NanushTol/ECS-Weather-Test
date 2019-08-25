using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeTemperature : MonoBehaviour
{
    public Vector3 MouseClickPosition;
    public bool AddTemp;
    public bool RemoveTemp;
    public GameObject Heater;
    public GameObject Coller;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
        {
            RaycastHit hit;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
                Instantiate(Heater, hit.point, Quaternion.identity);
        }
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButton(1))
        {
            RaycastHit hit;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
                Instantiate(Coller, hit.point, Quaternion.identity);
        }
    }
}
