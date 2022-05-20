using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shakes the screen when a unit attacks another unit.
/// </summary>
public class CameraShake : MonoBehaviour
{
    // Shakes the screen when a unit attacks another unit.
    public IEnumerator ShakeCamera(float duration, float strength, Vector3 direction)
    {
        float tempStrength = strength;

        if (strength > 10)
            strength = 10;

        Vector3 startPos = transform.position;
        //Vector3 endPos = new Vector3(direction.x, 0, direction.z) * (strength / 2);

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float xPos = Random.Range(-0.1f, 0.1f) * strength;
            float zPos = Random.Range(-0.1f, 0.1f) * strength;

            Vector3 newPos = new Vector3(transform.position.x + xPos, transform.position.y, transform.position.z + zPos);

            transform.position = Vector3.Lerp(transform.position, newPos, 0.15f);

            elapsedTime += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }

        transform.position = startPos;
    }
}
