using Microsoft.MixedReality.WebRTC;


// Audio tracks variables
AudioTrackSource microphoneSource = null;
Transceiver audioTranceiver = null;
LocalAudioTrack localAudioTrack = null;

// Video tracks variables
VideoTrackSource webcamSource = null;
Transceiver videoTranceiver = null;
LocalVideoTrack localVideoTrack = null;

// setup video track that will send the video over webrtc
webcamSource = await DeviceVideoTrackSource.CreateAsync();
LocalVideoTrackInitConfig? videoTrackConfig = new LocalVideoTrackInitConfig { trackName = "webcam_track" };
localVideoTrack = LocalVideoTrack.CreateFromSource(webcamSource, videoTrackConfig);

// setupo audio track that will send the audio over webrtc
microphoneSource = await DeviceAudioTrackSource.CreateAsync();
LocalAudioTrackInitConfig? audioTrackConfig = new LocalAudioTrackInitConfig { trackName = "microphone_track" };
localAudioTrack = LocalAudioTrack.CreateFromSource(microphoneSource, audioTrackConfig);

// Create a peer connection
using var pc = new PeerConnection();

//Bind the video tranceivers to the peer connection
videoTranceiver = pc.AddTransceiver(MediaKind.Video);
videoTranceiver.LocalVideoTrack = localVideoTrack;
videoTranceiver.DesiredDirection = Transceiver.Direction.SendReceive;

//Bind the audio tranceivers to the peer connection
audioTranceiver = pc.AddTransceiver(MediaKind.Audio);
audioTranceiver.LocalAudioTrack = localAudioTrack;
audioTranceiver.DesiredDirection = Transceiver.Direction.SendReceive;


try
{
    var configuration = new PeerConnectionConfiguration
    {
        IceServers = new List<IceServer> { new IceServer { Urls = { "stun:stun.l.google.com" } } }
    };

    await pc.InitializeAsync(configuration);
    Console.WriteLine("Peer connection initialized.");


    // Setup the signaler wehere the two peers will connect to establish a p2p connection
    var signaler = new NamedPipeSignaler.NamedPipeSignaler(pc, "testPipe");

    // connect handlers to the signaler's messages and forward them to the peer conenction
    signaler.SdpMessageReceived += async (SdpMessage message) =>
    {
        // Note: we use 'await' to ensure the remote description is applied
        // before calling CreateAnswer(). Failing to do so will prevent the
        // answer from being generated, and the connection from establishing.

        await pc.SetRemoteDescriptionAsync(message);
        if (message.Type == SdpMessageType.Offer)
        {
            // a typical application would display some user feedback and wait for confirmation to accept the incoming call.
            pc.CreateAnswer();
        }
    };

    signaler.IceCandidateReceived += (IceCandidate IceCandidate) =>
    {
        pc.AddIceCandidate(IceCandidate);
    };

    // Start the signaler and connect it to the remote peer's signaler.
    await signaler.StartAsync();


}
catch (Exception ex)
{
        Console.WriteLine(ex.Message);
}
finally
{
    localAudioTrack?.Dispose();
    localVideoTrack?.Dispose();
    microphoneSource?.Dispose();
    webcamSource?.Dispose();
}
