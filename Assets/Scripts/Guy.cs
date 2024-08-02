using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using IA2;

public enum ActionEntity
{
	Kill,
    PickUp,
	NextStep,
	FailedStep,
	Open,
	Success
	
}

public class Guy : MonoBehaviour
{
	private EventFSM<ActionEntity> _fsm;
    private Item _target;

    private Entity _ent;

	IEnumerable<Tuple<ActionEntity, Item>> _plan;

	private bool PerformAttack(Entity us, Item other) {
		Debug.Log($"PerformAttack on {other.name}",other.gameObject);
		//if(other != _target) return;

		var weapon = _ent.items.FirstOrDefault(it => it.type == ItemType.Weapon);
		if(weapon) {
			other.Kill();
			if(other.type == ItemType.Door)
				Destroy(_ent.Removeitem(weapon).gameObject);
			_fsm.Feed(ActionEntity.NextStep);
			return true;
		}
		else
			_fsm.Feed(ActionEntity.FailedStep);
		return false;
	}

	private bool PerformOpen(Entity us, Item other) {
        //if(other != _target) return;

        Debug.Log($"PerformOpen on {other.name}", other.gameObject);
        var key = _ent.items.FirstOrDefault(it => it.type == ItemType.Key);
		var door = other.GetComponent<Door>();
		if(door && key) {
			door.Open();
			Destroy(_ent.Removeitem(key).gameObject);
			_fsm.Feed(ActionEntity.NextStep);
			return true;
		}
		else
			_fsm.Feed(ActionEntity.FailedStep);
		return true;
	}

	void PerformRestock(Entity us , Item other)
	{
		if(other.type == ItemType.Frutilla) other.Restock();
	}

	private bool PerformPickUp(Entity us, Item other) {
        //if(other != _target) return;

        Debug.Log($"PerformPickUp on {other.name}", other.gameObject);
        _ent.AddItem(other);
		_fsm.Feed(ActionEntity.NextStep);
		return true;
	}

	private void NextStep(Entity ent, Waypoint wp, bool reached) {
		_fsm.Feed(ActionEntity.NextStep);
	}

	private void Awake() {
		_ent = GetComponent<Entity>();		

        var any = new State<ActionEntity>("any");

        var idle = new State<ActionEntity>("idle");
        var bridgeStep = new State<ActionEntity>("planStep");
        var failStep = new State<ActionEntity>("failStep");
        var kill = new State<ActionEntity>("kill");
        var pickup = new State<ActionEntity>("pickup");
        var open = new State<ActionEntity>("open");
        var success = new State<ActionEntity>("success");
		var go = new State<ActionEntity>("go");

		go.OnEnter += a =>
		{ 
			_ent.GoTo(_target.transform.position);
			_ent.OnReachDestination += NextStep;
		};

		go.OnExit += a => { _ent.OnReachDestination -= NextStep; };

  //      kill.OnEnter += a => {
		//	_ent.GoTo(_target.transform.position);
		//	_ent.OnHitItem += PerformAttack;
		//};

		//kill.OnExit += a => _ent.OnHitItem -= PerformAttack;

		//failStep.OnEnter += a => { _ent.Stop(); Debug.Log("Plan failed"); };

		//pickup.OnEnter += a => { _ent.GoTo(_target.transform.position); _ent.OnHitItem += PerformPickUp; };
		//pickup.OnExit += a => _ent.OnHitItem -= PerformPickUp;

		//open.OnEnter += a => { if (_target != null) _ent.GoTo(_target.transform.position); _ent.OnHitItem += PerformOpen; };
		//open.OnExit += a => _ent.OnHitItem -= PerformOpen;

		bridgeStep.OnEnter += a => {
			var step = _plan.FirstOrDefault();
			if(step != null) {

				_plan = _plan.Skip(1);
				var oldTarget = _target;
				_target = step.Item2;
				if(!_fsm.Feed(step.Item1))
					_target = oldTarget;
			}
			else {
				_fsm.Feed(ActionEntity.Success);
			}
		};

		success.OnEnter += a => { Debug.Log("Success"); };
		success.OnUpdate += () => { _ent.Jump(); };
		
		StateConfigurer.Create(any)
			.SetTransition(ActionEntity.NextStep, bridgeStep)
			.SetTransition(ActionEntity.FailedStep, idle)
			.Done();

		StateConfigurer.Create(bridgeStep)
            .SetTransition(ActionEntity.Kill, kill)
            .SetTransition(ActionEntity.PickUp, pickup)
            .SetTransition(ActionEntity.Open, open)
            .SetTransition(ActionEntity.Success, success)
			.Done();
        
		_fsm = new EventFSM<ActionEntity>(idle, any);
    }

	IEnumerator Execution()
	{
		int steps = 0;
		foreach(var step in _plan) 
		{
            steps++;

            Debug.Log("");
            Debug.Log($"----- BEGIN STEP {steps} -----");
			Debug.Log("");
			
			print($"EXECUTING STEP {steps}: heading towards {step.Item2.name} at {step.Item2.transform.position} in order to {step.Item1}");
			//print($"EXECUTING NEXT STEP: heading towards {step.Item2.name} at {step.Item2.transform.position} in order to {step.Item1}");

			_ent.enabled = true;
			_ent.GoTo(step.Item2.transform.position);
			print("Applying movement direction: " + _ent.Vel());

			var stepComplete = false;
			switch(step.Item1)
			{
				case ActionEntity.PickUp: stepComplete = PerformPickUp(_ent, step.Item2); break;
				case ActionEntity.Open: stepComplete = PerformOpen(_ent, step.Item2); break;
				case ActionEntity.Kill: stepComplete = PerformAttack(_ent, step.Item2); break;
				case ActionEntity.Success: Debug.Log("Success"); victoryDance = true; break;
				default: Debug.Log("Paso otra cosa"); break;
			}

			yield return new WaitUntil(() => stepComplete == true);
			yield return new WaitForSeconds(1.66f);
		}

		if (_plan.Count() <= 0)
		{
			_fsm.Feed(ActionEntity.Success);
			Debug.Log("Success");
			victoryDance = true;
		}

	}

	public void ExecutePlan(List<Tuple<ActionEntity, Item>> plan) {
		_plan = plan;
		print(plan.Count);
		//_fsm.Feed(ActionEntity.NextStep);

		StartCoroutine(Execution());
	}

	bool victoryDance = false;
	private void Update ()
    {
		//Never forget
        _fsm.Update();

		if(victoryDance) { _ent.Jump(); }
	}
}
