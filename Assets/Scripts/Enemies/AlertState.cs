/// <summary>
/// Estados de alerta de un enemigo con farol.
/// Calma (patrulla normal) -> Sospecha (vio algo) -> Alerta (confirmó al objetivo) -> Captura.
/// </summary>
public enum AlertState
{
    Calm,
    Suspicion,
    Alert,
    Capture
}
