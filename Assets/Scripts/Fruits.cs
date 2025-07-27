using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class Fruits : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   public FruitDropper fruitDropper;
   int maxCollisions = 10;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
     
    public void OnCollisionEnter2D(Collision2D collision){
        if(maxCollisions-- <= 0)
        {
            return;
        }
        if (fruitDropper == null)
        {
            fruitDropper = FindFirstObjectByType<FruitDropper>();
        }
        if(collision.gameObject.tag == "Walls")
        {
            return;
        }
        string name1 = collision.gameObject.name.Replace("(Clone)", "");
        string name2 = this.gameObject.name.Replace("(Clone)", "");
    
        // Debug.Log("Collision detected with: " + collision.gameObject.name + " on " + this.gameObject.name);
        if (name1 != null && name2 != null && name1 == name2)
        {
            // Destroy the fruit when it collides with this GameObject
            GameObject nextFruit = fruitDropper.GetNextFruit(name1);
            Instantiate(nextFruit, transform.position, Quaternion.identity);
            collision.gameObject.SetActive(false);
            this.gameObject.SetActive(false);
            Destroy(collision.gameObject);
            Destroy(gameObject);
        }
    }
}
