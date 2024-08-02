using System;
using System.Collections.Generic;
using System.Threading;
using BestHTTP;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class EventService : MonoBehaviour
{
    private const int CooldownBeforeSend = 3;
    private const string ServerUrl = "";
    private const string SavedEventsPrefsKey = "SavedEvents";

    private List<GameEvent> _events = new();
    
    private CancellationTokenSource _sendingTokenSource;

    private void Start()
    {
        LoadSavedEvents();
        ResendEvents(_events);
    }
    
    private void OnDestroy()
    {
        DisposeSendingTokenSource();
        SaveEvents();
    }
    
    public void TrackEvent(string type, string data)
    {
        RestartCooldown();
        AddEvent(type, data);
        SendEventsProcess().Forget();
    }
    
    private void ResendEvents(List<GameEvent> eventsToSend)
    {
        ReturnEvents(eventsToSend);
        SendEventsProcess().Forget();
    }
    
    private async UniTask SendEventsProcess()
    {
        await WaitCooldown();
        SendEvents().Forget();
    }

    private async UniTask WaitCooldown()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(CooldownBeforeSend), cancellationToken: _sendingTokenSource.Token);
    }

    private void AddEvent(string type, string data)
    {
        var gameEvent = new GameEvent(type, data);
        _events.Add(gameEvent);
    }

    private async UniTaskVoid SendEvents()
    {
        var eventsToSend = new List<GameEvent>(_events);
        _events.Clear();

        HTTPRequest request = CreateRequest(eventsToSend);

        try
        {
            await request.Send();

            if (request.IsOk() == false)
            {
                ResendEvents(eventsToSend);
            }
        }
        catch
        {
            ResendEvents(eventsToSend);
        }
    }

    private void ReturnEvents(List<GameEvent> eventsToSend)
    {
        _events.AddRange(eventsToSend);
    }

    private static HTTPRequest CreateRequest(List<GameEvent> eventsToSend)
    {
        var request = new HTTPRequest(new Uri(ServerUrl), HTTPMethods.Post);
        request.SetHeader("Content-Type", "application/json");
        request.RawData = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { events = eventsToSend }));

        return request;
    }

    private void RestartCooldown()
    {
        CreateNewSendingTokenSource();
    }

    private void CreateNewSendingTokenSource()
    {
        DisposeSendingTokenSource();
        _sendingTokenSource = new CancellationTokenSource();
    }

    private void DisposeSendingTokenSource()
    {
        _sendingTokenSource?.Cancel();
        _sendingTokenSource?.Dispose();
        _sendingTokenSource = null;
    }
    
    private void LoadSavedEvents()
    {
        string savedEvents = PlayerPrefs.GetString(SavedEventsPrefsKey, "[]");
        _events = JsonConvert.DeserializeObject<List<GameEvent>>(savedEvents) ?? new List<GameEvent>();
    }

    private void SaveEvents()
    {
        PlayerPrefs.SetString(SavedEventsPrefsKey, JsonConvert.SerializeObject(_events));
        PlayerPrefs.Save();
    }
}