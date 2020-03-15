using UnityEngine;

public class MainMenu : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if(SoundManager.instance != null)
        {
            SoundManager.instance.StopAllSounds();
        }
    }


}
