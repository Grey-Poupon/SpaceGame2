using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpaceship : SpaceShip
{

    public GameObject enemyShipPrefab;
    void Awake()
    {
        ResetTempRoomStats();
        GameManager.Instance.RegisterEnemyShip(this);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void onDestroy()
    {
        if(this.gameObject.activeInHierarchy){
            
            //GameObject newEnemy = Instantiate(enemyShipPrefab,this.GetComponent<Transform>().position,this.GetComponent<Transform>().rotation);
            //GameManager.Instance.RegisterEnemyShip(newEnemy.GetComponent<EnemySpaceship>());
            GameManager.Instance.BuildAShip();
            this.gameObject.SetActive(false);
            
        // Remove this player ship from the GameManager when destroyed.
        //Destroy(this.gameObject);
        }
        
        
    }
}
