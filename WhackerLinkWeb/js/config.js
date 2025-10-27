// WhackerLink Mobile - Configuration

const Config = {
    // Default server settings
    DEFAULT_SERVER: 'ws://localhost:3131',

    // Storage keys
    STORAGE_KEY_SERVER: 'wl_server_address',
    STORAGE_KEY_RID: 'wl_dispatcher_rid',
    STORAGE_KEY_NAME: 'wl_dispatcher_name',
    STORAGE_KEY_SELECTED_CHANNEL: 'wl_selected_channel',

    // WebSocket message types
    MSG_TYPE: {
        GRP_VCH_REQ: 0x00,  // PTT Request
        GRP_VCH_RLS: 0x01,  // PTT Release
        GRP_AFF_REQ: 0x28,  // Affiliation Request
        GRP_AFF_RSP: 0x29,  // Affiliation Response
        GRANT: 0x30,        // Channel Grant
        DENY: 0x31,         // Channel Deny
    },

    // Audio settings
    AUDIO: {
        SAMPLE_RATE: 8000,
        CHANNELS: 1,
        BUFFER_SIZE: 4096,
    },

    // UI settings
    UI: {
        TOAST_DURATION: 3000,
        STATUS_UPDATE_INTERVAL: 1000,
    }
};

// Helper function to save settings
function saveSettings(key, value) {
    try {
        localStorage.setItem(key, JSON.stringify(value));
    } catch (e) {
        console.error('Failed to save setting:', e);
    }
}

// Helper function to load settings
function loadSettings(key, defaultValue = null) {
    try {
        const value = localStorage.getItem(key);
        return value ? JSON.parse(value) : defaultValue;
    } catch (e) {
        console.error('Failed to load setting:', e);
        return defaultValue;
    }
}
