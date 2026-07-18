using UnityEngine;

/// <summary>Receives directed alerts such as a Screamer's call.</summary>
public interface IZombieAlertReceiver
{
    void ReceiveZombieAlert(Vector3 alertPosition, Transform targetHint);
}
