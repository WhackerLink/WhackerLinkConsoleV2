# WhackerLink Mobile Web Client

A Progressive Web App (PWA) for iOS and Android that connects to WhackerLink servers for P25 dispatch console functionality.

## Features

- **Mobile-First Design** - Optimized for iPhone and Android devices
- **Touch-Based PTT** - Hold the button to transmit, release to stop
- **Progressive Web App** - Install to home screen like a native app
- **Works Offline** - Service worker caches resources
- **WebSocket Communication** - Real-time connection to WhackerLink server
- **Audio Support** - Uses Web Audio API for microphone access
- **Settings Persistence** - Saves RID and server settings locally
- **Responsive UI** - Beautiful dark theme optimized for mobile

## Quick Start

### 1. Server Setup

Make sure you have a WhackerLink server running and accessible. The default is:
```
ws://localhost:3131
```

For remote access, use your server's IP address:
```
ws://192.168.1.100:3131
```

Or for secure WebSocket:
```
wss://your-server.com:3131
```

### 2. Deploy the Web App

#### Option A: Simple HTTP Server (Local Testing)

```bash
cd WhackerLinkWeb
python -m http.server 8080
```

Then open on your phone: `http://your-computer-ip:8080`

#### Option B: Node.js HTTP Server

```bash
npm install -g http-server
cd WhackerLinkWeb
http-server -p 8080
```

#### Option C: Deploy to Web Host

Upload all files in `WhackerLinkWeb` to your web server:
- Apache
- Nginx
- GitHub Pages
- Netlify
- Vercel

### 3. Access on iPhone

1. **Open in Safari**: Navigate to your web app URL
2. **Add to Home Screen**:
   - Tap the Share button (box with arrow)
   - Scroll down and tap "Add to Home Screen"
   - Tap "Add"
3. **Launch**: Tap the WhackerLink icon on your home screen
4. **Grant Microphone Permission**: When you first press PTT

## Usage

### First Time Setup

1. **Launch the app** (from home screen or browser)
2. **Enter Settings**:
   - Server Address: `ws://your-server:3131`
   - Dispatcher RID: Your radio ID (e.g., `1234567`)
   - Your Name: Optional dispatcher name
3. **Tap Connect**

### Selecting a Channel

1. **Browse Channels**: View list of available talkgroups
2. **Tap Channel**: Select the one you want to use
3. **See PTT Screen**: Large PTT button appears

### Using Push-to-Talk

1. **Press and Hold** the large PTT button
2. **Speak** into your iPhone/Android microphone
3. **Release** the button when done
4. Watch the status indicator:
   - **IDLE** (gray) - Not transmitting
   - **TRANSMITTING** (red) - Currently transmitting

### Changing Settings

1. **From Channels Screen**: Tap "Settings" button
2. **From PTT Screen**: Tap "Disconnect" button
3. Modify server address or RID
4. Tap "Connect" to reconnect

## Configuration

### Server Address Format

- **Local (same network)**: `ws://192.168.1.100:3131`
- **Remote HTTP**: `ws://whackerlink.example.com:3131`
- **Secure WSS**: `wss://whackerlink.example.com:3131`

### Dispatcher RID

- Must be numeric
- Between 1 and 16777215
- Should be unique for each dispatcher
- Overrides codeplug RID on server

### Sample Channels

The app includes sample channels for testing:
- Police Dispatch (TG 1001)
- Fire Department (TG 2001)
- EMS (TG 3001)
- Public Works (TG 4001)
- Emergency Ops (TG 5001)
- Mutual Aid (TG 9001)

**Note:** In production, channels would be loaded from the server or codeplug.

## Technical Details

### Architecture

```
┌─────────────────────────────────────┐
│  WhackerLink Mobile (Web Client)    │
│                                     │
│  ┌─────────────────────────────┐   │
│  │  HTML5 + CSS3 + JavaScript  │   │
│  └─────────────────────────────┘   │
│                                     │
│  ┌──────────────┐  ┌────────────┐  │
│  │ WebSocket    │  │ Web Audio  │  │
│  │ Connection   │  │ API        │  │
│  └──────────────┘  └────────────┘  │
│                                     │
│  ┌──────────────────────────────┐  │
│  │ Service Worker (PWA)         │  │
│  └──────────────────────────────┘  │
└─────────────────────────────────────┘
          │
          │ WebSocket (ws:// or wss://)
          ↓
┌─────────────────────────────────────┐
│  WhackerLink Server                 │
│  Port: 3131                         │
└─────────────────────────────────────┘
```

### Technologies Used

- **HTML5** - Structure
- **CSS3** - Styling with mobile-first responsive design
- **Vanilla JavaScript** - No frameworks, pure ES6+
- **WebSocket API** - Real-time bidirectional communication
- **Web Audio API** - Microphone access and audio processing
- **LocalStorage** - Settings persistence
- **Service Worker** - PWA capabilities, offline mode
- **Progressive Web App** - Install to home screen

### Browser Compatibility

- ✅ **iOS Safari** 14+ (iPhone, iPad)
- ✅ **Android Chrome** 80+
- ✅ **Desktop Chrome** 80+
- ✅ **Desktop Firefox** 75+
- ✅ **Desktop Safari** 14+
- ⚠️ **iOS Chrome** (Uses Safari engine, works but use Safari for best experience)

### File Structure

```
WhackerLinkWeb/
├── index.html              # Main HTML file
├── manifest.json           # PWA manifest
├── service-worker.js       # Service worker for PWA
├── css/
│   └── styles.css          # All styles
├── js/
│   ├── config.js          # Configuration and constants
│   ├── websocket.js       # WebSocket manager
│   ├── audio.js           # Audio manager
│   ├── ui.js              # UI controller
│   └── app.js             # Main application
├── assets/
│   └── icon-*.png         # PWA icons (various sizes)
└── README.md              # This file
```

## Troubleshooting

### Cannot Connect to Server

- Check server address format (must start with `ws://` or `wss://`)
- Verify server is running and accessible
- Check firewall settings on server
- Try using server IP instead of hostname
- Ensure port is correct (default: 3131)

### Microphone Not Working

- Grant microphone permission in browser
- Settings → Safari → Microphone → Allow
- Check that no other app is using the microphone
- On iPhone, make sure silent mode is off

### App Won't Install to Home Screen

- Must use Safari on iOS (not Chrome)
- Some features require HTTPS (use `wss://` for production)
- Clear Safari cache and try again

### Audio Not Transmitting

- Check microphone permissions
- Verify PTT button is held down
- Check server logs for incoming messages
- Test with another device

### Settings Not Saving

- Check browser storage permissions
- Clear browser cache may reset settings
- Ensure app has permission to store data

## Development

### Local Development

```bash
# Clone repo
git clone https://github.com/yourrepo/WhackerLinkConsoleV234.git
cd WhackerLinkConsoleV234/WhackerLinkWeb

# Start development server
python -m http.server 8080

# Open in browser
open http://localhost:8080
```

### Modifying Channels

Edit `js/app.js` and modify the `loadSampleChannels()` method:

```javascript
const sampleChannels = [
    { name: 'Your Channel', tgid: '1234', site: '1' },
    // Add more channels...
];
```

### Customizing Theme

Edit `css/styles.css` and modify the CSS variables:

```css
:root {
    --primary-color: #2563eb;    /* Primary blue */
    --danger-color: #ef4444;     /* Red for PTT */
    --bg-dark: #1a1a2e;          /* Dark background */
    /* ... more colors ... */
}
```

### Testing WebSocket Messages

Open browser console and test:

```javascript
// Check connection status
console.log(window.WLWebSocket.isConnected);

// Send test message
window.WLWebSocket.send('{"type":"test"}');

// Check selected channel
console.log(window.UI.selectedChannel);
```

## Security Considerations

- **Use WSS in Production** - Secure WebSocket for encryption
- **HTTPS Required** - For full PWA features and microphone access
- **Validate RID** - Server should validate dispatcher RID
- **Authentication** - Consider adding login/password
- **Rate Limiting** - Server should implement rate limiting

## Future Enhancements

- [ ] Load channels from server dynamically
- [ ] Audio receive (play incoming audio)
- [ ] Call history log
- [ ] Emergency button
- [ ] GPS location sharing
- [ ] Text messaging
- [ ] Channel scanning
- [ ] Recording functionality
- [ ] Multi-channel select
- [ ] Night/day theme toggle

## License

GNU General Public License v3.0 - See LICENSE file for details

## Credits

- **WhackerLink Console** - Original desktop application
- **WhackerLink Mobile** - Web client adaptation
- Built with ❤️ for public safety dispatchers

## Support

For issues or questions:
- GitHub Issues: https://github.com/yourrepo/WhackerLinkConsoleV234/issues
- Check browser console for error messages
- Test with browser developer tools

---

**Ready to dispatch from anywhere!** 📱🎤
