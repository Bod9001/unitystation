using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using UnityEngine;

// Client
public partial class SubSceneManager
{
	private bool clientIsLoadingSubscene = false;
	private List<string> clientLoadedSubScenes = new List<string>();

	private float waitTime = 0f;
	private readonly float tickRate = 1f;

	public string StartingScene;

	void MonitorServerSceneListOnClient()
	{
		if (isServer || clientIsLoadingSubscene || AddressableCatalogueManager.FinishLoaded == false) return;

		waitTime += Time.deltaTime;
		if (waitTime >= tickRate)
		{
			waitTime = 0f;
			if (clientLoadedSubScenes.Count < loadedScenesList.Count)
			{
				clientIsLoadingSubscene = true;
				var sceneToLoad = loadedScenesList[clientLoadedSubScenes.Count];
				clientLoadedSubScenes.Add(sceneToLoad.SceneName);
				StartCoroutine(LoadClientSubScene(sceneToLoad));
			}
		}
	}

	IEnumerator LoadClientSubScene(SceneInfo sceneInfo)
	{
		if (sceneInfo.SceneType == SceneType.MainStation)
		{
			var clientLoadTimer = new SubsceneLoadTimer();
			//calculate load time:
			clientLoadTimer.MaxLoadTime = 10f;
			clientLoadTimer.IncrementLoadBar($"Loading {sceneInfo.SceneName}");
			yield return StartCoroutine(LoadSubScene(sceneInfo.SceneName, clientLoadTimer));
			MainStationLoaded = true;
			yield return WaitFor.Seconds(0.1f);
			UIManager.Display.preRoundWindow.CloseMapLoadingPanel();
		}
		else
		{
			yield return StartCoroutine(LoadSubScene(sceneInfo.SceneName));
		}

		clientIsLoadingSubscene = false;
	}


	public void LoadSpecifiedScenes(List<string> inLoad)
	{
		StartCoroutine(LoadInitialClientScenes(inLoad));
	}

	IEnumerator LoadInitialClientScenes(List<string> inLoad)
	{
		clientIsLoadingSubscene = true;
		var clientLoadTimer = new SubsceneLoadTimer();
		//calculate load time:
		clientLoadTimer.MaxLoadTime = 10f;
		clientLoadTimer.IncrementLoadBar($"Loading {inLoad}");

		foreach (var Scene in inLoad)
		{
			yield return StartCoroutine(LoadSubScene(Scene, clientLoadTimer, false));
		}

		RequestObserverRefresh.Send(StartingScene);

		yield return WaitFor.Seconds(0.2f);

		foreach (var Scene in inLoad)
		{
			RequestObserverRefresh.Send(Scene);
			clientLoadedSubScenes.Add(Scene);
		}

		yield return WaitFor.Seconds(0.2f);

		UIManager.Display.preRoundWindow.CloseMapLoadingPanel();
		clientIsLoadingSubscene = false;

	}
}
