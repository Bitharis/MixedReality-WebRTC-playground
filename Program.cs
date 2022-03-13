using Microsoft.MixedReality.WebRTC;


public class Program
{
    public static async Task Main(string[] args)
    {
        Transceiver audioTransceiver = null;
        Transceiver videoTransceiver = null;
        AudioTrackSource audioTrackSource = null;
        VideoTrackSource videoTrackSource = null;
        LocalAudioTrack localAudioTrack = null;
        LocalVideoTrack localVideoTrack = null;

        try
        {
            // check if we enable video/audio capture
            bool needVideo = Array.Exists(args, arg => (arg == "-v") || (arg == "--video"));
            bool needAudio = Array.Exists(args, arg => (arg == "-a") || (arg == "--audio"));

            // Asynchronously retrieve a list of available video capture devices (webcams).
            var deviceList = await DeviceVideoTrackSource.GetCaptureDevicesAsync();

            // For example, print them to the standard output
            foreach (var device in deviceList)
            {
                Console.WriteLine($"Found webcam {device.name} (id: {device.id})");
            }

            // Create a peer connection
            using var pc = new PeerConnection();

            var configuration = new PeerConnectionConfiguration
            {
                IceServers = new List<IceServer>
                {
                    new IceServer
                    {
                        Urls = { "stun:stun.l.google.com" },
                        // TurnPassword = "pass", 
                        // TurnUserName = "username" 
                    }
                }
            };

            await pc.InitializeAsync(configuration);
            Console.WriteLine("Peer connection initialized.");

            // Record video from local webcam, and send to remote peer
            if (needVideo)
            {
                Console.WriteLine("Opening local webcam...");
                videoTrackSource = await DeviceVideoTrackSource.CreateAsync();

                Console.WriteLine("Create local video track...");
                var trackSettings = new LocalVideoTrackInitConfig { trackName = "webcam_track" };
                localVideoTrack = LocalVideoTrack.CreateFromSource(videoTrackSource, trackSettings);

                Console.WriteLine("Create video transceiver and add webcam track...");
                videoTransceiver = pc.AddTransceiver(MediaKind.Video);
                videoTransceiver.DesiredDirection = Transceiver.Direction.SendReceive;
                videoTransceiver.LocalVideoTrack = localVideoTrack;
            }

            // Record audio from local microphone, and send to remote peer
            if (needAudio)
            {
                Console.WriteLine("Opening local microphone...");
                audioTrackSource = await DeviceAudioTrackSource.CreateAsync();

                Console.WriteLine("Create local audio track...");
                var trackSettings = new LocalAudioTrackInitConfig { trackName = "mic_track" };
                localAudioTrack = LocalAudioTrack.CreateFromSource(audioTrackSource, trackSettings);

                Console.WriteLine("Create audio transceiver and add mic track...");
                audioTransceiver = pc.AddTransceiver(MediaKind.Audio);
                audioTransceiver.DesiredDirection = Transceiver.Direction.SendReceive;
                audioTransceiver.LocalAudioTrack = localAudioTrack;
            }



            // Setup the signaler wehere the two peers will connect to establish a p2p connection
            Console.WriteLine("Starting signaling...");
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
            Console.WriteLine(ex);
        }
        finally
        {
            localAudioTrack?.Dispose();
            localVideoTrack?.Dispose();
            videoTrackSource?.Dispose();
            audioTrackSource?.Dispose();
        }
    }
}
