// WhackerLink Mobile - Audio Manager

class AudioManager {
    constructor() {
        this.audioContext = null;
        this.microphone = null;
        this.isTransmitting = false;
        this.mediaStream = null;
        this.audioWorkletNode = null;
    }

    async initialize() {
        try {
            // Create audio context
            this.audioContext = new (window.AudioContext || window.webkitAudioContext)({
                sampleRate: Config.AUDIO.SAMPLE_RATE
            });

            console.log('Audio context initialized');
            return true;
        } catch (error) {
            console.error('Failed to initialize audio:', error);
            return false;
        }
    }

    async requestMicrophonePermission() {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({
                audio: {
                    echoCancellation: true,
                    noiseSuppression: true,
                    sampleRate: Config.AUDIO.SAMPLE_RATE,
                    channelCount: Config.AUDIO.CHANNELS
                }
            });

            this.mediaStream = stream;
            console.log('Microphone permission granted');
            return true;
        } catch (error) {
            console.error('Microphone permission denied:', error);
            if (window.UI) {
                window.UI.showToast('Microphone access denied. Please enable it in your browser settings.', 'error');
            }
            return false;
        }
    }

    async startTransmitting() {
        if (this.isTransmitting) {
            console.warn('Already transmitting');
            return;
        }

        try {
            if (!this.audioContext) {
                await this.initialize();
            }

            if (!this.mediaStream) {
                const granted = await this.requestMicrophonePermission();
                if (!granted) return false;
            }

            // Resume audio context if suspended (required on mobile)
            if (this.audioContext.state === 'suspended') {
                await this.audioContext.resume();
            }

            // Create microphone source
            this.microphone = this.audioContext.createMediaStreamSource(this.mediaStream);

            // Create processor for audio data
            const processor = this.audioContext.createScriptProcessor(
                Config.AUDIO.BUFFER_SIZE,
                Config.AUDIO.CHANNELS,
                Config.AUDIO.CHANNELS
            );

            processor.onaudioprocess = (e) => {
                if (this.isTransmitting) {
                    const inputData = e.inputBuffer.getChannelData(0);
                    this.processAudioData(inputData);
                }
            };

            this.microphone.connect(processor);
            processor.connect(this.audioContext.destination);

            this.isTransmitting = true;
            console.log('Started transmitting');
            return true;

        } catch (error) {
            console.error('Failed to start transmitting:', error);
            return false;
        }
    }

    stopTransmitting() {
        if (!this.isTransmitting) {
            return;
        }

        try {
            if (this.microphone) {
                this.microphone.disconnect();
                this.microphone = null;
            }

            this.isTransmitting = false;
            console.log('Stopped transmitting');

        } catch (error) {
            console.error('Failed to stop transmitting:', error);
        }
    }

    processAudioData(audioData) {
        // Process audio data for transmission
        // In a real implementation, this would encode the audio
        // and send it via WebSocket to the server

        // For now, just log that we're processing audio
        // console.log('Processing audio data, length:', audioData.length);

        // TODO: Implement actual audio encoding and transmission
        // - Convert to appropriate format (P25 IMBE, etc.)
        // - Send via WebSocket
    }

    playAudio(audioData) {
        // Play received audio
        if (!this.audioContext) return;

        try {
            const audioBuffer = this.audioContext.createBuffer(
                Config.AUDIO.CHANNELS,
                audioData.length,
                Config.AUDIO.SAMPLE_RATE
            );

            audioBuffer.getChannelData(0).set(audioData);

            const source = this.audioContext.createBufferSource();
            source.buffer = audioBuffer;
            source.connect(this.audioContext.destination);
            source.start();

        } catch (error) {
            console.error('Failed to play audio:', error);
        }
    }

    cleanup() {
        this.stopTransmitting();

        if (this.mediaStream) {
            this.mediaStream.getTracks().forEach(track => track.stop());
            this.mediaStream = null;
        }

        if (this.audioContext) {
            this.audioContext.close();
            this.audioContext = null;
        }
    }
}

// Global instance
window.AudioMgr = new AudioManager();
