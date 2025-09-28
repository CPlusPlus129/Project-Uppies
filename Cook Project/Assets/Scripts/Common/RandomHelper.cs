using System;
using System.Collections.Generic;

public class RandomHelper
{
    static HashSet<int> chosenIndecies  = new HashSet<int>();

    public static T PickOne<T>(T[] array)
    {
        if (array == null || array.Length == 0)
            throw new ArgumentException("Array is empty");
        int index = UnityEngine.Random.Range(0, array.Length);
        return array[index];
    }

    /// <summary>
    /// Pick N random elements from an array. With replacement (duplicates allowed).
    /// </summary>
    public static T[] PickWithReplacement<T>(T[] array, int n)
    {
        if (array == null || array.Length == 0)
            throw new ArgumentException("Array is empty");

        T[] result = new T[n];
        for (int i = 0; i < n; i++)
        {
            int index = UnityEngine.Random.Range(0, array.Length);
            result[i] = array[index];
        }
        return result;
    }

    /// <summary>
    /// Pick N random elements from an array. Without replacement (no duplicates).
    /// </summary>
    public static T[] PickWithoutReplacement<T>(T[] array, int n)
    {
        if (array == null || array.Length == 0)
            throw new ArgumentException("Array is empty");
        if (n > array.Length)
            throw new ArgumentException("Cannot pick more elements than array length");

        chosenIndecies.Clear();
        int m = array.Length;

        // Floyd's algorithm
        for (int k = m - n; k < m; k++)
        {
            int r = UnityEngine.Random.Range(0, k + 1);

            if (!chosenIndecies.Add(r))
            {
                chosenIndecies.Add(k);
            }
        }

        T[] result = new T[n];
        int i = 0;
        foreach (int index in chosenIndecies)
        {
            result[i++] = array[index];
        }
        return result;
    }
}