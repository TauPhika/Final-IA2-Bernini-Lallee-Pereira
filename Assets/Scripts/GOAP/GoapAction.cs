using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Esta parte transformado en utilizar Funcs, pero por ahora hay una mezcla
/// </summary>
public class GoapAction
{
    public Dictionary<string, bool> preconditions { get; private set; }

    public Func<GoapState, bool> Preconditions = delegate { return true; };
    public Dictionary<string, bool> effects { get; private set; }

    public Func<GoapState, GoapState> Effects;

    public float Cost { get; private set; }

    public ItemType item;
    public string Name { get; private set; }

    public GoapAction(string name)
    {

        this.Name = name;
        Cost = 1f;
        preconditions = new Dictionary<string, bool>();
        effects = new Dictionary<string, bool>();

        //Para que funcione en la mezcla se hizo esto, pero se le podria settear a cada Action su propia logica de effect
        Effects = (s) =>
        {
            foreach (var item in effects)
            {
                s.worldState.values[item.Key] = item.Value;
            }
            return s;
        };
    }

    public GoapAction SetCost(float cost)
    {
        if (cost < 1f)
        {
            Debug.Log(string.Format("Warning: Using cost < 1f for '{0}' could yield sub-optimal results", Name));
        }
        this.Cost = cost;
        return this;
    }
    public GoapAction Pre(string s, bool value)
    {
        preconditions[s] = value;
        return this;
    }

    public GoapAction Pre(Func<GoapState, bool> p)
    {
        Preconditions = p;
        return this;
    }
    public GoapAction Effect(string s, bool value)
    {
        effects[s] = value;
        return this;
    }

    public GoapAction Effect(Func<GoapState, GoapState> e)
    {
        Effects =e;
        return this;
    }

    public GoapAction SetItem(ItemType type)
    {
        item = type;
        return this;
    }
}
