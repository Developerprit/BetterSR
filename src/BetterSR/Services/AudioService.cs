using BetterSR.Models;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;

namespace BetterSR.Services;

public class AudioService : IDisposable
{
    private WasapiLoopbackCapture? _systemCapture;
    private WasapiCapture? _micCapture;
    private BufferedWaveProvider? _systemBuffered;
    private BufferedWaveProvider? _micBuffered;
    private MediaFoundationResampler? _systemResampler;
    private MediaFoundationResampler? _micResampler;
    private MixingSampleProvider? _mixer;
    private readonly WaveFormat _outputFormat = new(48000, 16, 2);

    public bool IsRecording { get; private set; }

    public void Start(AppSettings settings)
    {
        if (IsRecording) return;

        var mixerInputs = new List<ISampleProvider>();

        if (settings.RecordSystemAudio)
        {
            _systemCapture = new WasapiLoopbackCapture();
            _systemBuffered = new BufferedWaveProvider(_systemCapture.WaveFormat);
            mixerInputs.Add(CreateSampleProvider(_systemBuffered, _systemCapture.WaveFormat, ref _systemResampler));
            _systemCapture.DataAvailable += OnSystemDataAvailable;
            _systemCapture.StartRecording();
        }

        if (settings.RecordMicrophone)
        {
            _micCapture = new WasapiCapture(GetDefaultMicrophone());
            _micBuffered = new BufferedWaveProvider(_micCapture.WaveFormat);
            mixerInputs.Add(CreateSampleProvider(_micBuffered, _micCapture.WaveFormat, ref _micResampler));
            _micCapture.DataAvailable += OnMicDataAvailable;
            _micCapture.StartRecording();
        }

        if (mixerInputs.Count > 0)
        {
            _mixer = new MixingSampleProvider(mixerInputs)
            {
                ReadFully = true
            };
        }

        IsRecording = true;
    }

    private ISampleProvider CreateSampleProvider(BufferedWaveProvider source, WaveFormat sourceFormat, ref MediaFoundationResampler? resamplerField)
    {
        if (sourceFormat.SampleRate == _outputFormat.SampleRate && sourceFormat.Channels == _outputFormat.Channels)
        {
            return source.ToSampleProvider();
        }
        resamplerField = new MediaFoundationResampler(source, _outputFormat);
        return resamplerField.ToSampleProvider();
    }

    private void OnSystemDataAvailable(object? sender, WaveInEventArgs e)
    {
        _systemBuffered?.AddSamples(e.Buffer, 0, e.BytesRecorded);
    }

    private void OnMicDataAvailable(object? sender, WaveInEventArgs e)
    {
        _micBuffered?.AddSamples(e.Buffer, 0, e.BytesRecorded);
    }

    public byte[] ReadMixedS16(int milliseconds)
    {
        if (_mixer == null) return Array.Empty<byte>();

        var samplesNeeded = (int)(_outputFormat.SampleRate * _outputFormat.Channels * (milliseconds / 1000.0));
        var samples = new float[samplesNeeded];
        _mixer.Read(samples.AsSpan());

        var bytes = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            var sample = samples[i];
            if (sample > 1f) sample = 1f;
            if (sample < -1f) sample = -1f;
            var s = (short)(sample * short.MaxValue);
            var b = BitConverter.GetBytes(s);
            bytes[i * 2] = b[0];
            bytes[i * 2 + 1] = b[1];
        }
        return bytes;
    }

    public void Stop()
    {
        _systemCapture?.StopRecording();
        _systemCapture?.Dispose();
        _systemCapture = null;

        _micCapture?.StopRecording();
        _micCapture?.Dispose();
        _micCapture = null;

        _systemResampler?.Dispose();
        _systemResampler = null;
        _micResampler?.Dispose();
        _micResampler = null;

        _systemBuffered = null;
        _micBuffered = null;
        _mixer = null;

        IsRecording = false;
    }

    private static MMDevice GetDefaultMicrophone()
    {
        using var enumerator = new MMDeviceEnumerator();
        return enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
    }

    public void Dispose() => Stop();
}
