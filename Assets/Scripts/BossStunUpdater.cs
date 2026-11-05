using UnityEngine;

public class BossStunUpdater : MonoBehaviour
{
    void Update()
    {
        BossStunSystem.UpdateStun();
    }
}
