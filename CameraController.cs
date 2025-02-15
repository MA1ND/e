using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Bewegung")]
    public float panSpeed = 20f;           // Geschwindigkeit für die Seitenbewegung
    public float zoomSpeed = 20f;          // Geschwindigkeit für den Zoom

    [Header("Rotation")]
    public float rotationSpeed = 100f;     // Geschwindigkeit für die horizontale (Yaw) Rotation
    public float pitchSpeed = 100f;        // Geschwindigkeit für die vertikale (Pitch) Rotation

    [Header("Zoom-Einstellungen")]
    public float minY = 10f;               // Mindesthöhe der Kamera
    public float maxY = 600f;              // Maximalhöhe der Kamera

    [Header("Rotationseinschränkungen")]
    public float minXRotation = 65f;       // Minimaler Pitch (z. B. mehr seitlich)
    public float maxXRotation = 90f;       // Maximaler Pitch (Top-Down: 90°)

    // Interne Variablen zum Speichern der aktuellen Rotationswerte
    private float currentYRotation = 0f;   // Yaw (horizontale Rotation)
    private float currentXRotation = 90f;  // Pitch (vertikale Rotation)

    void Update()
    {
        // --- Panning (Bewegung) mit Pfeiltasten oder WASD ---
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 panMovement = new Vector3(h, 0, v) * panSpeed * Time.deltaTime;
        transform.Translate(panMovement, Space.World);

        // --- Zoom (Mausrad) ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        Vector3 pos = transform.position;
        pos.y -= scroll * zoomSpeed * Time.deltaTime;
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        transform.position = pos;

        // --- Rotation: Nur wenn rechte Maustaste gedrückt ist ---
        if (Input.GetMouseButton(1)) // Rechte Maustaste gedrückt
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            // Yaw (horizontale Rotation): Mausbewegung nach links/rechts
            currentYRotation += mouseX * rotationSpeed * Time.deltaTime;

            // Pitch (vertikale Rotation): Mausbewegung nach oben/unten (invertiert, damit "hochziehen" die Kamera nach oben bewegt)
            currentXRotation += -mouseY * pitchSpeed * Time.deltaTime;
            currentXRotation = Mathf.Clamp(currentXRotation, minXRotation, maxXRotation);
        }

        // --- Setze die neue Rotation ---
        transform.rotation = Quaternion.Euler(currentXRotation, currentYRotation, 0f);
    }
}
