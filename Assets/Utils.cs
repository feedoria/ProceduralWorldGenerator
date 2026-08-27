using UnityEngine;

public static class Utils 
{
    // The Fractal Brownian Motion function 
    // the name fBM is also used as a convention in maths 
    public static float fBM(float x, float y, int oct, float persistance)
    {
        float total = 0; // total heightvalue that I'm calculating 
        float frequency = 1; // how close the waves are together -> changes the octaves in this case 
        float amplitude = 1; 
        float maxValue = 0; // the addition of each amplitude that is used in each octave 

        for (int i = 0; i < oct; i++)
        {
            total += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistance; // the persistance value is gonna get smaller and smaller 
            frequency *= 2; // 2 is hardcoded must experiment a bit 
        }
        
        return total / maxValue; // doesnt matter how many octaves there are its not gonna be a stupidly high value for the terrain 
    }
}
