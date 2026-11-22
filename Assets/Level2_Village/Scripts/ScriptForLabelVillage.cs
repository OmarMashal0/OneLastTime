using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptForLabelVillage : MonoBehaviour
{
    public GameObject sprite; // Assign the label prefab in the Inspector
    public ParticleSystem  particles; // Assign the parent transform for the labels in the Inspector
    public void Start()
    {
        StartCoroutine(waitForIt());
    }
    IEnumerator waitForIt()
    {
        yield return new WaitForSeconds(1.5f); // Wait for 1 second
        play(); // Play the particle system
    }
    void play()
    {
        sprite.gameObject.SetActive(false); // Activate the label
        particles.Emit(9999);
    }
}
