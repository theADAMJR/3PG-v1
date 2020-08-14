using System;
using System.Collections.Generic;

public static class Deps {
    private static Dictionary<Type, object> deps = new Dictionary<Type, object>();

    public static T Get<T>() where T : new() {
        return (T)(deps[typeof(T)] ?? Add(new T()));
    }

    private static T Add<T>(T instance) where T : new() {
        deps.Add(typeof(T), instance);
        return instance;
    }
}