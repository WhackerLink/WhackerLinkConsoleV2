// WhackerLink Mobile - Service Worker

const CACHE_NAME = 'whackerlink-v1';
const urlsToCache = [
    '/',
    '/index.html',
    '/css/styles.css',
    '/js/config.js',
    '/js/websocket.js',
    '/js/audio.js',
    '/js/ui.js',
    '/js/app.js',
    '/manifest.json',
    '/codeplug.json'
];

// Install event
self.addEventListener('install', event => {
    console.log('Service Worker installing...');
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => {
                console.log('Caching app shell');
                return cache.addAll(urlsToCache);
            })
    );
    self.skipWaiting();
});

// Activate event
self.addEventListener('activate', event => {
    console.log('Service Worker activating...');
    event.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames.map(cacheName => {
                    if (cacheName !== CACHE_NAME) {
                        console.log('Deleting old cache:', cacheName);
                        return caches.delete(cacheName);
                    }
                })
            );
        })
    );
    self.clients.claim();
});

// Fetch event
self.addEventListener('fetch', event => {
    // Skip WebSocket requests
    if (event.request.url.startsWith('ws://') || event.request.url.startsWith('wss://')) {
        return;
    }

    event.respondWith(
        caches.match(event.request)
            .then(response => {
                // Return cached version or fetch from network
                return response || fetch(event.request)
                    .then(fetchResponse => {
                        // Cache new resources
                        if (fetchResponse && fetchResponse.status === 200) {
                            const responseToCache = fetchResponse.clone();
                            caches.open(CACHE_NAME)
                                .then(cache => {
                                    cache.put(event.request, responseToCache);
                                });
                        }
                        return fetchResponse;
                    });
            })
            .catch(() => {
                // Return offline page if available
                return caches.match('/index.html');
            })
    );
});

// Handle messages from the main app
self.addEventListener('message', event => {
    if (event.data.action === 'skipWaiting') {
        self.skipWaiting();
    }
});
