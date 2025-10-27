// WhackerLink Mobile - Main Application

class WhackerLinkApp {
    constructor() {
        this.isInitialized = false;
    }

    async initialize() {
        if (this.isInitialized) return;

        console.log('Initializing WhackerLink Mobile...');

        // Initialize UI
        window.UI.initialize();

        // Setup WebSocket event handlers
        this.setupWebSocketHandlers();

        // Request audio permissions on first user interaction
        this.setupAudioPermissions();

        // Load sample channels (in real app, get from server)
        this.loadSampleChannels();

        // Register service worker for PWA
        this.registerServiceWorker();

        this.isInitialized = true;
        console.log('WhackerLink Mobile initialized');
    }

    setupWebSocketHandlers() {
        window.WLWebSocket.onConnected = () => {
            console.log('Connected to server');
            window.UI.updateConnectionStatus(true);
            window.UI.showToast('Connected to server', 'success');
        };

        window.WLWebSocket.onDisconnected = () => {
            console.log('Disconnected from server');
            window.UI.updateConnectionStatus(false);
            window.UI.showToast('Disconnected from server', 'warning');
        };

        window.WLWebSocket.onError = (error) => {
            console.error('WebSocket error:', error);
            window.UI.showToast('Connection error', 'error');
        };

        window.WLWebSocket.onMessage = (data) => {
            // Handle incoming messages
            // This could include audio data, status updates, etc.
        };
    }

    setupAudioPermissions() {
        // Request microphone permission on first PTT press
        // This is handled in AudioManager.startTransmitting()
        console.log('Audio permissions will be requested on first PTT');
    }

    async loadSampleChannels() {
        try {
            // Try to load codeplug.json
            const response = await fetch('codeplug.json');

            if (response.ok) {
                const codeplug = await response.json();

                // Extract all channels from all zones
                const allChannels = [];
                codeplug.zones.forEach(zone => {
                    zone.channels.forEach(channel => {
                        allChannels.push({
                            name: channel.name,
                            tgid: channel.tgid,
                            site: channel.site || '1',
                            system: channel.system,
                            zone: zone.name
                        });
                    });
                });

                console.log(`Loaded ${allChannels.length} channels from codeplug`);
                window.UI.populateChannels(allChannels);
            } else {
                // Fallback to hardcoded channels
                console.warn('Could not load codeplug.json, using fallback channels');
                this.loadFallbackChannels();
            }
        } catch (error) {
            console.error('Error loading codeplug:', error);
            this.loadFallbackChannels();
        }
    }

    loadFallbackChannels() {
        // Fallback channels if codeplug.json is not available
        const fallbackChannels = [
            { name: 'ORG-NORTH', tgid: '30001', site: '1', system: 'System 1' },
            { name: 'TAC 1', tgid: '30002', site: '1', system: 'System 1' },
            { name: 'TAC 2', tgid: '30003', site: '1', system: 'System 1' },
            { name: 'SWAT', tgid: '30004', site: '1', system: 'System 1' },
            { name: 'TalkAround', tgid: '30005', site: '1', system: 'System 1' },
        ];

        window.UI.populateChannels(fallbackChannels);
    }

    registerServiceWorker() {
        if ('serviceWorker' in navigator) {
            navigator.serviceWorker.register('service-worker.js')
                .then(registration => {
                    console.log('Service Worker registered:', registration);
                })
                .catch(error => {
                    console.error('Service Worker registration failed:', error);
                });
        }
    }
}

// Initialize app when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    const app = new WhackerLinkApp();
    app.initialize();
});

// Handle page visibility changes
document.addEventListener('visibilitychange', () => {
    if (document.hidden) {
        console.log('App hidden');
        // Optionally pause audio processing
    } else {
        console.log('App visible');
        // Resume audio processing
    }
});

// Handle beforeunload
window.addEventListener('beforeunload', () => {
    if (window.WLWebSocket.isConnected) {
        window.WLWebSocket.disconnect();
    }
    if (window.AudioMgr) {
        window.AudioMgr.cleanup();
    }
});

// Prevent iOS from zooming on input focus
document.addEventListener('touchstart', function() {}, {passive: true});

// Prevent pull-to-refresh
let touchStartY = 0;
document.addEventListener('touchstart', e => {
    touchStartY = e.touches[0].clientY;
}, {passive: true});

document.addEventListener('touchmove', e => {
    const touchY = e.touches[0].clientY;
    const touchYDelta = touchY - touchStartY;
    if (touchYDelta > 0 && window.scrollY === 0) {
        e.preventDefault();
    }
}, {passive: false});
