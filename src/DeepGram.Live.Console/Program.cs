using DeepGram.Console;

// Get your API key from the Deepgram Console at https://console.deepgram.com/
var deepgramApiKey = "SET YOUR KEY HERE";

Console.WriteLine("Starting Deepgram live console.");
Console.WriteLine();

// Get the audio device to use for recording.
var recordingDeviceId = Audio.GetRecordingDeviceId();
if (recordingDeviceId < 0)
{
    return;
}

// Setup the Deepgram connection and the NAudio audio capture.
using (var deepgramLive = new DeepgramLive(deepgramApiKey, HandleTranscription))
using (var audioCapture = new Audio(availableBytes => AudioAvailable(deepgramLive, availableBytes)))
{
    // Start the Deepgram connection and the audio capture.
    await deepgramLive.Start();
    audioCapture.Start(recordingDeviceId);

    // Setup Deepgram keepalive
    var keepAliveTimer = new Timer(_ =>
    {
        deepgramLive.KeepAlive();
    }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

    // Ready to run. Wait for keypress.
    Console.WriteLine("Starting transcription.");
    Console.WriteLine("Press any key to exit.");
    Console.ReadKey();
    Console.WriteLine("Exit requested. Shutting down.");

    // Stop the Deepgram keepalive timer.
    keepAliveTimer.Change(Timeout.Infinite, Timeout.Infinite);
    keepAliveTimer.Dispose();

    // Stop the Deepgram connection and the audio capture.
    audioCapture.Stop();
    await deepgramLive.Stop();
}

Console.WriteLine("Deepgram live console stopped.");
// ----------------------------------------------------------------------------------------------

/// <summary>
/// Callback for when audio is available from the recording device.
/// </summary>
/// <param name="deepgramLive">The DeepgramLive instance to send the audio to.</param>
/// <param name="audioBytes">The audio bytes to send to Deepgram.</param>
void AudioAvailable(DeepgramLive deepgramLive, byte[] audioBytes)
{
    deepgramLive.SendAudio(audioBytes);
}

/// <summary>
/// Callback from Deepgram when a transcription is available.
/// </summary>
/// <param name="alternatives">The list of alternatives for transcriptions that Deepgram has parsed.</param>
Task HandleTranscription(List<Deepgram.Transcription.Alternative> alternatives)
{
    Console.WriteLine("Recieved transcription with {0} alternatives.", alternatives.Count);
    for (int transcriptNumber = 0; transcriptNumber < alternatives.Count; transcriptNumber++)
    {
        Console.WriteLine($"Alternative {transcriptNumber + 1}, confidence {alternatives[transcriptNumber].Confidence}:");
        Console.WriteLine(alternatives[transcriptNumber].Transcript);
    }
    Console.WriteLine("---");
    return Task.CompletedTask;
}
