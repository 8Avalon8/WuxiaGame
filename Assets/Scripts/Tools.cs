using System;
using System.Collections;
using System.Collections.Generic;

public static class Tools {

    public static List<T> RandomSortList<T>(List<T> ListT)
    {
        Random random = new Random(GetRandomSeed());
        List<T> newList = new List<T>();
        foreach (T item in ListT)
        {
            newList.Insert(random.Next(newList.Count), item);
        }
        return newList;
    }

    static int GetRandomSeed()
    {
        byte[] bytes = new byte[4];
        System.Security.Cryptography.RNGCryptoServiceProvider rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
        rng.GetBytes(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }
}
