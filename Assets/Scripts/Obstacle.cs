using System;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public float minSizeX = 0.25f;
    public float maxSizeX = 2.0f;
    public float minSizeY = 0.25f;
    public float maxSizeY = 2.0f;
    public float minSpeed = 50.0f;
    public float maxSpeed = 150.0f;
    public GameObject bounceEffectPrefab;

    public float maxSpinSpeed = 10f;
    Rigidbody2D rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {


        float randomSizeX = UnityEngine.Random.Range(minSizeX, maxSizeX);
        float randomSizeY = UnityEngine.Random.Range(minSizeY, maxSizeY);
        transform.localScale = new Vector3(randomSizeX, randomSizeY, 1);

        float randomMass = (float)Math.Pow(randomSizeX * randomSizeY ,0.5f);

        rb = GetComponent<Rigidbody2D>();

        float randomSpeed = UnityEngine.Random.Range(minSpeed, maxSpeed)/randomMass;
        Vector2 randomDirection = UnityEngine.Random.insideUnitCircle;

        rb.AddForce(randomDirection * randomSpeed);

        float randomTorque = UnityEngine.Random.Range(-maxSpinSpeed, maxSpinSpeed);
        rb.AddTorque(randomTorque);
    }

    // Update is called once per frame
    void Update()
    {

    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        Vector2 contactPoint = collision.GetContact(0).point;
        GameObject bounceEffect = Instantiate(bounceEffectPrefab, contactPoint, Quaternion.identity);

        // Destroy the effect after 1 second
        Destroy(bounceEffect, 1f);
    }
}
