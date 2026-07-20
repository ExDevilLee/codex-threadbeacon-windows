using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace ThreadBeacon.App.Tests.Sounds;

public sealed class BundledSoundAssetTests
{
    private static readonly string SoundDirectory = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "..",
        "..",
        "src",
        "ThreadBeacon.App",
        "Resources",
        "Sounds"));

    public static TheoryData<string, short> AllSoundFiles => new()
    {
        { "Done-Fupicat-Notification.wav", 2 },
        { "Done-Bassguitar-Notification.wav", 2 },
        { "Done-Beacon.wav", 1 },
        { "Done-Chime.wav", 1 },
        { "Done-Pulse.wav", 1 },
        { "Done-Alert.wav", 1 },
        { "Done-Resolve.wav", 1 },
        { "Done-Knock.wav", 1 },
    };

    public static TheoryData<string, string> NewSoundHashes => new()
    {
        {
            "Done-Alert.wav",
            "A294A8142F636F5641AA04F6974A304F46E5148068DD06E42BA3D1002654D497"
        },
        {
            "Done-Resolve.wav",
            "F2E077E6E926FF315EABD11D26B53716E29DB5B7A1D96334B988867D1554B6AE"
        },
        {
            "Done-Knock.wav",
            "127AA68C18EB0A419627D33FAA8395A1A5CA601BA8B5ABA3181EAE894CD4889D"
        },
        {
            "Done-Fupicat-Notification.wav",
            "D4338430E4A7AC375A18191AAA0932671B2C8B23790B62AB5FFB18EE839B0617"
        },
        {
            "Done-Bassguitar-Notification.wav",
            "69636EA0D3DED6E05A27EDC579757B966A78EC4DB99B4696BE6277565939286A"
        },
    };

    [Theory]
    [MemberData(nameof(AllSoundFiles))]
    public void BundledFile_Is44100Hertz16BitPcmWave(string fileName, short expectedChannels)
    {
        string path = Path.Combine(SoundDirectory, fileName);

        byte[] header = File.ReadAllBytes(path)[..44];

        Assert.Equal("RIFF", Encoding.ASCII.GetString(header, 0, 4));
        Assert.Equal("WAVE", Encoding.ASCII.GetString(header, 8, 4));
        Assert.Equal(1, BinaryPrimitives.ReadInt16LittleEndian(header.AsSpan(20, 2)));
        Assert.Equal(expectedChannels, BinaryPrimitives.ReadInt16LittleEndian(header.AsSpan(22, 2)));
        Assert.Equal(44_100, BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(24, 4)));
        Assert.Equal(16, BinaryPrimitives.ReadInt16LittleEndian(header.AsSpan(34, 2)));
    }

    [Theory]
    [MemberData(nameof(NewSoundHashes))]
    public void NewBundledFile_MatchesMacOsAssetHash(string fileName, string expectedHash)
    {
        string path = Path.Combine(SoundDirectory, fileName);

        string actualHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

        Assert.Equal(expectedHash, actualHash);
    }
}
