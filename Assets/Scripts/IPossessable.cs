using UnityEngine;

public interface IPossessable
{
    void OnPossess();
    void OnUnpossess();
    void ReceiveInput(float xaxis, float yaxis, bool actionPressed);
}
