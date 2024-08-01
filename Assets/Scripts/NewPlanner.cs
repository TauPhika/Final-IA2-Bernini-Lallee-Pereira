using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class NewPlanner : MonoBehaviour
{
    private readonly List<Tuple<Vector3, Vector3>> _debugRayList = new List<Tuple<Vector3, Vector3>>();

    [Header("ACTION COSTS")]
    [Range(5, 10)] public int GoToWork;
    [Range(6, 10)] public int Bargain;
    [Range(3, 6)] public int BuyItem, BuyWeapon, RobItem;
    [Range(1, 4)] public int Intimidate, WaitForRestocking;

    private void Start()
    {
        StartCoroutine(Plan());
    }

    private void Check(Dictionary<string, bool> state, ItemType type)
    {

        var items = Navigation.instance.AllItems();
        var inventories = Navigation.instance.AllInventories();
        var floorItems = items.Except(inventories);//devuelve una coleccion como la primera pero removiendo los que estan en la segunda
        var item = floorItems.FirstOrDefault(x => x.type == type);
        var here = transform.position;
        state["accessible" + type.ToString()] = item != null && Navigation.instance.Reachable(here, item.transform.position, _debugRayList);

        var inv = inventories.Any(x => x.type == type);
        state["otherHas" + type.ToString()] = inv;

        state["dead" + type.ToString()] = false;
    }

    //No es necesario que sea una corrutina excepto que se calcule GOAP con timeslicing.
    private IEnumerator Plan()
    {
        yield return new WaitForSeconds(0.2f);


        #region Si no se usan objetos modulares, se puede eliminar
        var observedState = new Dictionary<string, bool>();

        var nav = Navigation.instance;//Consigo los items
        var floorItems = nav.AllItems();
        var inventory = nav.AllInventories();
        var everything = nav.AllItems().Union(nav.AllInventories());// .Union() une 2 colecciones sin agregar duplicados(eso incluye duplicados en la misma coleccion)

        //Chequeo los booleanos para cada Item, generando mi modelo de mundo (mi diccionario de bools) en ObservedState
        Check(observedState, ItemType.Frutilla);
        Check(observedState, ItemType.Entity);
        Check(observedState, ItemType.Weapon);
        Check(observedState, ItemType.Office);
        Check(observedState, ItemType.Home);
        #endregion

        GoapState initial = new GoapState(); //Crear GoapState
        initial.worldState = new WorldState()
        {
            //Estos valores de aca los pueden pasar a mano pero tienen que coordinar on el estado del mundo actual.
            //Lo ideal es que consiga el estado de todas las variables proceduralmente, pero no es necesario.
            MoneyAmount = 5,
            ItemPrice = 10,
            ItemInStock = "zapallo",
            AlertPercentage = 0.00f,
            HasWeapon = false,
            HasItem = false,

            values = new Dictionary<string, bool>() //Eliminar!
        };


        //Si uso items modulares:
        initial.worldState.values = observedState; //le asigno los valores actuales, conseguidos antes
        initial.worldState.values["doorOpen"] = false; //agrego el bool "doorOpen"

        //Calculo las acciones
        var actions = CreatePossibleActionsList();

        #region opcional
        foreach (var item in initial.worldState.values)
        {
            Debug.Log(item.Key + " ---> " + item.Value);
        }
        #endregion

        //Es opcional, no es necesario buscar por un nodo que cumpla perfectamente con las condiciones
        GoapState goal = new GoapState();
        //goal.values["has" + ItemType.Key.ToString()] = true;
        goal.worldState.values["has" + ItemType.PastaFrola.ToString()] = true;
        //goal.values["has"+ ItemType.Mace.ToString()] = true;
        //goal.values["dead" + ItemType.Entity.ToString()] = true;}


        //Crear la heuristica personalizada para no calcular nodos de mas
        Func<GoapState, float> heuristic = (curr) =>
        {
            int count = 0;
            if (!curr.worldState.HasItem) count++;
            return count;
        };

        //Esto seria el reemplazo de goal, donde se pide que cumpla con las condiciones pasadas.
        Func<GoapState, bool> objective = (curr) =>
         {
             return curr.worldState.HasItem == true;
         };

        #region Opcional
        var actDict = new Dictionary<string, ActionEntity>() {
              { "Kill"  , ActionEntity.Kill }
            , { "Pickup", ActionEntity.PickUp }
            , { "Open"  , ActionEntity.Open }
        };
        #endregion

        var plan = Goap.Execute(initial, null, objective, heuristic, actions);

        if (plan == null)
            Debug.Log("Couldn't plan");
        else
        {
            GetComponent<Guy>().ExecutePlan(
                plan
                .Select(a =>
                {
                    Item i2 = everything.FirstOrDefault(i => i.type == a.item);
                    if (actDict.ContainsKey(a.Name) && i2 != null)
                    {
                        return Tuple.Create(actDict[a.Name], i2);
                    }
                    else
                    {
                        return null;
                    }
                }).Where(a => a != null)
                .ToList()
            );
        }
    }

    private List<GoapAction> CreatePossibleActionsList()
    {
        return new List<GoapAction>()
        {
            // Va a trabajar para dejar pasar el tiempo y conseguir plata, siempre y cuando no sospechen de el.
            new GoapAction("GoToWork")
            .SetCost(GoToWork)
            .SetItem(ItemType.Office)
            .Pre((w) =>
            {
                return w.worldState.AlertPercentage <= 0.25f;
            })
            .NewEffect((w) =>
            {
                w.worldState.MoneyAmount += 5;
                w.worldState.ItemInStock = "frutilla";               
            }),

            
            // Va a su casa y espera hasta que restockeen las frutillas si es que no estan en stock todavia.
            new GoapAction("WaitForRestocking")
            .SetCost(WaitForRestocking)
            .SetItem(ItemType.Home)
            .Pre((w) =>
            {
                return w.worldState.ItemInStock != "frutilla";
            })
            .NewEffect((w) =>
            {
                w.worldState.ItemInStock = "frutilla";
            }),


            // Regatea con el verdulero para que le deje las frutillas mas baratas si tiene algo para ofrecer
            // y no es tan sospechoso.
            new GoapAction("Bargain")
            .SetCost(Bargain)
            .SetItem(ItemType.Entity)
            .Pre((w) =>
            {
                return w.worldState.MoneyAmount >= 5
                && w.worldState.AlertPercentage <= 0.5f
                && w.worldState.ItemInStock == "frutilla";
            })
            .NewEffect((w) =>
            {
                w.worldState.ItemPrice = w.worldState.MoneyAmount;
            }),


            // Si las frutillas estan disponibles, no es sospechoso y tiene plata, las compra.
            new GoapAction("BuyItem")
            .SetCost(BuyItem)
            .SetItem(ItemType.Frutilla)
            .Pre((w) =>
            {
                return w.worldState.MoneyAmount >= w.worldState.ItemPrice
                && w.worldState.AlertPercentage <= 0.5f
                && w.worldState.ItemInStock == "frutilla";
            })
            .NewEffect((w) =>
            {
                // OBJETIVO CUMPLIDO
                w.worldState.HasItem = true;
            }),


            // Compra un arma (si la puede pagar y no la tiene ya) para facilitar la coleccion de frutillas.
            new GoapAction("BuyWeapon")
            .SetCost(BuyItem)
            .SetItem(ItemType.Weapon)
            .Pre((w) =>
            {
                return w.worldState.MoneyAmount >= 3
                && w.worldState.HasWeapon == false;
            })
            .NewEffect((w) =>
            {
                w.worldState.HasWeapon = true;
                w.worldState.AlertPercentage += 0.5f;
                w.worldState.MoneyAmount -= 3;
            }),


            // Usa el arma y las sospechas sobre el para intimidar al verdulero y que baje el precio
            // de las frutillas.
            new GoapAction("Intimidate")
            .SetCost(Intimidate)
            .SetItem(ItemType.Entity)
            .Pre((w) =>
            {
                return w.worldState.AlertPercentage >= 0.5f
                && w.worldState.HasWeapon == true
                && w.worldState.ItemInStock == "frutilla";
            })
            .NewEffect((w) =>
            {
                w.worldState.ItemPrice = w.worldState.MoneyAmount;
            }),

            
            // Asalta la verduleria con el arma y se lleva las frutillas si es que estan en stock.
            new GoapAction("RobItem")
            .SetCost(RobItem)
            .SetItem(ItemType.Frutilla)
            .Pre((w) =>
            {
                return w.worldState.HasWeapon == true
                && w.worldState.ItemInStock == "frutilla";
            })
            .NewEffect((w) =>
            {
                w.worldState.AlertPercentage += 0.5f;

                // OBJETIVO CUMPLIDO
                w.worldState.HasItem = true;
            }),

        };
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        foreach (var t in _debugRayList)
        {
            Gizmos.DrawRay(t.Item1, (t.Item2 - t.Item1).normalized);
            Gizmos.DrawCube(t.Item2 + Vector3.up, Vector3.one * 0.2f);
        }
    }
}
