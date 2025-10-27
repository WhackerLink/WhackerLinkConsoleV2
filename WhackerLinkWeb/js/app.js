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

    loadSampleChannels() {
        // Sample channels for testing
        // In a real app, these would come from the server or a codeplug
        const sampleChannels = [
            { name: 'Police Dispatch', tgid: '1001', site: '1' },
            { name: 'Fire Department', tgid: '2001', site: '1' },
            { name: 'EMS', tgid: '3001', site: '1' },
            { name: 'Public Works', tgid: '4001', site: '1' },
            { name: 'Emergency Ops', tgid: '5001', site: '1' },
            { name: 'Mutual Aid', tgid: '9001', site: '1' },
        ];

        window.UI.populateChannels(sampleChannels);
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
