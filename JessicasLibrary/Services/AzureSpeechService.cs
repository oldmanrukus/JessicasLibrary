using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace JessicasLibrary.Services
{
    public class AzureSpeechService
    {
        // from your appsettings.json
        private const string Key = "2myysjZg7IQUvihZO6vWqvRMNcutoosdBn52LcAi73aB8PkOGK3EJQQJ99BFACYeBjFXJ3w3AAAYACOGAg7V";
        private const string Region = "eastus";
        private const string Voice = "en-US-EmmaNeural";

        public async Task<byte[]> SynthesizeSpeechAsync(string text)
        {
            // 1) Create config
            var config = SpeechConfig.FromSubscription(Key, Region);
            config.SpeechSynthesisVoiceName = Voice;

            // 2) Instantiate a synthesizer (in-memory output)
            using var synthesizer = new SpeechSynthesizer(config, null as AudioConfig);
            SpeechSynthesisResult result = await synthesizer.SpeakTextAsync(text);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                // 3) Create the AudioDataStream
                using var dataStream = AudioDataStream.FromResult(result);

                // 4) Write it out to a temp WAV file
                string tempPath = Path.Combine(
                    Path.GetTempPath(),
                    $"{Guid.NewGuid():N}.wav"
                );
                await dataStream.SaveToWaveFileAsync(tempPath);  // :contentReference[oaicite:0]{index=0}

                // 5) Read back into a byte[] and delete temp file
                byte[] wavBytes = await File.ReadAllBytesAsync(tempPath);
                try { File.Delete(tempPath); } catch { /* ignore */ }

                return wavBytes;
            }
            else
            {
                throw new Exception($"Speech synthesis failed: {result.Reason}");
            }
        }
    }
}
