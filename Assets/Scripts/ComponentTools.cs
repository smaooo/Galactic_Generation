using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;

//https://answers.unity.com/questions/530178/how-to-get-a-component-from-an-object-and-add-it-t.html

public static class ComponentTools
{

    public static T AddComponent<T>(this GameObject game, T duplicate) where T : Component
    {
        T target = game.AddComponent<T>();
        foreach (PropertyInfo x in typeof(T).GetProperties())
            if (x.CanWrite)
                x.SetValue(target, x.GetValue(duplicate));
        return target;
    }
}