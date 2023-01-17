using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class DontDestroy : MonoBehaviour
{
	private static List<GameObject> _allGameObject = new List<GameObject>();

	private static Dictionary<int, bool> _allActiveState = new Dictionary<int, bool>();
	public static void PushActiveState()
	{
		_allActiveState.Clear();
		for(int i = _allGameObject.Count() - 1; i >= 0; i--)
		{
			var go = _allGameObject[i];
			if (go)
			{
				var instanceId = go.GetInstanceID();
				if (!_allActiveState.ContainsKey(instanceId))
				{
					_allActiveState.Add(instanceId, go.activeSelf);
				}
				else
				{
					D.Log("PushActiveState:dumplacate");
				}
				
				D.Log(Color.yellow, "PushActiveState:{0}/{1} {3} ({2})", _allGameObject.Count - i, _allGameObject.Count, go.name, go.activeSelf);
			}
			else
			{
				_allGameObject.RemoveAt(i);
				D.Log(Color.yellow, "PushActiveState:{0}/{1}({2})", _allGameObject.Count - i, _allGameObject.Count, "game object already removed!");
			}
		}
	}

	public static void DeactiveAll()
	{
		for(int i = _allGameObject.Count() - 1; i >= 0; i--)
		{
			var go = _allGameObject[i];
			if (go)
			{
				go.SetActive(false);
				D.Log(Color.yellow, "DeactiveAll:{0}/{1}({2})", _allGameObject.Count - i, _allGameObject.Count, go.name);
			}
			else
			{
				_allGameObject.RemoveAt(i);
				D.Log(Color.yellow, "DeactiveAll:{0}/{1}({2})", _allGameObject.Count - i, _allGameObject.Count, "game object already removed!");
			}
		}
	}

	public static void PopActiveState()
	{
		for(int i = _allGameObject.Count() - 1; i >= 0; i--)
		{
			var go = _allGameObject[i];
			if (go)
			{
				bool activeState = false;
				if (_allActiveState.TryGetValue(go.GetInstanceID(), out activeState))
				{
					go.SetActive(activeState);
					D.Log(Color.yellow, "PopActiveState:{0}/{1} {3} ({2})", _allGameObject.Count - i, _allGameObject.Count, go.name, activeState);
                 				}
			}
			else
			{
				_allGameObject.RemoveAt(i);
				D.Log(Color.yellow, "PopActiveState:{0}/{1}({2})", _allGameObject.Count - i, _allGameObject.Count, "game object already removed!");
			}
		}
	}

	// Use this for initialization
	void Awake () 
	{
		if (this.transform.parent != null)
		{
			var stringBuilder = new StringBuilder();

			stringBuilder.Append(this.transform.parent.name);
			stringBuilder.Append("/");
			stringBuilder.Append(this.gameObject.name);
			
			Debug.LogError(stringBuilder.ToString());
		}

		DontDestroyOnLoad(gameObject);
		
		_allGameObject.Add(gameObject);
	}
}
