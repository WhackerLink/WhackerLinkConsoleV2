// WhackerLink Mobile - UI Controller

class UIController {
    constructor() {
        this.currentPanel = 'settings';
        this.selectedChannel = null;
        this.channels = [];
    }

    initialize() {
        this.bindElements();
        this.attachEventListeners();
        this.loadSavedSettings();
        this.showPanel('settings');
    }

    bindElements() {
        // Status bar
        this.statusBar = document.getElementById('status-bar');
        this.statusText = document.getElementById('status-text');
        this.ridDisplay = document.getElementById('rid-display');

        // Settings panel
        this.settingsPanel = document.getElementById('settings-panel');
        this.serverAddressInput = document.getElementById('server-address');
        this.authKeyInput = document.getElementById('auth-key');
        this.dispatcherRidInput = document.getElementById('dispatcher-rid');
        this.dispatcherNameInput = document.getElementById('dispatcher-name');
        this.connectBtn = document.getElementById('connect-btn');
        this.disconnectBtn = document.getElementById('disconnect-btn');

        // Channels panel
        this.channelsPanel = document.getElementById('channels-panel');
        this.channelsList = document.getElementById('channels-list');
        this.backToSettingsBtn = document.getElementById('back-to-settings');

        // PTT panel
        this.pttPanel = document.getElementById('ptt-panel');
        this.selectedChannelName = document.getElementById('selected-channel-name');
        this.pttIndicator = document.getElementById('ptt-indicator');
        this.pttButton = document.getElementById('ptt-button');
        this.currentRid = document.getElementById('current-rid');
        this.currentTgid = document.getElementById('current-tgid');
        this.currentSite = document.getElementById('current-site');
        this.changeChannelBtn = document.getElementById('change-channel');
        this.disconnectFromPttBtn = document.getElementById('disconnect-from-ptt');

        // Toast
        this.toast = document.getElementById('toast');
    }

    attachEventListeners() {
        // Connect/Disconnect
        this.connectBtn.addEventListener('click', () => this.handleConnect());
        this.disconnectBtn.addEventListener('click', () => this.handleDisconnect());

        // Navigation
        this.backToSettingsBtn.addEventListener('click', () => this.showPanel('settings'));
        this.changeChannelBtn.addEventListener('click', () => this.showPanel('channels'));
        this.disconnectFromPttBtn.addEventListener('click', () => this.handleDisconnect());

        // PTT Button (touch events for mobile)
        this.pttButton.addEventListener('touchstart', (e) => {
            e.preventDefault();
            this.handlePTTPress();
        });

        this.pttButton.addEventListener('touchend', (e) => {
            e.preventDefault();
            this.handlePTTRelease();
        });

        // Also support mouse events for desktop testing
        this.pttButton.addEventListener('mousedown', () => this.handlePTTPress());
        this.pttButton.addEventListener('mouseup', () => this.handlePTTRelease());
        this.pttButton.addEventListener('mouseleave', () => this.handlePTTRelease());
    }

    loadSavedSettings() {
        const serverAddress = loadSettings(Config.STORAGE_KEY_SERVER, Config.DEFAULT_SERVER);
        const authKey = loadSettings(Config.STORAGE_KEY_AUTH_KEY, '');
        const rid = loadSettings(Config.STORAGE_KEY_RID, '');
        const name = loadSettings(Config.STORAGE_KEY_NAME, '');

        this.serverAddressInput.value = serverAddress;
        this.authKeyInput.value = authKey;
        this.dispatcherRidInput.value = rid;
        this.dispatcherNameInput.value = name;
    }

    handleConnect() {
        const serverAddress = this.serverAddressInput.value.trim();
        const authKey = this.authKeyInput.value.trim();
        const rid = this.dispatcherRidInput.value.trim();
        const name = this.dispatcherNameInput.value.trim();

        // Validation
        if (!serverAddress) {
            this.showToast('Please enter server address', 'error');
            return;
        }

        if (!rid || isNaN(rid)) {
            this.showToast('Please enter a valid RID', 'error');
            return;
        }

        // Save settings
        saveSettings(Config.STORAGE_KEY_SERVER, serverAddress);
        saveSettings(Config.STORAGE_KEY_AUTH_KEY, authKey);
        saveSettings(Config.STORAGE_KEY_RID, rid);
        saveSettings(Config.STORAGE_KEY_NAME, name);

        // Connect
        window.WLWebSocket.connect(serverAddress, authKey, rid);

        this.showToast('Connecting...', 'warning');
    }

    handleDisconnect() {
        window.WLWebSocket.disconnect();
        window.AudioMgr.cleanup();
        this.updateConnectionStatus(false);
        this.showPanel('settings');
        this.showToast('Disconnected', 'warning');
    }

    handlePTTPress() {
        if (!this.selectedChannel) {
            this.showToast('No channel selected', 'error');
            return;
        }

        if (!window.WLWebSocket.isConnected) {
            this.showToast('Not connected to server', 'error');
            return;
        }

        // Start audio transmission
        window.AudioMgr.startTransmitting();

        // Send PTT press to server
        window.WLWebSocket.sendPTTPress(this.selectedChannel);

        // Update UI
        this.pttIndicator.textContent = 'TRANSMITTING';
        this.pttIndicator.className = 'ptt-indicator transmitting';
        this.statusBar.className = 'status-bar transmitting';

        // Add haptic feedback on iOS
        if (navigator.vibrate) {
            navigator.vibrate(50);
        }
    }

    handlePTTRelease() {
        // Stop audio transmission
        window.AudioMgr.stopTransmitting();

        // Send PTT release to server
        if (this.selectedChannel && window.WLWebSocket.isConnected) {
            window.WLWebSocket.sendPTTRelease(this.selectedChannel);
        }

        // Update UI
        this.pttIndicator.textContent = 'IDLE';
        this.pttIndicator.className = 'ptt-indicator idle';
        this.statusBar.className = window.WLWebSocket.isConnected ?
            'status-bar connected' : 'status-bar disconnected';
    }

    updateConnectionStatus(connected) {
        if (connected) {
            this.statusText.textContent = 'Connected';
            this.ridDisplay.textContent = `RID: ${this.dispatcherRidInput.value}`;
            this.statusBar.className = 'status-bar connected';
            this.connectBtn.style.display = 'none';
            this.disconnectBtn.style.display = 'block';
            this.showPanel('channels');
        } else {
            this.statusText.textContent = 'Disconnected';
            this.ridDisplay.textContent = '';
            this.statusBar.className = 'status-bar disconnected';
            this.connectBtn.style.display = 'block';
            this.disconnectBtn.style.display = 'none';
        }
    }

    selectChannel(channel) {
        this.selectedChannel = channel;

        // Save selected channel
        saveSettings(Config.STORAGE_KEY_SELECTED_CHANNEL, channel);

        // Update WebSocket
        window.WLWebSocket.setSelectedChannel(channel);

        // Update UI
        this.selectedChannelName.textContent = channel.name;
        this.currentRid.textContent = this.dispatcherRidInput.value;
        this.currentTgid.textContent = channel.tgid;
        this.currentSite.textContent = channel.site || '1';

        this.showPanel('ptt');
        this.showToast(`Selected: ${channel.name}`, 'success');
    }

    populateChannels(channels) {
        this.channels = channels;
        this.channelsList.innerHTML = '';

        channels.forEach(channel => {
            const channelItem = document.createElement('div');
            channelItem.className = 'channel-item';

            // Build info string
            let infoText = `TG: ${channel.tgid}`;
            if (channel.site) {
                infoText += ` | Site: ${channel.site}`;
            }
            if (channel.zone) {
                infoText += ` | ${channel.zone}`;
            }

            channelItem.innerHTML = `
                <div class="channel-name">${channel.name}</div>
                <div class="channel-info">${infoText}</div>
            `;
            channelItem.addEventListener('click', () => this.selectChannel(channel));
            this.channelsList.appendChild(channelItem);
        });
    }

    showPanel(panelName) {
        this.currentPanel = panelName;

        this.settingsPanel.style.display = 'none';
        this.channelsPanel.style.display = 'none';
        this.pttPanel.style.display = 'none';

        switch (panelName) {
            case 'settings':
                this.settingsPanel.style.display = 'block';
                break;
            case 'channels':
                this.channelsPanel.style.display = 'block';
                break;
            case 'ptt':
                this.pttPanel.style.display = 'block';
                break;
        }
    }

    showToast(message, type = 'info') {
        this.toast.textContent = message;
        this.toast.className = `toast ${type} show`;

        setTimeout(() => {
            this.toast.className = 'toast';
        }, Config.UI.TOAST_DURATION);
    }
}

// Global instance
window.UI = new UIController();
