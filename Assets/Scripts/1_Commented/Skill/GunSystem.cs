using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class GunSystem : MonoBehaviour
{
    //Gun Stats
    public float weaponDamage;
    public float fireInterval, bulletSpread, fireRange, reloadTime, timeBetweenShots;
    public int magazineSize, bulletPerTap;
    public bool isAutomatic;
    int bulletsRemain, bulletsShot;

    //condition checks
    private bool isShooting, readyToShoot, isReloading;

    //Reference
    public Camera playerCam;
    public Transform firePoint;
    public RaycastHit bulletHit;
    public LayerMask enemyLayer;
    
    //muzzle flash
    public GameObject muzzleFlashPre;

    private ParticleSystem muzzleFlash;
    
    private void Awake()
    {
        bulletsRemain = magazineSize;
        readyToShoot = true;
        muzzleFlash = muzzleFlashPre.GetComponent<ParticleSystem>();
    }

    private void InputHandler()
    {
        if (isAutomatic)
            isShooting = Input.GetKey(KeyCode.Mouse0);
        else
            isShooting = Input.GetKeyDown(KeyCode.Mouse0);

        if (Input.GetKeyDown(KeyCode.R) && bulletsRemain < magazineSize && !isReloading)
            Reload();

        if (readyToShoot && isShooting && !isReloading && bulletsRemain > 0)
        {
            bulletsShot = bulletPerTap;
            WeaponFire();
        }
    }
    
    private void Update()
    {
        InputHandler();
        
    }
    
    private void WeaponFire()
    {
        readyToShoot = false;

        //bullet spread
        float xSpread = Random.Range(-bulletSpread, bulletSpread);
        float ySpread = Random.Range(-bulletSpread, bulletSpread);

        Vector3 bulletHitDirection = playerCam.transform.forward + new Vector3(xSpread, ySpread, 0);

        //Debug.DrawRay(playerCam.transform.position, playerCam.transform.forward, Color.magenta, 20, false);
        
        //fire raycast 
         if (Physics.Raycast(playerCam.transform.position, bulletHitDirection, out bulletHit, fireRange, enemyLayer))
         {
             Debug.DrawRay(playerCam.transform.position, playerCam.transform.forward, Color.magenta, 20, false);
             
             if (bulletHit.collider.CompareTag("Enemy"))
             {
                 Debug.Log(bulletHit.collider.name);
                 bulletHit.collider.gameObject.SetActive(false);
             }
         }
         
        //muzzle flash
        if (muzzleFlashPre != null)
        {
            //GameObject tempMuzzleFlash = Instantiate(muzzleFlashPre, weaponMuzzle.position, weaponMuzzle.rotation);
            //Destroy(tempMuzzleFlash, t: 2);
            Debug.Log("dd");
            muzzleFlash.Play();
        }

        bulletsRemain--;
        bulletsShot--;

        Invoke("FireReset", timeBetweenShots);

        if (bulletsShot > 0 && bulletsRemain > 0) 
            Invoke("WeaponFire", fireInterval);
    }

    private void Reload()
    {
        isReloading = true;
        Invoke("reloadFinished", reloadTime);
    }

    private void reloadFinished()
    {
        bulletsRemain = magazineSize;
        isReloading = false;
    }

    void FireReset()
    {
        readyToShoot = true;
    }
}
