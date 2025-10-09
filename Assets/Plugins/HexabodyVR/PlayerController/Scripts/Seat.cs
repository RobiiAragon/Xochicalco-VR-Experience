using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Seat : MonoBehaviour
{
    [SerializeField]
    private Transform seatPosition;

    private void Reset()
    {
        // Asegurarse de que el BoxCollider sea un trigger
        var collider = GetComponent<BoxCollider>();
        collider.isTrigger = true;

        // Etiquetar el objeto como "Seat"
        gameObject.tag = "Seat";
    }

    private void OnDrawGizmos()
    {
        // Dibujar un gizmo para visualizar el asiento
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider>().size);
    }

    public Vector3 GetSeatPosition()
    {
        return seatPosition != null ? seatPosition.position : transform.position;
    }
}
