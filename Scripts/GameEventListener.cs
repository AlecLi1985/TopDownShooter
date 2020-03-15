using UnityEngine;
using UnityEngine.Events;

public class GameEventListener : MonoBehaviour
{
    public GameEvent Event;
    public UnityEvent Response;

    private void OnEnable()
    {
        
    }

    public void OnEventRaised()
    {
        Response.Invoke();
    }
}
