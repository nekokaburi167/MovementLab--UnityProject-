using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class GunSystem : MonoBehaviour
{
    //Gun Stats
    public float damage;
    public float fireInterval, bulletSpread, fireRange, reloadTime, timeBetweenShots;
    public int magazineSize, bulletPerTap;
    public bool canHold;
    public int bulletsRemain, bulletShot;

    //condition checks
    private bool shooting, readyToShoot, reloading;

    //Reference
    public Camera playerCam;
    public Transform firePoint;
    public RaycastHit hit;
    public LayerMask enemyLayer;
    
    
    private void Awake()
    {
        bulletsRemain = magazineSize;
        readyToShoot = true;
    }

    private void InputHandler()
    {
        if (canHold)
            shooting = Input.GetKey(KeyCode.Mouse0);
        else
            shooting = Input.GetKeyDown(KeyCode.Mouse0);

        if (Input.GetKeyDown(KeyCode.R) && bulletsRemain < magazineSize && !reloading)
            Reload();

        if (readyToShoot && shooting && !reloading && bulletsRemain > 0)
        {
            bulletShot = bulletPerTap;
            Shoot();
        }
    }
    
    private void Update()
    {
        InputHandler();
    }
    
    private void Shoot()
    {
        readyToShoot = false;

        //bullet spread
        float xSpread = Random.Range(-bulletSpread, bulletSpread);
        float ySpread = Random.Range(-bulletSpread, bulletSpread);

        Vector3 bulletHitDirection = playerCam.transform.forward + new Vector3(xSpread, ySpread, 0);
        
        //fire raycast 
        if (Physics.Raycast(playerCam.transform.position, bulletHitDirection, out hit, fireRange, enemyLayer))
        {
            Debug.DrawRay(playerCam.transform.position, bulletHitDirection, Color.magenta);

            // if (hit.collider.CompareTag("Enemy"))
            // {
            //     
            // }
        }

        bulletsRemain--;
        bulletShot--;

        Invoke("Shoot", timeBetweenShots);

        if (bulletShot > 0 && bulletsRemain > 0) 
            Invoke("ResetShot", fireInterval);
    }

    private void Reload()
    {
        reloading = true;
        Invoke("reloadFinished", reloadTime);
    }

    private void reloadFinished()
    {
        bulletsRemain = magazineSize;
        reloading = false;
    }

    void ResetShot()
    {
        readyToShoot = true;
    }
}
