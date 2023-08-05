using Microsoft.Extensions.Logging;
using Serilog;

using Deepgram;
using Deepgram.Transcription;
using Deepgram.Logger;

namespace DeepGram.Console;

/// <summary>
/// Handles the live transcription with Deepgram.
/// </summary>
internal class DeepgramLive : IDisposable
{
    private readonly ILiveTranscriptionClient _transcriptionClient;
    private readonly LiveTranscriptionOptions _options;
    private readonly Func<List<Alternative>, Task> _transcriptionHandler;

    private bool _isDisposed;

    /// <inheritdoc>
    /// <param name="apiKey">The Deepgram API key. Get it from the Deepgram Console at https://console.deepgram.com/</param>
    /// <param name="transcriptionHandler">The handler to call when there is a new transcription available</param>
    /// <param name="sampleRate">The sample rate of the audio that gets sent to Deepgram. Defaults to 16000</param>
    /// <param name="logEvents">Selects if connection changes should be output to System.Console</param>
    public DeepgramLive(string apiKey, Func<List<Alternative>, Task> transcriptionHandler, int sampleRate = 16000, bool logEvents = true)
    {
        _transcriptionHandler = transcriptionHandler;

        _options = new LiveTranscriptionOptions()
        {
            Punctuate = true,
            Diarize = true,
            Encoding = Deepgram.Common.AudioEncoding.Linear16,
            Language = "en-gb",
            Channels = 1,
            SampleRate = sampleRate
        };

        var client = new DeepgramClient(new Credentials(apiKey));
        _transcriptionClient = client.CreateLiveTranscriptionClient();

        _transcriptionClient.TranscriptReceived += HandleTranscriptReceived;

        if (logEvents)
        {
            _transcriptionClient.ConnectionOpened += DeepgramLive_ConnectionOpened;
            _transcriptionClient.ConnectionError += DeepgramLive_ConnectionError;
            _transcriptionClient.ConnectionClosed += DeepgramLive_ConnectionClosed;

            // Deepgram uses Serilog for logging, so we need to set up Serilog to log to the console.
            LogProvider.SetLogFactory(new LoggerFactory().AddSerilog(
                new LoggerConfiguration()
                    .MinimumLevel.Warning()
                    .WriteTo.Console(outputTemplate: "NAudio: {Timestamp:HH:mm} [{Level}]: {Message}\n")
                    .CreateLogger())
                );
        }
    }

    /// <summary>
    /// Opens the connection to Deepgram and starts the transcription.
    /// </summary>
    public async Task Start()
    {
        await _transcriptionClient.StartConnectionAsync(_options).ConfigureAwait(false);
    }

    /// <summary>
    /// Gracefully stops the transcription and closes the connection to Deepgram.
    /// </summary>
    public async Task Stop()
    {
        await _transcriptionClient.StopConnectionAsync().ConfigureAwait(false);
        await _transcriptionClient.FinishAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Sends audio to Deepgram for transcription.
    /// </summary>
    /// <param name="audio">A Linear16 encoded audio byte array</param>
    public void SendAudio(byte[] audio)
    {
        _transcriptionClient.SendData(audio);
    }

    /// <summary>
    /// Send a keep alive message to Deepgram to keep the connection alive.
    /// If you don't send a keep alive message, the connection will be closed from inactivity.
    /// </summary>
    public void KeepAlive()
    {
        var keepAliveMessage = System.Text.Json.JsonSerializer.Serialize(new { type = "KeepAlive" });
        var keepAliveBytes = System.Text.Encoding.Default.GetBytes(keepAliveMessage);

        _transcriptionClient.SendData(keepAliveBytes);
    }

    private async void HandleTranscriptReceived(object? sender, TranscriptReceivedEventArgs e)
    {
        if (!e.Transcript.IsFinal) return;

        var transcripts = e.Transcript.Channel.Alternatives
                .Where(a => a.Transcript.Length > 0);
        if (transcripts.Any())
        {
            await _transcriptionHandler(transcripts.ToList()).ConfigureAwait(false);
        }
    }

    private void DeepgramLive_ConnectionOpened(object? sender, ConnectionOpenEventArgs e)
    {
        System.Console.WriteLine("Deepgram connected");
    }

    private void DeepgramLive_ConnectionError(object? sender, ConnectionErrorEventArgs e)
    {
        System.Console.WriteLine(e.Exception.Message);
    }

    private void DeepgramLive_ConnectionClosed(object? sender, ConnectionClosedEventArgs e)
    {
        System.Console.WriteLine("Deepgram disconnected");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed && disposing)
        {
            _transcriptionClient?.Dispose();
        }

        _isDisposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~DeepgramLive()
    {
        Dispose(false);
    }
}
