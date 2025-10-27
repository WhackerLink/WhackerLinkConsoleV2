// WhackerLink Mobile - WebSocket Manager

class WhackerLinkWebSocket {
    constructor() {
        this.ws = null;
        this.isConnected = false;
        this.serverAddress = '';
        this.authKey = '';
        this.rid = '';
        this.selectedChannel = null;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.reconnectDelay = 2000;

        this.onConnected = null;
        this.onDisconnected = null;
        this.onMessage = null;
        this.onError = null;
    }

    connect(serverAddress, authKey, rid) {
        if (this.isConnected) {
            console.warn('Already connected');
            return;
        }

        this.serverAddress = serverAddress;
        this.authKey = authKey || '';
        this.rid = rid;

        try {
            // Build WebSocket URL with auth key as query parameter if provided
            let wsUrl = serverAddress;
            if (this.authKey) {
                const separator = serverAddress.includes('?') ? '&' : '?';
                wsUrl = `${serverAddress}${separator}authKey=${encodeURIComponent(this.authKey)}&rid=${encodeURIComponent(this.rid)}`;
            }

            console.log('Connecting to:', wsUrl.replace(this.authKey, '***'));
            this.ws = new WebSocket(wsUrl);
            this.ws.binaryType = 'arraybuffer';

            this.ws.onopen = () => this.handleOpen();
            this.ws.onclose = () => this.handleClose();
            this.ws.onerror = (error) => this.handleError(error);
            this.ws.onmessage = (event) => this.handleMessage(event);

        } catch (error) {
            console.error('WebSocket connection error:', error);
            if (this.onError) this.onError(error);
        }
    }

    disconnect() {
        if (this.ws) {
            this.ws.close();
            this.ws = null;
        }
        this.isConnected = false;
        this.reconnectAttempts = 0;
    }

    handleOpen() {
        console.log('WebSocket connected');
        this.isConnected = true;
        this.reconnectAttempts = 0;

        // Send authentication if authKey is provided
        if (this.authKey) {
            this.sendAuthentication();
        }

        if (this.onConnected) {
            this.onConnected();
        }

        // Send affiliation if we have a selected channel
        if (this.selectedChannel) {
            this.sendAffiliation(this.selectedChannel);
        }
    }

    handleClose() {
        console.log('WebSocket disconnected');
        this.isConnected = false;

        if (this.onDisconnected) {
            this.onDisconnected();
        }

        // Attempt reconnection
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
            this.reconnectAttempts++;
            console.log(`Reconnect attempt ${this.reconnectAttempts}/${this.maxReconnectAttempts}`);
            setTimeout(() => {
                this.connect(this.serverAddress, this.authKey, this.rid);
            }, this.reconnectDelay);
        }
    }

    handleError(error) {
        console.error('WebSocket error:', error);
        if (this.onError) {
            this.onError(error);
        }
    }

    handleMessage(event) {
        try {
            // Handle binary messages
            if (event.data instanceof ArrayBuffer) {
                const data = new Uint8Array(event.data);
                this.processMessage(data);
            }
            // Handle text messages (JSON)
            else if (typeof event.data === 'string') {
                const jsonData = JSON.parse(event.data);
                this.processJsonMessage(jsonData);
            }
        } catch (error) {
            console.error('Message processing error:', error);
        }

        if (this.onMessage) {
            this.onMessage(event.data);
        }
    }

    processMessage(data) {
        // Process WhackerLink binary messages
        // This is a simplified version - adjust based on actual protocol
        if (data.length < 1) return;

        const messageType = data[0];
        console.log('Received message type:', messageType);

        // Handle different message types
        switch (messageType) {
            case Config.MSG_TYPE.GRANT:
                console.log('Channel granted');
                break;
            case Config.MSG_TYPE.DENY:
                console.log('Channel denied');
                if (window.UI) window.UI.showToast('Channel Denied', 'error');
                break;
            case Config.MSG_TYPE.GRP_AFF_RSP:
                console.log('Affiliation response received');
                break;
        }
    }

    processJsonMessage(data) {
        // Handle JSON messages from server
        console.log('JSON message:', data);
    }

    // Send authentication
    sendAuthentication() {
        if (!this.isConnected) {
            console.warn('Not connected');
            return false;
        }

        try {
            const message = {
                type: 'AUTH',
                authKey: this.authKey,
                srcId: this.rid
            };

            this.send(JSON.stringify(message));
            console.log('Sent authentication');
            return true;
        } catch (error) {
            console.error('Failed to send authentication:', error);
            return false;
        }
    }

    // Send affiliation request
    sendAffiliation(channel) {
        if (!this.isConnected) {
            console.warn('Not connected');
            return false;
        }

        try {
            // Build GRP_AFF_REQ message
            const message = {
                type: 'GRP_AFF_REQ',
                srcId: this.rid,
                dstId: channel.tgid,
                site: channel.site || 1
            };

            // Send as JSON for simplicity (adjust if binary is required)
            this.send(JSON.stringify(message));
            console.log('Sent affiliation:', message);
            return true;
        } catch (error) {
            console.error('Failed to send affiliation:', error);
            return false;
        }
    }

    // Send PTT request (press)
    sendPTTPress(channel) {
        if (!this.isConnected) {
            console.warn('Not connected');
            return false;
        }

        try {
            const message = {
                type: 'GRP_VCH_REQ',
                srcId: this.rid,
                dstId: channel.tgid,
                site: channel.site || 1
            };

            this.send(JSON.stringify(message));
            console.log('PTT Press:', message);
            return true;
        } catch (error) {
            console.error('Failed to send PTT press:', error);
            return false;
        }
    }

    // Send PTT release
    sendPTTRelease(channel) {
        if (!this.isConnected) {
            console.warn('Not connected');
            return false;
        }

        try {
            const message = {
                type: 'GRP_VCH_RLS',
                srcId: this.rid,
                dstId: channel.tgid,
                site: channel.site || 1
            };

            this.send(JSON.stringify(message));
            console.log('PTT Release:', message);
            return true;
        } catch (error) {
            console.error('Failed to send PTT release:', error);
            return false;
        }
    }

    // Send raw data
    send(data) {
        if (this.ws && this.isConnected) {
            this.ws.send(data);
        } else {
            console.warn('Cannot send: not connected');
        }
    }

    // Set selected channel
    setSelectedChannel(channel) {
        this.selectedChannel = channel;
        if (this.isConnected && channel) {
            this.sendAffiliation(channel);
        }
    }
}

// Global instance
window.WLWebSocket = new WhackerLinkWebSocket();
