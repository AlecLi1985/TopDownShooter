using UnityEngine;
using UnityEngine.Events;

public class GameLoadTrigger : MonoBehaviour
{
    public int loadID;
    public UnityEvent loadEvents;

    // Start is called before the first frame update
    void Start()
    {
        GameManager.OnLoadCheckpointEvent -= CallLoadEvents;
        GameManager.OnLoadCheckpointEvent += CallLoadEvents;
    }

    void CallLoadEvents(int saveID)
    {
        if(loadID == saveID)
        {
            if (loadEvents != null)
            {
                loadEvents.Invoke();
            }
        }
        
    }

    private void OnDestroy()
    {
        GameManager.OnLoadCheckpointEvent -= CallLoadEvents;
    }
}
