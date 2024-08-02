using System;
using System.Collections.Generic;
using System.Net.Http;
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

    private readonly List<List<GameEvent>> _sendingEvents = new();
    private List<GameEvent> _events;
    private CancellationTokenSource _sendingTokenSource;

    private void Awake()
    {
        LoadSavedEvents();
    }

    private void Start()
    {
        if (_events.Count > 0)
        {
            RestartCooldown();
            SendEventsProcess().Forget();
        }
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
        CancellationToken token = _sendingTokenSource.Token;
        await UniTask.Delay(TimeSpan.FromSeconds(CooldownBeforeSend), cancellationToken: token);
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
        _sendingEvents.Add(eventsToSend);

        try
        {
            HTTPRequest request = CreateRequest(eventsToSend);
            await request.Send();

            if (request.IsOk())
                Debug.Log($"Request Sended = {JsonConvert.SerializeObject(eventsToSend)}");
            else
                throw new HttpRequestException("Response is not OK");
        }
        catch
        {
            ResendEvents(eventsToSend);
        }
        finally
        {
            _sendingEvents.Remove(eventsToSend);
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
        request.RawData = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventsToSend));

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
        string savedEvents = PlayerPrefs.GetString(SavedEventsPrefsKey, defaultValue: "[]");
        _events = JsonConvert.DeserializeObject<List<GameEvent>>(savedEvents) ?? new List<GameEvent>();
        Debug.Log($"Loaded = {JsonConvert.SerializeObject(_events)}");
    }

    private void SaveEvents()
    {
        List<GameEvent> eventsToSave = CreateEventsToSave();

        Debug.Log($"Save = {JsonConvert.SerializeObject(eventsToSave)}");
        PlayerPrefs.SetString(SavedEventsPrefsKey, JsonConvert.SerializeObject(eventsToSave));
        PlayerPrefs.Save();
    }

    private List<GameEvent> CreateEventsToSave()
    {
        var savingEvents = new List<GameEvent>(_events);
        foreach (var events in _sendingEvents)
        {
            savingEvents.AddRange(events);
        }

        return savingEvents;
    }
}