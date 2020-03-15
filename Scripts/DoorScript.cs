using UnityEngine;
using UnityEngine.AI;
using System;

public class DoorScript : MonoBehaviour
{
    public static event Func<int> OnEnterDoorTriggerEvent;
    public static event Action<int> OnUnlockDoorTriggerEvent;

    public string doorOpenSound;
    public string doorLockedSound;

    public bool isLocked;
    public int clearanceLevel;
    public int bypassRequired = 0;
    public bool checkParent;

    public bool isLockedPemanently = false;
    public bool alwaysCarve = false;

    [HideInInspector]
    public bool isOpenPermenantly = false; //set this with animation event

    Animator[] doorAnimators;
    NavMeshObstacle[] navMeshObstacles;

    bool drawGizmo = false;

    void Start()
    {
        if(checkParent)
        {
            doorAnimators = GetComponentsInParent<Animator>();
            navMeshObstacles = GetComponentsInParent<NavMeshObstacle>();
        }
        else
        {
            doorAnimators = GetComponentsInChildren<Animator>();
            navMeshObstacles = GetComponentsInChildren<NavMeshObstacle>();
        }

        if (isLocked || isLockedPemanently || alwaysCarve)
        {
            SetAlwaysCarve(alwaysCarve);
        }

    }

    private void AnimateOpenDoor()
    {
        if (doorAnimators.Length > 0)
        {
            for (int i = 0; i < doorAnimators.Length; i++)
            {
                Animator doorAnimator = doorAnimators[i];

                SoundManager.instance.PlaySound(doorOpenSound);
                doorAnimator.SetBool("IsActivated", true);
            }
        }
    }

    private void AnimateCloseDoor()
    {
        if (doorAnimators.Length > 0)
        {
            for (int i = 0; i < doorAnimators.Length; i++)
            {
                Animator doorAnimator = doorAnimators[i];

                SoundManager.instance.PlaySound(doorOpenSound);
                doorAnimator.SetBool("IsActivated", false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        drawGizmo = true;

        if (isOpenPermenantly == false)
        {
            if (isLocked && isLockedPemanently == false)
            {
                if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
                {
                    int clearanceLevel = OnEnterDoorTriggerEvent.Invoke();
                    CheckClearanceLevel(clearanceLevel);
                }
            }

            if (isLocked == false && isLockedPemanently == false)
            {
                AnimateOpenDoor();

                SetAlwaysCarve(alwaysCarve);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        //if(isLocked)
        //{
        //    doorAnimator.SetBool("IsActivated", false);
        //    SoundManager.instance.PlaySound(doorSound);
        //}
    }

    private void OnTriggerExit(Collider other)
    {
        if (isOpenPermenantly == false)
        {
            if (isLocked == false && isLockedPemanently == false)
            {
                AnimateCloseDoor();

                SetAlwaysCarve(alwaysCarve);
            }
        }
        
    }

    public void CheckClearanceLevel(int level)
    {
        if(level >= clearanceLevel)
        {
            isLocked = false;

        }
        else
        {
            if (OnUnlockDoorTriggerEvent != null)
            {
                OnUnlockDoorTriggerEvent.Invoke(clearanceLevel);
            }

            SoundManager.instance.PlaySound(doorLockedSound);

            isLocked = true;
        }
    }

    public void SetLockPermenantly(bool setLock)
    {
        isLockedPemanently = setLock;

        if (setLock == false)
        {
            //play unlock door sound
            SoundManager.instance.PlaySound(doorOpenSound);
        }
        else
        {
            if (doorAnimators.Length > 0)
            {
                for (int i = 0; i < doorAnimators.Length; i++)
                {
                    Animator doorAnimator = doorAnimators[i];

                    if(doorAnimator.GetBool("IsActivated") == true)
                    {
                        SoundManager.instance.PlaySound(doorOpenSound);
                        doorAnimator.SetBool("IsActivated", false);
                    }
                    else
                    {
                        //play locked door sound
                    }

                }
            }

            SetAlwaysCarve(true);
        }

    }

    public void SetAlwaysCarve(bool carve)
    {
        alwaysCarve = carve;

        if(carve)
        {
            if (navMeshObstacles.Length > 0)
            {
                for (int i = 0; i < navMeshObstacles.Length; i++)
                {
                    navMeshObstacles[i].carving = true;
                    navMeshObstacles[i].carvingMoveThreshold = 0.1f;
                    navMeshObstacles[i].carvingTimeToStationary = 0.01f;

                }
            }
        }
        else
        {
            if (navMeshObstacles.Length > 0)
            {
                for (int i = 0; i < navMeshObstacles.Length; i++)
                {
                    navMeshObstacles[i].carving = false;
                }
            }
        }
    }

    public void OpenPermenantly()
    {
        isOpenPermenantly = true;

        AnimateOpenDoor();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        if (drawGizmo)
        {
            Gizmos.DrawWireCube(transform.position, Vector3.one * 20f);
        }
    }
}
