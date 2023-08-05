using NAudio.Wave;

namespace DeepGram.Console;

/// <summary>
/// Gets the audio from the microphone and calls a callback with the audio data.
/// </summary>
internal class Audio : IDisposable
{
    private readonly Action<byte[]> _dataAvailable;

    private readonly WaveInEvent _waveIn;

    private bool _isDisposed;

    /// <inheritdoc />
    /// <param name="dataAvailable">The handler to call when there is a new audio available</param>
    public Audio(Action<byte[]> dataAvailable)
    {
        _dataAvailable = dataAvailable;

        _waveIn = new WaveInEvent();
    }

    /// <summary>
    /// Starts the audio capture.
    /// </summary>
    /// <param name="deviceId">The device id to record the audio from. Get a list from <see cref="GetRecordingDeviceId" /></param>
    /// <param name="sampleRate">The samplerate to record in. Must match samplerate in <see cref="DeepgramLive" /></param>
    public void Start(int deviceId, int sampleRate = 16000)
    {
        _waveIn.DeviceNumber = deviceId;
        _waveIn.WaveFormat = new WaveFormat(sampleRate, 1);
        _waveIn.DataAvailable += WaveIn_DataAvailable;
        _waveIn.StartRecording();
    }

    /// <summary>
    /// Stops the audio capture.
    /// </summary>
    public void Stop()
    {
        _waveIn.StopRecording();
    }

    /// <summary>
    /// Gets the device id to record the audio from.
    /// </summary>
    /// <returns>The device id that the user has selected via the console.</returns>
    public static int GetRecordingDeviceId()
    {
        switch (WaveInEvent.DeviceCount)
        {
            case 0:
                System.Console.WriteLine("There are no active audio input devices in your system.");
                return -1;
            case 1:
                return 0;
            default:
                System.Console.WriteLine("Select an audio input device:");
                for (int i = 0; i < WaveInEvent.DeviceCount; i++)
                {
                    var capabilities = WaveInEvent.GetCapabilities(i);
                    System.Console.WriteLine($"({i}): {capabilities.ProductName}");
                }

                var inputDeviceString = "";
                int deviceNumber;
                do
                {
                    System.Console.WriteLine("Select input device");
                    inputDeviceString = System.Console.ReadLine();
                } while (!Int32.TryParse(inputDeviceString, out deviceNumber) || deviceNumber >= WaveInEvent.DeviceCount);

                System.Console.WriteLine($"Selected device {WaveInEvent.GetCapabilities(deviceNumber).ProductName} as the input device.");
                return deviceNumber;
        }
    }

    private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
    {
        _dataAvailable(e.Buffer);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed && disposing && _waveIn != null)
        {
            _waveIn.Dispose();
        }

        _isDisposed = true;

    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    ~Audio()
    {
        Dispose(false);
    }
}
