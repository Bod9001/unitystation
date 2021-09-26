using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IProvideConsciousness
{
	ConsciousState ConsciousState { get; }
	GameObject ThisGameObject { get; }
}
